namespace SyncOMatic.Core.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class MapperFixture
    {
        [Test]
        public void CanAddAndEnumerate()
        {
            var a = new Parts("o/r1", TreeEntryTargetType.Blob, "b1", "a");
            var c = new Parts("o/r1", TreeEntryTargetType.Tree, "b1", "c");

            var one = new Parts("o/r2", TreeEntryTargetType.Blob, "b1", "a");
            var two = new Parts("o/r3", TreeEntryTargetType.Blob, "b1", "a");
            var three = new Parts("o/r2", TreeEntryTargetType.Tree, "b1", "c");

            var m = new Mapper()
                .Add(a, one)
                .Add(a, two)
                .Add(c, three);

            Assert.AreEqual(2, m.Count());

            Assert.AreEqual(2, m[a].Count());
            Assert.AreEqual(1, m[c].Count());

            var b = new Uri("http://github.com/o/r1/blob/b1/b");
            Assert.AreEqual(0, m[b].Count());
        }

        [Test]
        public void CanOnlyMapCorrespondingTypes()
        {
            var blob = new Parts("o/r1", TreeEntryTargetType.Blob, "b1", "a");
            var tree = new Parts("o/r1", TreeEntryTargetType.Tree, "b1", "c");

            var m = new Mapper();

            Assert.Throws<ArgumentException>(() => m.Add(blob, tree));
            Assert.Throws<ArgumentException>(() => m.Add(tree, blob));
        }

        [Test]
        public void TransposeRegroupsPerTargetRepositoryAndBranch()
        {
            var m = new Mapper()
                .Add(new Parts("o1/r1", TreeEntryTargetType.Blob, "b1", "a"),
                    new Parts("o1/r2", TreeEntryTargetType.Blob, "b1", "a"),
                    new Parts("o1/r2", TreeEntryTargetType.Blob, "b1", "b"),
                    new Parts("o1/r3", TreeEntryTargetType.Blob, "b1", "a"))
                .Add(new Parts("o1/r1", TreeEntryTargetType.Tree, "b1", "t1"),
                    new Parts("o1/r2", TreeEntryTargetType.Tree, "b1", "t"))
                .Add(new Parts("o2/r4", TreeEntryTargetType.Tree, "b3", "t3"),
                    new Parts("o1/r3", TreeEntryTargetType.Tree, "b1", "sub/t"),
                    new Parts("o1/r2", TreeEntryTargetType.Tree, "b1", "t2"))
                .Add(new Parts("o1/r1", TreeEntryTargetType.Tree, "b1", "t2"),
                    new Parts("o1/r2", TreeEntryTargetType.Tree, "b2", "t"),
                    new Parts("o1/r3", TreeEntryTargetType.Tree, "b1", "t"));

            var t = m.Transpose();

            var orbs = t.Keys.ToList();
            orbs.Sort(StringComparer.Ordinal);

            Assert.AreEqual(new[] { "o1/r2/b1", "o1/r2/b2", "o1/r3/b1" }, orbs.ToArray());
        }
    }
}
