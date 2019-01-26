using System;
using GitHubSync;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class PartsTests: TestBase
{
    [Fact]
    public void Tree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", "buildSupport");

        Assert.Equal("SimonCropp", parts.Owner);
        Assert.Equal("Fake", parts.Repository);
        Assert.Equal(TreeEntryTargetType.Tree, parts.Type);
        Assert.Equal("develop", parts.Branch);
        Assert.Equal("buildSupport", parts.Path);
        Assert.Equal(1, parts.NumberOfPathSegments);
        Assert.Equal("buildSupport", parts.Name);
        Assert.Equal("https://github.com/SimonCropp/Fake/tree/develop/buildSupport", parts.Url);

        var parent = parts.ParentTreePart;

        Assert.Equal("SimonCropp", parent.Owner);
        Assert.Equal("Fake", parent.Repository);
        Assert.Equal(TreeEntryTargetType.Tree, parent.Type);
        Assert.Equal("develop", parent.Branch);
        Assert.Null(parent.Path);
        Assert.Equal(0, parent.NumberOfPathSegments);
        Assert.Null(parent.Name);
        Assert.Equal("https://github.com/SimonCropp/Fake/tree/develop", parent.Url);
    }

    [Fact]
    public void Blob()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Blob, "develop", "src/settings");

        ObjectApprover.VerifyWithJson(parts);
    }

    [Fact]
    public void CannotEscapeOutOfARootTree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", null);

        ObjectApprover.VerifyWithJson(parts);
// ReSharper disable once UnusedVariable
        Assert.Throws<Exception>(() => { var parent = parts.ParentTreePart; });
    }

    public PartsTests(ITestOutputHelper output) : base(output)
    {
    }
}