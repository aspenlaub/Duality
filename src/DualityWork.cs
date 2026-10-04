using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Duality;

[XmlRoot("Folders", Namespace = "http://www.viperfisch.de")]
public class DualityWork() {
    [XmlElement("Folder")]
    public List<DualityFolder> DualityFolders { get; set; } = [];

    [XmlElement("ForMachine")]
    public string ForMachine { get; set; } = Environment.MachineName;

    public DualityWork(string fileName, string forMachine)
        : this() {
        ForMachine = forMachine;
        if (!File.Exists(fileName)) {
            return;
        }

        var fileStream = new FileStream(fileName, FileMode.Open);
        var xmlSerializer = new XmlSerializer(typeof(DualityWork));
        var collection = (DualityWork)xmlSerializer.Deserialize(fileStream);
        if (collection != null && collection.ForMachine == ForMachine) {
            foreach (DualityFolder folder in collection.DualityFolders) {
                DualityFolders.Add(folder);
            }
        }
        fileStream.Flush();
        fileStream.Close();
    }

    public bool Save(string fileName) {
        if (fileName == null || File.Exists(fileName)) {
            return false;
        }
        var streamWriter = new StreamWriter(fileName, false, Encoding.UTF8);
        var xmlSerializer = new XmlSerializer(typeof(DualityWork));
        xmlSerializer.Serialize(streamWriter, this);
        streamWriter.Close();
        return true;
    }

    public void UpdateFolders(IList<DualityFolder> dualityFolders) {
        var folders = dualityFolders.Where(x => x.MachineId.ToUpper() == ForMachine.ToUpper()).ToList();
        if (!folders.Any()) { return; }

        DualityFolders.ForEach(x => x.Needed = false);
        var newFolders = new List<DualityFolder>();
        foreach (DualityFolder folder in folders) {
            foreach (string subFolder in folder.TopSubFolders()) {
                var existingFolders = DualityFolders.Where(x => x.Folder == folder.Folder + subFolder && x.OtherFolder == folder.OtherFolder + subFolder).ToList();
                if (existingFolders.Any()) {
                    existingFolders.ForEach(x => {
                        x.Needed = true;
                        x.CheckInterval = folder.CheckInterval;
                        x.SendShortMessage = folder.SendShortMessage;
                    });
                } else {
                    newFolders.Add(new DualityFolder {
                        Folder = folder.Folder + subFolder,
                        OtherFolder = folder.OtherFolder + subFolder,
                        Needed = true,
                        LastCheckedAt = new DateTime(0),
                        CheckInterval = folder.CheckInterval,
                        SendShortMessage = folder.SendShortMessage
                    });
                }
            }
        }
        DualityFolders.Where(x => !x.Needed).ToList().ForEach(x => DualityFolders.Remove(x));
        DualityFolders.AddRange(newFolders);
    }
}