using Tasks.Client;

(string? transportName, string[] rest) = ExtractTransport(args);

TransportFactory? factory = transportName switch
{
    "raw" => TaskTransports.CreateRaw,
    "typed" => TaskTransports.CreateTyped,
    _ => null,
};

if (factory is null)
{
    await Console.Error.WriteLineAsync("usage: --transport must be raw or typed").ConfigureAwait(false);
    return ClientApplication.ExitUsage;
}

return await ClientApplication.RunAsync(
    rest,
    factory,
    Console.Out,
    Console.Error,
    prog: "tasks-cli",
    CancellationToken.None).ConfigureAwait(false);

// Selecting the transport is a host concern, so the shared application never
// sees the --transport option; the remaining arguments follow the CLI contract.
static (string? Transport, string[] Remaining) ExtractTransport(string[] arguments)
{
    string transport = "raw";
    var rest = new List<string>(arguments.Length);
    for (int index = 0; index < arguments.Length; index++)
    {
        string token = arguments[index];
        if (token.StartsWith("--transport=", StringComparison.Ordinal))
        {
            transport = token["--transport=".Length..];
            continue;
        }

        if (string.Equals(token, "--transport", StringComparison.Ordinal))
        {
            if (index + 1 >= arguments.Length)
            {
                return (null, []);
            }

            transport = arguments[++index];
            continue;
        }

        rest.Add(token);
    }

    return (transport is "raw" or "typed" ? transport : null, rest.ToArray());
}
