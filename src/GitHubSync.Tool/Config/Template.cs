class Template
{
    public Template() =>
        branch = "master";

    public string name { get; set; } = null!;

    public string url { get; set; } = null!;

    public string branch { get; set; }

    public override string ToString() =>
        $"{name} ({url})";
}