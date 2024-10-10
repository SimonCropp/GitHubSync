using System.Globalization;
using NGitLab.Models;

static class GitHashHelper
{
    const string gitLabHashFormat = "X2";
    static readonly Encoding encoding = new UTF8Encoding(false);

    public static async Task<string> GetBlobHash(byte[] buffer)
    {
        using var stream = new MemoryStream();
        await stream.WriteAsync($"blob {buffer.Length}\0");
        await stream.WriteAsync(buffer);
        await stream.FlushAsync();

        stream.Seek(0, SeekOrigin.Begin);

        return await ComputeHash(stream);
    }

    public static async Task<string> GetTreeHash(INewTree newTree)
    {
        var orderedItems = newTree
            .Tree
            .OrderBy(t => t.Name)
            .ToList();

        using var treeStream = new MemoryStream();

        foreach (var item in orderedItems)
        {
            await treeStream.WriteAsync($"{item.Mode.TrimStart('0')} {item.Name}\0");

            treeStream.Write(ConvertShaToSpan(item.Sha));
        }

        await treeStream.FlushAsync();
        treeStream.Seek(0, SeekOrigin.Begin);

        using var stream = new MemoryStream();
        await stream.WriteAsync($"tree {treeStream.Length}\0");
        await treeStream.CopyToAsync(stream);
        await stream.FlushAsync();

        stream.Seek(0, SeekOrigin.Begin);

        return await ComputeHash(stream);
    }

    public static async Task<string> GetCommitHash(string treeSha, string parentCommitSha, Session user)
    {
        var usernameAndDate = $"{user.Username} <{user.Email}> {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} +0000";

        var commitData = new MemoryStream();
        await commitData.WriteAsync($"tree {treeSha}\n");
        await commitData.WriteAsync($"parent {parentCommitSha}\n");
        await commitData.WriteAsync($"author {usernameAndDate}\n");
        await commitData.WriteAsync($"committer {usernameAndDate}\n\n");
        await commitData.WriteAsync("example git commit message");
        await commitData.FlushAsync();

        using var stream = new MemoryStream();

        await stream.WriteAsync($"commit {commitData.Length}\0");

        commitData.Seek(0, SeekOrigin.Begin);
        await commitData.CopyToAsync(stream);

        await stream.FlushAsync();

        stream.Seek(0, SeekOrigin.Begin);

        return await ComputeHash(stream);
    }

    static async Task<string> ComputeHash(Stream stream)
    {
        Debug.Assert(stream.Position == 0);

        using var sha1 = System.Security.Cryptography.SHA1.Create();

        var hash = (await sha1.ComputeHashAsync(stream))
            .Aggregate(
                new StringBuilder(),
                (s, b) => s.Append(b.ToString(gitLabHashFormat)),
                s => s.ToString());

        return hash;
    }

    static async Task WriteAsync(this Stream stream, string value)
    {
        var buffer = encoding.GetBytes(value);
        await stream.WriteAsync(buffer);
    }

    static ReadOnlySpan<byte> ConvertShaToSpan(string sha)
    {
        Debug.Assert(sha.Length == 40);

        var buffer = new byte[20];

        for (var i = 0; i < 40; i += 2)
        {
            buffer[i >> 1] = byte.Parse(sha.Substring(i, 2), NumberStyles.HexNumber);
        }

        return buffer;
    }
}