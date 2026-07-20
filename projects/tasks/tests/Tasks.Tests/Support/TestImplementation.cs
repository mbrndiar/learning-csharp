using System.Reflection;

namespace Tasks.Tests.Support;

/// <summary>
/// Exposes which implementation tree the suite is compiled against. Test
/// selection is done at compile time via the <c>CourseImplementation</c> and
/// <c>TasksMilestone</c> MSBuild properties, so there is no runtime skipping.
/// This value is only used to locate the matching CLI assembly for process tests.
/// </summary>
public static class TestImplementation
{
    /// <summary>The selected implementation name ("Starter" or "Solution").</summary>
    public static string Name { get; } = Read();

    private static string Read()
    {
        foreach (AssemblyMetadataAttribute attribute in
                 typeof(TestImplementation).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (string.Equals(attribute.Key, "CourseImplementation", StringComparison.Ordinal))
            {
                return attribute.Value ?? "Solution";
            }
        }

        return "Solution";
    }
}
