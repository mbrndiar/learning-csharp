using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tasks.Http;

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
