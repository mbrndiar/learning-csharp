using ReadingLog.Cli;

var remainingArgs = new List<string>(args);
var baseUrl = Environment.GetEnvironmentVariable("READING_LOG_API_URL") ?? "http://127.0.0.1:5071/";

for (var index = 0; index < remainingArgs.Count; index++)
{
    if (!string.Equals(remainingArgs[index], "--base-url", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    if (index == remainingArgs.Count - 1)
    {
        await Console.Error.WriteLineAsync("--base-url requires a value.");
        return 1;
    }

    baseUrl = remainingArgs[index + 1];
    remainingArgs.RemoveAt(index + 1);
    remainingArgs.RemoveAt(index);
    break;
}

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(baseUrl, UriKind.Absolute),
};
var apiClient = new ReadingLogApiClient(httpClient);
var app = new CliApplication(apiClient, Console.Out, Console.Error);
using var cancellationSource = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

Console.CancelKeyPress += cancelHandler;
try
{
    return await app.RunAsync(remainingArgs.ToArray(), cancellationSource.Token);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
