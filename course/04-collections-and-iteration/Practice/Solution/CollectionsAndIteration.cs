using System.Collections.Generic;

namespace LearningCSharp.CollectionsAndIteration;

public static class CollectionPractice
{
    public static List<string> CopyWithoutBlanks(string[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> cleaned = new List<string>();
        foreach (string item in items)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                cleaned.Add(item.Trim());
            }
        }

        return cleaned;
    }

    public static Dictionary<string, int> CountItems(List<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < items.Count; index++)
        {
            string current = items[index];
            if (string.IsNullOrWhiteSpace(current))
            {
                continue;
            }

            string normalized = current.Trim().ToLowerInvariant();
            if (counts.TryGetValue(normalized, out int currentCount))
            {
                counts[normalized] = currentCount + 1;
            }
            else
            {
                counts[normalized] = 1;
            }
        }

        return counts;
    }

    public static HashSet<string> FindDuplicates(string[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> duplicates = new HashSet<string>(StringComparer.Ordinal);

        foreach (string item in items)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            string normalized = item.Trim().ToLowerInvariant();
            if (!seen.Add(normalized))
            {
                duplicates.Add(normalized);
            }
        }

        return duplicates;
    }
}
