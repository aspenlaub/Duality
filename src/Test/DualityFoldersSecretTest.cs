using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Duality.Test;

[TestClass]
public class DualityFoldersSecretTest {
    private readonly IContainer _Container;

    public DualityFoldersSecretTest() {
        _Container = new ContainerBuilder().UsePegh("Duality", new DummyCsArgumentPrompter()).Build();
    }

    [TestMethod]
    public async Task CanGetSecretDualityFolders() {
        var secret = new DualityFoldersSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        var secretDualityFolders = await _Container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsTrue(secretDualityFolders.Count >= 10);
    }
}