using System.Net.Http;
using LearningCSharp.Exercises.ApplicationComposition.Application;
using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

/// <summary>
/// A server/client adapter for <see cref="IReadingLogSource"/>: it fetches
/// reading entries over HTTP instead of from a local file. Only this class -
/// not Domain, not Application - references <see cref="HttpClient"/>. The
/// composition root in Program.cs decides whether this adapter or
/// <see cref="JsonReadingLogSource"/> is active.
/// </summary>
public sealed class HttpReadingLogSource(HttpClient httpClient) : IReadingLogSource
{
    public Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        _ = httpClient;

        // TODO: Implement LoadAsync so it issues a GET request against `location`
        // using the injected HttpClient, checks the status first, and
        // deserializes a JSON array of reading entries from the response body.
        throw new NotImplementedException("TODO: Fetch reading entries over HTTP and deserialize the response.");
    }
}
