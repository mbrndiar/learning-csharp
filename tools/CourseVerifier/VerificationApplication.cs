using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LearningCSharp.CourseVerifier;

internal static partial class VerificationApplication
{
    private static readonly JsonSerializerOptions ManifestJsonOptions =
        new(JsonSerializerDefaults.Web);
    private static readonly string[] RequiredRoleIndexes =
    [
        "lessons/README.md",
        "exercises/README.md",
        "projects/README.md",
        "capstones/README.md",
    ];
    private static readonly string[] LegacyTopLevelRoles = ["course", "capstone", "examples"];
    private static readonly string[] InstructionalRoleRoots = ["lessons", "exercises"];

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
                "links" => RunMarkdownLinkVerification(root),
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

        if (manifest.Lessons.Count == 0)
        {
            throw new InvalidDataException("The course manifest contains no lessons.");
        }

        var duplicateOrders = manifest.Lessons
            .GroupBy(lesson => lesson.Order)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateOrders.Length > 0)
        {
            throw new InvalidDataException(
                $"Duplicate lesson orders: {string.Join(", ", duplicateOrders)}.");
        }

        int[] expectedOrders = Enumerable.Range(1, manifest.Lessons.Count).ToArray();
        int[] actualOrders = manifest.Lessons.Select(lesson => lesson.Order).Order().ToArray();
        if (!expectedOrders.SequenceEqual(actualOrders))
        {
            throw new InvalidDataException(
                $"Lesson orders must be contiguous from 1 through {manifest.Lessons.Count}.");
        }

        VerifyRoleStructure(root);

        foreach (CourseLesson lesson in manifest.Lessons.OrderBy(lesson => lesson.Order))
        {
            if (lesson.StarterProjects.Count == 0
                || lesson.SolutionProjects.Count == 0
                || lesson.TestProjects.Count == 0
                || lesson.Runnables.Count == 0)
            {
                throw new InvalidDataException(
                    $"Lesson {lesson.Order:00} must declare starter, solution, test, and runnable artifacts.");
            }

            RequireFile(root, lesson.Guide);
            foreach (string project in lesson.StarterProjects
                         .Concat(lesson.SolutionProjects)
                         .Concat(lesson.TestProjects))
            {
                RequireFile(root, project);
            }

            foreach (CourseRunnable runnable in lesson.Runnables)
            {
                RequireFile(root, runnable.Path);
                if (runnable.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    string lessonDirectory = Path.GetDirectoryName(runnable.Path)!;
                    string lockFile = Path.Combine(root, lessonDirectory, "packages.lock.json");
                    if (File.Exists(lockFile))
                    {
                        throw new InvalidDataException(
                            $"File-based lessons must not commit SDK- and OS-specific lock files: "
                            + $"{Path.GetRelativePath(root, lockFile)}.");
                    }
                }

                await RunRunnableAsync(root, runnable);
            }
        }

        foreach (LearningDestination destination in manifest.Destinations)
        {
            RequireFile(root, destination.Guide);
            foreach (string project in destination.StarterProjects
                         .Concat(destination.SolutionProjects)
                         .Concat(destination.TestProjects))
            {
                RequireFile(root, project);
            }
        }

        int readmeCount = VerifyReadmePresentation(root);
        int linkCount = VerifyMarkdownLinks(root);
        Console.WriteLine(
            $"Verified {manifest.Lessons.Count} lessons, "
            + $"{manifest.Lessons.Sum(lesson => lesson.Runnables.Count)} runnables, "
            + $"{manifest.Destinations.Count} applied destinations, "
            + $"{readmeCount} formatted READMEs, "
            + $"and {linkCount} local Markdown links.");
        return 0;
    }

    private static async Task<int> VerifyStartersAsync(string root)
    {
        CourseManifest manifest = await LoadManifestAsync(root);

        foreach (CourseLesson lesson in manifest.Lessons.OrderBy(lesson => lesson.Order))
        {
            foreach (string testProject in lesson.TestProjects)
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

        foreach (StarterCheck check in manifest.StarterChecks)
        {
            RequireFile(root, check.Project);
            var arguments = new List<string>
            {
                "test",
                "--project",
                check.Project,
                "--configuration",
                "Release",
            };
            arguments.AddRange(check.Properties.Select(property => $"-p:{property}"));
            if (check.RunnerArguments.Count > 0)
            {
                arguments.Add("--");
                arguments.AddRange(check.RunnerArguments);
            }

            ProcessResult result = await RunDotnetAsync(root, arguments);
            if (result.ExitCode != 0)
            {
                throw new InvalidDataException(
                    $"{check.Name} failed: {(result.StandardError + result.StandardOutput).Trim()}");
            }
        }

        Console.WriteLine(
            $"Verified compileable, intentionally failing feedback for "
            + $"{manifest.Lessons.Sum(lesson => lesson.TestProjects.Count)} lesson starters, "
            + $"plus {manifest.StarterChecks.Count} project/capstone starter smoke checks.");
        return 0;
    }

    private static void VerifyRoleStructure(string root)
    {
        foreach (string index in RequiredRoleIndexes)
        {
            RequireFile(root, index);
        }

        foreach (string legacyRoot in LegacyTopLevelRoles)
        {
            if (Directory.Exists(Path.Combine(root, legacyRoot)))
            {
                throw new InvalidDataException(
                    $"Legacy or unsupported top-level role remains: {legacyRoot}/");
            }
        }

        string[] legacyRoleDirectories = InstructionalRoleRoots
            .SelectMany(role => Directory.EnumerateDirectories(
                Path.Combine(root, role),
                "*",
                SearchOption.AllDirectories))
            .Where(path => !IsGeneratedPath(path))
            .Where(path => Path.GetFileName(path) is "Samples" or "Practice")
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
        if (legacyRoleDirectories.Length > 0)
        {
            throw new InvalidDataException(
                $"Legacy lesson role directories remain: {string.Join(", ", legacyRoleDirectories)}.");
        }
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
                if (Uri.TryCreate(target, UriKind.Absolute, out _)
                    || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] targetParts = target.Split('#', 2);
                string pathPart = Uri.UnescapeDataString(targetParts[0]);
                string fragment = targetParts.Length == 2
                    ? Uri.UnescapeDataString(targetParts[1])
                    : string.Empty;
                string resolved = pathPart.Length == 0
                    ? markdownPath
                    : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(markdownPath)!, pathPart));
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    string relativeSource = Path.GetRelativePath(root, markdownPath);
                    throw new InvalidDataException(
                        $"{relativeSource} links to missing path '{target}'.");
                }

                if (fragment.Length > 0)
                {
                    string markdownTarget = Directory.Exists(resolved)
                        ? Path.Combine(resolved, "README.md")
                        : resolved;
                    if (!File.Exists(markdownTarget)
                        || !string.Equals(Path.GetExtension(markdownTarget), ".md", StringComparison.OrdinalIgnoreCase)
                        || !ExtractMarkdownAnchors(File.ReadAllText(markdownTarget)).Contains(fragment))
                    {
                        string relativeSource = Path.GetRelativePath(root, markdownPath);
                        throw new InvalidDataException(
                            $"{relativeSource} links to missing Markdown anchor '{target}'.");
                    }
                }

                checkedLinks++;
            }
        }

        return checkedLinks;
    }

    private static int RunMarkdownLinkVerification(string root)
    {
        int linkCount = VerifyMarkdownLinks(root);
        Console.WriteLine($"Verified {linkCount} local Markdown links and anchors.");
        return 0;
    }

    private static int VerifyReadmePresentation(string root)
    {
        string[] readmePaths = Directory
            .EnumerateFiles(root, "README.md", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .ToArray();

        foreach (string readmePath in readmePaths)
        {
            string visibleMarkdown = RemoveFencedCodeBlocks(File.ReadAllText(readmePath));
            MatchCollection headings = ReadmeHeadingPattern().Matches(visibleMarkdown);
            if (headings.Count == 0 || headings.Count(match => match.Groups["level"].Value == "#") != 1)
            {
                throw new InvalidDataException(
                    $"{Path.GetRelativePath(root, readmePath)} must contain exactly one level-one heading.");
            }

            foreach (Match heading in headings)
            {
                string text = heading.Groups["heading"].Value.TrimStart();
                if (text.Length == 0 || !StartsWithEmoji(text))
                {
                    throw new InvalidDataException(
                        $"{Path.GetRelativePath(root, readmePath)} heading must start with a meaningful emoji: "
                        + $"'{heading.Value.Trim()}'.");
                }
            }
        }

        return readmePaths.Length;
    }

    private static bool StartsWithEmoji(string value)
    {
        int codePoint = char.ConvertToUtf32(value, 0);
        return codePoint is >= 0x2100 and <= 0x27ff
            or >= 0x2b00 and <= 0x2bff
            or >= 0x1f000 and <= 0x1faff;
    }

    private static HashSet<string> ExtractMarkdownAnchors(string markdown)
    {
        string visibleMarkdown = RemoveFencedCodeBlocks(markdown);
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var slugCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match match in MarkdownHeadingPattern().Matches(visibleMarkdown))
        {
            string slug = CreateHeadingSlug(match.Groups["heading"].Value);
            if (slug.Length == 0)
            {
                continue;
            }

            int duplicateCount = slugCounts.GetValueOrDefault(slug);
            slugCounts[slug] = duplicateCount + 1;
            anchors.Add(duplicateCount == 0 ? slug : $"{slug}-{duplicateCount}");
        }

        foreach (Match match in HtmlAnchorPattern().Matches(visibleMarkdown))
        {
            anchors.Add(match.Groups["id"].Value);
        }

        return anchors;
    }

    private static string CreateHeadingSlug(string heading)
    {
        string withoutMarkup = ClosingHeadingMarkerPattern()
            .Replace(InlineHtmlPattern().Replace(heading, string.Empty), string.Empty)
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Trim();
        var slug = new StringBuilder(withoutMarkup.Length);

        foreach (char character in withoutMarkup.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
            {
                slug.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                slug.Append('-');
            }
        }

        return slug.ToString();
    }

    private static string RemoveFencedCodeBlocks(string markdown)
    {
        var visible = new StringBuilder(markdown.Length);
        using var reader = new StringReader(markdown);
        bool insideFence = false;
        char fenceCharacter = '\0';
        int fenceLength = 0;
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.TrimStart();
            if (!insideFence && TryReadFence(trimmed, out fenceCharacter, out fenceLength))
            {
                insideFence = true;
                continue;
            }

            if (insideFence)
            {
                int closingLength = trimmed.TakeWhile(character => character == fenceCharacter).Count();
                if (closingLength >= fenceLength)
                {
                    insideFence = false;
                }

                continue;
            }

            visible.AppendLine(line);
        }

        return visible.ToString();
    }

    private static bool TryReadFence(string line, out char fenceCharacter, out int fenceLength)
    {
        char marker = line.Length > 0 ? line[0] : '\0';
        fenceCharacter = marker;
        fenceLength = marker is '`' or '~'
            ? line.TakeWhile(character => character == marker).Count()
            : 0;
        return fenceLength >= 3;
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
                + "for example 'coverage capstones/idiomatic/tests 0.85'.");
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

    private static async Task RunRunnableAsync(string root, CourseRunnable runnable)
    {
        List<string> arguments = [];
        if (runnable.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add(runnable.Path);
        }
        else
        {
            arguments.AddRange(["run", "--project", runnable.Path, "--no-restore"]);
        }

        arguments.AddRange(runnable.Arguments);
        ProcessResult result = await RunDotnetAsync(root, arguments);
        if (result.ExitCode != 0)
        {
            throw new InvalidDataException(
                $"Runnable failed ({runnable.Path}): {result.StandardError.Trim()}");
        }

        foreach (string expectedText in runnable.ExpectedOutputContains)
        {
            if (!result.StandardOutput.Contains(expectedText, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Runnable {runnable.Path} did not print expected text '{expectedText}'.");
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

    [GeneratedRegex(@"^#{1,6}\s+(?<heading>.+)$", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeadingPattern();

    [GeneratedRegex(@"^(?<level>#{1,2})\s+(?<heading>.+)$", RegexOptions.Multiline)]
    private static partial Regex ReadmeHeadingPattern();

    [GeneratedRegex(@"<a\s+(?:[^>]*?\s)?id=[""'](?<id>[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlAnchorPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex InlineHtmlPattern();

    [GeneratedRegex(@"\s+#+\s*$")]
    private static partial Regex ClosingHeadingMarkerPattern();

    [GeneratedRegex(@"(?<!\()https?://[^\s)>]+")]
    private static partial Regex PlainUrlPattern();
}

internal sealed record CourseManifest(
    IReadOnlyList<CourseLesson> Lessons,
    IReadOnlyList<LearningDestination> Destinations,
    IReadOnlyList<StarterCheck> StarterChecks);

internal sealed record CourseLesson(
    int Order,
    string Slug,
    string Guide,
    IReadOnlyList<string> StarterProjects,
    IReadOnlyList<string> SolutionProjects,
    IReadOnlyList<string> TestProjects,
    IReadOnlyList<CourseRunnable> Runnables);

internal sealed record CourseRunnable(
    string Path,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<string> ExpectedOutputContains);

internal sealed record LearningDestination(
    string Kind,
    string Slug,
    string Guide,
    IReadOnlyList<string> StarterProjects,
    IReadOnlyList<string> SolutionProjects,
    IReadOnlyList<string> TestProjects);

internal sealed record StarterCheck(
    string Name,
    string Project,
    IReadOnlyList<string> Properties,
    IReadOnlyList<string> RunnerArguments);

internal sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
