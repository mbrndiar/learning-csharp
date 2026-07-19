using System.Text;
using System.Text.Json;

namespace LearningCSharp.Course.Unit11.Practice;

public static class RecipePersistence
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string GetSafePath(string rootDirectory, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (Path.IsPathRooted(fileName) || Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException("Use a simple file name, not a rooted or nested path.", nameof(fileName));
        }

        if (!string.Equals(Path.GetExtension(fileName), ".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only .json files are allowed.", nameof(fileName));
        }

        return Path.Combine(Path.GetFullPath(rootDirectory), fileName);
    }

    public static string SerializeToJsonText(RecipeCatalog collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return JsonSerializer.Serialize(collection, SerializerOptions);
    }

    public static byte[] SerializeToUtf8(RecipeCatalog collection)
    {
        return Encoding.UTF8.GetBytes(SerializeToJsonText(collection));
    }

    public static RecipeCatalog DeserializeFromJsonText(string jsonText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonText);

        try
        {
            return JsonSerializer.Deserialize<RecipeCatalog>(jsonText, SerializerOptions)
                ?? throw new InvalidDataException("The JSON did not produce a recipe collection.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The recipe JSON is malformed.", exception);
        }
    }

    public static RecipeCatalog DeserializeFromUtf8(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            throw new ArgumentException("UTF-8 JSON bytes are required.", nameof(utf8Json));
        }

        string jsonText = Encoding.UTF8.GetString(utf8Json);
        return DeserializeFromJsonText(jsonText);
    }

    public static RecipeCatalog Load(
        string rootDirectory,
        string fileName)
    {
        string path = GetSafePath(rootDirectory, fileName);
        if (!File.Exists(path))
        {
            return RecipeCatalog.Empty;
        }

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        string jsonText = reader.ReadToEnd();
        return DeserializeFromJsonText(jsonText);
    }

    public static void SaveAtomically(
        string rootDirectory,
        string fileName,
        RecipeCatalog collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        Directory.CreateDirectory(rootDirectory);

        string destinationPath = GetSafePath(rootDirectory, fileName);
        string tempPath = destinationPath + ".tmp";
        byte[] utf8Bytes = SerializeToUtf8(collection);

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
}
