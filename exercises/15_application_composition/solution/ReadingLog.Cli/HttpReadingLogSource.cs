using System.Net.Http;
using System.Net.Http.Json;
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
    public async Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        using HttpResponseMessage response = await httpClient.GetAsync(location, cancellationToken);
        response.EnsureSuccessStatusCode();

        ReadingEntry[]? entries = await response.Content.ReadFromJsonAsync<ReadingEntry[]>(cancellationToken: cancellationToken);
        return entries ?? throw new InvalidDataException("The reading log service returned an empty response.");
    }
}
