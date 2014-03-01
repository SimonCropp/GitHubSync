namespace SyncOMatic.Core.Tests
{
    using System.Collections.Generic;

    public class RepoToSync
    {
        public string Name { get; set; }
        public string Branch{ get; set; }

        public string SrcRoot { get; set; }


        public string SolutionName { get; set; }

        public Mapper GetMapper(List<SyncItem> syncItems)
        {
            var mapper = new Mapper();


            foreach (var syncItem in syncItems)
            {
                var toPart = new Parts("Particular/" + Name, syncItem.Parts.Type, Branch, ApplyTargetPathTemplate(syncItem));

                mapper.Add(syncItem.Parts, toPart);
            }


            return mapper;

        }

        string ApplyTargetPathTemplate(SyncItem syncItem)
        {
            if (string.IsNullOrEmpty(syncItem.Target))
            {
                return syncItem.Parts.Path;
            }


            return syncItem.Target
                .Replace("{{src.root}}", SrcRoot)
                .Replace("{{solution.name}}", SolutionName);
        }
    }
}