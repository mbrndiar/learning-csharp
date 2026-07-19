using LearningCSharp.Course.Unit14.Practice.Application;

namespace LearningCSharp.Course.Unit14.Practice.Cli;

public sealed class SummaryCommand(SummaryApplication application)
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
            SummaryConfiguration configuration = await ConfigurationLoader.LoadAsync(args[1], cancellationToken);
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
    }
}
