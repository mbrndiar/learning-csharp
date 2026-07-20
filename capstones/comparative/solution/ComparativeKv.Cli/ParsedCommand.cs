using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Application;
using ComparativeKv.Core;
using ComparativeKv.Storage.Sqlite;

namespace ComparativeKv.Cli;

public sealed record ParsedCommand(
    string DatabasePath,
    CommandKind Kind,
    string? Key,
    string? ValueJson,
    string? Expectation);
