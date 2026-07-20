using System.Globalization;
using System.Text.Json;
using Tasks.Core;

namespace Tasks.Http;

/// <summary>
/// Framework-neutral HTTP boundary policy shared by both server adapters:
/// route matching, strict request decoding with exact messages, error-to-envelope
/// mapping, and response serialization. Keeping this policy here lets the
/// low-level and Minimal API servers differ in mechanics while remaining
/// byte-for-byte compatible on the wire.
/// </summary>
public static class TaskHttpContract
{
    /// <summary>Maximum accepted request body size in bytes.</summary>
    public const int MaxRequestBodyBytes = 64 * 1024;

    /// <summary>Route key for <c>GET /health</c>.</summary>
    public const string HealthRoute = "health";

    /// <summary>Route key for the <c>/tasks</c> collection.</summary>
    public const string TasksRoute = "tasks";

    /// <summary>Route key for a single <c>/tasks/{id}</c> resource.</summary>
    public const string TaskRoute = "task";

    private static readonly Dictionary<string, string[]> MethodsByRoute =
        new(StringComparer.Ordinal)
        {
            [HealthRoute] = ["GET"],
            [TasksRoute] = ["GET", "POST"],
            [TaskRoute] = ["GET", "PATCH", "DELETE"],
        };

    private static readonly HashSet<string> CreateProperties = new(StringComparer.Ordinal) { "title" };
    private static readonly HashSet<string> UpdateProperties = new(StringComparer.Ordinal) { "title", "completed" };

    /// <summary>Allowed query keys for <c>GET /tasks</c>.</summary>
    public static readonly IReadOnlySet<string> CompletedQueryKeys =
        new HashSet<string>(StringComparer.Ordinal) { "completed" };

    /// <summary>The empty allowed-query set for endpoints that accept none.</summary>
    public static readonly IReadOnlySet<string> NoQueryKeys =
        new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Match a request path to a known route, capturing any ID text.</summary>
    public static RouteMatch? Match(string path)
    {
        if (string.Equals(path, "/health", StringComparison.Ordinal))
        {
            return new RouteMatch(HealthRoute, null);
        }

        if (string.Equals(path, "/tasks", StringComparison.Ordinal))
        {
            return new RouteMatch(TasksRoute, null);
        }

        if (path.StartsWith("/tasks/", StringComparison.Ordinal))
        {
            string idText = path["/tasks/".Length..];
            if (idText.Length > 0 && !idText.Contains('/', StringComparison.Ordinal))
            {
                return new RouteMatch(TaskRoute, idText);
            }
        }

        return null;
    }

    /// <summary>The methods a known route supports, for dispatch and Allow headers.</summary>
    public static IReadOnlyList<string> AllowedMethods(string route)
        => MethodsByRoute.TryGetValue(route, out string[]? methods)
            ? methods
            : throw new ArgumentException($"unknown route: {route}", nameof(route));

