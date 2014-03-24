using System.Collections.Generic;
using NUnit.Framework;
using Octokit;
using SyncOMatic;

[TestFixture]
public class TreeSortingFixture
{
    [Test]
    public void canCorrectlySortTreeEntries()
    {
        /*
            $ git init .
            Initialized empty Git repository in d:/sort/.git/
            $ echo a > base
            $ mkdir foo
            $ echo b > foo/one
            $ mkdir foo.bar
            $ echo b > foo.bar/one
            $
            $ git add foo/one
            $ git add foo.bar/one
            $ git add base
            $
            $ git commit -m "sort"
             3 files changed, 3 insertions(+)
             create mode 100644 base
             create mode 100644 foo.bar/one
             create mode 100644 foo/one
            $
            $ git ls-tree HEAD
            100644 blob 78981922613b2afb6025042ff6bd878ac1994e85    base
            040000 tree 6130ff146e70f13cdd304cf9c2b1795a5ec5715c    foo.bar
            040000 tree 6130ff146e70f13cdd304cf9c2b1795a5ec5715c    foo
         */
        var list = new List<NewTreeItem>
                   {
                       new NewTreeItem { Path = "foo", Mode = "040000", Sha = "07753f428765ac1afe2020b24e40785869bd4a85" },
                       new NewTreeItem { Path = "foo.bar", Mode = "040000", Sha = "07753f428765ac1afe2020b24e40785869bd4a85" },
                       new NewTreeItem { Path = "base", Mode = "100644", Sha = "d95f3ad14dee633a758d2e331151e950dd13e4ed" },
                   };

        list.Sort(new NewTreeItemComparer());

        Assert.AreEqual("base", list[0].Path);
        Assert.AreEqual("foo.bar", list[1].Path);
        Assert.AreEqual("foo", list[2].Path);
    }
}
