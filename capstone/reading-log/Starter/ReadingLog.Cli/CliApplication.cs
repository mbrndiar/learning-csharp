using ReadingLog.Core;

namespace ReadingLog.Cli;

public enum CliExitCode
{
    Success = 0,
    InvalidArguments = 1,
    RequestFailed = 2,
    NotFound = 3,
    Cancelled = 4,
    UnexpectedResponse = 5,
    TimedOut = 6,
}

public sealed class CliApplication
{
    private readonly ReadingLogApiClient _apiClient;
    private readonly TextWriter _standardOutput;
    private readonly TextWriter _standardError;

    public CliApplication(ReadingLogApiClient apiClient, TextWriter standardOutput, TextWriter standardError)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _standardOutput = standardOutput ?? throw new ArgumentNullException(nameof(standardOutput));
        _standardError = standardError ?? throw new ArgumentNullException(nameof(standardError));
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0 || IsHelp(args[0]))
        {
            await WriteHelpAsync();
            return (int)CliExitCode.Success;
        }

        try
        {
            return args[0] switch
            {
                "list-books" => await ListBooksAsync(cancellationToken),
                "show-book" => await ShowBookAsync(args, cancellationToken),
                "add-book" => await WriteTodoAsync("TODO(m5): implement add-book in the starter."),
                "add-entry" => await WriteTodoAsync("TODO(m5): implement add-entry in the starter."),
                _ => await FailAsync("Unknown command. Run 'help' to see supported commands.", CliExitCode.InvalidArguments),
            };
        }
        catch (TimeoutException exception)
        {
            return await FailAsync(exception.Message, CliExitCode.TimedOut);
        }
        catch (OperationCanceledException)
        {
            return await FailAsync("The command was cancelled.", CliExitCode.Cancelled);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return await FailAsync(exception.Message, CliExitCode.NotFound);
        }
        catch (HttpRequestException exception)
        {
            return await FailAsync(exception.Message, CliExitCode.RequestFailed);
        }
        catch (InvalidDataException exception)
        {
            return await FailAsync(exception.Message, CliExitCode.UnexpectedResponse);
        }
        catch (NotSupportedException exception)
        {
            return await FailAsync(exception.Message, CliExitCode.InvalidArguments);
        }
    }

    private async Task<int> ListBooksAsync(CancellationToken cancellationToken)
    {
        var books = await _apiClient.ListBooksAsync(cancellationToken);
        if (books.Count == 0)
        {
            await _standardOutput.WriteLineAsync("No books yet.");
            return (int)CliExitCode.Success;
        }

        foreach (var book in books)
        {
            await _standardOutput.WriteLineAsync($"{book.Id} | {book.Title} by {book.Author}");
        }

        return (int)CliExitCode.Success;
    }

    private async Task<int> ShowBookAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!TryGetOption(args, "--book-id", out var rawBookId) || !Guid.TryParse(rawBookId, out var bookId))
        {
            return await FailAsync("show-book requires --book-id <guid>.", CliExitCode.InvalidArguments);
        }

        var book = await _apiClient.GetBookAsync(bookId, cancellationToken);
        await _standardOutput.WriteLineAsync($"{book.Book.Title} by {book.Book.Author}");
        await _standardOutput.WriteLineAsync($"Pages read: {book.TotalPagesRead}");
        await _standardOutput.WriteLineAsync(book.Entries.Count == 0 ? "No entries yet." : $"Entries: {book.Entries.Count}");
        return (int)CliExitCode.Success;
    }

    private async Task<int> WriteTodoAsync(string message)
    {
        return await FailAsync(message, CliExitCode.InvalidArguments);
    }

    private async Task<int> FailAsync(string message, CliExitCode exitCode)
    {
        await _standardError.WriteLineAsync(message);
        return (int)exitCode;
    }

    private Task WriteHelpAsync()
    {
        const string help = """
        ReadingLog CLI
          help
          list-books
          show-book --book-id <guid>
          add-book --title <text> --author <text> [--year <year>] [--isbn <text>]
          add-entry --book-id <guid> --started-on <yyyy-MM-dd> --pages-read <int> [--finished-on <yyyy-MM-dd>] [--rating <1-5>] [--notes <text>]
        """;

        return _standardOutput.WriteLineAsync(help);
    }

    private static bool IsHelp(string command)
    {
        return string.Equals(command, "help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "--help", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "-h", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetOption(string[] args, string optionName, out string value)
    {
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                value = args[index + 1];
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    // TODO(m6): After milestones 1-5 are complete, run the full shared suite and polish the CLI behavior.
}
