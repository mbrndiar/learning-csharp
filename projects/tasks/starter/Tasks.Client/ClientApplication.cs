using System.Globalization;
using Tasks.Core;

namespace Tasks.Client;

/// <summary>Connection settings shared by every client transport.</summary>
public sealed record ClientSettings(string BaseUrl, TimeSpan Timeout);

/// <summary>Base type for the closed set of client commands.</summary>
public abstract record ClientCommand;

/// <summary>Create one task from a title.</summary>
public sealed record AddCommand(string Title) : ClientCommand;

/// <summary>List tasks with an optional completion filter.</summary>
public sealed record ListCommand(bool? Completed) : ClientCommand;

/// <summary>Fetch one task by identifier.</summary>
public sealed record ShowCommand(long TaskId) : ClientCommand;

/// <summary>Update one or both mutable task fields.</summary>
public sealed record UpdateCommand(long TaskId, string? Title, bool? Completed) : ClientCommand;

/// <summary>Mark one task complete.</summary>
public sealed record CompleteCommand(long TaskId) : ClientCommand;

/// <summary>Delete one task.</summary>
public sealed record RemoveCommand(long TaskId) : ClientCommand;

/// <summary>Validated settings and command produced before any network I/O.</summary>
public sealed record ClientRequest(ClientSettings Settings, ClientCommand Command);

/// <summary>A command usage error, reported on stderr with exit code 2.</summary>
public sealed class ClientUsageException : Exception
{
    /// <summary>Create a usage error.</summary>
    public ClientUsageException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Shared parsing, execution, and rendering policy for every Task client. The
/// starter provides argument parsing and request mapping; execution is
/// milestone three.
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

    /// <summary>Run one invocation, reserving stdout for success and stderr for failures.</summary>
    public static Task<int> RunAsync(
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
        _ = prog;
        _ = cancellationToken;
        // TODO(milestone 3): parse the request, own one transport for the call,
        // validate the response, render success to stdout, and map failures to
        // the documented exit codes without leaking library exceptions.
        throw new IncompleteProjectException("milestone 3 client command execution");
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

/// <summary>The server returned a documented API error.</summary>
public sealed class ClientApiException : Exception
{
    /// <summary>Create an API failure with the observed status and code.</summary>
    public ClientApiException(int status, string code, string message)
        : base(message)
    {
        Status = status;
        Code = code;
    }

    /// <summary>The HTTP status returned.</summary>
    public int Status { get; }

    /// <summary>The stable error code returned.</summary>
    public string Code { get; }
}

/// <summary>The response had an unexpected status, content type, or JSON shape.</summary>
public sealed class ClientMalformedResponseException : Exception
{
    /// <summary>Create a malformed-response failure.</summary>
    public ClientMalformedResponseException(string message)
        : base(message)
    {
    }
}
