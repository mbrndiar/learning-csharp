using System.Text;

namespace LearningCSharp.Course.Unit11.Practice;

public static class RecipePersistence
{
    public static string GetSafePath(string rootDirectory, string fileName)
    {
        // TODO: Implement GetSafePath so it rejects a blank root directory or file
        // name, rejects rooted or nested names and anything that is not a simple
        // .json file, and returns an absolute path anchored inside the root
        // directory.
        throw new NotImplementedException("TODO: Validate the root directory and simple .json file name.");
    }

    public static string SerializeToJsonText(RecipeCatalog collection)
    {
        // TODO: Implement SerializeToJsonText so it rejects a null catalog and
        // produces JSON text whose property names match what the round-trip
        // expects.
        throw new NotImplementedException("TODO: Turn the object graph into JSON text.");
    }

    public static byte[] SerializeToUtf8(RecipeCatalog collection)
    {
        // TODO: Implement SerializeToUtf8 so it produces UTF-8 bytes that decode
        // back to exactly the same JSON text SerializeToJsonText produces for the
        // same catalog.
        throw new NotImplementedException("TODO: Turn the JSON text into UTF-8 bytes.");
    }

    public static RecipeCatalog DeserializeFromJsonText(string jsonText)
    {
        // TODO: Implement DeserializeFromJsonText so it rejects blank JSON text and
        // surfaces malformed or empty JSON as an InvalidDataException instead of a
        // raw parser error.
        throw new NotImplementedException("TODO: Turn JSON text back into a recipe collection.");
    }

    public static RecipeCatalog DeserializeFromUtf8(ReadOnlySpan<byte> utf8Json)
    {
        // TODO: Implement DeserializeFromUtf8 so it rejects empty byte input, then
        // decodes UTF-8 and reuses the text deserialization path.
        throw new NotImplementedException("TODO: Turn UTF-8 bytes back into JSON text and then into objects.");
    }

    public static RecipeCatalog Load(
        string rootDirectory,
        string fileName)
    {
        // TODO: Implement Load so it resolves a safe path first, returns the empty
        // catalog when the file is missing or zero bytes, and otherwise reads the
        // file's contents and rebuilds the catalog, surfacing malformed JSON as
        // InvalidDataException.
        throw new NotImplementedException("TODO: Read the file safely and convert it back into objects.");
    }

    public static void SaveAtomically(
        string rootDirectory,
        string fileName,
        RecipeCatalog collection)
    {
        // TODO: Implement SaveAtomically so it rejects a null catalog, ensures the
        // destination directory exists, writes bytes to a temporary file, then
        // moves it into place so readers never see a partial file, and cleans up
        // the temporary file even when the write or move fails.
        throw new NotImplementedException("TODO: Save JSON bytes through a temp file and move it into place.");
    }
}
