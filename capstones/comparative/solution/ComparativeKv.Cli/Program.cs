using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Application;
using ComparativeKv.Core;
using ComparativeKv.Storage.Sqlite;

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
    public static ParsedCommand ParseExact(IReadOnlyList<string>? arguments)
    {
        if (arguments is null || arguments.Count < 3 || !string.Equals(arguments[0], "--db", StringComparison.Ordinal))
        {
            throw KvException.Usage();
        }

        var path = arguments[1];
        return arguments[2] switch
        {
            "list" when arguments.Count == 3 => new ParsedCommand(path, CommandKind.List, null, null, null),
            "get" when arguments.Count == 4 => new ParsedCommand(path, CommandKind.Get, arguments[3], null, null),
            "delete" when arguments.Count == 4 => new ParsedCommand(path, CommandKind.Delete, arguments[3], null, "any"),
            "delete" when arguments.Count == 6 && string.Equals(arguments[4], "--expect", StringComparison.Ordinal) =>
                new ParsedCommand(path, CommandKind.Delete, arguments[3], null, arguments[5]),
            "set" when arguments.Count == 6 && string.Equals(arguments[4], "--value-json", StringComparison.Ordinal) =>
                new ParsedCommand(path, CommandKind.Set, arguments[3], arguments[5], "any"),
            "set" when arguments.Count == 8 &&
                string.Equals(arguments[4], "--value-json", StringComparison.Ordinal) &&
                string.Equals(arguments[6], "--expect", StringComparison.Ordinal) =>
                new ParsedCommand(path, CommandKind.Set, arguments[3], arguments[5], arguments[7]),
            _ => throw KvException.Usage(),
        };
    }

    public static int Run(IReadOnlyList<string>? arguments, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        try
        {
            var command = ParseExact(arguments);
            ValidateDatabasePath(command.DatabasePath);
            return Execute(command, output);
        }
        catch (KvException exception)
        {
            WriteError(output, exception);
            return (int)exception.ExitCode;
        }
        catch (Exception)
        {
            var exception = KvException.StorageFailure("open");
            WriteError(output, exception);
            return (int)exception.ExitCode;
        }
    }

    public static void ValidateDatabasePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Length == 0)
        {
            throw KvException.InvalidArgument("db", "empty");
        }

        if (string.Equals(path, ":memory:", StringComparison.Ordinal) ||
            path.StartsWith("file:", StringComparison.Ordinal))
        {
            throw KvException.InvalidArgument("db", "unsupported_form");
        }
    }

    private static int Execute(ParsedCommand command, TextWriter output)
    {
        object result;
        switch (command.Kind)
        {
            case CommandKind.Set:
                {
                    var key = KeyValueValidation.ValidateKey(command.Key!);
                    var expectation = KeyValueValidation.ParseSetExpectation(command.Expectation!);
                    var value = RestrictedJson.Parse(command.ValueJson!);
                    using IConfigurationStore store = SqliteConfigurationStore.Open(command.DatabasePath);
                    var application = new ConfigurationApplication(store);
                    result = application.SetValue(key, value, expectation);
                    break;
                }
            case CommandKind.Get:
                {
                    var key = KeyValueValidation.ValidateKey(command.Key!);
                    using IConfigurationStore store = SqliteConfigurationStore.Open(command.DatabasePath);
                    var application = new ConfigurationApplication(store);
                    result = application.GetValue(key);
                    break;
                }
            case CommandKind.Delete:
                {
                    var key = KeyValueValidation.ValidateKey(command.Key!);
                    var expectation = KeyValueValidation.ParseDeleteExpectation(command.Expectation!);
                    using IConfigurationStore store = SqliteConfigurationStore.Open(command.DatabasePath);
                    var application = new ConfigurationApplication(store);
                    result = application.DeleteValue(key, expectation);
                    break;
                }
            case CommandKind.List:
                {
                    using IConfigurationStore store = SqliteConfigurationStore.Open(command.DatabasePath);
                    var application = new ConfigurationApplication(store);
                    result = application.ListEntries();
                    break;
                }
            default:
                throw KvException.Usage();
        }

        WriteSuccess(output, result);
        return (int)KvExitCode.Success;
    }

    private static void WriteSuccess(TextWriter output, object result)
    {
        WriteEnvelope(
            output,
            writer =>
            {
                writer.WriteStartObject();
                writer.WriteBoolean("ok", true);
                writer.WritePropertyName("result");
                WriteResult(writer, result);
                writer.WriteEndObject();
            });
    }

    private static void WriteError(TextWriter output, KvException exception)
    {
        WriteEnvelope(
            output,
            writer =>
            {
                writer.WriteStartObject();
                writer.WriteBoolean("ok", false);
                writer.WritePropertyName("error");
                writer.WriteStartObject();
                writer.WriteString("category", exception.Category);
                writer.WritePropertyName("details");
                writer.WriteStartObject();
                foreach (var detail in exception.Details)
                {
                    writer.WritePropertyName(detail.Key);
                    WriteDetail(writer, detail.Value);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            });
    }

    private static void WriteEnvelope(TextWriter output, Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            write(writer);
        }

        output.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
        output.Write('\n');
        output.Flush();
    }

    private static void WriteResult(Utf8JsonWriter writer, object result)
    {
        switch (result)
        {
            case SetResult set:
                writer.WriteStartObject();
                WriteEntry(writer, set.Entry);
                writer.WriteBoolean("created", set.Created);
                writer.WriteEndObject();
                break;
            case Entry entry:
                writer.WriteStartObject();
                WriteEntry(writer, entry);
                writer.WriteEndObject();
                break;
            case DeleteResult delete:
                writer.WriteStartObject();
                writer.WriteString("key", delete.Key);
                writer.WriteNumber("deleted_revision", delete.DeletedRevision);
                writer.WriteNumber("revision", delete.Revision);
                writer.WriteEndObject();
                break;
            case ListResult list:
                writer.WriteStartObject();
                writer.WritePropertyName("entries");
                writer.WriteStartArray();
                foreach (var entry in list.Entries)
                {
                    writer.WriteStartObject();
                    WriteEntry(writer, entry);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteNumber("global_revision", list.GlobalRevision);
                writer.WriteEndObject();
                break;
            default:
                throw new InvalidOperationException("The application returned an unsupported result type.");
        }
    }

    private static void WriteEntry(Utf8JsonWriter writer, Entry entry)
    {
        writer.WriteString("key", entry.Key);
        writer.WritePropertyName("value");
        entry.Value.WriteTo(writer);
        writer.WriteNumber("revision", entry.Revision);
    }

    private static void WriteDetail(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case long number:
                writer.WriteNumberValue(number);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;
            default:
                throw new InvalidOperationException("The error included an unsupported detail value.");
        }
    }
}
