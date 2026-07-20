using System.Diagnostics;

namespace Tasks.Tests.Support;

/// <summary>Locates project-relative paths from the test output directory.</summary>
public static class TestPaths
{
    /// <summary>The absolute <c>projects/tasks</c> directory.</summary>
    public static string TasksRoot { get; } = LocateTasksRoot();

    /// <summary>The build configuration the tests were compiled with.</summary>
    public static string BuildConfiguration { get; } =
        AppContext.BaseDirectory.Replace('\\', '/').Contains("/bin/Release/", StringComparison.Ordinal)
            ? "Release"
            : "Debug";

    /// <summary>Resolve a checked-in documentation file.</summary>
    public static string Docs(string fileName) => Path.Combine(TasksRoot, "docs", fileName);

    /// <summary>Resolve the compiled CLI assembly for one implementation.</summary>
    public static string CliAssembly(string implementation) => Path.Combine(
        TasksRoot,
        implementation.ToLowerInvariant(),
        "client",
        "Tasks.Cli",
        "bin",
        BuildConfiguration,
        "net10.0",
        "Tasks.Cli.dll");

    private static string LocateTasksRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (string.Equals(directory.Name, "tasks", StringComparison.OrdinalIgnoreCase)
                && directory.Parent is { Name: "projects" })
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("could not locate the projects/tasks root from the test output");
    }
}

/// <summary>Runs the compiled CLI host as a real child process.</summary>
public static class CliRunner
{
    /// <summary>Run the selected implementation's CLI with the supplied arguments.</summary>
    public static async Task<ClientOutcome> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(TestPaths.CliAssembly(TestImplementation.Name));
        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // The CLI child is a separate program whose behavior (exit code, output)
        // is what we assert; its own coverage is out of scope. Detach it from any
        // coverage profiler so a child collection cannot corrupt the merged report.
        foreach (string variable in new[]
                 {
                     "CORECLR_ENABLE_PROFILING", "CORECLR_PROFILER", "CORECLR_PROFILER_PATH",
                     "CORECLR_PROFILER_PATH_32", "CORECLR_PROFILER_PATH_64",
                     "CODE_COVERAGE_SESSION_NAME", "MicrosoftInstrumentationEngine_Host",
                 })
        {
            startInfo.Environment.Remove(variable);
        }

        startInfo.Environment["CORECLR_ENABLE_PROFILING"] = "0";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("failed to start the CLI process");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new ClientOutcome(process.ExitCode, await stdout, await stderr);
    }
}
