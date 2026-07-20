using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Core;

namespace ComparativeKv.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return CommandLine.Run(args, Console.Out);
    }
}

public enum CommandKind
{
    Set,
    Get,
    Delete,
    List,
}

public sealed record ParsedCommand(
    string DatabasePath,
    CommandKind Kind,
    string? Key,
    string? ValueJson,
    string? Expectation);

public static class CommandLine
{
    public static ParsedCommand ParseExact(IReadOnlyList<string>? arguments) =>
        MilestoneIncomplete.Throw<ParsedCommand>("comparative CLI grammar");

    public static int Run(IReadOnlyList<string>? arguments, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        WriteIncompleteEnvelope(output);
        return 1;
    }

    public static void ValidateDatabasePath(string path) =>
        MilestoneIncomplete.Throw("comparative database path validation");

    private static void WriteIncompleteEnvelope(TextWriter output)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteBoolean("ok", false);
            writer.WritePropertyName("error");
            writer.WriteStartObject();
            writer.WriteString("category", "incomplete");
            writer.WritePropertyName("details");
            writer.WriteStartObject();
            writer.WriteString("reason", "starter_milestone");
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        output.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
        output.Write('\n');
        output.Flush();
    }
}
