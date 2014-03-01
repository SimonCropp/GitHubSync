namespace SyncOMatic.Core.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class PartsFixture
    {
        [Test]
        public void Tree()
        {
            var parts = new Parts("Particular/ServiceInsight", TreeEntryTargetType.Tree, "develop", "buildsupport");

            Assert.AreEqual("Particular", parts.Owner);
            Assert.AreEqual("ServiceInsight", parts.Repository);
            Assert.AreEqual(TreeEntryTargetType.Tree, parts.Type);
            Assert.AreEqual("develop", parts.Branch);
            Assert.AreEqual("buildsupport", parts.Path);
            Assert.AreEqual(1, parts.NumberOfPathSegments);
            Assert.AreEqual("buildsupport", parts.Name);
            Assert.AreEqual("https://github.com/Particular/ServiceInsight/tree/develop/buildsupport", parts.Url);

            var parent = parts.ParentTreePart;

            Assert.AreEqual("Particular", parent.Owner);
            Assert.AreEqual("ServiceInsight", parent.Repository);
            Assert.AreEqual(TreeEntryTargetType.Tree, parent.Type);
            Assert.AreEqual("develop", parent.Branch);
            Assert.AreEqual(null, parent.Path);
            Assert.AreEqual(0, parent.NumberOfPathSegments);
            Assert.AreEqual(null, parent.Name);
            Assert.AreEqual("https://github.com/Particular/ServiceInsight/tree/develop", parent.Url);
        }

        [Test]
        public void Blob()
        {
            var parts = new Parts("Particular/NServiceBus", TreeEntryTargetType.Blob, "develop", "src/NServiceBus.sln.DotSettings");

            Assert.AreEqual("Particular", parts.Owner);
            Assert.AreEqual("NServiceBus", parts.Repository);
            Assert.AreEqual(TreeEntryTargetType.Blob, parts.Type);
            Assert.AreEqual("develop", parts.Branch);
            Assert.AreEqual("src/NServiceBus.sln.DotSettings", parts.Path);
            Assert.AreEqual(2, parts.NumberOfPathSegments);
            Assert.AreEqual("NServiceBus.sln.DotSettings", parts.Name);
            Assert.AreEqual("https://github.com/Particular/NServiceBus/blob/develop/src/NServiceBus.sln.DotSettings", parts.Url);

            var parent = parts.ParentTreePart;

            Assert.AreEqual("Particular", parent.Owner);
            Assert.AreEqual("NServiceBus", parent.Repository);
            Assert.AreEqual(TreeEntryTargetType.Tree, parent.Type);
            Assert.AreEqual("develop", parent.Branch);
            Assert.AreEqual("src", parent.Path);
            Assert.AreEqual(1, parent.NumberOfPathSegments);
            Assert.AreEqual("src", parent.Name);
            Assert.AreEqual("https://github.com/Particular/NServiceBus/tree/develop/src", parent.Url);
        }


        [Test]
        public void CanEscapeOutOfARootTree()
        {
            var parts = new Parts("Particular/NServiceBus", TreeEntryTargetType.Tree, "develop", null);

            Assert.AreEqual("Particular", parts.Owner);
            Assert.AreEqual("NServiceBus", parts.Repository);
            Assert.AreEqual(TreeEntryTargetType.Tree, parts.Type);
            Assert.AreEqual("develop", parts.Branch);
            Assert.AreEqual(null, parts.Path);
            Assert.AreEqual(0, parts.NumberOfPathSegments);
            Assert.AreEqual(null, parts.Name);
            Assert.AreEqual("https://github.com/Particular/NServiceBus/tree/develop", parts.Url);

            Assert.Throws<InvalidOperationException>(() => { var parent = parts.ParentTreePart; });
        }
    }
}
