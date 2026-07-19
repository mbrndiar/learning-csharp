using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LearningCSharp.CourseVerifier;

internal static partial class VerificationApplication
{
    private static readonly JsonSerializerOptions ManifestJsonOptions =
        new(JsonSerializerDefaults.Web);

    public static async Task<int> RunAsync(string[] args)
    {
        string root = FindRepositoryRoot(Directory.GetCurrentDirectory());
        string command = args.FirstOrDefault() ?? "verify";

        try
        {
            return command switch
            {
                "verify" => await VerifyAsync(root),
                "starters" => await VerifyStartersAsync(root),
                "links" => VerifyMarkdownLinks(root),
                "external-links" => await VerifyExternalLinksAsync(root),
                "coverage" => VerifyCoverage(root, args),
                _ => ShowUsage(command),
            };
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"Course verification failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> VerifyAsync(string root)
    {
        CourseManifest manifest = await LoadManifestAsync(root);

        if (manifest.Modules.Count == 0)
        {
            throw new InvalidDataException("The course manifest contains no modules.");
        }

        var duplicateOrders = manifest.Modules
            .GroupBy(module => module.Order)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateOrders.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate module orders: {string.Join(", ", duplicateOrders)}.");
        }

        int[] expectedOrders = Enumerable.Range(1, manifest.Modules.Count).ToArray();
        int[] actualOrders = manifest.Modules.Select(module => module.Order).Order().ToArray();
        if (!expectedOrders.SequenceEqual(actualOrders))
        {
            throw new InvalidDataException(
                $"Module orders must be contiguous from 1 through {manifest.Modules.Count}.");
        }

        foreach (CourseModule module in manifest.Modules.OrderBy(module => module.Order))
        {
            if (module.StarterProjects.Count == 0
                || module.SolutionProjects.Count == 0
                || module.TestProjects.Count == 0
                || module.Samples.Count == 0)
            {
                throw new InvalidDataException(
                    $"Module {module.Order:00} must declare starter, solution, test, and sample artifacts.");
            }

            RequireFile(root, module.Guide);
            foreach (string project in module.StarterProjects
                         .Concat(module.SolutionProjects)
                         .Concat(module.TestProjects))
            {
                RequireFile(root, project);
            }

            foreach (CourseSample sample in module.Samples)
            {
                RequireFile(root, sample.Path);
                await RunSampleAsync(root, sample);
            }
        }

        int linkCount = VerifyMarkdownLinks(root);
        Console.WriteLine(
            $"Verified {manifest.Modules.Count} modules, "
            + $"{manifest.Modules.Sum(module => module.Samples.Count)} samples, "
            + $"and {linkCount} local Markdown links.");
        return 0;
    }

    private static async Task<int> VerifyStartersAsync(string root)
    {
        CourseManifest manifest = await LoadManifestAsync(root);

        foreach (CourseModule module in manifest.Modules.OrderBy(module => module.Order))
        {
            foreach (string testProject in module.TestProjects)
            {
                ProcessResult build = await RunDotnetAsync(
                    root,
                    [
                        "build",
                        testProject,
                        "--configuration",
                        "Release",
                        "--no-restore",
                        "-p:CourseImplementation=Starter",
                    ]);
                if (build.ExitCode != 0)
                {
                    throw new InvalidDataException(
                        $"Starter feedback project did not compile ({testProject}): "
                        + build.StandardError.Trim());
                }

                ProcessResult test = await RunDotnetAsync(
                    root,
                    [
                        "test",
                        "--project",
                        testProject,
                        "--configuration",
                        "Release",
                        "--no-build",
                        "-p:CourseImplementation=Starter",
                    ]);
                string combinedOutput = test.StandardOutput + test.StandardError;
                if (test.ExitCode == 0
                    || !combinedOutput.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Untouched starter feedback must report focused failing tests: {testProject}");
                }
            }
        }

        Console.WriteLine(
            $"Verified compileable, intentionally failing feedback for "
            + $"{manifest.Modules.Sum(module => module.TestProjects.Count)} module starters.");
        return 0;
    }

    private static async Task<CourseManifest> LoadManifestAsync(string root)
    {
        string manifestPath = Path.Combine(root, "course-manifest.json");
        await using FileStream stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<CourseManifest>(
                stream,
                ManifestJsonOptions)
            ?? throw new InvalidDataException("course-manifest.json is empty.");
    }

    private static int VerifyMarkdownLinks(string root)
    {
        int checkedLinks = 0;

        foreach (string markdownPath in Directory.EnumerateFiles(
                     root,
                     "*.md",
                     SearchOption.AllDirectories))
        {
            if (IsGeneratedPath(markdownPath))
            {
                continue;
            }

            string content = File.ReadAllText(markdownPath);
            foreach (Match match in MarkdownLinkPattern().Matches(content))
            {
                string target = match.Groups["target"].Value;
                if (target.StartsWith('#')
                    || Uri.TryCreate(target, UriKind.Absolute, out _)
                    || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string pathPart = Uri.UnescapeDataString(target.Split('#', 2)[0]);
                string resolved = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(markdownPath)!, pathPart));
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    string relativeSource = Path.GetRelativePath(root, markdownPath);
                    throw new InvalidDataException(
                        $"{relativeSource} links to missing path '{target}'.");
                }

                checkedLinks++;
            }
        }

