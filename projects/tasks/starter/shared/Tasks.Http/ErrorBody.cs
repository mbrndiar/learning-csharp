using System.Text.Json.Serialization;

namespace Tasks.Http;

/// <summary>Stable error information nested in the shared envelope.</summary>
public sealed record ErrorBody(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, object>? Details);
