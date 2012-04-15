﻿using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Solutionizer.Commands;
using Solutionizer.FileScanning;
using Solutionizer.Infrastructure;
using Solutionizer.Models;
using Solutionizer.Solution;
using Solutionizer.VisualStudio;

namespace Solutionizer.Tests {
    [TestFixture]
    public class SaveSolutionCommandTests : ProjectTestBase {
        [Test]
        public void CanAddSaveSolution() {
            CopyTestDataToPath("CsTestProject1.csproj", _testDataPath);

            _fileScanner = new FileScanningViewModel();
            _fileScanner.Path = _testDataPath;
            _fileScanner.LoadProjects();

            WaitForProjectLoaded(_fileScanner);

            Project project;
            _fileScanner.Projects.TryGetValue(Path.Combine(_testDataPath, "CsTestProject1.csproj"), out project);

            var solution = new SolutionViewModel(_testDataPath, _fileScanner.Projects);
            solution.AddProject(project);

            var targetPath = Path.Combine(_testDataPath, "test.sln");

            var cmd = new SaveSolutionCommand(targetPath, VisualStudioVersion.VS2010, solution);
            cmd.Execute();

            Assert.AreEqual(ReadFromResource("CsTestProject1.sln"), File.ReadAllText(targetPath));
        }

        [Test]
        public void CanAddSaveSolutionWithProjectReferences() {
            CopyTestDataToPath("CsTestProject1.csproj", Path.Combine(_testDataPath, "p1"));
            CopyTestDataToPath("CsTestProject2.csproj", Path.Combine(_testDataPath, "p2"));

            _fileScanner = new FileScanningViewModel();
            _fileScanner.Path = _testDataPath;
            _fileScanner.LoadProjects();

            WaitForProjectLoaded(_fileScanner);

            Project project;
            _fileScanner.Projects.TryGetValue(Path.Combine(_testDataPath, "p2", "CsTestProject2.csproj"), out project);

            var solution = new SolutionViewModel(_testDataPath, _fileScanner.Projects);
            solution.AddProject(project);

            // we need to change the Guid of the reference folder
            solution.SolutionItems.OfType<SolutionFolder>().First().Guid = new Guid("{95374152-F021-4ABB-B317-74A183A89F00}");

            var targetPath = Path.Combine(_testDataPath, "test.sln");

            var cmd = new SaveSolutionCommand(targetPath, VisualStudioVersion.VS2010, solution);
            cmd.Execute();

            Assert.AreEqual(ReadFromResource("CsTestProject2.sln"), File.ReadAllText(targetPath));
        }

        [Test]
        public void CanAddSaveSolutionWithNestedProjectReferences() {
            CopyTestDataToPath("CsTestProject1.csproj", Path.Combine(_testDataPath, "sub", "p1"));
            CopyTestDataToPath("CsTestProject2.csproj", Path.Combine(_testDataPath, "sub", "p2"));
            CopyTestDataToPath("CsTestProject3.csproj", Path.Combine(_testDataPath, "p3", "sub"));

            _fileScanner = new FileScanningViewModel();
            _fileScanner.Path = _testDataPath;
            _fileScanner.LoadProjects();

            WaitForProjectLoaded(_fileScanner);

            Project project;
            _fileScanner.Projects.TryGetValue(Path.Combine(_testDataPath, "p3", "sub", "CsTestProject3.csproj"), out project);

            var solution = new SolutionViewModel(_testDataPath, _fileScanner.Projects);
            solution.AddProject(project);

            // we need to change the Guid of the reference folder
            var refFolder = solution.SolutionItems.OfType<SolutionFolder>().First();
            refFolder.Guid = new Guid("{95374152-F021-4ABB-B317-74A183A89F00}");
            refFolder.Items.OfType<SolutionFolder>().First().Guid = new Guid("{CE1BA3BF-4957-4CBC-9D45-3DC68106B311}");

            var targetPath = Path.Combine(_testDataPath, "test.sln");

            var cmd = new SaveSolutionCommand(targetPath, VisualStudioVersion.VS2010, solution);
            cmd.Execute();

            Assert.AreEqual(ReadFromResource("CsTestProject3.sln"), File.ReadAllText(targetPath));
        }
    }
}