        return checkedLinks;
    }

    private static async Task<int> VerifyExternalLinksAsync(string root)
    {
        Uri[] links = Directory
            .EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .SelectMany(path => ExtractExternalLinkTargets(File.ReadAllText(path)))
            .Select(target => Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) ? uri : null)
            .Where(uri => uri is not null && (uri.Scheme == "https" || uri.Scheme == "http"))
            .Cast<Uri>()
            .Where(uri => !uri.IsLoopback)
            .Select(RemoveFragment)
            .Distinct()
            .OrderBy(uri => uri.AbsoluteUri, StringComparer.Ordinal)
            .ToArray();

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("learning-csharp-link-checker/1.0");

        foreach (Uri link in links)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, link);
                using HttpResponseMessage response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidDataException(
                        $"External link returned {(int)response.StatusCode}: {link}");
                }
            }
            catch (HttpRequestException exception)
            {
                throw new InvalidDataException(
                    $"External link could not be reached: {link} ({exception.Message})",
                    exception);
            }
            catch (TaskCanceledException exception)
            {
                throw new InvalidDataException(
                    $"External link timed out: {link}",
                    exception);
            }
        }

        Console.WriteLine($"Verified {links.Length} external Markdown links.");
        return 0;
    }

    private static IEnumerable<string> ExtractExternalLinkTargets(string markdown)
    {
        foreach (Match match in MarkdownLinkPattern().Matches(markdown))
        {
            yield return match.Groups["target"].Value;
        }

        foreach (Match match in PlainUrlPattern().Matches(markdown))
        {
            yield return match.Value;
        }
    }

    private static int VerifyCoverage(string root, string[] args)
    {
        if (args.Length != 3
            || !double.TryParse(
                args[2],
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out double minimumBranchRate)
            || minimumBranchRate is < 0 or > 1)
        {
            throw new InvalidDataException(
                "Use 'coverage <results-directory> <minimum-branch-rate>', "
                + "for example 'coverage capstone/reading-log/Tests 0.85'.");
        }

        string resultsRoot = Path.GetFullPath(Path.Combine(root, args[1]));
        if (!Directory.Exists(resultsRoot))
        {
            throw new InvalidDataException($"Coverage directory is missing: {args[1]}.");
        }

        string[] reports = Directory.GetFiles(
            resultsRoot,
            "*.cobertura.xml",
            SearchOption.AllDirectories);
        if (reports.Length != 1)
        {
            throw new InvalidDataException(
                $"Expected one Cobertura report under {args[1]}, found {reports.Length}.");
        }

        XDocument document = XDocument.Load(reports[0]);
        string? branchRateText = document.Root?.Attribute("branch-rate")?.Value;
        if (!double.TryParse(
                branchRateText,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out double branchRate))
        {
            throw new InvalidDataException(
                $"Coverage report has no valid branch-rate: {reports[0]}.");
        }

        if (branchRate < minimumBranchRate)
        {
            throw new InvalidDataException(
                $"Branch coverage {branchRate:P1} is below the {minimumBranchRate:P1} gate.");
        }

        Console.WriteLine(
            $"Branch coverage {branchRate:P1} meets the {minimumBranchRate:P1} gate.");
        return 0;
    }

    private static async Task RunSampleAsync(string root, CourseSample sample)
    {
        List<string> arguments = [];
        if (sample.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add(sample.Path);
        }
        else
        {
            arguments.AddRange(["run", "--project", sample.Path, "--no-restore"]);
        }

        arguments.AddRange(sample.Arguments);
        ProcessResult result = await RunDotnetAsync(root, arguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidDataException(
                $"Sample failed ({sample.Path}): {result.StandardError.Trim()}");
        }

        foreach (string expectedText in sample.ExpectedOutputContains)
        {
            if (!result.StandardOutput.Contains(expectedText, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Sample {sample.Path} did not print expected text '{expectedText}'.");
            }
        }
    }

    private static async Task<ProcessResult> RunDotnetAsync(
        string root,
        IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw new InvalidDataException(
                $"dotnet command timed out: dotnet {string.Join(' ', arguments)}");
        }

        return new ProcessResult(
            process.ExitCode,
            await standardOutput,
            await standardError);
    }

    private static string FindRepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LearningCSharp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidDataException(
        "Run CourseVerifier from inside the learning-csharp repository.");
    }

    private static void RequireFile(string root, string relativePath)
    {
        if (!File.Exists(Path.Combine(root, relativePath)))
        {
            throw new InvalidDataException(
                $"Manifest path is missing: {relativePath}.");
        }
    }

    private static bool IsGeneratedPath(string path)
    {
        string[] segments = path.Split(Path.DirectorySeparatorChar);
        return segments.Contains("bin", StringComparer.Ordinal)
        || segments.Contains("obj", StringComparer.Ordinal);
    }

    private static int ShowUsage(string command)
    {
        Console.Error.WriteLine(
        $"Unknown command '{command}'. Use 'verify', 'starters', 'links', "
        + "'external-links', or 'coverage'.");
        return 2;
    }

    private static Uri RemoveFragment(Uri uri)
    {
        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri;
    }

    [GeneratedRegex(@"(?<!!)\[[^\]]+\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex MarkdownLinkPattern();

    [GeneratedRegex(@"(?<!\()https?://[^\s)>]+")]
    private static partial Regex PlainUrlPattern();
}

internal sealed record CourseManifest(IReadOnlyList<CourseModule> Modules);

internal sealed record CourseModule(
    int Order,
    string Slug,
    string Guide,
    IReadOnlyList<string> StarterProjects,
    IReadOnlyList<string> SolutionProjects,
    IReadOnlyList<string> TestProjects,
    IReadOnlyList<CourseSample> Samples);

internal sealed record CourseSample(
    string Path,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<string> ExpectedOutputContains);

internal sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
