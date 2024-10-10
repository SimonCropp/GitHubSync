using NGitLab.Mock.Config;
using NGitLab.Models;

public class GitLabGatewayTests(ITestOutputHelper output)
    : XunitContextBase(output)
{
    const string helloWorldSha = "b45ef6fec89518d314f546fd6c3025367b721684";

    [Fact]
    public async Task GetCurrentUser()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var user = await gateway.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
    }

    [Theory]
    [InlineData(AccessLevel.NoAccess, false)]
    [InlineData(AccessLevel.Guest, false)]
    [InlineData(AccessLevel.Reporter, false)]
    [InlineData(AccessLevel.Developer, true)]
    [InlineData(AccessLevel.Maintainer, true)]
    [InlineData(AccessLevel.Owner, true)]
    [InlineData(AccessLevel.Admin, true)]
    public async Task IsCollaborator_WithProjectAccess(AccessLevel accessLevel, bool expected)
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithProjectOfFullPath("group/project", configure: p => p.WithUserPermission("user", accessLevel))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var isCollaborator = await gateway.IsCollaborator("group", "project");

        // Assert
        Assert.Equal(expected, isCollaborator);
    }

    [Theory]
    [InlineData(AccessLevel.NoAccess, false)]
    [InlineData(AccessLevel.Guest, false)]
    [InlineData(AccessLevel.Reporter, false)]
    [InlineData(AccessLevel.Developer, true)]
    [InlineData(AccessLevel.Maintainer, true)]
    [InlineData(AccessLevel.Owner, true)]
    [InlineData(AccessLevel.Admin, true)]
    public async Task IsCollaborator_WithGroupAccess(AccessLevel accessLevel, bool expected)
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithGroupOfFullPath("group", configure: g => g.WithUserPermission("user", accessLevel))
            .WithProjectOfFullPath("group/project")
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var isCollaborator = await gateway.IsCollaborator("group", "project");

        // Assert
        Assert.Equal(expected, isCollaborator);
    }

    [Fact]
    public async Task IsCollaborator_WithNoAccess()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user")
            .WithProjectOfFullPath("group/project")
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var isCollaborator = await gateway.IsCollaborator("group", "project");

        // Assert
        Assert.False(isCollaborator);
    }

    [Fact]
    public async Task Fork()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user")
            .WithProjectOfFullPath("group/project")
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var fork = await gateway.Fork("group", "project");

        // Assert
        Assert.NotNull(fork);
    }

    [Fact]
    public async Task DownloadBlob()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        using var targetStream = new MemoryStream();

        // Act
        await gateway.DownloadBlob(new("group", "project", TreeEntryTargetType.Blob, "main", "readme.md", helloWorldSha), targetStream);

        // Assert
        Assert.NotEqual(0, targetStream.Position);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("other title", false)]
    [InlineData("title", true)]
    public async Task HasOpenPullRequests(string? title, bool expected)
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", configure: p =>
            {
                if (title is not null)
                {
                    p.WithMergeRequest(title: title);
                }
            })
            .BuildServer();
        var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var hasOpenPullRequests = await gateway.HasOpenPullRequests("group", "project", "title");

        // Assert
        Assert.Equal(expected, hasOpenPullRequests);
    }

    [Fact]
    public async Task RootCommitFrom()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", id: 1, addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!"))
                .WithCommit("Second commit", configure: c => c.WithFile("subFolder/hello.txt", "Hello, World!")))
            .BuildServer();
        var client = server.CreateClient();
        using var gateway = new GitLabGateway(client, WriteLine);

        // Act
        var commit = await gateway.RootCommitFrom(new("group", "project", TreeEntryTargetType.Tree, "main", null));

        // Assert
        Assert.NotNull(commit);
    }

    [Fact]
    public async Task TreeFrom_DoNotThrow_Exists()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c
                    .WithFile("readme.md", "my readme")
                    .WithFile("subFolder/hello.txt", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var tree = await gateway.TreeFrom(new("group", "project", TreeEntryTargetType.Tree, "main", null), false);

        // Assert
        await Verify(tree!.Item2);
    }

    [Fact]
    public async Task TreeFrom_DoNotThrow_Exists_InSubDir()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c
                    .WithFile("readme.md", "my readme")
                    .WithFile("subFolder/hello.txt", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var tree = await gateway.TreeFrom(new("group", "project", TreeEntryTargetType.Tree, "main", "subFolder"), false);

        // Assert
        await Verify(tree!.Item2);
    }

    [Fact]
    public async Task TreeFrom_DoNotThrow_DoesNotExist()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var tree = await gateway.TreeFrom(new("group", "project", TreeEntryTargetType.Tree, "main", "subFolder"), false);

        // Assert
        Assert.Null(tree);
    }

    [Fact]
    public async Task TreeFrom_Throws_DoesNotExist()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var handler = () => gateway.TreeFrom(new("group", "project", TreeEntryTargetType.Tree, "main", "subFolder"), true);

        // Assert
        await Assert.ThrowsAsync<Exception>(handler);
    }

    [Fact]
    public async Task BlobFrom_DoNotThrow_Exists()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var blob = await gateway.BlobFrom(new("group", "project", TreeEntryTargetType.Blob, "main", "readme.md"), false);

        // Assert
        await Verify(blob!.Item2);
    }

    [Fact]
    public async Task BlobFrom_DoNotThrow_Exists_InSubDir()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c
                    .WithFile("readme.md", "Hello, World!")
                    .WithFile("subFolder/hello.txt", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var blob = await gateway.BlobFrom(new("group", "project", TreeEntryTargetType.Blob, "main", "subFolder/hello.txt"), false);

        // Assert
        await Verify(blob!.Item2);
    }

    [Fact]
    public async Task BlobFrom_DoNotThrow_DoesNotExist()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var blob = await gateway.BlobFrom(new("group", "project", TreeEntryTargetType.Blob, "main", "hello.txt"), false);

        // Assert
        Assert.Null(blob);
    }

    [Fact]
    public async Task BlobFrom_Throws_DoesNotExist()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var handler = () => gateway.BlobFrom(new("group", "project", TreeEntryTargetType.Blob, "main", "hello.txt"), true);

        // Assert
        await Assert.ThrowsAsync<Exception>(handler);
    }

    [Fact]
    public async Task CreateCommit()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        // Act
        var commit = await gateway.CreateCommit("treeSha", "group", "project", "parentSha", "branch");

        // Assert
        Assert.NotNull(commit);
    }

    [Fact]
    public async Task CreateTree_Empty()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project")
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var newTree = gateway.CreateNewTree(null);

        // Act
        var treeSha = await gateway.CreateTree(newTree, "group", "project");

        // Assert
        Assert.Equal(TargetTree.EmptyTreeSha, treeSha, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateTree_EmptyBlob()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", configure: project => project
                .WithCommit("Initial commit", configure: commit => commit
                    .WithFile("directory/.gitkeep")))
            .BuildServer();
        var client = server.CreateClient();
        using var gateway = new GitLabGateway(client, WriteLine);

        var repository = client.GetRepository(1);
        var gitKeep = repository.GetTreeAsync(new() { Path = "directory" }).Single();

        var newTree = gateway.CreateNewTree(null);
        newTree.Tree.Add(gitKeep.Mode, gitKeep.Name, gitKeep.Id.ToString(), TreeType.Blob);

        // Act
        var treeSha = await gateway.CreateTree(newTree, "group", "project");

        // Assert
        var directory = repository.GetTreeAsync(new()).Single();
        Assert.Equal(directory.Id.ToString(), treeSha, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBlob()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        await gateway.FetchBlob("group", "project", helloWorldSha);

        // Act
        await gateway.CreateBlob("group", "project", helloWorldSha);

        // Assert
        Assert.True(gateway.IsKnownBy<IBlob>(helloWorldSha, "group", "project"));
    }

    [Fact]
    public async Task CreateBranch_NewFile_NotExecutable()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var newTree = gateway.CreateNewTree(null);
        newTree.Tree.Add("100644", "hello.txt", helloWorldSha, TreeType.Blob);
        await gateway.FetchBlob("group", "project", helloWorldSha);

        var treeId = await gateway.CreateTree(newTree, "group", "project");
        var commitId = await gateway.CreateCommit(treeId, "group", "project", "main", "branch");

        // Act
        var branch = await gateway.CreateBranch("group", "project", "branch", commitId);

        // Assert
        Assert.NotNull(branch);
    }

    [Fact]
    public async Task CreateBranch_NewFile_Executable()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var newTree = gateway.CreateNewTree(null);
        var subTree = gateway.CreateNewTree("bin");
        var emptyTree = gateway.CreateNewTree("empty");
        subTree.Tree.Add("100755", "hello.sh", helloWorldSha, TreeType.Blob);
        newTree.Tree.Add("040000", "bin", await gateway.CreateTree(subTree, "group", "project"), TreeType.Tree);
        newTree.Tree.Add("040000", "empty", await gateway.CreateTree(emptyTree, "group", "project"), TreeType.Tree);
        await gateway.FetchBlob("group", "project", helloWorldSha);

        var treeId = await gateway.CreateTree(newTree, "group", "project");
        var commitId = await gateway.CreateCommit(treeId, "group", "project", "main", "branch");

        // Act
        var branch = await gateway.CreateBranch("group", "project", "branch", commitId);

        // Assert
        Assert.NotNull(branch);
    }

    [Fact]
    public async Task CreateBranch_UpdatedFile_NotExecutable()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, world!")))
            .WithProjectOfFullPath("group/project2", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var newTree = gateway.CreateNewTree(null);
        newTree.Tree.Add("100644", "readme.md", helloWorldSha, TreeType.Blob);
        await gateway.FetchBlob("group", "project2", helloWorldSha);

        var treeId = await gateway.CreateTree(newTree, "group", "project");
        var commitId = await gateway.CreateCommit(treeId, "group", "project", "main", "branch");

        // Act
        var branch = await gateway.CreateBranch("group", "project", "branch", commitId);

        // Assert
        Assert.NotNull(branch);
    }

    [Fact]
    public async Task CreateBranch_UpdatedFile_Executable()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, world!"))
                .WithCommit("Add script", configure: c => c.WithFile("bin/hello.sh", "Hello, World!")))
            .WithProjectOfFullPath("group/project2", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Add script", configure: c => c.WithFile("bin/hello.sh", "echo 'Hello, World!'")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var sourceTree = await gateway.TreeFrom(new("group", "project2", TreeEntryTargetType.Tree, "main", "bin"), false);
        var scriptSha = sourceTree!.Item2.Tree.Single().Sha;
        await gateway.FetchBlob("group", "project2", scriptSha);

        var newTree = gateway.CreateNewTree("bin");
        newTree.Tree.Add("100755", "hello.sh", scriptSha, TreeType.Blob);

        var treeId = await gateway.CreateTree(newTree, "group", "project");
        var commitId = await gateway.CreateCommit(treeId, "group", "project", "main", "branch");

        // Act
        var branch = await gateway.CreateBranch("group", "project", "branch", commitId);

        // Assert
        Assert.NotNull(branch);
    }

    [Fact]
    public async Task CreatePullRequest()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("owner/group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var newTree = gateway.CreateNewTree(null);
        newTree.Tree.Add("100644", "hello.txt", helloWorldSha, TreeType.Blob);
        await gateway.FetchBlob("owner", "group/project", helloWorldSha);

        var treeId = await gateway.CreateTree(newTree, "owner", "group/project");
        var commitId = await gateway.CreateCommit(treeId, "owner", "group/project", "main", "branch");

        _ = await gateway.CreateBranch("owner", "group/project", "branch", commitId);

        // Act
        var id = await gateway.CreatePullRequest("owner", "group/project", "branch", "main", false, null);

        // Assert
        Assert.NotEqual(0, id);
    }

    [Fact]
    public async Task ApplyLabels()
    {
        // Arrange
        using var server = new GitLabConfig()
            .WithUser("user", isDefault: true)
            .WithProjectOfFullPath("group/project", addDefaultUserAsMaintainer: true, configure: p => p
                .WithCommit("Initial commit", configure: c => c.WithFile("readme.md", "Hello, World!")))
            .BuildServer();
        using var gateway = new GitLabGateway(server.CreateClient(), WriteLine);

        var newTree = gateway.CreateNewTree(null);
        newTree.Tree.Add("100644", "hello.txt", helloWorldSha, TreeType.Blob);
        await gateway.FetchBlob("group", "project", helloWorldSha);

        var treeId = await gateway.CreateTree(newTree, "group", "project");
        var commitId = await gateway.CreateCommit(treeId, "group", "project", "main", "branch");

        _ = await gateway.CreateBranch("group", "project", "branch", commitId);

        var id = await gateway.CreatePullRequest("group", "project", "branch", "main", false, null);

        // Act
        var labels = await gateway.ApplyLabels("group", "project", id, ["label"]);

        // Assert
        Assert.NotEmpty(labels);
    }
}