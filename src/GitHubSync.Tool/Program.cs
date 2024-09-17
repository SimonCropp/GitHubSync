static class Program
{
    static Task<int> Main(string[] args)
    {
        var credentials = GitCredentialFactory.Create();

        if (credentials is null)
        {
            Console.WriteLine("No credentials found. Please set either 'Octokit_OAuthToken' or 'GitLab_OAuthToken' environment variable.");
            return Task.FromResult(1);
        }

        if (args.Length == 1)
        {
            var path = Path.GetFullPath(args[0]);
            if (!File.Exists(path))
            {
                Console.WriteLine("Path does not exist:" + path);
                return Task.FromResult(1);
            }

            return SynchronizeRepositoriesAsync(path, credentials);
        }

        return SynchronizeRepositoriesAsync("githubsync.yaml", credentials);
    }

    static async Task<int> SynchronizeRepositoriesAsync(string fileName, ICredentials credentials)
    {
        var context = ContextLoader.Load(fileName);

        var returnValue = 0;
        var repositories = context.Repositories;
        for (var i = 0; i < repositories.Count; i++)
        {
            var targetRepository = repositories[i];

            var prefix = $"[({i + 1} / {repositories.Count})]";

            Console.WriteLine($"{prefix} Setting up synchronization for '{targetRepository}'");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await SyncRepository(context, targetRepository, credentials);

                Console.WriteLine($"{prefix} Synchronized '{targetRepository}', took {stopwatch.Elapsed:hh\\:mm\\:ss}");
            }
            catch (Exception exception)
            {
                returnValue = 1;
                Console.WriteLine($"Failed to synchronize '{targetRepository}'. Exception: {exception}");

                Console.WriteLine("Press a key to continue...");
                Console.ReadKey();
            }
        }

        return returnValue;
    }

    static Task SyncRepository(Context context, Repository targetRepository, ICredentials credentials)
    {
        var sync = new RepoSync(Console.WriteLine);

        var targetInfo = BuildInfo(targetRepository.Url, targetRepository.Branch, credentials);
        sync.AddTargetRepository(targetInfo);

        foreach (var sourceRepository in targetRepository.Templates
            .Select(_ => context.Templates.First(x => x.name == _)))
        {
            var sourceInfo = BuildInfo(sourceRepository.url, sourceRepository.branch, credentials);
            sync.AddSourceRepository(sourceInfo);
        }

        var syncOutput = SyncOutput.CreatePullRequest;

        if (targetRepository.AutoMerge)
        {
            syncOutput = SyncOutput.MergePullRequest;
        }

        return sync.Sync(syncOutput);
    }

    static RepositoryInfo BuildInfo(string url, string branch, ICredentials credentials)
    {
        var company = UrlHelper.GetCompany(url);
        var project = UrlHelper.GetProject(url);
        return new(credentials, company, project, branch);
    }
}