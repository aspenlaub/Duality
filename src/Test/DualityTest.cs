using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Duality.Test {
    [TestClass]
    public class DualityTest {
        private IFolder TempFolder(bool master) {
            var folder = new Folder(Path.GetTempPath()).SubFolder(nameof(DualityTest) + (master ? "Master" : ""));
            folder.CreateIfNecessary();
            if (!master) { return folder; }

            var subFolders = new List<string> {
                @"\CanDoBasicProcessing\Machine1\Folder\A",
                @"\CanDoBasicProcessing\Machine1\Folder\B",
                @"\CanDoBasicProcessing\Machine1\Folder2\G",
                @"\CanDoBasicProcessing\Machine1\Folder2\H",
                @"\CanDoBasicProcessing\Machine1\OtherFolder\A",
                @"\CanDoBasicProcessing\Machine1\OtherFolder\C",
                @"\CanDoBasicProcessing\Machine1\OtherFolder2\H",
                @"\CanDoBasicProcessing\Machine1\OtherFolder2\I",
                @"\CanDoBasicProcessing\Machine1\Persistence",
                @"\CanDoBasicProcessing\Machine2\Folder\D",
                @"\CanDoBasicProcessing\Machine2\Folder\E",
                @"\CanDoBasicProcessing\Machine2\OtherFolder\E",
                @"\CanDoBasicProcessing\Machine2\OtherFolder\F",
                @"\CanDoBasicProcessing\Machine2\Persistence",
                @"\Machine1\Folder\A",
                @"\Machine1\Folder\B",
                @"\Machine1\Folder2\G",
                @"\Machine1\Folder2\H",
                @"\Machine1\OtherFolder\A",
                @"\Machine1\OtherFolder\C",
                @"\Machine1\OtherFolder2\H",
                @"\Machine1\OtherFolder2\I",
                @"\Machine1\Persistence",
                @"\Machine2\Folder\D",
                @"\Machine2\Folder\E",
                @"\Machine2\OtherFolder\E",
                @"\Machine2\OtherFolder\F",
                @"\Machine2\Persistence",
                @"\NextCheckIsWithinCheckInterval\Machine1\Folder\A",
                @"\NextCheckIsWithinCheckInterval\Machine1\Folder\B",
                @"\NextCheckIsWithinCheckInterval\Machine1\Folder2\G",
                @"\NextCheckIsWithinCheckInterval\Machine1\Folder2\H",
                @"\NextCheckIsWithinCheckInterval\Machine1\OtherFolder\A",
                @"\NextCheckIsWithinCheckInterval\Machine1\OtherFolder\B",
                @"\NextCheckIsWithinCheckInterval\Machine1\OtherFolder\C",
                @"\NextCheckIsWithinCheckInterval\Machine1\OtherFolder2\H",
                @"\NextCheckIsWithinCheckInterval\Machine1\OtherFolder2\I",
                @"\NextCheckIsWithinCheckInterval\Machine1\Persistence",
                @"\NextCheckIsWithinCheckInterval\Machine2\Folder\D",
                @"\NextCheckIsWithinCheckInterval\Machine2\Folder\E",
                @"\NextCheckIsWithinCheckInterval\Machine2\OtherFolder\E",
                @"\NextCheckIsWithinCheckInterval\Machine2\OtherFolder\F",
                @"\NextCheckIsWithinCheckInterval\Machine2\Persistence",
            };
            foreach (var subFolder in subFolders.Select(f => folder.SubFolder(f))) {
                subFolder.CreateIfNecessary();
            }
            return folder;
        }

        [TestMethod]
        public void CanSaveAndLoadFolders() {
            var folders = CreateTestFoldersOnTwoMachines(TempFolder(true), new DateTime(0));
            var fileName = TempFolder(false).FullName + @"\CanSaveAndLoadFolders.xml";
            File.Delete(fileName);
            Assert.IsFalse(File.Exists(fileName));
            folders.Save(fileName);
            var folders2 = new DualityFolders(fileName);
            Assert.AreEqual(folders.Count, folders2.Count);
            File.Delete(fileName);
            Assert.IsFalse(File.Exists(fileName));
        }

        protected static DualityFolders CreateTestFoldersOnTwoMachines(IFolder testRootFolder, DateTime checkInterval) {
            var folders = new DualityFolders();
            folders.AddRange(CreateTestFolders("Machine1", 2, testRootFolder, checkInterval));
            folders.AddRange(CreateTestFolders("Machine2", 1, testRootFolder, checkInterval));
            return folders;
        }

        protected static List<DualityFolder> CreateTestFolders(string id, uint expectedSources, IFolder testRootFolder, DateTime checkInterval) {
            uint actualSources = 0;
            var machineRootFolder = testRootFolder.SubFolder(id).FullName + '\\';
            var result = new List<DualityFolder>();
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var dir in Directory.EnumerateDirectories(machineRootFolder, "Folder*", SearchOption.TopDirectoryOnly)) {
                var folder = new DualityFolder {
                    MachineId = id,
                    Folder = dir + '\\',
                    OtherFolder = dir.Replace(@"\Folder", @"\OtherFolder") + '\\',
                    CheckInterval = checkInterval
                };
                result.Add(folder);
                actualSources++;
            }

            Assert.AreEqual(expectedSources, actualSources);
            return result;
        }

        [TestMethod]
        public void CanCreateWorkForMachine1() {
            var folders = CreateTestFoldersOnTwoMachines(TempFolder(true), new DateTime(0));
            var work = new DualityWork { ForMachine = folders[0].MachineId };
            work.UpdateFolders(folders);
            Assert.AreEqual(6, work.DualityFolders.Count);
        }

        [TestMethod]
        public void CanUpdateWorkForMachine1() {
            var folders = CreateTestFoldersOnTwoMachines(TempFolder(true), new DateTime(0));
            var work = new DualityWork { ForMachine = folders[0].MachineId };
            work.UpdateFolders(folders);
            Assert.AreEqual(6, work.DualityFolders.Count);
            var timeStamp = new DateTime(2013, 11, 3, 14, 20, 0);
            work.DualityFolders.ForEach(x => x.LastCheckedAt = timeStamp);
            work.UpdateFolders(folders);
            Assert.IsTrue(work.DualityFolders.All(x => x.LastCheckedAt == timeStamp));
        }

        [TestMethod]
        public void CanSaveAndLoadWorkForMachine1() {
            var folders = CreateTestFoldersOnTwoMachines(TempFolder(true), new DateTime(0));
            var work = new DualityWork { ForMachine = folders[0].MachineId };
            work.UpdateFolders(folders);
            var timeStamp = DateTime.Now;
            work.DualityFolders[0].LastCheckedAt = timeStamp;
            var fileName = TempFolder(false).FullName + @"\CanSaveAndLoadWorkForMachine1.xml";
            File.Delete(fileName);
            Assert.IsFalse(File.Exists(fileName));
            work.Save(fileName);
            var work2 = new DualityWork(fileName, folders[0].MachineId);
            Assert.AreEqual((object) work.DualityFolders.Count, work2.DualityFolders.Count);
            Assert.AreEqual(timeStamp, work.DualityFolders[0].LastCheckedAt);
            File.Delete(fileName);
            Assert.IsFalse(File.Exists(fileName));
        }

        private void CopyTemplateTestFileSystemTo(IFolder testRootFolder) {
            if (Directory.Exists(testRootFolder.FullName)) {
                var deleter = new FolderDeleter();
                Assert.IsTrue(deleter.CanDeleteFolder(testRootFolder));
                deleter.DeleteFolder(testRootFolder);
            }
            CopyFolderRecursivelyButNoFiles(TempFolder(true).SubFolder("Machine1").FullName + '\\', testRootFolder.FullName + @"\Machine1\");
            CopyFolderRecursivelyButNoFiles(TempFolder(true).SubFolder("Machine2").FullName + '\\', testRootFolder.FullName + @"\Machine2\");
        }

        private static void CopyFolderRecursivelyButNoFiles(string sourceDirName, string destDirName) {
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();
            if (!dir.Exists) { throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName); }

            if (!Directory.Exists(destDirName)) { Directory.CreateDirectory(destDirName); }
            foreach (var subDir in dirs) {
                var tempPath = Path.Combine(destDirName, subDir.Name);
                CopyFolderRecursivelyButNoFiles(subDir.FullName, tempPath);
            }
        }

        [TestMethod]
        public void CanDoBasicProcessing() {
            var errorsAndInfos = new ErrorsAndInfos();
            var testRootFolder = TempFolder(true).SubFolder("CanDoBasicProcessing");
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            CopyTemplateTestFileSystemTo(testRootFolder);
            var folders = CreateTestFoldersOnTwoMachines(testRootFolder, new DateTime(0));
            var work = new DualityWork { ForMachine = folders[0].MachineId };
            work.UpdateFolders(folders);
            var folder = work.DualityFolders[5];
            Assert.AreEqual("", folder.Process());
            Assert.IsTrue(folder.NeedsProcessing());
            var theFileName = testRootFolder.FullName + @"\Machine1\Folder2\H\A_File.txt";
            File.WriteAllText(theFileName, @"This is some text.");
            var timeStamp = new DateTime(2013, 11, 2, 12, 24, 6);
            File.SetLastWriteTime(theFileName, timeStamp);
            var expectedMessage = "There is " + theFileName + ",\r\nbut that file does not exist in " + testRootFolder.FullName + @"\Machine1\OtherFolder2\H\";
            Assert.AreEqual(expectedMessage, folder.Process());
            Assert.IsTrue(folder.NeedsProcessing());
            var theOtherFileName = testRootFolder.FullName + @"\Machine1\OtherFolder2\H\A_File.txt";
            File.WriteAllText(theOtherFileName, @"This is some text.");
            File.SetLastWriteTime(theOtherFileName, new DateTime(2013, 11, 6, 22, 6, 24));
            Assert.AreEqual("", folder.Process());
            Assert.IsTrue(folder.NeedsProcessing());
            Assert.AreEqual(timeStamp, File.GetLastWriteTime(theOtherFileName));
            File.WriteAllText(theOtherFileName, @"This is some text..");
            expectedMessage = "The contents (or last-write-time) of " + theFileName + "\r\ndiffers from the contents (lwt) of " + theOtherFileName;
            Assert.AreEqual(expectedMessage, folder.Process());
            Assert.IsTrue(folder.NeedsProcessing());
            File.WriteAllText(theOtherFileName, @"This is some text.");
            var theRenamedOtherFileName = theOtherFileName.Replace(".txt", ".log");
            File.WriteAllText(theRenamedOtherFileName, @"This is some text.");
            expectedMessage = "There is " + theRenamedOtherFileName + ",\r\nbut that file does not exist in " + testRootFolder.FullName + @"\Machine1\Folder2\H\";
            Assert.AreEqual(expectedMessage, folder.Process());
            Assert.IsTrue(folder.NeedsProcessing());
            File.Delete(theFileName);
            Assert.IsFalse(File.Exists(theFileName));
            File.Delete(theOtherFileName);
            Assert.IsFalse(File.Exists(theOtherFileName));
            File.Delete(theRenamedOtherFileName);
            Assert.IsFalse(File.Exists(theRenamedOtherFileName));
        }

        [TestMethod]
        public void NextCheckIsWithinCheckInterval() {
            var errorsAndInfos = new ErrorsAndInfos();
            var testRootFolder = TempFolder(true).SubFolder("NextCheckIsWithinCheckInterval");
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            CopyTemplateTestFileSystemTo(testRootFolder);
            const long ticks = 1000000000000;
            var folders = CreateTestFoldersOnTwoMachines(testRootFolder, new DateTime(ticks));
            var work = new DualityWork { ForMachine = folders[0].MachineId };
            work.UpdateFolders(folders);
            var folder = work.DualityFolders[2];
            Assert.IsTrue(folder.NeedsProcessing());
            Assert.AreEqual("", folder.Process());
            Assert.IsFalse(folder.NeedsProcessing());
            var minimum = DateTime.Now.AddTicks(ticks / 2);
            var maximum = DateTime.Now.AddTicks(ticks);
            Assert.IsTrue(folder.NextCheckAt >= minimum);
            Assert.IsTrue(folder.NextCheckAt <= maximum);
        }
    }
}
