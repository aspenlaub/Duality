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
    private readonly IContainer _Container = new ContainerBuilder().UsePegh("Duality").Build();

    [TestMethod]
    public async Task CanGetSecretDualityFolders() {
        var secret = new DualityFoldersSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        DualityFolders secretDualityFolders = await _Container.Resolve<ISecretRepository>().GetAsync(secret, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsToString());
        Assert.IsGreaterThanOrEqualTo(10, secretDualityFolders.Count);
    }
}