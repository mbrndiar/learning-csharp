using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Application;

/// <summary>
/// Loads reading entries from wherever they live. <paramref name="location"/> is
/// deliberately adapter-specific: a file-backed implementation treats it as a
/// path, an HTTP-backed implementation treats it as a resource path against its
/// own base address. Application code depends only on this abstraction and never
/// learns which concrete adapter the composition root selected.
/// </summary>
public interface IReadingLogSource
{
    Task<IReadOnlyList<ReadingEntry>> LoadAsync(string location, CancellationToken cancellationToken);
}
