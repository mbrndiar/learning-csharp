namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;

public sealed record CatalogOptions
{
    public const string SectionName = "Catalog";

    public int MaxResults { get; init; } = 20;
}
