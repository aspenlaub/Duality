using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Entities.Web;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Web;

namespace Aspenlaub.Net.GitHub.CSharp.Duality.Test;

internal class FakeSecuredHttpGate : ISecuredHttpGate {
    public Task<HtmlValidationResult> IsHtmlMarkupValidAsync(string markup) {
        throw new NotImplementedException();
    }
    public Task<bool> RegisterDefectAsync(string headline, string description, bool old) {
        throw new NotImplementedException();
    }
    public Task<bool> SendShortMessageAsync(string message) {
        throw new NotImplementedException();
    }
}
