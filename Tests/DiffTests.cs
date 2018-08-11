using System;
using System.Linq;
using System.Threading.Tasks;
using GitHubSync;
using Xunit;
using Xunit.Abstractions;

public class DiffTests: TestBase
{
    Syncer BuildSUT()
    {
        return new Syncer(CredentialsHelper.Credentials, null, WriteLog);
    }
    
    [Fact]
    public async Task NothingToUpdateWhenSourceBlobAndDestinationBlobHaveTheSameSha()
    {
        var blob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "file.txt");

        var map = new Mapper()
            .Add(blob, blob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Empty(diff);
    }

    [Fact]
    public async Task CanDetectBlobUpdation()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "file.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Single(diff);
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.Single(diff.Single().Value);
        Assert.NotNull(diff.Single().Value.Single().Sha);
    }

    [Fact]
    public async Task CanDetectBlobCreation()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "new-file.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "new-file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Single(diff);
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.Single(diff.Single().Value);
        Assert.Null(diff.Single().Value.Single().Sha);
    }

    [Fact]
    public async Task ThrowsWhenSourceBlobDoesNotExist()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "IDoNotExist.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        using (var som = BuildSUT())
        {
            await Assert.ThrowsAsync<Exception>(async () => await som.Diff(map));
        }
    }

    [Fact]
    public async Task NothingToUpdateWhenSourceTreeAndDestinationTreeHaveTheSameSha()
    {
        var tree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder");

        var map = new Mapper()
            .Add(tree, tree);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Empty(diff);
    }

    [Fact]
    public async Task CanDetectTreeUpdation()
    {
        var sourceTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder");
        var destinationTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Single(diff);
        var pair = diff.Single();
        Assert.NotNull(pair.Key.Sha);
        Assert.Single(pair.Value);
        Assert.NotNull(pair.Value.Single().Sha);
    }

    [Fact]
    public async Task CanDetectTreeCreation()
    {
        var sourceTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder/sub2");
        var destinationTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder/sub2");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Single(diff);
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.Single(diff.Single().Value);
        Assert.Null(diff.Single().Value.Single().Sha);
    }

    [Fact]
    public async Task ThrowsWhenSourceTreeDoesNotExist()
    {
        var sourceTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "IDoNotExist/folder/sub2");
        var destinationTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder/sub2");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        using (var som = BuildSUT())
        {
            await Assert.ThrowsAsync<Exception>(async () => await som.Diff(map));
        }
    }

    [Fact]
    public async Task CanDetectBlobCreationWhenTargetTreeFolderDoesNotExist()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "new-file.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "IDoNotExist/MeNeither/new-file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        Diff diff;
        using (var som = BuildSUT())
        {
            diff = await som.Diff(map);
        }

        Assert.Single(diff);
        Assert.NotNull(diff.Single().Key.Sha);
        Assert.Single(diff.Single().Value);
        Assert.Null(diff.Single().Value.Single().Sha);
    }

    public DiffTests(ITestOutputHelper output) : base(output)
    {
    }
}