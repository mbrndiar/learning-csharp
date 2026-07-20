using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;

namespace ComparativeKv.ProcessSupport;

public sealed class ProcessSupportMarker;

public static class Program
{
    public static Task<int> Main(string[] args) =>
        args.Length == 0
            ? Task.FromResult(64)
            : args[0] switch
            {
                "barrier" => RunBarrierAsync(args[1..]),
                "lock" => RunLockAsync(args[1..]),
                _ => Task.FromResult(64),
            };

    private static async Task<int> RunBarrierAsync(string[] args)
    {
        var separator = Array.IndexOf(args, "--");
        if (!TryReadOption(args, "--ready", out var ready) ||
            !TryReadOption(args, "--release", out var release) ||
            separator < 0 ||
            separator == args.Length - 1)
        {
            return 64;
        }

        await File.WriteAllTextAsync(ready, "ready", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).ConfigureAwait(false);
        await WaitForFileAsync(release).ConfigureAwait(false);

        var startInfo = new ProcessStartInfo
        {
            FileName = args[separator + 1],
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        for (var index = separator + 2; index < args.Length; index++)
        {
            startInfo.ArgumentList.Add(args[index]);
        }

        using var child = new Process { StartInfo = startInfo };
        if (!child.Start())
        {
            return 70;
        }

        var stdout = child.StandardOutput.BaseStream.CopyToAsync(Console.OpenStandardOutput());
        var stderr = child.StandardError.BaseStream.CopyToAsync(Console.OpenStandardError());
        await child.WaitForExitAsync().ConfigureAwait(false);
        await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        return child.ExitCode;
    }

    private static async Task<int> RunLockAsync(string[] args)
    {
        if (!TryReadOption(args, "--db", out var database) ||
            !TryReadOption(args, "--ready", out var ready) ||
            !TryReadOption(args, "--release", out var release))
        {
            return 64;
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = database,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            DefaultTimeout = 15,
            Pooling = false,
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        Execute(connection, "PRAGMA busy_timeout = 10000");
        Execute(connection, "PRAGMA journal_mode = WAL");
        Execute(connection, "PRAGMA foreign_keys = ON");
        Execute(connection, "BEGIN IMMEDIATE");
        try
        {
            await File.WriteAllTextAsync(ready, "ready", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)).ConfigureAwait(false);
            await WaitForFileAsync(release).ConfigureAwait(false);
        }
        finally
        {
            Execute(connection, "ROLLBACK");
        }

        return 0;
    }

    private static bool TryReadOption(string[] args, string option, out string value)
    {
        value = string.Empty;
        var index = -1;
        for (var candidate = 0; candidate < args.Length; candidate++)
        {
            if (string.Equals(args[candidate], option, StringComparison.Ordinal))
            {
                if (index >= 0 || candidate == args.Length - 1)
                {
                    return false;
                }

                index = candidate;
            }
        }

        if (index < 0)
        {
            return false;
        }

        value = args[index + 1];
        return true;
    }

    private static async Task WaitForFileAsync(string path)
    {
        while (!File.Exists(path))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);
        }
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
