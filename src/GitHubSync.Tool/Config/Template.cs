class Template
{
    public string name { get; set; } = null!;

    public string url { get; set; } = null!;

    public string branch { get; set; } = "master";

    public override string ToString() =>
        $"{name} ({url})";
}