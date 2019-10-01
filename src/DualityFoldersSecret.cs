using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Duality {
    public class DualityFoldersSecret : ISecret<DualityFolders> {
        private DualityFolders vDualityFolders;
        public DualityFolders DefaultValue => vDualityFolders ??= new DualityFolders { new DualityFolder() };

        public string Guid => "D509308B-A0A3-46AD-A595-4E2A386894A9";
    }
}
