class Repository
{
    public Repository()
    {
        Branch = "master";

        Templates = new();
    }

    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string Branch { get; set; } = null!;

    public bool AutoMerge { get; set; }

    public List<string> Templates { get; set; } = null!;

    public override string ToString() =>
        $"{Name} ({Url})";
}