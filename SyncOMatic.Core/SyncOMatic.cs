namespace SyncOMatic.Core
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Octokit;

    public class SyncOMatic : IDisposable
    {
        readonly GitHubGateway gw;
        readonly Action<LogEntry> logCallBack;

        // TODO: Maybe expose api rate info per connection?
        // TODO: Add SyncResult (BranchCreated, PullRequestCreated, PullRequestMerged) + url

        public SyncOMatic(
            IEnumerable<Tuple<Credentials, string>> credentialsPerRepos,
            IWebProxy proxy = null,
            Action<LogEntry> loggerCallback = null)
        {
            logCallBack = loggerCallback ?? NullLogger;

            gw = new GitHubGateway(credentialsPerRepos, proxy, logCallBack);
        }

        public SyncOMatic(
            Credentials defaultCredentials,
            IWebProxy proxy = null,
            Action<LogEntry> loggerCallback = null)
        {
            logCallBack = loggerCallback ?? NullLogger;

            gw = new GitHubGateway(defaultCredentials, proxy, logCallBack);
        }

        static Action<LogEntry> NullLogger = _ => { };

        private void log(string message, params object[] values)
        {
            logCallBack(new LogEntry(message, values));
        }

        public Diff Diff(Mapper input)
        {
            var outMapper = new Diff();

            foreach (var kvp in input)
            {
                var source = kvp.Key;

                log("Diff - Analyze {0} source '{1}'.",
                    source.Type, source.Url);

                var richSource = EnrichWithShas(source, true);

                foreach (var dest in kvp.Value)
                {
                    log("Diff - Analyze {0} target '{1}'.",
                        source.Type, dest.Url);

                    var richDest = EnrichWithShas(dest, false);

                    if (richSource.Sha == richDest.Sha)
                    {
                        log("Diff - No sync required. Matching sha ({0}) between target '{1}' and source '{2}.",
                            richSource.Sha.Substring(0, 7), dest.Url, source.Url);

                        continue;
                    }

                    log("Diff - {4} required. Non-matching sha ({0} vs {1}) between target '{2}' and source '{3}.",
                        richSource.Sha.Substring(0, 7), richDest.Sha == null ? "NULL" : richDest.Sha.Substring(0, 7), dest.Url, source.Url, richDest.Sha == null ? "Creation" : "Updation");

                    outMapper.Add(richSource, richDest);
                }
            }

            return outMapper;
        }

        Parts EnrichWithShas(Parts part, bool throwsIfNotFound)
        {
            var outPart = part;

            switch (part.Type)
            {
                case TreeEntryTargetType.Tree:
                    var t = gw.TreeFrom(part, throwsIfNotFound);

                    if (t != null)
                        outPart = t.Item1;

                    break;
                case TreeEntryTargetType.Blob:
                    var b = gw.BlobFrom(part, throwsIfNotFound);

                    if (b != null)
                        outPart = b.Item1;

                    break;
                default:
                    throw new NotSupportedException();
            }

            return outPart;
        }

        public void Dispose()
        {
            gw.Dispose();
        }
    }
}
