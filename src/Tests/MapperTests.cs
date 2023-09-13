using GitHubSync;

[UsesVerify]
public class MapperTests :
    XunitContextBase
{
    [Fact]
    public Task CanAddAndEnumerate()
    {
        var a = new Parts("o", "r1", TreeEntryTargetType.Blob, "b1", "a");
        var c = new Parts("o", "r1", TreeEntryTargetType.Tree, "b1", "c");

        var one = new Parts("o", "r2", TreeEntryTargetType.Blob, "b1", "a");
        var two = new Parts("o", "r3", TreeEntryTargetType.Blob, "b1", "a");
        var three = new Parts("o", "r2", TreeEntryTargetType.Tree, "b1", "c");

        var m = new Mapper()
            .Add(a, one)
            .Add(a, two)
            .Add(c, three);
        return Verify(m.ToBeAddedOrUpdatedEntries);
    }

    [Fact]
    public void CanOnlyMapCorrespondingTypes()
    {
        var blob = new Parts("o", "r1", TreeEntryTargetType.Blob, "b1", "a");
        var tree = new Parts("o", "r1", TreeEntryTargetType.Tree, "b1", "c");

        var m = new Mapper();

        Assert.Throws<ArgumentException>(() => m.Add(blob, tree));
        Assert.Throws<ArgumentException>(() => m.Add(tree, blob));
    }

    [Fact]
    public Task TransposeRegroupsPerTargetRepositoryAndBranch()
    {
        var m = new Mapper()
            .Add(new("o1", "r1", TreeEntryTargetType.Blob, "b1", "a"),
                new("o1", "r2", TreeEntryTargetType.Blob, "b1", "a"),
                new("o1", "r2", TreeEntryTargetType.Blob, "b1", "b"),
                new("o1", "r3", TreeEntryTargetType.Blob, "b1", "a"))
            .Add(new("o1", "r1", TreeEntryTargetType.Tree, "b1", "t1"),
                new Parts("o1", "r2", TreeEntryTargetType.Tree, "b1", "t"))
            .Add(new("o2", "r4", TreeEntryTargetType.Tree, "b3", "t3"),
                new("o1", "r3", TreeEntryTargetType.Tree, "b1", "sub/t"),
                new("o1", "r2", TreeEntryTargetType.Tree, "b1", "t2"))
            .Add(new("o1", "r1", TreeEntryTargetType.Tree, "b1", "t2"),
                new("o1", "r2", TreeEntryTargetType.Tree, "b2", "t"),
                new("o1", "r3", TreeEntryTargetType.Tree, "b1", "t"))
            .Remove(new("o1", "r2", TreeEntryTargetType.Blob, "b1", "c"))
            .Remove(new("o1", "r3", TreeEntryTargetType.Blob, "b1", "sub/level/d"))
            .Remove(new("o1", "r4", TreeEntryTargetType.Blob, "b1", "e"));

        var t = m.Transpose();

        Assert.Equal(3, t.Values.SelectMany(_ => _.Where(_ => _.Item2 is Parts.NullParts)).Count());

        var orbs = t.Keys.ToList();
        orbs.Sort(StringComparer.Ordinal);

        return Verify(orbs);
    }

    [Fact]
    public void CannotRemoveTreeFromOneTarget()
    {
        var m = new Mapper();
        var to = new Parts("target", "r", TreeEntryTargetType.Tree, "branch", "file.txt");

        Assert.Throws<NotSupportedException>(() => m.Remove(to));
    }

    [Fact]
    public void CanRemoveBlobFromOneTarget()
    {
        var m = new Mapper();
        var to = new Parts("target1", "r", TreeEntryTargetType.Blob, "branch", "file.txt");

        m.Remove(to);
        Assert.Single(m.ToBeRemovedEntries);
        Assert.Equal(to, m.ToBeRemovedEntries.First());
    }

    [Fact]
    public void CannotRemoveAnAlreadyAddedTargetPath()
    {
        var m = new Mapper();
        var from = new Parts("source", "r", TreeEntryTargetType.Blob, "branch", "file.txt");
        var to = new Parts("target", "r", TreeEntryTargetType.Blob, "branch", "file.txt");

        m.Add(from, to);
        Assert.Throws<InvalidOperationException>(() => m.Remove(to));
    }

    [Fact]
    public void CannotAddAnAlreadyRemovedTargetPath()
    {
        var m = new Mapper();
        var from = new Parts("source", "r", TreeEntryTargetType.Blob, "branch", "file.txt");
        var to = new Parts("target", "r", TreeEntryTargetType.Blob, "branch", "file.txt");

        m.Remove(to);
        Assert.Throws<InvalidOperationException>(() => m.Add(from, to));
    }

    public MapperTests(ITestOutputHelper output) :
        base(output)
    {
    }
}