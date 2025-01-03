﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Duality;

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
        var errorMessage = Process(false);
        return string.IsNullOrEmpty(errorMessage) ? Process(true) : errorMessage;
    }

    public string Process(bool checkContents) {
        var errorMessage = "";
        if (!Folder.EndsWith("\\")) {
            errorMessage = $"Folder does not end with a backslash: {Folder}";
        } else {
            if (!Directory.Exists(Folder)) {
                Directory.CreateDirectory(Folder);
            }
            if (!OtherFolder.EndsWith("\\")) {
                errorMessage = $"Other folder does not end with a backslash: {OtherFolder}";
            } else if (!Directory.Exists(OtherFolder)) {
                Directory.CreateDirectory(OtherFolder);
            }
        }
        if (errorMessage.Length == 0) {
            var searchOption = Folder.Substring(Folder.Length - 2) == ".\\" ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            List<string> shortFileNames;
            try {
                shortFileNames = (from fileName in Directory.EnumerateFiles(Folder, "*", searchOption)
                      where !fileName.EndsWith(".HDP")
                      select fileName.Substring(Folder.Length)).ToList();
            } catch (Exception) {
                errorMessage = "Something is wrong, could not list files in: " + Folder;
                shortFileNames = [];
            }
            foreach (var shortFileName in shortFileNames) {
                if (!File.Exists(Folder + shortFileName)) {
                    errorMessage = "Something is wrong, file not found: " + Folder + shortFileName;
                    break;
                }
                if ((new FileInfo(Folder + shortFileName).Attributes & FileAttributes.Hidden) != 0) {
                    errorMessage = "File is hidden: " + Folder + shortFileName;
                    break;
                }
                if ((new FileInfo(Folder + shortFileName).Attributes & FileAttributes.System) != 0) {
                    errorMessage = "File is a system file: " + Folder + shortFileName;
                    break;
                }
                if (!File.Exists(OtherFolder + shortFileName)) {
                    errorMessage = "There is\r\n" + Folder + shortFileName + ",\r\nbut that file does not exist in\r\n" + OtherFolder;
                    break;
                }

                if (!checkContents) {
                    continue;
                }

                bool identical;
                var f = new FileInfo(Folder + shortFileName);
                if (f.Length >= 300000000) {
                    var f2 = new FileInfo(OtherFolder + shortFileName);
                    identical = f.Length == f2.Length;
                } else {
                    byte[] contents, contents2;
                    try {
                        contents = File.ReadAllBytes(Folder + shortFileName);
                    } catch {
                        errorMessage = "Could not read " + Folder + shortFileName ;
                        break;
                    }
                    try{
                        contents2 = File.ReadAllBytes(OtherFolder + shortFileName);
                    } catch {
                        errorMessage = "Could not read " + OtherFolder + shortFileName;
                        break;
                    }
                    identical = contents.Length == contents2.Length;
                    if (identical) {
                        if (contents.Where((t, i) => t != contents2[i]).Any()) {
                            identical = false;
                        }
                    }
                }
                DateTime timeStamp = File.GetLastWriteTime(Folder + shortFileName), timeStamp2 = File.GetLastWriteTime(OtherFolder + shortFileName);
                if (identical) {
                    if (timeStamp > timeStamp2) {
                        File.SetLastWriteTime(Folder + shortFileName, timeStamp2);
                    } else if (timeStamp < timeStamp2) {
                        File.SetLastWriteTime(OtherFolder + shortFileName, timeStamp);
                    }
                    continue;
                }

                if (timeStamp == timeStamp2) {
                    errorMessage = "The contents of\r\n" + Folder + shortFileName + "\r\ndiffers from the contents of\r\n" + OtherFolder + shortFileName;
                } else {
                    errorMessage = "The contents and last-write-time of\r\n" + Folder + shortFileName + "\r\ndiffers from the contents/lwt of\r\n" + OtherFolder + shortFileName;
                }
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

                errorMessage = "There is\r\n" + OtherFolder + shortFileName + ",\r\nbut that file does not exist in\r\n" + Folder;
                break;
            }
        }

        if (errorMessage.Length != 0 || !checkContents) {
            return errorMessage;
        }

        LastCheckedAt = DateTime.Now;
        var random = new Random();
        var ticks = (long)((random.NextDouble() + 1) * CheckInterval.Ticks / 2);
        NextCheckAt = DateTime.Now.AddTicks(ticks);
        return errorMessage;
    }

    public List<string> TopSubFolders() {
        var topSubDirs = new List<string> { ".\\" };
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var dir in Directory.EnumerateDirectories(Folder, "*", SearchOption.TopDirectoryOnly)) {
            var subDir = dir.Substring(Folder.Length);
            if (!(Directory.Exists(Folder + subDir) || Directory.Exists(OtherFolder + subDir))) { continue; }
            topSubDirs.Add(subDir + '\\');
        }
        // ReSharper disable once LoopCanBePartlyConvertedToQuery
        foreach (var dir in Directory.EnumerateDirectories(OtherFolder, "*", SearchOption.TopDirectoryOnly)) {
            var subDir = dir.Substring(OtherFolder.Length);
            if (topSubDirs.Contains(subDir + '\\')) {
                continue;
            }
            if (!(Directory.Exists(Folder + subDir) || Directory.Exists(OtherFolder + subDir))) { continue; }
            topSubDirs.Add(subDir + '\\');
        }
        return topSubDirs;
    }

    public override string ToString() {
        int charsToOmit;
        for (charsToOmit = 0; charsToOmit <= OtherFolder.Length
            && Folder[Folder.Length - charsToOmit - 1] == OtherFolder[OtherFolder.Length - charsToOmit - 1]; charsToOmit++) {
        }

        return $"{Folder} ⇔ {OtherFolder.Substring(0, OtherFolder.Length - charsToOmit)}";
    }
}