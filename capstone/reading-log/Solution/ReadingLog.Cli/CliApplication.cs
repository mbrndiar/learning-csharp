using System.Globalization;
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
                "add-book" => await AddBookAsync(args, cancellationToken),
                "add-entry" => await AddEntryAsync(args, cancellationToken),
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
            var year = book.PublicationYear is null ? string.Empty : $" ({book.PublicationYear.Value})";
            await _standardOutput.WriteLineAsync($"{book.Id} | {book.Title} by {book.Author}{year}");
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
        await _standardOutput.WriteLineAsync($"Total pages read: {book.TotalPagesRead}");
        await _standardOutput.WriteLineAsync(book.HasFinished ? "Finished: yes" : "Finished: no");
        if (book.AverageRating is not null)
        {
            await _standardOutput.WriteLineAsync($"Average rating: {book.AverageRating:0.##}");
        }

        if (book.Entries.Count == 0)
        {
            await _standardOutput.WriteLineAsync("No entries yet.");
            return (int)CliExitCode.Success;
        }

        foreach (var entry in book.Entries)
        {
            var summary = $"- {entry.StartedOn:yyyy-MM-dd}: {entry.PagesRead} pages";
            if (entry.FinishedOn is not null)
            {
                summary += $", finished {entry.FinishedOn:yyyy-MM-dd}";
            }

            if (entry.Rating is not null)
            {
                summary += $", rating {entry.Rating}";
            }

            await _standardOutput.WriteLineAsync(summary);
            if (!string.IsNullOrWhiteSpace(entry.Notes))
            {
                await _standardOutput.WriteLineAsync($"  Notes: {entry.Notes}");
            }
        }

        return (int)CliExitCode.Success;
    }

    private async Task<int> AddBookAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!TryGetOption(args, "--title", out var title) || !TryGetOption(args, "--author", out var author))
        {
            return await FailAsync("add-book requires --title <text> and --author <text>.", CliExitCode.InvalidArguments);
        }

        int? publicationYear = null;
        if (TryGetOption(args, "--year", out var rawYear))
        {
            if (!int.TryParse(rawYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYear))
            {
                return await FailAsync("--year must be a whole number.", CliExitCode.InvalidArguments);
            }

            publicationYear = parsedYear;
        }

        _ = TryGetOption(args, "--isbn", out var isbn);
        var created = await _apiClient.AddBookAsync(new CreateBookRequest(title, author, publicationYear, string.IsNullOrWhiteSpace(isbn) ? null : isbn), cancellationToken);
        await _standardOutput.WriteLineAsync($"Added book {created.Title} ({created.Id}).");
        return (int)CliExitCode.Success;
    }

    private async Task<int> AddEntryAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!TryGetOption(args, "--book-id", out var rawBookId) || !Guid.TryParse(rawBookId, out var bookId))
        {
            return await FailAsync("add-entry requires --book-id <guid>.", CliExitCode.InvalidArguments);
        }

        if (!TryGetOption(args, "--started-on", out var rawStartedOn)
            || !TryParseIsoDate(rawStartedOn, out var startedOn))
        {
            return await FailAsync("add-entry requires --started-on <yyyy-MM-dd>.", CliExitCode.InvalidArguments);
        }

        if (!TryGetOption(args, "--pages-read", out var rawPagesRead)
            || !int.TryParse(rawPagesRead, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pagesRead))
        {
            return await FailAsync("add-entry requires --pages-read <int>.", CliExitCode.InvalidArguments);
        }

        DateOnly? finishedOn = null;
        if (TryGetOption(args, "--finished-on", out var rawFinishedOn))
        {
            if (!TryParseIsoDate(rawFinishedOn, out var parsedFinishedOn))
            {
                return await FailAsync("--finished-on must use yyyy-MM-dd.", CliExitCode.InvalidArguments);
            }

            finishedOn = parsedFinishedOn;
        }

        int? rating = null;
        if (TryGetOption(args, "--rating", out var rawRating))
        {
            if (!int.TryParse(rawRating, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRating))
            {
                return await FailAsync("--rating must be a whole number between 1 and 5.", CliExitCode.InvalidArguments);
            }

            rating = parsedRating;
        }

        _ = TryGetOption(args, "--notes", out var notes);
        var created = await _apiClient.AddReadingEntryAsync(
            new CreateReadingEntryRequest(bookId, startedOn, finishedOn, pagesRead, rating, string.IsNullOrWhiteSpace(notes) ? null : notes),
            cancellationToken);
        await _standardOutput.WriteLineAsync($"Added entry {created.Id} for book {created.BookId}.");
        return (int)CliExitCode.Success;
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

    private static bool TryParseIsoDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

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
}
