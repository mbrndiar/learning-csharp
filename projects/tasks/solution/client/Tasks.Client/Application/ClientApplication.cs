using System.Globalization;
using System.Text.Json;
using Tasks.Core;
using Tasks.Http;

namespace Tasks.Client;

/// <summary>
/// Shared parsing, execution, and rendering policy for every Task client. Each
/// transport obeys the same CLI validation, API contract, output formatting,
/// and exit-code rules.
/// </summary>
public static class ClientApplication
{
    /// <summary>Successful invocation.</summary>
    public const int ExitSuccess = 0;

    /// <summary>A command usage error such as a missing argument or bad ID.</summary>
    public const int ExitUsage = 2;

    /// <summary>The server returned a documented API error.</summary>
    public const int ExitApi = 3;

    /// <summary>The response had an unexpected status, content type, or shape.</summary>
    public const int ExitMalformedResponse = 4;

    /// <summary>A connection, DNS, TLS, or timeout failure.</summary>
    public const int ExitTransport = 5;

    /// <summary>The default loopback base URL.</summary>
    public const string DefaultBaseUrl = "http://127.0.0.1:8000";

    private static readonly Dictionary<int, string> ErrorCodeByStatus = new()
    {
        [400] = ErrorCodes.InvalidJson,
        [404] = ErrorCodes.NotFound,
        [405] = ErrorCodes.MethodNotAllowed,
        [422] = ErrorCodes.ValidationError,
        [500] = ErrorCodes.InternalError,
    };

    /// <summary>Run one invocation, reserving stdout for success and stderr for failures.</summary>
    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TransportFactory transportFactory,
        TextWriter stdout,
        TextWriter stderr,
        string prog = "tasks-cli",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(transportFactory);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        ClientRequest request;
        try
        {
            request = ParseRequest(args, prog);
        }
        catch (ClientUsageException error)
        {
            await stderr.WriteAsync($"usage: {error.Message}\n").ConfigureAwait(false);
            return ExitUsage;
        }

        ClientResult result = await ExecuteAsync(request, transportFactory, cancellationToken).ConfigureAwait(false);
        if (result.Stdout is not null)
        {
            await stdout.WriteAsync($"{result.Stdout}\n").ConfigureAwait(false);
        }

        if (result.Stderr is not null)
        {
            await stderr.WriteAsync($"{result.Stderr}\n").ConfigureAwait(false);
        }

