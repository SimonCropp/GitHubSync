namespace SyncOMatic.Core.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class SyncFixture
    {
        private SyncOMatic BuildSUT()
        {
            return new SyncOMatic(Helper.Credentials, Helper.Proxy, DiffFixture.ConsoleLogger);
        }

        [Test]
        public void SyncScenario()
        {
            var gitIgnoreFile = new Parts("Particular/RepoStandards", TreeEntryTargetType.Blob, "master", ".gitignore");
            var gitAttributesFile = new Parts("Particular/RepoStandards", TreeEntryTargetType.Blob, "master", ".gitattributes");
            var dotSettingsFile = new Parts("Particular/RepoStandards", TreeEntryTargetType.Blob, "master", "src/RepoName.sln.DotSettings");
            var buildSupportFolder = new Parts("Particular/RepoStandards", TreeEntryTargetType.Tree, "master", "buildsupport");

            var map = new Mapper()
                .Add(gitIgnoreFile,
                    new Parts("Particular/SyncOMatic", TreeEntryTargetType.Blob, "develop", ".gitignore"),
                    new Parts("Particular/GitFlowVersion", TreeEntryTargetType.Blob, "master", ".gitignore"),
                    new Parts("Particular/NServiceBus", TreeEntryTargetType.Blob, "develop", ".gitignore"),
                    new Parts("Particular/ServiceControl", TreeEntryTargetType.Blob, "develop", ".gitignore")
                )
                .Add(gitAttributesFile,
                    new Parts("Particular/SyncOMatic", TreeEntryTargetType.Blob, "develop", ".gitattributes"),
                    new Parts("Particular/GitFlowVersion", TreeEntryTargetType.Blob, "master", ".gitattributes"),
                    new Parts("Particular/NServiceBus", TreeEntryTargetType.Blob, "develop", ".gitattributes"),
                    new Parts("Particular/ServiceControl", TreeEntryTargetType.Blob, "develop", ".gitattributes")
                )
                .Add(dotSettingsFile,
                    new Parts("Particular/GitFlowVersion", TreeEntryTargetType.Blob, "master", "GitFlowVersion.sln.DotSettings"),
                    new Parts("Particular/NServiceBus", TreeEntryTargetType.Blob, "develop", "src/NServiceBus.sln.DotSettings"),
                    new Parts("Particular/ServiceControl", TreeEntryTargetType.Blob, "develop", "src/ServiceControl.sln.DotSettings")
                )
                .Add(buildSupportFolder,
                    new Parts("Particular/NServiceBus", TreeEntryTargetType.Tree, "develop", "buildsupport"),
                    new Parts("Particular/ServiceControl", TreeEntryTargetType.Tree, "develop", "buildsupport")
                )
                ;

            using (var som = BuildSUT())
            {
                var diff = som.Diff(map);
                Assert.NotNull(diff);

                foreach (var url in som.Sync(diff, SyncOutput.CreateCommit))
                {
                    //Console.WriteLine(url);
                }
            }
        }
    }
}
