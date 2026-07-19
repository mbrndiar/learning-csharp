using System.Text;
using System.Text.Json;

JsonSerializerOptions jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
};

string dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDirectory);

var catalog = new RecipeCatalog(
[
    new Recipe("pancakes", "Weekend Pancakes", ["flour", "milk", "eggs"]),
    new Recipe("soup", "Tomato Soup", ["tomatoes", "garlic", "salt"]),
]);

string destinationPath = SafePath(dataDirectory, "recipes.json");
string jsonText = JsonSerializer.Serialize(catalog, jsonOptions);
byte[] utf8Bytes = Encoding.UTF8.GetBytes(jsonText);

Console.WriteLine("Object -> JSON text");
Console.WriteLine(jsonText);
Console.WriteLine();
Console.WriteLine($"JSON text -> UTF-8 bytes: {utf8Bytes.Length} bytes");

SaveAtomically(destinationPath, utf8Bytes);
RecipeCatalog loaded = Load(destinationPath, jsonOptions);

Console.WriteLine();
Console.WriteLine($"Loaded {loaded.Recipes.Count} recipes from {destinationPath}");

string brokenPath = SafePath(dataDirectory, "broken.json");
File.WriteAllText(brokenPath, "{ not valid json");

try
{
    _ = Load(brokenPath, jsonOptions);
}
catch (InvalidDataException exception)
{
    Console.WriteLine();
    Console.WriteLine($"Malformed input example: {exception.Message}");
}

static string SafePath(string rootDirectory, string fileName)
{
    string simpleName = Path.GetFileName(fileName);
    if (simpleName != fileName || Path.IsPathRooted(fileName))
    {
        throw new ArgumentException("Use a simple file name inside the sample data directory.", nameof(fileName));
    }

    return Path.Combine(Path.GetFullPath(rootDirectory), simpleName);
}

static void SaveAtomically(string destinationPath, byte[] utf8Bytes)
{
    string tempPath = destinationPath + ".tmp";

    try
    {
        using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(utf8Bytes);
            stream.Flush();
        }

        File.Move(tempPath, destinationPath, overwrite: true);
    }
    finally
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }
}

static RecipeCatalog Load(string path, JsonSerializerOptions options)
{
    using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    string jsonText = reader.ReadToEnd();

    try
    {
        return JsonSerializer.Deserialize<RecipeCatalog>(jsonText, options)
            ?? throw new InvalidDataException("The JSON file did not contain a recipe catalog.");
    }
    catch (JsonException exception)
    {
        throw new InvalidDataException("The recipe file contains malformed JSON.", exception);
    }
}

internal sealed record Recipe(string Slug, string Title, IReadOnlyList<string> Ingredients);

internal sealed record RecipeCatalog(IReadOnlyList<Recipe> Recipes);
