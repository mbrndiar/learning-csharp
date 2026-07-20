namespace LearningCSharp.Course.Unit11.Practice;

public sealed record RecipeCatalog(IReadOnlyList<Recipe> Recipes)
{
    public static RecipeCatalog Empty { get; } = new(Array.Empty<Recipe>());
}
