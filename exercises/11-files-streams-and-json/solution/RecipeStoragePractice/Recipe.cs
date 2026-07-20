namespace LearningCSharp.Course.Unit11.Practice;

public sealed record Recipe(string Slug, string Title, IReadOnlyList<string> Ingredients);
