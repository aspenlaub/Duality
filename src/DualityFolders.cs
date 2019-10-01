using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Duality {
    [XmlRoot("DualityFolders", Namespace = "http://www.aspenlaub.net")]
    public class DualityFolders : List<DualityFolder>, ISecretResult<DualityFolders> {
        public DualityFolders() { }

        public DualityFolders(string fileName) {
            if (!File.Exists(fileName)) { return; }

            var fileStream = new FileStream(fileName, FileMode.Open);
            var xmlSerializer = new XmlSerializer(typeof(DualityFolders));
            var folders = (DualityFolders)xmlSerializer.Deserialize(fileStream);
            foreach (var folder in folders) {
                Add(folder);
            }
            fileStream.Flush();
            fileStream.Close();
        }

        public bool Save(string fileName) {
            if (File.Exists(fileName)) {
                return false;
            }
            var streamWriter = new StreamWriter(fileName, false, Encoding.UTF8);
            var xmlSerializer = new XmlSerializer(typeof(DualityFolders));
            xmlSerializer.Serialize(streamWriter, this);
            streamWriter.Close();
            return true;
        }

        public DualityFolders Clone() {
            var clone = new DualityFolders();
            clone.AddRange(this);
            return clone;
        }
    }
}
