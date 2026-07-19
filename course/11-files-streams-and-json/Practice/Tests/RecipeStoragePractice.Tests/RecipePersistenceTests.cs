using System.Text;
using LearningCSharp.Course.Unit11.Practice;

namespace LearningCSharp.Course.Unit11.Practice.Tests;

public sealed class RecipePersistenceTests
{
    [Fact]
    public void GetSafePathAcceptsSimpleJsonFileName()
    {
        string rootDirectory = CreateWorkspace();

        string path = RecipePersistence.GetSafePath(rootDirectory, "recipes.json");

        Assert.EndsWith(Path.Combine("recipes.json"), path, StringComparison.Ordinal);
        Assert.StartsWith(Path.GetFullPath(rootDirectory), path, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../recipes.json")]
    [InlineData("nested/recipes.json")]
    [InlineData("/absolute.json")]
    [InlineData("recipes.txt")]
    public void GetSafePathRejectsUnsafeNames(string fileName)
    {
        string rootDirectory = CreateWorkspace();

        Assert.Throws<ArgumentException>(() => RecipePersistence.GetSafePath(rootDirectory, fileName));
    }

    [Fact]
    public void SerializeAndDeserializeRoundTripThroughTextAndUtf8()
    {
        RecipeCatalog expected = CreateCollection();

        string jsonText = RecipePersistence.SerializeToJsonText(expected);
        byte[] utf8Bytes = RecipePersistence.SerializeToUtf8(expected);
        RecipeCatalog fromText = RecipePersistence.DeserializeFromJsonText(jsonText);
        RecipeCatalog fromUtf8 = RecipePersistence.DeserializeFromUtf8(utf8Bytes);

        Assert.Contains("weekend pancakes", jsonText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(jsonText, Encoding.UTF8.GetString(utf8Bytes));
        Assert.Equivalent(expected, fromText);
        Assert.Equivalent(expected, fromUtf8);
    }

    [Fact]
    public void LoadReturnsEmptyWhenFileIsMissing()
    {
        string rootDirectory = CreateWorkspace();

        RecipeCatalog loaded = RecipePersistence.Load(rootDirectory, "missing.json");

        Assert.Equal(RecipeCatalog.Empty, loaded);
    }

    [Fact]
    public void LoadReturnsEmptyWhenFileIsZeroBytes()
    {
        string rootDirectory = CreateWorkspace();
        File.WriteAllBytes(Path.Combine(rootDirectory, "recipes.json"), []);

        RecipeCatalog loaded = RecipePersistence.Load(rootDirectory, "recipes.json");

        Assert.Equal(RecipeCatalog.Empty, loaded);
    }

    [Fact]
    public void SaveAtomicallyPersistsAndReloadsCatalogWithoutTempFiles()
    {
        string rootDirectory = CreateWorkspace();
        RecipeCatalog expected = CreateCollection();

        RecipePersistence.SaveAtomically(rootDirectory, "recipes.json", expected);
        RecipeCatalog loaded = RecipePersistence.Load(rootDirectory, "recipes.json");

        Assert.Equivalent(expected, loaded);
        Assert.Empty(Directory.GetFiles(rootDirectory, "*.tmp"));
    }

    [Fact]
    public void LoadThrowsInvalidDataForMalformedJson()
    {
        string rootDirectory = CreateWorkspace();
        string path = Path.Combine(rootDirectory, "recipes.json");
        File.WriteAllText(path, "{ not valid json");

        Assert.Throws<InvalidDataException>(() => RecipePersistence.Load(rootDirectory, "recipes.json"));
    }

    private static RecipeCatalog CreateCollection()
    {
        return new RecipeCatalog(
        [
            new Recipe("pancakes", "Weekend Pancakes", ["flour", "milk", "eggs"]),
            new Recipe("soup", "Tomato Soup", ["tomatoes", "garlic", "salt"]),
        ]);
    }

    private static string CreateWorkspace()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "generated", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
