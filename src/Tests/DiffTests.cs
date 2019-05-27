using System;
using System.Threading.Tasks;
using GitHubSync;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

[Trait("Category", "Integration")]
public class DiffTests :
    XunitLoggingBase
{
    Syncer BuildSyncer()
    {
        return new Syncer(CredentialsHelper.Credentials, null, WriteLine);
    }

    [Fact]
    public async Task NothingToUpdateWhenSourceBlobAndDestinationBlobHaveTheSameSha()
    {
        var blob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "file.txt");

        var map = new Mapper()
            .Add(blob, blob);

        using (var syncer = BuildSyncer())
        {
            var diff = await syncer.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeAddedOrUpdatedEntries);
        }
    }

    [Fact]
    public async Task CanDetectBlobUpdation()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "file.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        using (var som = BuildSyncer())
        {
            var diff = await som.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeAddedOrUpdatedEntries);
        }
    }

    [Fact]
    public async Task CanDetectBlobCreation()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "new-file.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "new-file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        using (var som = BuildSyncer())
        {
            var diff = await som.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeAddedOrUpdatedEntries);
        }
    }

    [Fact]
    public async Task ThrowsWhenSourceBlobDoesNotExist()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "IDoNotExist.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        using (var syncer = BuildSyncer())
        {
            await Assert.ThrowsAsync<Exception>(async () => await syncer.Diff(map));
        }
    }

    [Fact]
    public async Task NothingToUpdateWhenSourceTreeAndDestinationTreeHaveTheSameSha()
    {
        var tree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder");

        var map = new Mapper()
            .Add(tree, tree);

        using (var syncer = BuildSyncer())
        {
            var diff = await syncer.Diff(map);
            Assert.Empty(diff.ToBeAddedOrUpdatedEntries);
            Assert.Empty(diff.ToBeRemovedEntries);
        }
    }

    [Fact]
    public async Task CanDetectTreeUpdation()
    {
        var sourceTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder");
        var destinationTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        using (var syncer = BuildSyncer())
        {
            var diff = await syncer.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeAddedOrUpdatedEntries);
        }
    }

    [Fact]
    public async Task CanDetectTreeCreation()
    {
        var sourceTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "folder/sub2");
        var destinationTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder/sub2");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        using (var syncer = BuildSyncer())
        {
            var diff = await syncer.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeAddedOrUpdatedEntries);
        }
    }

    [Fact]
    public async Task ThrowsWhenSourceTreeDoesNotExist()
    {
        var sourceTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "blessed-source", "IDoNotExist/folder/sub2");
        var destinationTree = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Tree, "consumer-one", "folder/sub2");

        var map = new Mapper()
            .Add(sourceTree, destinationTree);

        using (var syncer = BuildSyncer())
        {
            await Assert.ThrowsAsync<Exception>(async () => await syncer.Diff(map));
        }
    }

    [Fact]
    public async Task CanDetectBlobCreationWhenTargetTreeFolderDoesNotExist()
    {
        var sourceBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "blessed-source", "new-file.txt");
        var destinationBlob = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "IDoNotExist/MeNeither/new-file.txt");

        var map = new Mapper()
            .Add(sourceBlob, destinationBlob);

        using (var syncer = BuildSyncer())
        {
            var diff = await syncer.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeAddedOrUpdatedEntries);
        }
    }

    [Fact]
    public async Task OnRemoval_DoesNotThrowWhenBlobDoesNotExistInTargets()
    {
        var destinationBlob1 = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "new-new-file.txt");
        var destinationBlob2 = new Parts("SimonCropp/GitHubSync.TestRepository", TreeEntryTargetType.Blob, "consumer-one", "IDoNotExist/MeNeither/new-file.txt");

        var map = new Mapper()
            .Remove(destinationBlob1)
            .Remove(destinationBlob2);

        using (var syncer = BuildSyncer())
        {
            var diff = await syncer.Diff(map);
            ObjectApprover.VerifyWithJson(diff.ToBeRemovedEntries);
        }
    }

    public DiffTests(ITestOutputHelper output) :
        base(output)
    {
    }
}