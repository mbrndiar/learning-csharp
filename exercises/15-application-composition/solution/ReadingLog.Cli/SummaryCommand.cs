using System.Net.Http;
using LearningCSharp.Exercises.ApplicationComposition.Application;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

public sealed class SummaryCommand(SummaryApplication application, bool resolveDataFileAsFilePath = true)
{
    public async Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (args.Length != 2 || !string.Equals(args[0], "summary", StringComparison.OrdinalIgnoreCase))
        {
            await stderr.WriteLineAsync("Usage: summary <config-path>");
            return 2;
        }

        try
        {
            SummaryConfiguration configuration = await ConfigurationLoader.LoadAsync(args[1], resolveDataFileAsFilePath, cancellationToken);
            SummaryReport report = await application.RunAsync(configuration, cancellationToken);
            foreach (string line in report.OutputLines)
            {
                await stdout.WriteLineAsync(line);
            }

            return 0;
        }
        catch (ConfigurationException exception)
        {
            await stderr.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (FileNotFoundException exception)
        {
            await stderr.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (InvalidDataException exception)
        {
            await stderr.WriteLineAsync(exception.Message);
            return 3;
        }
        catch (HttpRequestException exception)
        {
            // Only reachable when the composition root wired up
            // HttpReadingLogSource: a network/server failure is a distinct
            // failure class from malformed data or a usage mistake, so it
            // gets its own exit code.
            await stderr.WriteLineAsync($"Reading log service request failed: {exception.Message}");
            return 4;
        }
    }
}
