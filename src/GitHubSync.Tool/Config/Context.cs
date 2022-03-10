class Context
{
    public Context()
    {
        Templates = new();
        Repositories = new();
    }

    public List<Template> Templates { get; set; }

    public List<Repository> Repositories { get; set; }
}