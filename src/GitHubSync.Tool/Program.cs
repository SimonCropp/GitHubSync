using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitHubSync;

static class Program
{
    static Task<int> Main(string[] args)
    {
        if (args.Length == 1)
        {
            var path = Path.GetFullPath(args[0]);
            if (!File.Exists(path))
            {
                Console.WriteLine("Path does not exist:" + path);
                return Task.FromResult(1);
            }
        }

        var context = ContextLoader.Load(".\\synchronization.yaml");

        return SynchronizeRepositoriesAsync(context);
    }

    static async Task<int> SynchronizeRepositoriesAsync(Context context)
    {
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
                await SyncRepository(context, targetRepository);

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

    static Task SyncRepository(Context context, Repository targetRepository)
    {
        var sync = new RepoSync(Console.WriteLine);

        var targetInfo = BuildInfo(context, targetRepository.Url, targetRepository.Branch);
        sync.AddTargetRepository(targetInfo);

        foreach (var sourceRepository in targetRepository.Templates
            .Select(_ => context.Templates.First(x => x.name == _)))
        {
            var sourceInfo = BuildInfo(context, sourceRepository.url, sourceRepository.branch);
            sync.AddSourceRepository(sourceInfo);
        }

        var syncOutput = SyncOutput.CreatePullRequest;

        if (targetRepository.AutoMerge)
        {
            syncOutput = SyncOutput.MergePullRequest;
        }

        return sync.Sync(syncOutput);
    }

    static RepositoryInfo BuildInfo(Context context, string url, string branch)
    {
        var company = UrlHelper.GetCompany(url);
        var project = UrlHelper.GetProject(url);
        return new RepositoryInfo(context.Credentials, company, project, branch);
    }
}