using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Reactive.Linq;
using NLog;
using Ookii.Dialogs.Wpf;
using ReactiveUI;
using ReactiveUI.Xaml;
using Solutionizer.Commands;
using Solutionizer.Models;
using Solutionizer.Services;

namespace Solutionizer.ViewModels {
    public class SolutionViewModel : ReactiveObject {
        private static readonly Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _rootPath;
        private readonly IDictionary<string, Project> _projects;
        private readonly IReactiveCommand _dropCommand;
        private readonly IReactiveCommand _launchCommand;
        private readonly IReactiveCommand _saveCommand;
        private readonly IReactiveCommand _clearCommand;
        private readonly IReactiveCommand _removeSelectedProjectCommand;
        private readonly SolutionFolder _solutionRoot = new SolutionFolder(null);
        private readonly ISettings _settings;
        private bool _isSccBound;
        private bool _isDirty;
        private SolutionItem _selectedItem;

        public SolutionViewModel(ISettings settings, string rootPath, IDictionary<string, Project> projects) {
            _rootPath = rootPath;
            _projects = projects;
            _settings = settings;

            _dropCommand = ReactiveCommand.Create(obj => obj is ProjectViewModel, OnDrop);

            var hasProject = _solutionRoot.Items.IsEmpty.Select(b => !b).StartWith(false);
            
            _launchCommand = new ReactiveCommand(hasProject);
            _launchCommand.Subscribe(_ => Launch());
            
            _saveCommand = new ReactiveCommand(hasProject);
            _saveCommand.Subscribe(_ => Save());
            
            _clearCommand = new ReactiveCommand(hasProject);
            _clearCommand.Subscribe(_ => Clear());

            var hasSelectedItem = this.ObservableForProperty(vm => vm.SelectedItem).Select(item => item.Value != null && item.Value != _solutionRoot).StartWith(false);

            _removeSelectedProjectCommand = new ReactiveCommand(hasSelectedItem);
            _removeSelectedProjectCommand.Subscribe(_ => RemoveSolutionItem());
        }

        private void OnDrop(object node) {
            var project = ((ProjectViewModel) node).Project;
            project.Load();
            AddProject(project);
        }

        public void Launch() {
            var newFilename = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyy-MM-dd_HHmmss")) + ".sln";
            new SaveSolutionCommand(_settings, newFilename, _settings.VisualStudioVersion, this).Execute();
            Process.Start(newFilename);
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        public void Save() {
            var dlg = new VistaSaveFileDialog {
                Filter = "Solution File (*.sln)|*.sln",
                AddExtension = true,
                DefaultExt = ".sln"
            };
            if (dlg.ShowDialog() == true) {
                new SaveSolutionCommand(_settings, dlg.FileName, _settings.VisualStudioVersion, this).Execute();
                IsDirty = false;
            }
        }

        public void Clear() {
            _solutionRoot.Items.Clear();
            SelectedItem = null;
        }

        public ICommand DropCommand {
            get { return _dropCommand; }
        }

        public ICommand LaunchCommand {
            get { return _launchCommand; }
        }

        public ICommand SaveCommand {
            get { return _saveCommand; }
        }

        public ICommand ClearCommand {
            get { return _clearCommand; }
        }

        public ICommand RemoveSelectedProjectCommand {
            get { return _removeSelectedProjectCommand; }
        }

        public IList<SolutionItem> SolutionItems {
            get { return _solutionRoot.Items; }
        }

        public bool IsDirty {
            get { return _isDirty; }
            set { this.RaiseAndSetIfChanged(x => x.IsDirty, ref _isDirty, value); }
        }

        public string RootPath {
            get { return _rootPath; }
        }

        public bool IsSccBound {
            get { return _isSccBound; }
            set { this.RaiseAndSetIfChanged(x => x.IsSccBound, ref _isSccBound, value); }
        }

        public SolutionItem SelectedItem {
            get { return _selectedItem; }
            set { this.RaiseAndSetIfChanged(x => x.SelectedItem, ref _selectedItem, value); }
        }

        public void AddProject(Project project) {
            IsSccBound |= project.IsSccBound;

            if (_solutionRoot.ContainsProject(project)) {
                return;
            }

            _solutionRoot.AddProject(project);

            var referenceFolder = _solutionRoot.Items.OfType<SolutionFolder>().SingleOrDefault();
            if (referenceFolder != null) {
                RemoveProject(referenceFolder, project);
            }

            if (_settings.IncludeReferencedProjects) {
                AddReferencedProjects(project, _settings.ReferenceTreeDepth);
            }
        }

        private static bool RemoveProject(SolutionFolder solutionFolder, Project project) {
            bool removed = false;
            var item = solutionFolder.Items.SingleOrDefault(p => p.Guid == project.Guid);
            if (item != null) {
                solutionFolder.Items.Remove(item);
                removed = true;
            }

            var foldersToRemove = new List<SolutionFolder>();
            foreach (var subfolder in solutionFolder.Items.OfType<SolutionFolder>()) {
                if (RemoveProject(subfolder, project)) {
                    removed = true;
                    if (subfolder.Items.Count == 0) {
                        foldersToRemove.Add(subfolder);
                    }
                }
            }
            if (foldersToRemove.Count > 0) {
                foreach (var folder in foldersToRemove) {
                    solutionFolder.Items.Remove(folder);
                }
            }
            return removed;
        }

        private void AddReferencedProjects(Project project, int depth) {
            foreach (var projectReference in project.ProjectReferences) {
                Project referencedProject;
                if (!_projects.TryGetValue(projectReference, out referencedProject)) {
                    _log.Warn("Project {0} references unknown project {1}", project.Name, projectReference);
                    continue;
                }

                if (_solutionRoot.ContainsProject(referencedProject)) {
                    continue;
                }

                var folder = GetSolutionFolder(referencedProject);
                if (!folder.ContainsProject(referencedProject)) {
                    folder.AddProject(referencedProject);

                    if (depth > 1) {
                        AddReferencedProjects(referencedProject, depth - 1);
                    }
                }
            }
        }

        private SolutionFolder GetSolutionFolder(Project project) {
            // get chain of folders from root to project
            var folderNames = new List<string>();
            var projectFolder = project.Parent;
            while (projectFolder.Parent != null) {
                folderNames.Add(projectFolder.Name);
                projectFolder = projectFolder.Parent;
            }
            folderNames.Reverse();

            var folder = GetOrCreateReferenceFolder();
            foreach (var folderName in folderNames) {
                folder = folder.GetOrCreateSubfolder(folderName);
            }
            return folder;
        }

        private SolutionFolder GetOrCreateReferenceFolder() {
            return _solutionRoot.GetOrCreateSubfolder(_settings.ReferenceFolderName);
        }

        public void RemoveSolutionItem() {
            if (_selectedItem != null) {
                var parentFolder = _selectedItem.Parent;

                var index = parentFolder.Items.IndexOf(_selectedItem);
                parentFolder.Items.Remove(_selectedItem);

                if (index >= 0) {
                    if (index >= parentFolder.Items.Count) {
                        index--;
                    }
                    SelectedItem = index >= 0 ? parentFolder.Items[index] : parentFolder;
                }
            }
        }
    }
}