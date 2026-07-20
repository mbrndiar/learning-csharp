using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Core;

namespace ComparativeKv.Cli;

public enum CommandKind
{
    Set,
    Get,
    Delete,
    List,
}
