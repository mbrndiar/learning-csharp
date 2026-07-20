using System.Security.Cryptography;
using System.Text;

namespace ComparativeKv.Tests.Support;

internal static class SpecManifestVerifier
{
    public static string Root =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "spec");

    public static void Verify()
    {
        Assert.True(Directory.Exists(Root), $"The copied spec directory is missing: {Root}");
        Assert.Equal("1.0.0\n", File.ReadAllText(System.IO.Path.Combine(Root, "SPEC_VERSION"), Encoding.UTF8));

        var expected = ReadManifest(System.IO.Path.Combine(Root, "MANIFEST.sha256"));
        var actualPaths = Directory
            .EnumerateFiles(Root, "*", SearchOption.AllDirectories)
            .Select(path => RelativePath(path))
            .Where(static path => !string.Equals(path, "MANIFEST.sha256", StringComparison.Ordinal))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Keys.OrderBy(static path => path, StringComparer.Ordinal), actualPaths);
        foreach (var (relativePath, expectedHash) in expected)
        {
            var file = System.IO.Path.Combine(Root, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Assert.True(File.Exists(file), $"Manifest file is absent: {relativePath}");
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
            Assert.Equal(expectedHash, actualHash);
        }
    }

    private static Dictionary<string, string> ReadManifest(string manifestPath)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        var previousPath = string.Empty;
        foreach (var line in File.ReadLines(manifestPath, Encoding.UTF8))
        {
            var separator = line.IndexOf("  ", StringComparison.Ordinal);
            Assert.True(separator == 64 && line.Length > 66, $"Malformed manifest line: {line}");

            var hash = line[..separator];
            var path = line[(separator + 2)..];
            Assert.Matches("^[0-9a-f]{64}$", hash);
            Assert.True(string.CompareOrdinal(previousPath, path) < 0, "Manifest paths must be bytewise sorted.");
            Assert.True(entries.TryAdd(path, hash), $"Duplicate manifest path: {path}");
            previousPath = path;
        }

        return entries;
    }

    private static string RelativePath(string path) =>
        System.IO.Path.GetRelativePath(Root, path).Replace(System.IO.Path.DirectorySeparatorChar, '/');
}
