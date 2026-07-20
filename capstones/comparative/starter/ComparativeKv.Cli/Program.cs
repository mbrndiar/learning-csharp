using System.Buffers;
using System.Text;
using System.Text.Json;
using ComparativeKv.Core;

namespace ComparativeKv.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return CommandLine.Run(args, Console.Out);
    }
}
