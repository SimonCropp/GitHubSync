using System.Collections.Generic;

namespace GitHubSync
{
    public class RepoToSync
    {
        public string Owner { get; set; }
        public string Repo { get; set; }
        public string TargetBranch { get; set; }
        public Dictionary<string,string> ReplacementTokens { get; set; }

        public Mapper GetMapper(List<SyncItem> syncItems)
        {
            Guard.AgainstNull(syncItems, nameof(syncItems));
            var mapper = new Mapper();

            foreach (var syncItem in syncItems)
            {
                var toPart = new Parts($"{Owner}/{Repo}", syncItem.Parts.Type, TargetBranch, ApplyTargetPathTemplate(syncItem));
                mapper.Add(syncItem.Parts, toPart);
            }

            return mapper;
        }

        string ApplyTargetPathTemplate(SyncItem syncItem)
        {
            var target = syncItem.Target;
            if (string.IsNullOrEmpty(target))
            {
                return syncItem.Parts.Path;
            }

            if (ReplacementTokens != null)
            {
                foreach (var token in ReplacementTokens)
                {
                    target = target.Replace(token.Key, token.Value);
                }
            }

            return target;
        }
    }
}