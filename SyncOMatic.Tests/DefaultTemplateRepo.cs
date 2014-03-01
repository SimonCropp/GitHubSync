namespace SyncOMatic.Core.Tests
{
    using System.Collections.Generic;

    public static class DefaultTemplateRepo
    {
        public static List<SyncItem> ItemsToSync = new List<SyncItem>();

        static DefaultTemplateRepo()
        {

            ItemsToSync.Add(new SyncItem
            {
                Parts = new Parts("Particular/RepoStandards", TreeEntryTargetType.Blob, "master", ".gitignore")
            });

            ItemsToSync.Add(new SyncItem
            {
                Parts = new Parts("Particular/RepoStandards", TreeEntryTargetType.Blob, "master", ".gitattributes")
            });

            ItemsToSync.Add(new SyncItem
            {
                Parts = new Parts("Particular/RepoStandards", TreeEntryTargetType.Tree, "master", "buildsupport")
            });


            ItemsToSync.Add(new SyncItem
            {
                Target = "{{src.root}}/{{solution.name}}.sln.DotSettings",
                Parts = new Parts("Particular/RepoStandards", TreeEntryTargetType.Blob, "master", "src/RepoName.sln.DotSettings")
            });
        }
    }
}