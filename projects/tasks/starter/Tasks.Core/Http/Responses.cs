using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tasks.Core.Http;

/// <summary>Serialized readiness response.</summary>
public sealed record HealthResponse(string Status);

/// <summary>Serialized task value returned to clients.</summary>
public sealed record TaskResponse(long Id, string Title, bool Completed);

/// <summary>Client-side rendering of a successful delete.</summary>
public sealed record DeletedResponse(long Deleted);

/// <summary>Stable error information nested in the shared envelope.</summary>
public sealed record ErrorBody(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, object>? Details);

/// <summary>The shared JSON error envelope.</summary>
public sealed record ErrorResponse(ErrorBody Error);

/// <summary>Shared serializer settings so every adapter emits identical JSON.</summary>
public static class TaskJson
{
    /// <summary>
    /// Camel-cased, compact options used for both request reading defaults and
    /// response writing. Null <c>details</c> are omitted from error envelopes.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
