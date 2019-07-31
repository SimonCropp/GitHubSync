using System;
using GitHubSync;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class PartsTests :
    XunitLoggingBase
{
    [Fact]
    public void Tree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", "buildSupport");
        ObjectApprover.Verify(parts);
    }

    [Fact]
    public void Blob()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Blob, "develop", "src/settings");

        ObjectApprover.Verify(parts);
    }

    [Fact]
    public void CannotEscapeOutOfARootTree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", null);

        ObjectApprover.Verify(parts);
// ReSharper disable once UnusedVariable
        Assert.Throws<Exception>(() =>
        {
            var parent = parts.ParentTreePart;
        });
    }

    public PartsTests(ITestOutputHelper output) :
        base(output)
    {
    }
}