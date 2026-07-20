using System.Text;

namespace LearningCSharp.Course.Unit11.Practice;

public static class RecipePersistence
{
    public static string GetSafePath(string rootDirectory, string fileName)
    {
        throw new NotImplementedException("TODO: Validate the root directory and simple .json file name.");
    }

    public static string SerializeToJsonText(RecipeCatalog collection)
    {
        throw new NotImplementedException("TODO: Turn the object graph into JSON text.");
    }

    public static byte[] SerializeToUtf8(RecipeCatalog collection)
    {
        throw new NotImplementedException("TODO: Turn the JSON text into UTF-8 bytes.");
    }

    public static RecipeCatalog DeserializeFromJsonText(string jsonText)
    {
        throw new NotImplementedException("TODO: Turn JSON text back into a recipe collection.");
    }

    public static RecipeCatalog DeserializeFromUtf8(ReadOnlySpan<byte> utf8Json)
    {
        throw new NotImplementedException("TODO: Turn UTF-8 bytes back into JSON text and then into objects.");
    }

    public static RecipeCatalog Load(
        string rootDirectory,
        string fileName)
    {
        throw new NotImplementedException("TODO: Read the file safely and convert it back into objects.");
    }

    public static void SaveAtomically(
        string rootDirectory,
        string fileName,
        RecipeCatalog collection)
    {
        throw new NotImplementedException("TODO: Save JSON bytes through a temp file and move it into place.");
    }
}
