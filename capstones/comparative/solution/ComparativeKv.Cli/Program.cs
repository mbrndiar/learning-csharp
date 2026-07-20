using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Application;
using ComparativeKv.Core;
using ComparativeKv.Storage.Sqlite;

namespace ComparativeKv.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return CommandLine.Run(args, Console.Out);
    }
}
