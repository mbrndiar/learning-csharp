using System.Text;

namespace LearningCSharp.Course.Unit11.Practice;

public static class RecipePersistence
{
    public static string GetSafePath(string rootDirectory, string fileName)
    {
        // TODO: Reject a blank root directory and a blank file name.
        // TODO: Reject rooted or nested names and anything that is not a simple .json file.
        // TODO: Return an absolute path anchored inside the root directory.
        throw new NotImplementedException("TODO: Validate the root directory and simple .json file name.");
    }

    public static string SerializeToJsonText(RecipeCatalog collection)
    {
        // TODO: Reject a null catalog before serializing.
        // TODO: Produce JSON text whose property names match what the round-trip expects.
        throw new NotImplementedException("TODO: Turn the object graph into JSON text.");
    }

    public static byte[] SerializeToUtf8(RecipeCatalog collection)
    {
        // TODO: Reuse the text serializer so both paths emit identical JSON, then encode as UTF-8 bytes.
        throw new NotImplementedException("TODO: Turn the JSON text into UTF-8 bytes.");
    }

    public static RecipeCatalog DeserializeFromJsonText(string jsonText)
    {
        // TODO: Reject blank JSON text.
        // TODO: Surface malformed or empty JSON as an InvalidDataException instead of a raw parser error.
        throw new NotImplementedException("TODO: Turn JSON text back into a recipe collection.");
    }

    public static RecipeCatalog DeserializeFromUtf8(ReadOnlySpan<byte> utf8Json)
    {
        // TODO: Reject empty byte input, then decode UTF-8 and reuse the text deserialization path.
        throw new NotImplementedException("TODO: Turn UTF-8 bytes back into JSON text and then into objects.");
    }

    public static RecipeCatalog Load(
        string rootDirectory,
        string fileName)
    {
        // TODO: Resolve a safe path first.
        // TODO: Return the empty catalog when the file is missing or zero bytes.
        // TODO: Open the file with read sharing and dispose the stream/reader before returning objects.
        throw new NotImplementedException("TODO: Read the file safely and convert it back into objects.");
    }

    public static void SaveAtomically(
        string rootDirectory,
        string fileName,
        RecipeCatalog collection)
    {
        // TODO: Reject a null catalog and make sure the destination directory exists.
        // TODO: Write bytes to a temporary file, then move it into place so readers never see a partial file.
        // TODO: Clean up the temporary file even when the write or move fails.
        throw new NotImplementedException("TODO: Save JSON bytes through a temp file and move it into place.");
    }
}
