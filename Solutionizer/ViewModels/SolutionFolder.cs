using System;
using System.Collections.ObjectModel;
using System.Linq;
using Solutionizer.Infrastructure;
using Solutionizer.Models;

namespace Solutionizer.ViewModels {
    public class SolutionFolder : SolutionItem {
        private readonly SortedObservableCollection<SolutionItem> _items =
            new SortedObservableCollection<SolutionItem>(new SolutionItemComparer());

        public SolutionFolder(SolutionFolder parent) : base(parent) {
        }

        public ObservableCollection<SolutionItem> Items {
            get { return _items; }
        }

        public bool ContainsProject(Project project) {
            return _items.OfType<SolutionProject>().Any(p => p.Guid == project.Guid);
        }

        public SolutionFolder GetOrCreateSubfolder(string folderName) {
            var folder = _items.OfType<SolutionFolder>().SingleOrDefault(p => p.Name == folderName);
            if (folder == null) {
                folder = new SolutionFolder(this) {
                    Guid = Guid.NewGuid(),
                    Name = folderName
                };
                _items.Add(folder);
            }
            return folder;
        }

        public void AddProject(Project project) {
            _items.Add(new SolutionProject(this) {
                Guid = project.Guid,
                Name = project.Name,
                Filepath = project.Filepath
            });
        }
    }
}