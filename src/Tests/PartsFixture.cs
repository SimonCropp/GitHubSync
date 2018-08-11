using System;
using NUnit.Framework;
using SyncOMatic;

[TestFixture]
public class PartsFixture
{
    [Test]
    public void Tree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", "buildsupport");

        Assert.AreEqual("SimonCropp", parts.Owner);
        Assert.AreEqual("Fake", parts.Repository);
        Assert.AreEqual(TreeEntryTargetType.Tree, parts.Type);
        Assert.AreEqual("develop", parts.Branch);
        Assert.AreEqual("buildsupport", parts.Path);
        Assert.AreEqual(1, parts.NumberOfPathSegments);
        Assert.AreEqual("buildsupport", parts.Name);
        Assert.AreEqual("https://github.com/SimonCropp/Fake/tree/develop/buildsupport", parts.Url);

        var parent = parts.ParentTreePart;

        Assert.AreEqual("SimonCropp", parent.Owner);
        Assert.AreEqual("Fake", parent.Repository);
        Assert.AreEqual(TreeEntryTargetType.Tree, parent.Type);
        Assert.AreEqual("develop", parent.Branch);
        Assert.AreEqual(null, parent.Path);
        Assert.AreEqual(0, parent.NumberOfPathSegments);
        Assert.AreEqual(null, parent.Name);
        Assert.AreEqual("https://github.com/SimonCropp/Fake/tree/develop", parent.Url);
    }

    [Test]
    public void Blob()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Blob, "develop", "src/NServiceBus.sln.DotSettings");

        Assert.AreEqual("SimonCropp", parts.Owner);
        Assert.AreEqual("Fake", parts.Repository);
        Assert.AreEqual(TreeEntryTargetType.Blob, parts.Type);
        Assert.AreEqual("develop", parts.Branch);
        Assert.AreEqual("src/NServiceBus.sln.DotSettings", parts.Path);
        Assert.AreEqual(2, parts.NumberOfPathSegments);
        Assert.AreEqual("NServiceBus.sln.DotSettings", parts.Name);
        Assert.AreEqual("https://github.com/SimonCropp/Fake/blob/develop/src/NServiceBus.sln.DotSettings", parts.Url);

        var parent = parts.ParentTreePart;

        Assert.AreEqual("SimonCropp", parent.Owner);
        Assert.AreEqual("Fake", parent.Repository);
        Assert.AreEqual(TreeEntryTargetType.Tree, parent.Type);
        Assert.AreEqual("develop", parent.Branch);
        Assert.AreEqual("src", parent.Path);
        Assert.AreEqual(1, parent.NumberOfPathSegments);
        Assert.AreEqual("src", parent.Name);
        Assert.AreEqual("https://github.com/SimonCropp/Fake/tree/develop/src", parent.Url);
    }


    [Test]
    public void CanEscapeOutOfARootTree()
    {
        var parts = new Parts("SimonCropp/Fake", TreeEntryTargetType.Tree, "develop", null);

        Assert.AreEqual("SimonCropp", parts.Owner);
        Assert.AreEqual("Fake", parts.Repository);
        Assert.AreEqual(TreeEntryTargetType.Tree, parts.Type);
        Assert.AreEqual("develop", parts.Branch);
        Assert.AreEqual(null, parts.Path);
        Assert.AreEqual(0, parts.NumberOfPathSegments);
        Assert.AreEqual(null, parts.Name);
        Assert.AreEqual("https://github.com/SimonCropp/Fake/tree/develop", parts.Url);

// ReSharper disable once UnusedVariable
        Assert.Throws<InvalidOperationException>(() => { var parent = parts.ParentTreePart; });
    }
}