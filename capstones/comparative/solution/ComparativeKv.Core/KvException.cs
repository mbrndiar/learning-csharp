using System.Collections.ObjectModel;

namespace ComparativeKv.Core;

public enum KvExitCode
{
    Success = 0,
    Validation = 2,
    Conflict = 3,
    NotFound = 4,
    Storage = 5,
}

public sealed class KvException : Exception
{
    public KvException(string category, IReadOnlyDictionary<string, object?> details, KvExitCode exitCode)
        : base(category)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);
        ArgumentNullException.ThrowIfNull(details);

        Category = category;
        Details = new ReadOnlyDictionary<string, object?>(
            details.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal));
        ExitCode = exitCode;
    }

    public string Category { get; }

    public IReadOnlyDictionary<string, object?> Details { get; }

    public KvExitCode ExitCode { get; }

    public static KvException Usage() =>
        new("usage", new Dictionary<string, object?> { ["reason"] = "invalid_cli" }, KvExitCode.Validation);

    public static KvException InvalidArgument(string field, string reason) =>
        new(
            "invalid_argument",
            new Dictionary<string, object?> { ["field"] = field, ["reason"] = reason },
            KvExitCode.Validation);

    public static KvException InvalidJsonSyntax() =>
        new("invalid_json", new Dictionary<string, object?> { ["reason"] = "syntax" }, KvExitCode.Validation);

    public static KvException InvalidValue(string reason) =>
        new("invalid_value", new Dictionary<string, object?> { ["reason"] = reason }, KvExitCode.Validation);

    public static KvException Conflict(string key, object expected, long? actual) =>
        new(
            "conflict",
            new Dictionary<string, object?>
            {
                ["key"] = key,
                ["expected"] = expected,
                ["actual"] = actual,
            },
            KvExitCode.Conflict);

    public static KvException NotFound(string key) =>
        new("not_found", new Dictionary<string, object?> { ["key"] = key }, KvExitCode.NotFound);

    public static KvException Busy() =>
        new(
            "busy",
            new Dictionary<string, object?> { ["timeout_ms"] = KvLimits.BusyTimeoutMilliseconds },
            KvExitCode.Storage);

    public static KvException UnsupportedSchema(long found) =>
        new(
            "unsupported_schema",
            new Dictionary<string, object?>
            {
                ["found"] = found,
                ["supported"] = KvLimits.SchemaVersion,
            },
            KvExitCode.Storage);

    public static KvException InvalidStorage(string reason) =>
        new(
            "invalid_storage",
            new Dictionary<string, object?> { ["reason"] = reason },
            KvExitCode.Storage);

    public static KvException InvalidStoredKey(string key) =>
        new(
            "invalid_storage",
            new Dictionary<string, object?> { ["reason"] = "invalid_key", ["key"] = key },
            KvExitCode.Storage);

    public static KvException InvalidStoredValue(string key) =>
        new(
            "invalid_storage",
            new Dictionary<string, object?> { ["reason"] = "invalid_value", ["key"] = key },
            KvExitCode.Storage);

    public static KvException RevisionExhausted() =>
        new(
            "revision_exhausted",
            new Dictionary<string, object?> { ["maximum"] = KvLimits.MaximumSafeInteger },
            KvExitCode.Storage);

    public static KvException StorageFailure(string operation) =>
        new(
            "storage_error",
            new Dictionary<string, object?> { ["operation"] = operation, ["reason"] = "storage_failure" },
            KvExitCode.Storage);
}
