using System.Xml.Linq;
using Xunit;

namespace Alerts.IntegrationTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void ProductionProjects_References_FollowApprovedInwardGraph()
    {
        var root = FindRoot();
        var expected = new Dictionary<string, string[]>
        {
            ["Alerts.Domain"] = [],
            ["Alerts.Application"] = ["Alerts.Domain"],
            ["Alerts.Infrastructure"] = ["Alerts.Application", "Alerts.Domain"],
            ["Alerts.Api"] = ["Alerts.Application", "Alerts.Infrastructure"]
        };
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);
        Assert.Equal(4, projects.Length);
        foreach (var path in projects)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var document = XDocument.Load(path);
            var references = document.Descendants("ProjectReference")
                .Select(node => Path.GetFileNameWithoutExtension((string)node.Attribute("Include")!))
                .Order().ToArray();
            Assert.Equal(expected[name].Order(), references);
            if (name == "Alerts.Infrastructure")
            {
                Assert.Equal(new[] { "Microsoft.AspNetCore.Identity.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Sqlite" },
                    document.Descendants("PackageReference").Select(n => (string)n.Attribute("Include")!).Order());
                Assert.Equal("Microsoft.AspNetCore.App", (string?)Assert.Single(document.Descendants("FrameworkReference")).Attribute("Include"));
            }
            else
            {
                Assert.Empty(document.Descendants("PackageReference"));
                Assert.Empty(document.Descendants("FrameworkReference"));
            }
            Assert.Equal(name == "Alerts.Api" ? "Microsoft.NET.Sdk.Web" : "Microsoft.NET.Sdk",
                (string?)document.Root!.Attribute("Sdk"));
        }
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Alerts.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("Run architecture tests from the repository checkout.");
    }
}
