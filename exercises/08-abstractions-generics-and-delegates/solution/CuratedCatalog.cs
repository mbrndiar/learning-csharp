namespace AbstractionsGenericsDelegatesPractice;

public sealed class CuratedCatalog<T> where T : class, IKeyedItem
{
    private readonly Action<string> audit;
    private readonly List<T> items = [];
    private readonly IRule<T> rule;

    public CuratedCatalog(IRule<T> rule, Action<string>? audit = null)
    {
        this.rule = rule ?? throw new ArgumentNullException(nameof(rule));
        this.audit = audit ?? (_ => { });
    }

    public int Count => items.Count;

    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!rule.Accepts(item))
        {
            throw new InvalidOperationException("The rule rejected the item.");
        }

        items.Add(item);
        audit($"Added {item.Key}");
    }

    public T? FindByKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var normalizedKey = key.Trim();

        foreach (var item in items)
        {
            if (string.Equals(item.Key, normalizedKey, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    public IReadOnlyList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var results = new List<TResult>(items.Count);

        foreach (var item in items)
        {
            results.Add(selector(item));
        }

        return results;
    }

    public int RemoveWhere(Func<T, bool> predicate, Action<T>? onRemoved = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var removed = 0;

        for (var index = items.Count - 1; index >= 0; index--)
        {
            var item = items[index];
            if (!predicate(item))
            {
                continue;
            }

            items.RemoveAt(index);
            onRemoved?.Invoke(item);
            removed++;
        }

        return removed;
    }
}
