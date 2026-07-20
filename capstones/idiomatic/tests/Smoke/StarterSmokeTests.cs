using ReadingLog.Tests.TestInfrastructure;

namespace ReadingLog.Tests.Smoke;

public sealed class StarterSmokeTests
{
    [Fact]
    public async Task CliHelpCommandSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var httpClient = new HttpClient(new StubHttpMessageHandler((_, _) => throw new InvalidOperationException("The help command should not make HTTP requests.")))
        {
            BaseAddress = new Uri("http://127.0.0.1:5071"),
        };
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        var helpApp = new CliApplication(new ReadingLogApiClient(httpClient), standardOutput, standardError);
        var exitCode = await helpApp.RunAsync([], cancellationToken);

        Assert.Equal((int)CliExitCode.Success, exitCode);
        Assert.Contains("ReadingLog CLI", standardOutput.ToString());
        Assert.Equal(string.Empty, standardError.ToString());
    }

    [Fact]
    public async Task ApiCanStartAndListBooks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new ReadingLogApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/books", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", json.Trim());
    }
}
