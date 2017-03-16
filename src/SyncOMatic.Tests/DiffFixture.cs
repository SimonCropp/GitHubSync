using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SyncOMatic;

[TestFixture]
public class DiffFixture
{
    Syncer BuildSUT()
    {
        return new Syncer(Helper.Credentials, null, ConsoleLogger);
    }

    static void ConsoleLogger(LogEntry obj)
    {
        Console.WriteLine("{0:o}\t{1}", obj.At, obj.What);
    }

    [Test]
    public void NothingToUpdateWhenSourceBlobAndDestinationBlobHaveTheSameSha()
    {
        var blob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "file.txt");

        var map = new Mapper()
            .Add(blob, blob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(0, diff.Count());
    }

    [Test]
    public void CanDetectBlobUpdation()
    {
        var sourceBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "file.txt");
        var destinationBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(1, diff.Count());
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.AreEqual(1, diff.Single().Value.Count());
        Assert.NotNull(diff.Single().Value.Single().Sha);
    }

    [Test]
    public void CanDetectBlobCreation()
    {
        var sourceBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "new-file.txt");
        var destinationBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "new-file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(1, diff.Count());
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.AreEqual(1, diff.Single().Value.Count());
        Assert.Null(diff.Single().Value.Single().Sha);
    }

    [Test]
    public async Task ThrowsWhenSourceBlobDoesNotExist()
    {
        var sourceBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "IDoNotExist.txt");
        var destinationBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        using (var som = BuildSUT())
        {
            await AssertEx.ThrowsAsync<Exception>(async () => await som.Diff(map).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }

    [Test]
    public void NothingToUpdateWhenSourceTreeAndDestinationTreeHaveTheSameSha()
    {
        var tree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder");

        var map = new Mapper()
            .Add(tree, tree);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(0, diff.Count());
    }

    [Test]
    public void CanDetectTreeUpdation()
    {
        var sourceTree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder");
        var destinationTree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(1, diff.Count());
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.AreEqual(1, diff.Single().Value.Count());
        Assert.NotNull(diff.Single().Value.Single().Sha);
    }

    [Test]
    public void CanDetectTreeCreation()
    {
        var sourceTree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder/sub2");
        var destinationTree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder/sub2");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(1, diff.Count());
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.AreEqual(1, diff.Single().Value.Count());
        Assert.Null(diff.Single().Value.Single().Sha);
    }

    [Test]
    public async Task ThrowsWhenSourceTreeDoesNotExist()
    {
        var sourceTree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "IDoNotExist/folder/sub2");
        var destinationTree = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder/sub2");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        using (var som = BuildSUT())
        {
            await AssertEx.ThrowsAsync<Exception>(async () => await som.Diff(map).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }

    [Test]
    public void CanDetectBlobCreationWhenTargetTreeFolderDoesNotExist()
    {
        var sourceBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "new-file.txt");
        var destinationBlob = new Parts("Particular/SyncOMatic.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "IDoNotExist/MeNeither/new-file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = som.Diff(map).Result;
        }

        Assert.AreEqual(1, diff.Count());
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.AreEqual(1, diff.Single().Value.Count());
        Assert.Null(diff.Single().Value.Single().Sha);
    }
}