using System.Diagnostics;
using System.Text;
namespace ComparativeKv.Tests.Support;

internal sealed record ProcessResult(int ExitCode, byte[] StandardOutput, byte[] StandardError, TimeSpan Duration)
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public string OutputText => StrictUtf8.GetString(StandardOutput);

    public string ErrorText => StrictUtf8.GetString(StandardError);
}

internal sealed class RunningProcess : IDisposable
{
    private readonly Process process;
    private readonly Task<byte[]> standardOutput;
    private readonly Task<byte[]> standardError;
    private readonly Stopwatch stopwatch;
    private bool consumed;

    private RunningProcess(Process process)
    {
        this.process = process;
        stopwatch = Stopwatch.StartNew();
        standardOutput = ReadAllBytesAsync(process.StandardOutput.BaseStream);
        standardError = ReadAllBytesAsync(process.StandardError.BaseStream);
    }

    public static RunningProcess Start(string executable, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Could not start {executable}.");
        }

        return new RunningProcess(process);
    }

    public async Task<ProcessResult> WaitAsync(TimeSpan timeout)
    {
        if (consumed)
        {
            throw new InvalidOperationException("A process result can only be collected once.");
        }

        consumed = true;
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Terminate();
            throw new TimeoutException($"Process {process.Id} exceeded its {timeout.TotalSeconds:F0}-second timeout.");
        }

        var output = await standardOutput.ConfigureAwait(false);
        var error = await standardError.ConfigureAwait(false);
        stopwatch.Stop();
        return new ProcessResult(process.ExitCode, output, error, stopwatch.Elapsed);
    }

    public void Dispose()
    {
        Terminate();
        process.Dispose();
    }

    private void Terminate()
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(milliseconds: 5_000);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer).ConfigureAwait(false);
        return buffer.ToArray();
    }
}

internal static class ProcessRunner
{
    private static readonly string CliAssembly = typeof(ComparativeKv.Cli.Program).Assembly.Location;
    private static readonly string SupportAssembly = typeof(ComparativeKv.ProcessSupport.ProcessSupportMarker).Assembly.Location;

    public static RunningProcess StartCli(IEnumerable<string> arguments)
    {
        var processArguments = new List<string> { CliAssembly };
        processArguments.AddRange(arguments);
        return RunningProcess.Start("dotnet", processArguments);
    }

    public static RunningProcess StartBarrier(string readyPath, string releasePath, IEnumerable<string> cliArguments)
    {
        var processArguments = new List<string>
        {
            SupportAssembly,
            "barrier",
            "--ready",
            readyPath,
            "--release",
            releasePath,
            "--",
            "dotnet",
            CliAssembly,
        };
        processArguments.AddRange(cliArguments);
        return RunningProcess.Start("dotnet", processArguments);
    }

    public static RunningProcess StartLockHelper(string databasePath, string readyPath, string releasePath) =>
        RunningProcess.Start(
            "dotnet",
            [
                SupportAssembly,
                "lock",
                "--db",
                databasePath,
                "--ready",
                readyPath,
                "--release",
                releasePath,
            ]);

    public static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (!File.Exists(path))
        {
            if (Stopwatch.GetTimestamp() >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for helper signal: {path}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
        }
    }
}
