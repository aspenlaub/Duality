using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Duality.Test {
    [TestClass]
    public class DualityFoldersSecretTest {
        [TestMethod]
        public async Task CanGetSecretDualityFolders() {
            var componentProvider = new ComponentProvider();
            var secret = new DualityFoldersSecret();
            var errorsAndInfos = new ErrorsAndInfos();
            var secretDualityFolders = await componentProvider.SecretRepository.GetAsync(secret, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
            Assert.IsTrue(secretDualityFolders.Count >= 10);
        }
    }
}
