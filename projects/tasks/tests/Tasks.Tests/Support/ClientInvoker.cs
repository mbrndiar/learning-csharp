using System.Text.Json;
using Tasks.Client;

namespace Tasks.Tests.Support;

/// <summary>One captured client invocation result.</summary>
public sealed record ClientOutcome(int ExitCode, string Stdout, string Stderr);

/// <summary>Runs the shared client application with owned text streams.</summary>
public static class ClientInvoker
{
    /// <summary>Invoke one transport-neutral client command against a base URL.</summary>
    public static async Task<ClientOutcome> InvokeAsync(
        TransportFactory factory,
        string baseUrl,
        IReadOnlyList<string> args,
        CancellationToken cancellationToken)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var full = new List<string> { "--base-url", baseUrl, "--timeout", "5" };
        full.AddRange(args);
        int exitCode = await ClientApplication.RunAsync(
            full,
            factory,
            stdout,
            stderr,
            "test-tasks-client",
            cancellationToken);
        return new ClientOutcome(exitCode, stdout.ToString(), stderr.ToString());
    }
}

/// <summary>Shared assertions over the black-box HTTP contract.</summary>
public static class HttpAssertions
{
    /// <summary>Decode a JSON body after enforcing the JSON media type.</summary>
    public static JsonElement DecodeJson(ProbeResponse response)
    {
        string? contentType = response.Header("Content-Type");
        Assert.NotNull(contentType);
        Assert.Equal("application/json", contentType!.Split(';')[0].Trim(), ignoreCase: true);
        return response.Json();
    }

    /// <summary>Assert a task object has exactly the expected fields.</summary>
    public static void AssertTask(JsonElement element, long id, string title, bool completed)
    {
        Assert.Equal(JsonValueKind.Object, element.ValueKind);
        Assert.Equal(id, element.GetProperty("id").GetInt64());
        Assert.Equal(title, element.GetProperty("title").GetString());
        Assert.Equal(completed, element.GetProperty("completed").GetBoolean());
        Assert.Equal(3, element.EnumerateObject().Count());
    }

    /// <summary>Assert the full error envelope, optionally including the field detail.</summary>
    public static void AssertError(ProbeResponse response, int status, string code, string message, string? field = null)
    {
        Assert.Equal(status, response.Status);
        JsonElement element = DecodeJson(response);
        JsonElement error = element.GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetString());
        Assert.Equal(message, error.GetProperty("message").GetString());
        if (field is not null)
        {
            Assert.Equal(field, error.GetProperty("details").GetProperty("field").GetString());
        }
    }

    /// <summary>Return the parsed set of methods from an Allow header.</summary>
    public static IReadOnlySet<string> AllowMethods(ProbeResponse response)
    {
        string? allow = response.Header("Allow");
        Assert.NotNull(allow);
        return allow!
            .Split(',')
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }
}
