using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Core;

namespace ComparativeKv.Cli;

public sealed record ParsedCommand(
    string DatabasePath,
    CommandKind Kind,
    string? Key,
    string? ValueJson,
    string? Expectation);
