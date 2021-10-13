using GitHubSync;
using Octokit;

static class Program
{
    static Task<int> Main(string[] args)
    {
        var githubToken = Environment.GetEnvironmentVariable("Octokit_OAuthToken");
        if (string.IsNullOrWhiteSpace(githubToken))
        {
            Console.WriteLine("No environment variable 'Octokit_OAuthToken' found");
            return Task.FromResult(1);
        }

        var credentials = new Credentials(githubToken);

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

    static async Task<int> SynchronizeRepositoriesAsync(string fileName, Credentials credentials)
    {
        var context = ContextLoader.Load(fileName);

        var returnValue = 0;
        var repositories = context.Repositories;
        for (var i = 0; i < repositories.Count; i++)
        {
            var targetRepository = repositories[i];

            var prefix = $"({i + 1} / {repositories.Count})]";

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

    static Task SyncRepository(Context context, Repository targetRepository, Credentials credentials)
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

    static RepositoryInfo BuildInfo(string url, string branch, Credentials credentials)
    {
        var company = UrlHelper.GetCompany(url);
        var project = UrlHelper.GetProject(url);
        return new RepositoryInfo(credentials, company, project, branch);
    }
}