namespace Config
{
    using Octokit;
    using System.Collections.Generic;

    internal class Context
    {
        public Context()
        {
            Templates = new List<Template>();
            Repositories = new List<Repository>();
        }

        public List<Template> Templates { get; private set; }

        public List<Repository> Repositories { get; private set; }

        public Credentials Credentials { get; set; }
    }
}