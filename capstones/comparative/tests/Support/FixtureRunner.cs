using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace ComparativeKv.Tests.Support;

internal static class FixtureRunner
{
    private static readonly TimeSpan OrdinaryTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ParallelTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BusyTimeout = TimeSpan.FromSeconds(20);

    public static async Task RunSequentialFileAsync(string relativePath)
    {
        var path = SpecPath(relativePath);
        using var document = OpenFixture(path, "sequential_scenarios");
        foreach (var scenario in document.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            await RunSequentialScenarioAsync(path, scenario).ConfigureAwait(false);
        }
    }

    public static async Task RunMultiprocessFileAsync(string relativePath)
    {
        var path = SpecPath(relativePath);
        using var document = OpenFixture(path, "multiprocess_scenarios");
        foreach (var scenario in document.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            var repeat = scenario.TryGetProperty("repeat", out var repeatElement) ? repeatElement.GetInt32() : 1;
            for (var iteration = 0; iteration < repeat; iteration++)
            {
                await RunMultiprocessScenarioAsync(scenario, iteration).ConfigureAwait(false);
            }
        }
    }

    public static async Task RunKeyFixtureAsync()
    {
        using var document = OpenFixture(SpecPath("fixtures/keys.json"), "key_cases");
        var fixture = document.RootElement;

        foreach (var item in fixture.GetProperty("accepted").EnumerateArray())
        {
            using var scenario = new ScenarioDirectory($"key-accepted-{item.GetProperty("id").GetString()}");
            var key = ReadKey(item);
            var set = await RunCommandAsync(
                ["--db", scenario.DatabasePath, "set", key, "--value-json", "null", "--expect", "absent"],
                OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(0, set.Result.ExitCode);
            AssertSetResult(set.Envelope, key, ParseJson("null"), revision: 1, created: true);

            var get = await RunCommandAsync(
                ["--db", scenario.DatabasePath, "get", key],
                OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(0, get.Result.ExitCode);
            AssertEntryResult(get.Envelope, key, ParseJson("null"), revision: 1);
        }

        foreach (var item in fixture.GetProperty("rejected").EnumerateArray())
        {
            using var scenario = new ScenarioDirectory($"key-rejected-{item.GetProperty("id").GetString()}");
            var result = await RunCommandAsync(
                ["--db", scenario.DatabasePath, "get", ReadKey(item)],
                OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(2, result.Result.ExitCode);
            AssertError(result.Envelope, "invalid_argument", ParseJson("""{"field":"key","reason":"format"}"""));
            Assert.False(File.Exists(scenario.DatabasePath));
        }

        using (var scenario = new ScenarioDirectory("key-ordering"))
        {
            var ordering = fixture.GetProperty("ordering").EnumerateArray().Select(static item => item.GetString()!).ToArray();
            foreach (var key in ordering.Reverse())
            {
                var result = await RunCommandAsync(
                    ["--db", scenario.DatabasePath, "set", key, "--value-json", "true"],
                    OrdinaryTimeout).ConfigureAwait(false);
                Assert.Equal(0, result.Result.ExitCode);
            }

            var list = await RunCommandAsync(["--db", scenario.DatabasePath, "list"], OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(0, list.Result.ExitCode);
            var actual = list.Envelope.GetProperty("result").GetProperty("entries").EnumerateArray()
                .Select(static entry => entry.GetProperty("key").GetString())
                .ToArray();
            Assert.Equal(ordering, actual);
        }
    }

    public static async Task RunAcceptedValueFixtureAsync()
    {
        using var document = OpenFixture(SpecPath("fixtures/values-accepted.json"), "accepted_value_cases");
        foreach (var item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var scenario = new ScenarioDirectory($"value-accepted-{item.GetProperty("id").GetString()}");
            var input = ReadGeneratedJson(item, "input_json", "input_generator");
            var expected = ReadGeneratedValue(item, "normalized", "normalized_generator");
            var set = await RunCommandAsync(
                ["--db", scenario.DatabasePath, "set", "value", "--value-json", input, "--expect", "absent"],
                OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(0, set.Result.ExitCode);
            AssertSetResult(set.Envelope, "value", expected, revision: 1, created: true);

            var get = await RunCommandAsync(["--db", scenario.DatabasePath, "get", "value"], OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(0, get.Result.ExitCode);
            AssertEntryResult(get.Envelope, "value", expected, revision: 1);
        }
    }

    public static async Task RunRejectedValueFixtureAsync()
    {
        using var document = OpenFixture(SpecPath("fixtures/values-rejected.json"), "rejected_value_cases");
        foreach (var item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var scenario = new ScenarioDirectory($"value-rejected-{item.GetProperty("id").GetString()}");
            var input = ReadGeneratedJson(item, "input_json", "input_generator");
            var result = await RunCommandAsync(
                ["--db", scenario.DatabasePath, "set", "value", "--value-json", input],
                OrdinaryTimeout).ConfigureAwait(false);
            Assert.Equal(item.GetProperty("exit").GetInt32(), result.Result.ExitCode);
            AssertError(result.Envelope, item.GetProperty("category").GetString()!, item.GetProperty("details"));
            Assert.False(File.Exists(scenario.DatabasePath), "Rejected domain input must not create storage.");
        }
    }

    private static async Task RunSequentialScenarioAsync(string scenarioPath, JsonElement scenario)
    {
        using var directory = new ScenarioDirectory(scenario.GetProperty("id").GetString()!);
        await PrepareDatabaseAsync(scenario, directory).ConfigureAwait(false);
        foreach (var step in scenario.GetProperty("steps").EnumerateArray())
        {
            if (step.TryGetProperty("run", out var run))
            {
                var outcome = await RunCommandAsync(ExpandArguments(run.GetProperty("args"), directory), OrdinaryTimeout).ConfigureAwait(false);
                AssertExpected(outcome, step.GetProperty("expect"));
            }
            else if (step.TryGetProperty("sqlite_assert", out var sqliteAssertion))
            {
                AssertSqliteQueries(directory.DatabasePath, sqliteAssertion);
            }
            else if (step.TryGetProperty("fixture_references", out var references))
            {
                foreach (var reference in references.EnumerateArray())
                {
                    var path = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(scenarioPath)!,
                            reference.GetString()!));
                    await RunFixtureReferenceAsync(path).ConfigureAwait(false);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unknown sequential step: {step.GetRawText()}");
            }
        }

        if (!scenario.GetRawText().Contains("\"invalid_storage\"", StringComparison.Ordinal))
        {
            AssertIntegrity(directory.DatabasePath);
        }
    }

    private static async Task RunFixtureReferenceAsync(string fullPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(fullPath));
        Assert.Equal("1.0.0", document.RootElement.GetProperty("spec_version").GetString());
        switch (document.RootElement.GetProperty("kind").GetString())
        {
            case "key_cases":
                await RunKeyFixtureAsync().ConfigureAwait(false);
                break;
            case "accepted_value_cases":
                await RunAcceptedValueFixtureAsync().ConfigureAwait(false);
                break;
            case "rejected_value_cases":
                await RunRejectedValueFixtureAsync().ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown referenced fixture: {fullPath}");
        }
    }

    private static async Task RunMultiprocessScenarioAsync(JsonElement scenario, int iteration)
    {
        using var directory = new ScenarioDirectory($"{scenario.GetProperty("id").GetString()}-{iteration}");
        await PrepareDatabaseAsync(scenario, directory).ConfigureAwait(false);
        var active = new Dictionary<string, RunningProcess>(StringComparer.Ordinal);
        var captures = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        try
        {
            foreach (var operation in scenario.GetProperty("operations").EnumerateArray())
            {
                if (operation.TryGetProperty("parallel", out var parallel))
                {
                    await RunParallelAsync(parallel, directory).ConfigureAwait(false);
                }
                else if (operation.TryGetProperty("run_assert", out var runAssert))
                {
                    var outcome = await RunCommandAsync(ExpandArguments(runAssert.GetProperty("args"), directory), OrdinaryTimeout).ConfigureAwait(false);
                    AssertExpected(outcome, runAssert.GetProperty("expect"));
                    if (runAssert.TryGetProperty("assert", out var assertion))
                    {
                        AssertRunAssertions(outcome.Envelope, assertion, captures);
                    }

                    if (runAssert.TryGetProperty("capture", out var capture))
                    {
                        captures.Add(capture.GetString()!, outcome.Envelope);
                    }
                }
                else if (operation.TryGetProperty("start_lock_helper", out var lockHelper))
                {
                    var id = lockHelper.GetProperty("id").GetString()!;
                    var ready = System.IO.Path.Combine(directory.Path, $"{id}.ready");
                    var release = System.IO.Path.Combine(directory.Path, $"{id}.release");
                    var helper = ProcessRunner.StartLockHelper(directory.DatabasePath, ready, release);
                    active.Add(id, helper);
                    await ProcessRunner.WaitForFileAsync(ready, OrdinaryTimeout).ConfigureAwait(false);
                }
                else if (operation.TryGetProperty("start_cli", out var startCli))
                {
                    var id = startCli.GetProperty("id").GetString()!;
                    active.Add(id, ProcessRunner.StartCli(ExpandArguments(startCli.GetProperty("args"), directory)));
                }
                else if (operation.TryGetProperty("sleep_ms", out var sleep))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(sleep.GetInt32())).ConfigureAwait(false);
                }
                else if (operation.TryGetProperty("await_cli", out var awaitCli))
                {
                    var id = awaitCli.GetProperty("id").GetString()!;
                    var running = active[id];
                    CommandOutcome outcome;
                    try
                    {
                        outcome = await AwaitProcessAsync(running, BusyTimeout).ConfigureAwait(false);
                    }
                    catch (TimeoutException exception)
                    {
                        throw new TimeoutException(
                            $"CLI '{id}' timed out in multiprocess scenario '{scenario.GetProperty("id").GetString()}'.",
                            exception);
                    }

                    active.Remove(id);
                    AssertExpected(outcome, awaitCli.GetProperty("expect"));
                    AssertDuration(outcome.Result.Duration, awaitCli.TryGetProperty("assert", out var assertion) ? assertion : default);
                    running.Dispose();
                }
                else if (operation.TryGetProperty("release_lock_helper", out var releaseLock))
                {
                    var id = releaseLock.GetProperty("id").GetString()!;
                    var release = System.IO.Path.Combine(directory.Path, $"{id}.release");
                    await File.WriteAllTextAsync(release, "release", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).ConfigureAwait(false);
                    var helper = active[id];
                    var result = await helper.WaitAsync(ParallelTimeout).ConfigureAwait(false);
                    active.Remove(id);
                    Assert.Equal(0, result.ExitCode);
                    Assert.Empty(result.StandardOutput);
                    Assert.Equal(string.Empty, result.ErrorText);
                    helper.Dispose();
                }
                else
                {
                    throw new InvalidOperationException($"Unknown multiprocess operation: {operation.GetRawText()}");
                }
            }
        }
        finally
        {
            foreach (var process in active.Values)
            {
                process.Dispose();
            }
        }

        AssertIntegrity(directory.DatabasePath);
    }

    private static async Task RunParallelAsync(JsonElement parallel, ScenarioDirectory directory)
    {
        var generator = parallel.GetProperty("actors_generator");
        Assert.Equal("indexed_commands", generator.GetProperty("kind").GetString());
        var count = generator.GetProperty("count").GetInt32();
        var release = System.IO.Path.Combine(directory.Path, $"parallel-{Guid.NewGuid():N}.release");
        var processes = new List<RunningProcess>();
        var readyPaths = new List<string>();
        try
        {
            for (var index = 0; index < count; index++)
            {
                var ready = System.IO.Path.Combine(directory.Path, $"parallel-{index}-{Guid.NewGuid():N}.ready");
                readyPaths.Add(ready);
                processes.Add(
                    ProcessRunner.StartBarrier(
                        ready,
                        release,
                        ExpandIndexedArguments(generator.GetProperty("args"), directory, generator, index)));
            }

            await Task.WhenAll(readyPaths.Select(path => ProcessRunner.WaitForFileAsync(path, OrdinaryTimeout))).ConfigureAwait(false);
            await File.WriteAllTextAsync(release, "release", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).ConfigureAwait(false);
            var outcomes = await Task.WhenAll(processes.Select(process => AwaitProcessAsync(process, ParallelTimeout))).ConfigureAwait(false);
            AssertParallelAssertions(outcomes, parallel.GetProperty("assert"), directory);
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static async Task<CommandOutcome> RunCommandAsync(IEnumerable<string> arguments, TimeSpan timeout)
    {
        using var process = ProcessRunner.StartCli(arguments);
        return await AwaitProcessAsync(process, timeout).ConfigureAwait(false);
    }

    private static async Task<CommandOutcome> AwaitProcessAsync(RunningProcess process, TimeSpan timeout)
    {
        var result = await process.WaitAsync(timeout).ConfigureAwait(false);
        var envelope = JsonContractAssertions.AssertEnvelope(result);
        StarterMilestoneFeedback.ThrowIfActive(result, envelope);
        return new CommandOutcome(result, envelope);
    }

    private static void AssertExpected(CommandOutcome outcome, JsonElement expected)
    {
        using var actual = JsonContractAssertions.AssertProcess(outcome.Result, expected);
        JsonContractAssertions.AssertSemanticEqual(actual.RootElement, outcome.Envelope);
    }

    private static void AssertParallelAssertions(
        IReadOnlyList<CommandOutcome> outcomes,
        JsonElement assertions,
        ScenarioDirectory directory)
    {
        if (assertions.TryGetProperty("all_exit", out var allExit))
        {
            Assert.All(outcomes, outcome => Assert.Equal(allExit.GetInt32(), outcome.Result.ExitCode));
        }

        if (assertions.TryGetProperty("all_ok", out var allOk))
        {
            Assert.All(outcomes, outcome => Assert.Equal(allOk.GetBoolean(), outcome.Envelope.GetProperty("ok").GetBoolean()));
        }

        if (assertions.TryGetProperty("stdout_semantic_all", out var allOutput))
        {
            Assert.All(outcomes, outcome => JsonContractAssertions.AssertSemanticEqual(allOutput, outcome.Envelope));
        }

        var successes = outcomes.Where(static outcome => outcome.Envelope.GetProperty("ok").GetBoolean()).ToArray();
        if (assertions.TryGetProperty("success_count", out var successCount))
        {
            Assert.Equal(successCount.GetInt32(), successes.Length);
        }

        if (assertions.TryGetProperty("category_counts", out var categoryCounts))
        {
            foreach (var expected in categoryCounts.EnumerateObject())
            {
                var count = outcomes.Count(
                    outcome =>
                        !outcome.Envelope.GetProperty("ok").GetBoolean() &&
                        string.Equals(
                            outcome.Envelope.GetProperty("error").GetProperty("category").GetString(),
                            expected.Name,
                            StringComparison.Ordinal));
                Assert.Equal(expected.Value.GetInt32(), count);
            }
        }

        if (assertions.TryGetProperty("result_revision_set", out var revisionSet))
        {
            AssertRange(
                successes.Select(static outcome => outcome.Envelope.GetProperty("result").GetProperty("revision").GetInt64()),
                revisionSet);
        }

        if (assertions.TryGetProperty("success_revision", out var successRevision))
        {
            Assert.All(
                successes,
                outcome => Assert.Equal(
                    successRevision.GetInt64(),
                    outcome.Envelope.GetProperty("result").GetProperty("revision").GetInt64()));
        }

        if (assertions.TryGetProperty("conflict_actual", out var conflictActual))
        {
            foreach (var outcome in outcomes.Where(
                         static outcome =>
                             !outcome.Envelope.GetProperty("ok").GetBoolean() &&
                             outcome.Envelope.GetProperty("error").GetProperty("category").GetString() == "conflict"))
            {
                Assert.Equal(
                    conflictActual.GetInt64(),
                    outcome.Envelope.GetProperty("error").GetProperty("details").GetProperty("actual").GetInt64());
            }
        }

        if (assertions.TryGetProperty("not_found_count", out var notFoundCount))
        {
            var actual = outcomes.Count(
                static outcome =>
                    !outcome.Envelope.GetProperty("ok").GetBoolean() &&
                    outcome.Envelope.GetProperty("error").GetProperty("category").GetString() == "not_found");
            Assert.Equal(notFoundCount.GetInt32(), actual);
        }

        if (assertions.TryGetProperty("winner_value_matches_final", out var winnerMatches) && winnerMatches.GetBoolean())
        {
            Assert.Single(successes);
            var winner = successes[0].Envelope.GetProperty("result");
            var get = RunCommandAsync(
                    ["--db", directory.DatabasePath, "get", winner.GetProperty("key").GetString()!],
                    OrdinaryTimeout)
                .GetAwaiter()
                .GetResult();
            Assert.Equal(0, get.Result.ExitCode);
            JsonContractAssertions.AssertSemanticEqual(
                winner.GetProperty("value"),
                get.Envelope.GetProperty("result").GetProperty("value"));
        }

        AssertDuration(
            outcomes.MaxBy(static outcome => outcome.Result.Duration)!.Result.Duration,
            assertions);
    }

    private static void AssertRunAssertions(
        JsonElement envelope,
        JsonElement assertions,
        Dictionary<string, JsonElement> captures)
    {
        var result = envelope.GetProperty("result");
        if (assertions.TryGetProperty("keys_in_order", out var expectedKeys))
        {
            var actualKeys = result.GetProperty("entries").EnumerateArray()
                .Select(static entry => entry.GetProperty("key").GetString())
                .ToArray();
            var keys = expectedKeys.EnumerateArray().Select(static item => item.GetString()).ToArray();
            Assert.Equal(keys, actualKeys);
        }

        if (assertions.TryGetProperty("global_revision", out var globalRevision))
        {
            Assert.Equal(globalRevision.GetInt64(), result.GetProperty("global_revision").GetInt64());
        }

        if (assertions.TryGetProperty("entry_count", out var entryCount))
        {
            Assert.Equal(entryCount.GetInt32(), result.GetProperty("entries").GetArrayLength());
        }

        if (assertions.TryGetProperty("entry_revision_set", out var entryRevisions))
        {
            AssertRange(
                result.GetProperty("entries").EnumerateArray().Select(static entry => entry.GetProperty("revision").GetInt64()),
                entryRevisions);
        }

        if (assertions.TryGetProperty("values_by_key", out var valuesByKey))
        {
            var actual = result.GetProperty("entries").EnumerateArray()
                .ToDictionary(static entry => entry.GetProperty("key").GetString()!, static entry => entry.GetProperty("value"), StringComparer.Ordinal);
            foreach (var expected in valuesByKey.EnumerateObject())
            {
                Assert.True(actual.TryGetValue(expected.Name, out var value));
                JsonContractAssertions.AssertSemanticEqual(expected.Value, value);
            }
        }

        if (assertions.TryGetProperty("revision_by_key", out var revisionsByKey))
        {
            var actual = result.GetProperty("entries").EnumerateArray()
                .ToDictionary(static entry => entry.GetProperty("key").GetString()!, static entry => entry.GetProperty("revision").GetInt64(), StringComparer.Ordinal);
            foreach (var expected in revisionsByKey.EnumerateObject())
            {
                Assert.Equal(expected.Value.GetInt64(), actual[expected.Name]);
            }
        }

        if (assertions.TryGetProperty("state_unchanged_from", out var captured))
        {
            Assert.True(captures.TryGetValue(captured.GetString()!, out var expected));
            JsonContractAssertions.AssertSemanticEqual(expected, envelope);
        }
    }

    private static void AssertDuration(TimeSpan duration, JsonElement assertions)
    {
        if (assertions.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (assertions.TryGetProperty("duration_less_than_ms", out var lessThan))
        {
            Assert.True(duration.TotalMilliseconds < lessThan.GetInt32(), $"Duration {duration.TotalMilliseconds:F0}ms was too long.");
        }

        if (assertions.TryGetProperty("duration_at_least_ms", out var atLeast))
        {
            Assert.True(duration.TotalMilliseconds >= atLeast.GetInt32(), $"Duration {duration.TotalMilliseconds:F0}ms was too short.");
        }
    }

    private static void AssertRange(IEnumerable<long> values, JsonElement range)
    {
        var actual = values.OrderBy(static value => value).ToArray();
        var expected = Enumerable.Range(
                range.GetProperty("from").GetInt32(),
                range.GetProperty("to").GetInt32() - range.GetProperty("from").GetInt32() + 1)
            .Select(static value => (long)value)
            .ToArray();
        Assert.Equal(expected, actual);
    }

    private static async Task PrepareDatabaseAsync(JsonElement scenario, ScenarioDirectory directory)
    {
        var database = scenario.GetProperty("database").GetString();
        Assert.True(database is "fresh" or "sqlite_setup");
        if (database == "sqlite_setup")
        {
            RunSqliteSetup(directory.DatabasePath, scenario.GetProperty("setup").GetProperty("statements"));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static void RunSqliteSetup(string databasePath, JsonElement statements)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            Pooling = false,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        foreach (var statement in statements.EnumerateArray())
        {
            using var command = connection.CreateCommand();
            command.CommandText = statement.GetString();
            command.ExecuteNonQuery();
        }
    }

    private static void AssertSqliteQueries(string databasePath, JsonElement assertion)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Default,
            Pooling = false,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        foreach (var query in assertion.GetProperty("queries").EnumerateArray())
        {
            using var command = connection.CreateCommand();
            command.CommandText = query.GetProperty("sql").GetString();
            using var reader = command.ExecuteReader();
            var expectedRows = query.GetProperty("rows").EnumerateArray().ToArray();
            var rowIndex = 0;
            while (reader.Read())
            {
                Assert.True(rowIndex < expectedRows.Length, "SQLite query returned too many rows.");
                var expected = expectedRows[rowIndex++].EnumerateArray().ToArray();
                Assert.Equal(expected.Length, reader.FieldCount);
                for (var column = 0; column < expected.Length; column++)
                {
                    AssertSqliteCell(expected[column], reader.GetValue(column));
                }
            }

            Assert.Equal(expectedRows.Length, rowIndex);
        }
    }

    private static void AssertSqliteCell(JsonElement expected, object value)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Null:
                Assert.Equal(DBNull.Value, value);
                break;
            case JsonValueKind.String:
                Assert.Equal(expected.GetString(), value as string);
                break;
            case JsonValueKind.Number:
                Assert.Equal(expected.GetInt64(), Assert.IsType<long>(value));
                break;
            default:
                throw new InvalidOperationException("Fixture SQLite rows support only null, text, and integer cells.");
        }
    }

    private static void AssertIntegrity(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return;
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Default,
            Pooling = false,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("ok", reader.GetString(0));
        Assert.False(reader.Read());
    }

    private static string[] ExpandArguments(JsonElement arguments, ScenarioDirectory directory) =>
        arguments.EnumerateArray()
            .Select(item => Expand(item.GetString()!, directory, index: null, paddedWidth: null))
            .ToArray();

    private static string[] ExpandIndexedArguments(
        JsonElement arguments,
        ScenarioDirectory directory,
        JsonElement generator,
        int index)
    {
        int? padWidth = generator.TryGetProperty("pad_width", out var width) ? width.GetInt32() : null;
        return arguments.EnumerateArray()
            .Select(item => Expand(item.GetString()!, directory, index, padWidth))
            .ToArray();
    }

    private static string Expand(string text, ScenarioDirectory directory, int? index, int? paddedWidth)
    {
        var result = text
            .Replace("${DB}", directory.DatabasePath, StringComparison.Ordinal)
            .Replace("${MISSING_PARENT}", directory.MissingParentPath, StringComparison.Ordinal);
        if (index is int actorIndex)
        {
            result = result
                .Replace("${i}", actorIndex.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("${n}", (actorIndex + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            if (paddedWidth is int width)
            {
                result = result.Replace(
                    "${padded_n}",
                    (actorIndex + 1).ToString($"D{width}", CultureInfo.InvariantCulture),
                    StringComparison.Ordinal);
            }
        }

        return result;
    }

    private static string ReadKey(JsonElement item)
    {
        if (item.TryGetProperty("key", out var key))
        {
            return key.GetString()!;
        }

        var generator = item.GetProperty("key_generator");
        Assert.Equal("repeat_suffix", generator.GetProperty("kind").GetString());
        var result = generator.GetProperty("prefix").GetString()! +
            new string(
                generator.GetProperty("character").GetString()![0],
                generator.GetProperty("count").GetInt32());
        Assert.Equal(result.Length, Encoding.UTF8.GetByteCount(result));
        return result;
    }

    private static string ReadGeneratedJson(JsonElement item, string directName, string generatorName)
    {
        if (item.TryGetProperty(directName, out var direct))
        {
            return direct.GetString()!;
        }

        var generator = item.GetProperty(generatorName);
        return generator.GetProperty("kind").GetString() switch
        {
            "nested_arrays" => BuildNestedArrayJson(generator),
            "ascii_string_total_bytes" => BuildAsciiStringJson(generator),
            _ => throw new InvalidOperationException($"Unknown JSON generator: {generator.GetRawText()}"),
        };
    }

    private static JsonElement ReadGeneratedValue(JsonElement item, string directName, string generatorName)
    {
        if (item.TryGetProperty(directName, out var direct))
        {
            return direct.Clone();
        }

        var generator = item.GetProperty(generatorName);
        var json = generator.GetProperty("kind").GetString() switch
        {
            "nested_arrays" => BuildNestedArrayJson(generator),
            "ascii_string_total_bytes" => BuildAsciiStringJson(generator),
            _ => throw new InvalidOperationException($"Unknown normalized generator: {generator.GetRawText()}"),
        };
        return ParseJson(json);
    }

    private static string BuildNestedArrayJson(JsonElement generator)
    {
        var depth = generator.GetProperty("depth").GetInt32();
        var json = generator.GetProperty("leaf").GetRawText();
        for (var index = 0; index < depth; index++)
        {
            json = $"[{json}]";
        }

        Assert.Equal(depth, CountArrayDepth(json));
        return json;
    }

    private static string BuildAsciiStringJson(JsonElement generator)
    {
        var character = generator.GetProperty("character").GetString()!;
        var totalBytes = generator.GetProperty("total_bytes").GetInt32();
        Assert.Equal(1, Encoding.UTF8.GetByteCount(character));
        var json = $"\"{new string(character[0], totalBytes - 2)}\"";
        Assert.Equal(totalBytes, Encoding.UTF8.GetByteCount(json));
        return json;
    }

    private static int CountArrayDepth(string json)
    {
        using var document = JsonDocument.Parse(json);
        var depth = 0;
        var value = document.RootElement;
        while (value.ValueKind == JsonValueKind.Array)
        {
            depth++;
            Assert.Equal(1, value.GetArrayLength());
            value = value[0];
        }

        return depth;
    }

    private static void AssertSetResult(JsonElement envelope, string key, JsonElement value, long revision, bool created)
    {
        Assert.True(envelope.GetProperty("ok").GetBoolean());
        var result = envelope.GetProperty("result");
        Assert.Equal(key, result.GetProperty("key").GetString());
        JsonContractAssertions.AssertSemanticEqual(value, result.GetProperty("value"));
        Assert.Equal(revision, result.GetProperty("revision").GetInt64());
        Assert.Equal(created, result.GetProperty("created").GetBoolean());
    }

    private static void AssertEntryResult(JsonElement envelope, string key, JsonElement value, long revision)
    {
        Assert.True(envelope.GetProperty("ok").GetBoolean());
        var result = envelope.GetProperty("result");
        Assert.Equal(key, result.GetProperty("key").GetString());
        JsonContractAssertions.AssertSemanticEqual(value, result.GetProperty("value"));
        Assert.Equal(revision, result.GetProperty("revision").GetInt64());
    }

    private static void AssertError(JsonElement envelope, string category, JsonElement details)
    {
        Assert.False(envelope.GetProperty("ok").GetBoolean());
        var error = envelope.GetProperty("error");
        Assert.Equal(category, error.GetProperty("category").GetString());
        JsonContractAssertions.AssertSemanticEqual(details, error.GetProperty("details"));
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static JsonDocument OpenFixture(string path, string kind)
    {
        var document = JsonDocument.Parse(File.ReadAllBytes(path));
        Assert.Equal(kind, document.RootElement.GetProperty("kind").GetString());
        Assert.Equal("1.0.0", document.RootElement.GetProperty("spec_version").GetString());
        return document;
    }

    private static string SpecPath(string relativePath) =>
        System.IO.Path.Combine(SpecManifestVerifier.Root, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    private sealed record CommandOutcome(ProcessResult Result, JsonElement Envelope);
}
