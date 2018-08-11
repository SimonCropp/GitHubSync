using System;
using GitHubSync;
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
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Blob, "develop", "src/NServiceBus.sln.DotSettings");

        Assert.Equal("SimonCropp", parts.Owner);
        Assert.Equal("Fake", parts.Repository);
        Assert.Equal(TreeEntryTargetType.Blob, parts.Type);
        Assert.Equal("develop", parts.Branch);
        Assert.Equal("src/NServiceBus.sln.DotSettings", parts.Path);
        Assert.Equal(2, parts.NumberOfPathSegments);
        Assert.Equal("NServiceBus.sln.DotSettings", parts.Name);
        Assert.Equal("https://github.com/SimonCropp/Fake/blob/develop/src/NServiceBus.sln.DotSettings", parts.Url);

        var parent = parts.ParentTreePart;

        Assert.Equal("SimonCropp", parent.Owner);
        Assert.Equal("Fake", parent.Repository);
        Assert.Equal(TreeEntryTargetType.Tree, parent.Type);
        Assert.Equal("develop", parent.Branch);
        Assert.Equal("src", parent.Path);
        Assert.Equal(1, parent.NumberOfPathSegments);
        Assert.Equal("src", parent.Name);
        Assert.Equal("https://github.com/SimonCropp/Fake/tree/develop/src", parent.Url);
    }

    [Fact]
    public void CanEscapeOutOfARootTree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", null);

        Assert.Equal("SimonCropp", parts.Owner);
        Assert.Equal("Fake", parts.Repository);
        Assert.Equal(TreeEntryTargetType.Tree, parts.Type);
        Assert.Equal("develop", parts.Branch);
        Assert.Null(parts.Path);
        Assert.Equal(0, parts.NumberOfPathSegments);
        Assert.Null(parts.Name);
        Assert.Equal("https://github.com/SimonCropp/Fake/tree/develop", parts.Url);

// ReSharper disable once UnusedVariable
        Assert.Throws<InvalidOperationException>(() => { var parent = parts.ParentTreePart; });
    }

    public PartsTests(ITestOutputHelper output) : base(output)
    {
    }
}