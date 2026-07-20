using System.Globalization;
using System.Net;

namespace Tasks.Core.Hosting;

/// <summary>Selects which persistence adapter a server process uses.</summary>
public enum StorageBackend
{
    /// <summary>The SQLite database repository.</summary>
    Sqlite,

    /// <summary>The versioned Markdown checklist repository.</summary>
    Markdown,
}

/// <summary>Raised when server launcher arguments are invalid.</summary>
public sealed class ServerConfigurationException : Exception
{
    /// <summary>Create a configuration failure with a user-facing message.</summary>
    public ServerConfigurationException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Validated launcher settings shared by every server implementation. Parsing
/// never opens a socket or a store, so an invalid option fails before any
/// resource is acquired.
/// </summary>
public sealed record ServerSettings
{
    /// <summary>Create validated server settings.</summary>
    public ServerSettings(string host, int port, StorageBackend backend, string dataPath)
    {
        Host = host;
        Port = port;
        Backend = backend;
        DataPath = dataPath;
    }

    /// <summary>The loopback host or address to bind.</summary>
    public string Host { get; }

    /// <summary>The TCP port to bind (0 selects an ephemeral port).</summary>
    public int Port { get; }

    /// <summary>The selected persistence backend.</summary>
    public StorageBackend Backend { get; }

    /// <summary>The storage location (database file or Markdown document).</summary>
    public string DataPath { get; }

    /// <summary>The loopback URL this server binds.</summary>
    public string BaseUrl => $"http://{Host}:{Port.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Parse and validate command-line launcher arguments.</summary>
    public static ServerSettings Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string host = "127.0.0.1";
        int port = 8000;
        StorageBackend? backend = null;
        string? dataPath = null;

        for (int index = 0; index < args.Count; index++)
        {
            (string name, string value) = ReadOption(args, ref index);
            switch (name)
            {
                case "--host":
                    host = ParseLoopbackHost(value);
                    break;
                case "--port":
                    port = ParsePort(value);
                    break;
                case "--backend":
                    backend = ParseBackend(value);
                    break;
                case "--data":
                    dataPath = value.Length == 0
                        ? throw new ServerConfigurationException("--data requires a path")
                        : value;
                    break;
                default:
                    throw new ServerConfigurationException($"unknown option: {name}");
            }
        }

        if (backend is null)
        {
            throw new ServerConfigurationException("--backend is required (sqlite or markdown)");
        }

        if (dataPath is null)
        {
            throw new ServerConfigurationException("--data is required");
        }

        return new ServerSettings(host, port, backend.Value, dataPath);
    }

    private static (string Name, string Value) ReadOption(IReadOnlyList<string> args, ref int index)
    {
        string token = args[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ServerConfigurationException($"unexpected argument: {token}");
        }

        int separator = token.IndexOf('=', StringComparison.Ordinal);
        if (separator >= 0)
        {
            return (token[..separator], token[(separator + 1)..]);
        }

        if (index + 1 >= args.Count)
        {
            throw new ServerConfigurationException($"{token} requires a value");
        }

        index++;
        return (token, args[index]);
    }

    private static string ParseLoopbackHost(string value)
    {
        if (string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (IPAddress.TryParse(value, out IPAddress? address) && IPAddress.IsLoopback(address))
        {
            return value;
        }

        throw new ServerConfigurationException("--host must identify a loopback interface");
    }

    private static int ParsePort(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port)
            || port is < 0 or > 65535)
        {
            throw new ServerConfigurationException("--port must be between 0 and 65535");
        }

        return port;
    }

    private static StorageBackend ParseBackend(string value) => value switch
    {
        "sqlite" => StorageBackend.Sqlite,
        "markdown" => StorageBackend.Markdown,
        _ => throw new ServerConfigurationException("--backend must be sqlite or markdown"),
    };
}
