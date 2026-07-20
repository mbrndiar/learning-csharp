using System.Net.Http;
using LearningCSharp.Exercises.ApplicationComposition.Application;
using LearningCSharp.Exercises.ApplicationComposition.Cli;

// The composition root - and only the composition root - decides which
// concrete IReadingLogSource adapter is wired up, based on the process
// environment. Domain and Application code never reference HttpClient, the
// file system, or environment variables directly; they only see
// IReadingLogSource.
bool useHttpSource = ReadingLogSourceSelector.ShouldUseHttpSource(Environment.GetEnvironmentVariable("READINGLOG_SOURCE"));

IReadingLogSource source = useHttpSource
    ? new HttpReadingLogSource(CreateApiHttpClient())
    : new JsonReadingLogSource();

var command = new SummaryCommand(new SummaryApplication(source), resolveDataFileAsFilePath: !useHttpSource);
return await command.RunAsync(args, Console.Out, Console.Error);

static HttpClient CreateApiHttpClient()
{
    string baseUrl = Environment.GetEnvironmentVariable("READINGLOG_API_BASEURL")
        ?? throw new InvalidOperationException(
            "READINGLOG_API_BASEURL must be set when READINGLOG_SOURCE=http.");

    return new HttpClient
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(5),
    };
}
