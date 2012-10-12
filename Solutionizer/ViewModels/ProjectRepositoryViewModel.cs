using System;
using System.Collections;
using System.Linq;
using ReactiveUI;
using ReactiveUI.Xaml;
using Solutionizer.Models;
using Solutionizer.Services;
using Solutionizer.Extensions;

namespace Solutionizer.ViewModels {
    public class ProjectRepositoryViewModel : ReactiveObject {
        private readonly ISettings _settings;
        private string _rootPath;
        private ProjectFolder _rootFolder;
        private IList _nodes;
        private readonly IReactiveCommand _mouseDblClickCommand;
        private readonly IReactiveCommand _selectRootCommand;
        private ItemViewModel _selectedItem;

        public ProjectRepositoryViewModel(ISettings settings) {
            _settings = settings;

            this.ObservableForProperty(x => x.RootFolder).Subscribe(x => Nodes = CreateDirectoryViewModel(x.Value, null).Children.ToList());

            _mouseDblClickCommand = new ReactiveCommand();
            _selectRootCommand = new ReactiveCommand();
        }

        public IReactiveCommand MouseDblClickCommand {
            get { return _mouseDblClickCommand; }
        }

        public IReactiveCommand SelectRootCommand {
            get { return _selectRootCommand; }
        }

        public ItemViewModel SelectedItem {
            get { return _selectedItem; }
            set { this.RaiseAndSetIfChanged(x => x.SelectedItem, ref _selectedItem, value); }
        }

        public string RootPath {
            get { return _rootPath; }
            set { this.RaiseAndSetIfChanged(x => x.RootPath, ref _rootPath, value); }
        }

        public ProjectFolder RootFolder {
            get { return _rootFolder; }
            set { this.RaiseAndSetIfChanged(x => x.RootFolder, ref _rootFolder, value); }
        }

        public IList Nodes {
            get { return _nodes; }
            set { this.RaiseAndSetIfChanged(x => x.Nodes, ref _nodes, value); }
        }

        private DirectoryViewModel CreateDirectoryViewModel(ProjectFolder projectFolder, DirectoryViewModel parent) {
            var viewModel = new DirectoryViewModel(parent, projectFolder);
            if (_settings.IsFlatMode) {
                foreach (var project in new[]{projectFolder}.Flatten(f => f.Projects, f => f.Folders)) {
                    viewModel.Projects.Add(CreateProjectViewModel(project, viewModel));
                }
            } else {
                foreach (var folder in projectFolder.Folders) {
                    viewModel.Directories.Add(CreateDirectoryViewModel(folder, viewModel));
                }
                foreach (var project in projectFolder.Projects) {
                    viewModel.Projects.Add(CreateProjectViewModel(project, viewModel));
                }
            }
            return viewModel;
        }

        private static ProjectViewModel CreateProjectViewModel(Project project, DirectoryViewModel parent) {
            return new ProjectViewModel(parent, project);
        }
    }
}