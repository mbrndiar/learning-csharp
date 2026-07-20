using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Application;
using ComparativeKv.Core;
using ComparativeKv.Storage.Sqlite;

namespace ComparativeKv.Cli;

public enum CommandKind
{
    Set,
    Get,
    Delete,
    List,
}
