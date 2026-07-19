namespace LearningCSharp.Course.Unit13.Practice.Api;

public sealed record CatalogOptions
{
    public const string SectionName = "Catalog";

    public int MaxResults { get; init; } = 20;
}
