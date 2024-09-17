class Repository
{
    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string Branch { get; set; } = "master";

    public bool AutoMerge { get; set; }

    public List<string> Templates { get; set; } = new();

    public override string ToString() =>
        $"{Name} ({Url})";
}