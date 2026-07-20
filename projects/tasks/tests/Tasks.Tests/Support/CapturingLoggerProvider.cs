using Microsoft.Extensions.Logging;

namespace Tasks.Tests.Support;

/// <summary>A captured log entry.</summary>
public sealed record LogEntry(LogLevel Level, string Message, string? Exception);

/// <summary>
/// A logger provider that records entries so tests can assert a server logs
/// storage details internally even while it returns a sanitized response.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = [];

    /// <summary>A snapshot of the captured entries.</summary>
    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void Add(LogEntry entry)
    {
        lock (_entries)
        {
            _entries.Add(entry);
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly CapturingLoggerProvider _provider;

        public CapturingLogger(CapturingLoggerProvider provider) => _provider = provider;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            _provider.Add(new LogEntry(logLevel, formatter(state, exception), exception?.Message));
        }
    }
}
