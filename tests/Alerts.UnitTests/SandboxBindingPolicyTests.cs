using Alerts.Api;
using Xunit;

namespace Alerts.UnitTests;

public sealed class SandboxBindingPolicyTests
{
    [Theory]
    [InlineData("http://127.0.0.1:5080", true)]
    [InlineData("http://[::1]:5080", true)]
    [InlineData("http://0.0.0.0:5080", false)]
    [InlineData("http://*:5080", false)]
    [InlineData("http://192.168.1.2:5080", false)]
    [InlineData("http://example.test:5080", false)]
    [InlineData("http://localhost:5080", false)]
    [InlineData("https://127.0.0.1:5080", false)]
    [InlineData("http://user:password@127.0.0.1:5080", false)]
    [InlineData("http://127.0.0.1:5080/path", false)]
    [InlineData("http://127.0.0.1:5080/?key=value", false)]
    [InlineData("http://127.0.0.1:5080/#fragment", false)]
    [InlineData("", false)]
    public void IsAllowed_Binding_ReturnsExpectedDecision(string binding, bool expected)
    {
        Assert.Equal(expected, SandboxBindingPolicy.IsAllowed(binding));
    }
}