    /// <summary>Require a JSON content type with an optional UTF-8 charset.</summary>
    public static void ValidateJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw ApiErrorException.InvalidJson("request Content-Type must be application/json");
        }

        string[] parts = contentType.Split(';');
        if (!string.Equals(parts[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
        {
            throw ApiErrorException.InvalidJson("request Content-Type must be application/json");
        }

        for (int index = 1; index < parts.Length; index++)
        {
            string parameter = parts[index];
            int equals = parameter.IndexOf('=', StringComparison.Ordinal);
            if (equals < 0)
            {
                continue;
            }

            string key = parameter[..equals].Trim();
            string value = parameter[(equals + 1)..].Trim().Trim('"');
            if (string.Equals(key, "charset", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "utf-8", StringComparison.OrdinalIgnoreCase))
            {
                throw ApiErrorException.InvalidJson("request JSON charset must be UTF-8");
            }
        }
    }

    /// <summary>Validate a positive base-10 identifier from path text.</summary>
    public static long ParseTaskId(string idText)
    {
        if (!string.IsNullOrEmpty(idText)
            && IsAsciiDigits(idText)
            && long.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out long id)
            && id > 0)
        {
            return id;
        }

        throw new TaskValidationException("task ID must be a positive integer", "id");
    }

    /// <summary>Interpret the optional <c>completed</c> query filter.</summary>
    public static bool? ParseCompletedFilter(IReadOnlyList<string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count == 1)
        {
            switch (values[0])
            {
                case "true":
                    return true;
                case "false":
                    return false;
            }
        }

        throw new TaskValidationException("completed filter must be true or false", "completed");
    }

    /// <summary>Reject any query key outside the allowed set for a route.</summary>
    public static void RejectUnexpectedQuery(IEnumerable<string> keys, IReadOnlySet<string> allowed)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(allowed);
        string? unknown = FirstUnknown(keys, allowed);
        if (unknown is not null)
        {
            throw new TaskValidationException($"unknown query parameter: {unknown}", unknown);
        }
    }

    /// <summary>Decode a create request body into its raw title value.</summary>
    public static string DecodeCreateTitle(byte[] body)
    {
        using JsonObjectView view = ParseObject(body);
        RejectUnknownProperties(view.Members.Keys, CreateProperties);
        if (!view.Members.TryGetValue("title", out JsonElement titleElement))
        {
            throw new TaskValidationException("missing property: title", "title");
        }

        return RequireString(titleElement, "title");
    }

    /// <summary>Decode a partial update body, preserving omitted fields.</summary>
    public static UpdateTaskInput DecodeUpdate(byte[] body)
    {
        using JsonObjectView view = ParseObject(body);
        RejectUnknownProperties(view.Members.Keys, UpdateProperties);

        Maybe<string> title = default;
        Maybe<bool> completed = default;
        if (view.Members.TryGetValue("title", out JsonElement titleElement))
        {
            title = RequireString(titleElement, "title");
        }

        if (view.Members.TryGetValue("completed", out JsonElement completedElement))
        {
            completed = RequireBool(completedElement);
        }

        return new UpdateTaskInput(title, completed);
    }

    /// <summary>Convert a domain task to its serialized response shape.</summary>
    public static TaskResponse ToResponse(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return new TaskResponse(task.Id, task.Title, task.Completed);
    }

    /// <summary>Serialize one task to UTF-8 JSON bytes.</summary>
    public static byte[] SerializeTask(TaskItem task)
        => JsonSerializer.SerializeToUtf8Bytes(ToResponse(task), TaskJson.Options);

    /// <summary>Serialize an ordered task list to UTF-8 JSON bytes.</summary>
    public static byte[] SerializeTasks(IReadOnlyList<TaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        TaskResponse[] payload = new TaskResponse[tasks.Count];
        for (int index = 0; index < tasks.Count; index++)
        {
            payload[index] = ToResponse(tasks[index]);
        }

        return JsonSerializer.SerializeToUtf8Bytes(payload, TaskJson.Options);
    }

    /// <summary>Serialize the readiness payload to UTF-8 JSON bytes.</summary>
    public static byte[] SerializeHealth()
        => JsonSerializer.SerializeToUtf8Bytes(new HealthResponse("ok"), TaskJson.Options);

    /// <summary>Serialize an error envelope to UTF-8 JSON bytes.</summary>
    public static byte[] SerializeError(ErrorResponse error)
        => JsonSerializer.SerializeToUtf8Bytes(error, TaskJson.Options);

    /// <summary>Map any thrown failure to a status, sanitized envelope, and Allow header.</summary>
    public static MappedError Describe(Exception exception)
    {
        switch (exception)
        {
            case ApiErrorException api:
                return new MappedError(api.StatusCode, Envelope(api.Code, api.Message, api.Details), api.Allow);
            case TaskNotFoundException notFound:
                return new MappedError(404, Envelope(notFound.Code, notFound.Message, notFound.Details), null);
            case TaskValidationException validation:
                return new MappedError(422, Envelope(validation.Code, validation.Message, validation.Details), null);
            case TaskException domain when string.Equals(domain.Code, ErrorCodes.NotFound, StringComparison.Ordinal):
                return new MappedError(404, Envelope(domain.Code, domain.Message, domain.Details), null);
            case TaskException domain when string.Equals(domain.Code, ErrorCodes.ValidationError, StringComparison.Ordinal):
                return new MappedError(422, Envelope(domain.Code, domain.Message, domain.Details), null);
            default:
                return new MappedError(
                    500,
                    Envelope(ErrorCodes.InternalError, "the server could not complete the request", null),
                    null);
        }
    }

    private static ErrorResponse Envelope(string code, string message, IReadOnlyDictionary<string, object>? details)
        => new(new ErrorBody(code, message, details));

    private static JsonObjectView ParseObject(byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw ApiErrorException.InvalidJson("request body must be valid JSON");
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new TaskValidationException("request body must be a JSON object", "body");
        }

        var members = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (!members.TryAdd(property.Name, property.Value))
            {
                document.Dispose();
                throw ApiErrorException.InvalidJson("request body must be valid JSON");
            }
        }

        return new JsonObjectView(document, members);
    }

    private static string RequireString(JsonElement element, string field)
        => element.ValueKind == JsonValueKind.String
            ? element.GetString()!
            : throw new TaskValidationException($"{field} must be a string", field);

    private static bool RequireBool(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new TaskValidationException("completed must be a Boolean", "completed"),
    };

    private static void RejectUnknownProperties(IEnumerable<string> names, IReadOnlySet<string> allowed)
    {
        string? unknown = FirstUnknown(names, allowed);
        if (unknown is not null)
        {
            throw new TaskValidationException($"unknown property: {unknown}", unknown);
        }
    }

    private static string? FirstUnknown(IEnumerable<string> names, IReadOnlySet<string> allowed)
        => names.Where(name => !allowed.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault();

    private static bool IsAsciiDigits(string value)
    {
        foreach (char character in value)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private sealed class JsonObjectView : IDisposable
    {
        private readonly JsonDocument _document;

        public JsonObjectView(JsonDocument document, IReadOnlyDictionary<string, JsonElement> members)
        {
            _document = document;
            Members = members;
        }

        public IReadOnlyDictionary<string, JsonElement> Members { get; }

        public void Dispose() => _document.Dispose();
    }
}
