using System.Text.RegularExpressions;
using ReadingLog.Tests.TestInfrastructure;

namespace ReadingLog.Tests.EndToEnd;

public sealed partial class ReadingLogEndToEndTests
{
    [Fact]
    public async Task CliAndApiSupportRepresentativeFlow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var httpClient = factory.CreateClient();

        using var addBookOutput = new StringWriter();
        using var addBookError = new StringWriter();
        var app = new CliApplication(new ReadingLogApiClient(httpClient), addBookOutput, addBookError);

        var addBookExitCode = await app.RunAsync(["add-book", "--title", "Dune", "--author", "Frank Herbert", "--year", "1965"], CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, addBookExitCode);
        var match = BookIdPattern().Match(addBookOutput.ToString());
        Assert.True(match.Success);
        var bookId = Guid.Parse(match.Groups[1].Value);

        using var addEntryOutput = new StringWriter();
        using var addEntryError = new StringWriter();
        app = new CliApplication(new ReadingLogApiClient(httpClient), addEntryOutput, addEntryError);
        var addEntryExitCode = await app.RunAsync(
            ["add-entry", "--book-id", bookId.ToString(), "--started-on", "2026-07-19", "--pages-read", "45", "--finished-on", "2026-07-19", "--rating", "5", "--notes", "Great pacing"],
            CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, addEntryExitCode);
        Assert.Contains(bookId.ToString(), addEntryOutput.ToString(), StringComparison.Ordinal);

        using var showBookOutput = new StringWriter();
        using var showBookError = new StringWriter();
        app = new CliApplication(new ReadingLogApiClient(httpClient), showBookOutput, showBookError);
        var showBookExitCode = await app.RunAsync(["show-book", "--book-id", bookId.ToString()], CancellationToken.None);

        Assert.Equal((int)CliExitCode.Success, showBookExitCode);
        Assert.Contains("Dune by Frank Herbert", showBookOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("Total pages read: 45", showBookOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("Average rating: 5", showBookOutput.ToString(), StringComparison.Ordinal);

        var overview = await httpClient.GetFromJsonAsync<ReadingOverviewResponse>("/overview", cancellationToken);
        Assert.NotNull(overview);
        Assert.Equal(1, overview.BookCount);
        Assert.Equal(1, overview.EntryCount);
    }

    [GeneratedRegex(@"\(([0-9a-fA-F-]{36})\)")]
    private static partial Regex BookIdPattern();
}
