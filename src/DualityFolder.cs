﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Duality {
    public class DualityFolder {
        [XmlAttribute("MachineId")]
        public string MachineId { get; set; }

        [XmlAttribute("Folder")]
        public string Folder { get; set; }

        [XmlAttribute("OtherFolder")]
        public string OtherFolder { get; set; }

        [XmlAttribute("LastCheckedAt")]
        public DateTime LastCheckedAt { get; set; }

        [XmlAttribute("CheckInterval")]
        public DateTime CheckInterval { get; set; }

        [XmlAttribute("NextCheckAt")]
        public DateTime NextCheckAt { get; set; }

        [XmlIgnore]
        public bool Needed { get; set; }

        public bool NeedsProcessing() {
            return NextCheckAt <= DateTime.Now;
        }

        public string Process() {
            var errorMessage = "";
            if (!Directory.Exists(Folder)) {
                Directory.CreateDirectory(Folder);
            }
            if (!Directory.Exists(OtherFolder)) {
                Directory.CreateDirectory(OtherFolder);
            }
            if (errorMessage.Length == 0) {
                var searchOption = Folder.Substring(Folder.Length - 2) == ".\\" ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                foreach (var shortFileName in from fileName in Directory.EnumerateFiles(Folder, "*", searchOption) where !fileName.EndsWith(".HDP") select fileName.Substring(Folder.Length)) {
                    if (!File.Exists(Folder + shortFileName)) {
                        errorMessage = "Something is wrong, file not found: " + Folder + shortFileName;
                        break;
                    }
                    if (!File.Exists(OtherFolder + shortFileName)) {
                        errorMessage = "There is " + Folder + shortFileName + ",\r\nbut that file does not exist in " + OtherFolder;
                        break;
                    }
                    bool identical;
                    var f = new FileInfo(Folder + shortFileName);
                    if (f.Length >= 300000000) {
                        var f2 = new FileInfo(OtherFolder + shortFileName);
                        identical = f.Length == f2.Length;
                    } else {
                        var contents = File.ReadAllBytes(Folder + shortFileName);
                        var contents2 = File.ReadAllBytes(OtherFolder + shortFileName);
                        identical = contents.Length == contents2.Length;
                        if (identical) {
                            if (contents.Where((t, i) => t != contents2[i]).Any()) {
                                identical = false;
                            }
                        }
                    }
                    if (identical) {
                        DateTime timeStamp = File.GetLastWriteTime(Folder + shortFileName), timeStamp2 = File.GetLastWriteTime(OtherFolder + shortFileName);
                        if (timeStamp > timeStamp2) {
                            File.SetLastWriteTime(Folder + shortFileName, timeStamp2);
                        } else if (timeStamp < timeStamp2) {
                            File.SetLastWriteTime(OtherFolder + shortFileName, timeStamp);
                        }
                        continue;
                    }
                    errorMessage = "The contents (or last-write-time) of " + Folder + shortFileName + "\r\ndiffers from the contents (lwt) of " + OtherFolder + shortFileName;
                    break;
                }
            }
            if (errorMessage.Length == 0) {
                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                foreach (var fileName in Directory.EnumerateFiles(OtherFolder, "*", SearchOption.TopDirectoryOnly)) {
                    var shortFileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    if (!File.Exists(OtherFolder + shortFileName)) {
                        errorMessage = "Something is wrong, file not found: " + OtherFolder + shortFileName;
                        break;
                    }

                    if (File.Exists(Folder + shortFileName)) {
                        continue;
                    }

                    errorMessage = "There is " + OtherFolder + shortFileName + ",\r\nbut that file does not exist in " + Folder;
                    break;
                }
            }

            if (errorMessage.Length != 0) {
                return errorMessage;
            }

            LastCheckedAt = DateTime.Now;
            var random = new Random();
            var ticks = (long)((random.NextDouble() + 1) * CheckInterval.Ticks / 2);
            NextCheckAt = DateTime.Now.AddTicks(ticks);
            return errorMessage;
        }

        public List<string> TopSubFolders() {
            var topSubDirs = new List<string>() { ".\\" };
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var dir in Directory.EnumerateDirectories(Folder, "*", SearchOption.TopDirectoryOnly)) {
                var subDir = dir.Substring(Folder.Length);
                if (!(Directory.Exists(Folder + subDir) || Directory.Exists(OtherFolder + subDir))) { continue; }
                topSubDirs.Add(subDir + '\\');
            }
            return topSubDirs;
        }
    }
}
