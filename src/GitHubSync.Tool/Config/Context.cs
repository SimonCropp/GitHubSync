using System.Collections.Generic;

class Context
{
    public Context()
    {
        Templates = new List<Template>();
        Repositories = new List<Repository>();
    }

    public List<Template> Templates { get; set; }

    public List<Repository> Repositories { get; set; }
}