        return result.ExitCode;
    }

    /// <summary>Parse and validate CLI text into a transport-independent request.</summary>
    public static ClientRequest ParseRequest(IReadOnlyList<string> args, string prog = "tasks-cli")
    {
        ArgumentNullException.ThrowIfNull(args);
        _ = prog;
        var reader = new ArgumentReader(args);

        string baseUrl = DefaultBaseUrl;
        TimeSpan timeout = TimeSpan.FromSeconds(5);
        while (reader.TryPeekOption(out string option))
        {
            switch (option)
            {
                case "--base-url":
                    baseUrl = NormalizeBaseUrl(reader.ReadOptionValue("--base-url"));
                    break;
                case "--timeout":
                    timeout = ParseTimeout(reader.ReadOptionValue("--timeout"));
                    break;
                default:
                    throw new ClientUsageException($"unrecognized option: {option}");
            }
        }

        if (!reader.TryReadPositional(out string commandName))
        {
            throw new ClientUsageException("a command is required (add, list, show, update, complete, remove)");
        }

        ClientCommand command = commandName switch
        {
            "add" => ParseAdd(reader),
            "list" => ParseList(reader),
            "show" => new ShowCommand(ReadRequiredId(reader, "show")),
            "update" => ParseUpdate(reader),
            "complete" => new CompleteCommand(ReadRequiredId(reader, "complete")),
            "remove" => new RemoveCommand(ReadRequiredId(reader, "remove")),
            _ => throw new ClientUsageException($"unknown command: {commandName}"),
        };

        reader.EnsureNoRemaining();
        return new ClientRequest(new ClientSettings(baseUrl, timeout), command);
    }

    /// <summary>Map one command to its Task API method, path, query, and body.</summary>
    public static TransportRequest RequestFor(ClientCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command switch
        {
            AddCommand add => new TransportRequest(
                "POST",
                "/tasks",
                jsonBody: new Dictionary<string, object?> { ["title"] = add.Title }),
            ListCommand list => new TransportRequest(
                "GET",
                "/tasks",
                query: list.Completed is null
                    ? null
                    : new Dictionary<string, string> { ["completed"] = list.Completed.Value ? "true" : "false" }),
            ShowCommand show => new TransportRequest("GET", $"/tasks/{show.TaskId}"),
            UpdateCommand update => new TransportRequest("PATCH", $"/tasks/{update.TaskId}", jsonBody: UpdateBody(update)),
            CompleteCommand complete => new TransportRequest(
                "PATCH",
                $"/tasks/{complete.TaskId}",
                jsonBody: new Dictionary<string, object?> { ["completed"] = true }),
            RemoveCommand remove => new TransportRequest("DELETE", $"/tasks/{remove.TaskId}"),
            _ => throw new ArgumentOutOfRangeException(nameof(command)),
        };
    }

    private static Dictionary<string, object?> UpdateBody(UpdateCommand update)
    {
        var body = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (update.Title is not null)
        {
            body["title"] = update.Title;
        }

        if (update.Completed is not null)
        {
            body["completed"] = update.Completed.Value;
        }

        return body;
    }

    private static async Task<ClientResult> ExecuteAsync(
        ClientRequest request,
        TransportFactory transportFactory,
        CancellationToken cancellationToken)
    {
        ITaskTransport transport;
        try
        {
            transport = transportFactory(request.Settings.BaseUrl, request.Settings.Timeout);
        }
        catch (Exception error)
        {
            return TransportResult(error);
        }

        ClientResult? result = null;
        try
        {
            try
            {
                TransportResponse response =
                    await transport.SendAsync(RequestFor(request.Command), cancellationToken).ConfigureAwait(false);
                result = ClientResult.Success(SuccessValue(request.Command, response));
            }
            catch (ClientApiException error)
            {
                result = ClientResult.Failure(ExitApi, $"api: {error.Status} {error.Code}: {error.Message}");
            }
            catch (ClientMalformedResponseException error)
            {
                result = ClientResult.Failure(ExitMalformedResponse, $"malformed-response: {error.Message}");
            }
            catch (TransportException error)
            {
                result = TransportResult(error);
            }
        }
        finally
        {
            try
            {
                await transport.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                // A cleanup failure replaces success but must not hide a prior error.
                if (result is null || result.ExitCode == ExitSuccess)
                {
                    result = TransportResult(error);
                }
            }
        }

        return result!;
    }

    private static ClientResult TransportResult(Exception error) => error switch
    {
        TransportTimeoutException timeout => ClientResult.Failure(
            ExitTransport,
            $"connection: timeout: {Fallback(timeout.Message, "request timed out")}"),
        TransportConnectionException connection => ClientResult.Failure(
            ExitTransport,
            $"connection: {Fallback(connection.Message, "request failed")}"),
        TransportException transport => ClientResult.Failure(
            ExitTransport,
            $"transport: {Fallback(transport.Message, "request failed")}"),
        _ => ClientResult.Failure(ExitTransport, $"transport: {Fallback(error.Message, "request failed")}"),
    };

    private static string Fallback(string message, string fallback)
    {
        string trimmed = message.Trim();
        return trimmed.Length > 0 ? trimmed : fallback;
    }

    private static string SuccessValue(ClientCommand command, TransportResponse response)
    {
        switch (command)
        {
            case AddCommand:
                return Render(DecodeTask(response, 201, [400, 405, 422, 500]));
            case ListCommand:
                return Render(DecodeList(response, [405, 422, 500]));
            case ShowCommand:
                return Render(DecodeTask(response, 200, [404, 405, 422, 500]));
            case UpdateCommand or CompleteCommand:
                return Render(DecodeTask(response, 200, [400, 404, 405, 422, 500]));
            case RemoveCommand remove:
                ExpectStatus(response, 204, [404, 405, 422, 500]);
                if (response.Body.Length > 0)
                {
                    throw new ClientMalformedResponseException("204 response body was not empty");
                }

                return Render(new DeletedResponse(remove.TaskId));
            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }

    private static string Render(object value) => JsonSerializer.Serialize(value, TaskJson.Options);

    private static TaskResponse DecodeTask(TransportResponse response, int expected, int[] allowedErrors)
    {
        ExpectStatus(response, expected, allowedErrors);
        return DecodeTask(DecodeJson(response));
    }

    private static TaskResponse[] DecodeList(TransportResponse response, int[] allowedErrors)
    {
        ExpectStatus(response, 200, allowedErrors);
        JsonElement value = DecodeJson(response);
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ClientMalformedResponseException("Task list response was not an array");
        }

        var tasks = new List<TaskResponse>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            tasks.Add(DecodeTask(item));
        }

        for (int index = 1; index < tasks.Count; index++)
        {
            if (tasks[index - 1].Id >= tasks[index].Id)
            {
                throw new ClientMalformedResponseException("Task list was not ordered by ascending ID");
            }
        }

        return [.. tasks];
    }

    private static void ExpectStatus(TransportResponse response, int expected, int[] allowedErrors)
    {
        if (response.Status is < 100 or > 599)
        {
            throw new ClientMalformedResponseException("response status was invalid");
        }

        if (response.Status == expected)
        {
            return;
        }

        if (Array.IndexOf(allowedErrors, response.Status) >= 0)
        {
            throw DecodeError(response);
        }

        throw new ClientMalformedResponseException($"unexpected HTTP status: {response.Status}");
    }

    private static ClientApiException DecodeError(TransportResponse response)
    {
        JsonElement value = DecodeJson(response);
        var envelope = ReadObject(value, "API error envelope fields were malformed");
        if (envelope.Count != 1 || !envelope.TryGetValue("error", out JsonElement errorValue))
        {
            throw new ClientMalformedResponseException("API error envelope fields were malformed");
        }

        var body = ReadObject(errorValue, "API error value was not an object");
        foreach (string key in body.Keys)
        {
            if (key is not ("code" or "message" or "details"))
            {
                throw new ClientMalformedResponseException("API error fields were malformed");
            }
        }

        if (!body.TryGetValue("code", out JsonElement codeElement)
            || !body.TryGetValue("message", out JsonElement messageElement)
            || codeElement.ValueKind != JsonValueKind.String
            || messageElement.ValueKind != JsonValueKind.String)
        {
            throw new ClientMalformedResponseException("API error values were malformed");
        }

        if (body.TryGetValue("details", out JsonElement details) && details.ValueKind != JsonValueKind.Object)
        {
            throw new ClientMalformedResponseException("API error values were malformed");
        }

        string code = codeElement.GetString()!;
        string message = messageElement.GetString()!;
        if (message.Length == 0)
        {
            throw new ClientMalformedResponseException("API error values were malformed");
        }

        if (!ErrorCodeByStatus.TryGetValue(response.Status, out string? expectedCode))
        {
            throw new ClientMalformedResponseException($"unexpected HTTP status: {response.Status}");
        }

        if (!string.Equals(code, expectedCode, StringComparison.Ordinal))
        {
            throw new ClientMalformedResponseException(
                $"API error code {code} did not match HTTP status {response.Status}");
        }

        return new ClientApiException(response.Status, code, message);
    }

    private static TaskResponse DecodeTask(JsonElement value)
    {
        var members = ReadObject(value, "Task response fields were malformed");
        if (members.Count != 3
            || !members.TryGetValue("id", out JsonElement idElement)
            || !members.TryGetValue("title", out JsonElement titleElement)
            || !members.TryGetValue("completed", out JsonElement completedElement))
        {
            throw new ClientMalformedResponseException("Task response fields were malformed");
        }

        if (idElement.ValueKind != JsonValueKind.Number
            || !idElement.TryGetInt64(out long id)
            || id <= 0
            || titleElement.ValueKind != JsonValueKind.String
            || completedElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new ClientMalformedResponseException("Task response values were malformed");
        }

        string title = titleElement.GetString()!;
        if (!IsValidTitle(title))
        {
            throw new ClientMalformedResponseException("Task response values were malformed");
        }

        return new TaskResponse(id, title, completedElement.ValueKind == JsonValueKind.True);
    }

    private static JsonElement DecodeJson(TransportResponse response)
    {
        RequireJsonContentType(response.Headers);
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            RejectDuplicates(document.RootElement);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new ClientMalformedResponseException("response body was not strict UTF-8 JSON");
        }
    }

    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                {
                    throw new ClientMalformedResponseException("response body was not strict UTF-8 JSON");
                }

                RejectDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in element.EnumerateArray())
            {
                RejectDuplicates(item);
            }
        }
    }

    private static Dictionary<string, JsonElement> ReadObject(JsonElement value, string message)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new ClientMalformedResponseException(message);
        }

        var members = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            members[property.Name] = property.Value;
        }

        return members;
    }

    private static void RequireJsonContentType(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Content-Type", out string? contentType))
        {
            throw new ClientMalformedResponseException("response Content-Type was not application/json");
        }

        string[] parts = contentType.Split(';');
        if (!string.Equals(parts[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase))
        {
            throw new ClientMalformedResponseException("response Content-Type was not application/json");
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
            string charset = parameter[(equals + 1)..].Trim().Trim('"');
            if (string.Equals(key, "charset", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase))
            {
                throw new ClientMalformedResponseException("response JSON charset was not UTF-8");
            }
        }
    }

    private static bool IsValidTitle(string title)
    {
        if (!string.Equals(title, title.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        int codePoints = 0;
        for (int index = 0; index < title.Length; codePoints++)
        {
            char character = title[index];
            if (character is '\n' or '\r' or '\v' or '\f'
                or '\u001c' or '\u001d' or '\u001e' or '\u0085' or '\u2028' or '\u2029')
            {
                return false;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Control)
            {
                return false;
            }

            index += char.IsSurrogatePair(title, index) ? 2 : 1;
        }

        return codePoints is >= TaskValidation.MinTitleLength and <= TaskValidation.MaxTitleLength;
    }

    private static AddCommand ParseAdd(ArgumentReader reader)
    {
        if (!reader.TryReadPositional(out string title))
        {
            throw new ClientUsageException("add requires TITLE");
        }

        return new AddCommand(title);
    }

    private static ListCommand ParseList(ArgumentReader reader)
    {
        bool? completed = null;
        while (reader.TryPeekOption(out string option))
        {
            if (!string.Equals(option, "--completed", StringComparison.Ordinal))
            {
                throw new ClientUsageException($"unrecognized option: {option}");
            }

            completed = ParseBoolChoice(reader.ReadOptionValue("--completed"));
        }

        return new ListCommand(completed);
    }

    private static UpdateCommand ParseUpdate(ArgumentReader reader)
    {
        long id = ReadRequiredId(reader, "update");
        string? title = null;
        bool? completed = null;
        while (reader.TryPeekOption(out string option))
        {
            switch (option)
            {
                case "--title":
                    title = reader.ReadOptionValue("--title");
                    break;
                case "--completed":
                    completed = ParseBoolChoice(reader.ReadOptionValue("--completed"));
                    break;
                default:
                    throw new ClientUsageException($"unrecognized option: {option}");
            }
        }

        if (title is null && completed is null)
        {
            throw new ClientUsageException("update requires --title, --completed, or both");
        }

        return new UpdateCommand(id, title, completed);
    }

    private static long ReadRequiredId(ArgumentReader reader, string command)
    {
        if (!reader.TryReadPositional(out string idText))
        {
            throw new ClientUsageException($"{command} requires ID");
        }

        if (!long.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out long id) || id <= 0)
        {
            throw new ClientUsageException("ID must be a positive integer");
        }

        return id;
    }

    private static bool ParseBoolChoice(string value) => value switch
    {
        "true" => true,
        "false" => false,
        _ => throw new ClientUsageException("--completed must be true or false"),
    };

    private static string NormalizeBaseUrl(string value)
    {
        try
        {
            return TransportUrls.NormalizeBaseUrl(value);
        }
        catch (ArgumentException error)
        {
            throw new ClientUsageException(error.Message);
        }
    }

    private static TimeSpan ParseTimeout(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
            || double.IsNaN(seconds)
            || double.IsInfinity(seconds)
            || seconds <= 0)
        {
            throw new ClientUsageException("timeout must be positive and finite");
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private sealed record ClientResult(int ExitCode, string? Stdout, string? Stderr)
    {
        public static ClientResult Success(string stdout) => new(ExitSuccess, stdout, null);

        public static ClientResult Failure(int exitCode, string stderr) => new(exitCode, null, stderr);
    }

    private sealed class ArgumentReader
    {
        private readonly IReadOnlyList<string> _args;
        private int _index;

        public ArgumentReader(IReadOnlyList<string> args) => _args = args;

        public bool TryPeekOption(out string option)
        {
            if (_index < _args.Count && _args[_index].StartsWith("--", StringComparison.Ordinal))
            {
                string token = _args[_index];
                int separator = token.IndexOf('=', StringComparison.Ordinal);
                option = separator >= 0 ? token[..separator] : token;
                return true;
            }

            option = string.Empty;
            return false;
        }

        public string ReadOptionValue(string option)
        {
            string token = _args[_index];
            _index++;
            int separator = token.IndexOf('=', StringComparison.Ordinal);
            if (separator >= 0)
            {
                return token[(separator + 1)..];
            }

            if (_index >= _args.Count)
            {
                throw new ClientUsageException($"{option} requires a value");
            }

            return _args[_index++];
        }

        public bool TryReadPositional(out string value)
        {
            if (_index < _args.Count && !_args[_index].StartsWith("--", StringComparison.Ordinal))
            {
                value = _args[_index++];
                return true;
            }

            value = string.Empty;
            return false;
        }

        public void EnsureNoRemaining()
        {
            if (_index < _args.Count)
            {
                throw new ClientUsageException($"unexpected argument: {_args[_index]}");
            }
        }
    }
}
