using CurationConsole.Abstractions;
using CurationConsole.Models;

namespace CurationConsole;

internal static class Program
{
    private static void Main()
    {
        var auditEntries = new List<string>();
        var catalog = new CuratedCatalog<CourseCard>(new StartsWithLetterRule('c'), auditEntries.Add);

        catalog.Add(new CourseCard("cs-basics", "C# Basics"));
        catalog.Add(new CourseCard("linq-lab", "LINQ Lab"));

        var keys = catalog.Map(static card => card.Key);
        var titles = catalog.Map(static card => card.Title);

        Console.WriteLine($"Approved count: {catalog.Count}");
        Console.WriteLine($"Keys: {string.Join(", ", keys)}");
        Console.WriteLine($"Titles: {string.Join(" | ", titles)}");
        Console.WriteLine($"Audit entries: {string.Join("; ", auditEntries)}");
    }

    private sealed class StartsWithLetterRule : IRule<CourseCard>
    {
        private readonly char firstLetter;

        public StartsWithLetterRule(char firstLetter) => this.firstLetter = char.ToLowerInvariant(firstLetter);

        public bool Accepts(CourseCard item) => char.ToLowerInvariant(item.Title[0]) == firstLetter || item.Key.Contains("linq", StringComparison.OrdinalIgnoreCase);
    }
}
