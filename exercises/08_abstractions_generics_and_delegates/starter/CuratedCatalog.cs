namespace AbstractionsGenericsDelegatesPractice;

public sealed class CuratedCatalog<T> where T : class, IKeyedItem
{
    public CuratedCatalog(IRule<T> rule, Action<string>? audit = null) =>
        throw new NotImplementedException("Store the abstraction-based collaborators.");

    public int Count => throw new NotImplementedException("Return the number of stored items.");

    public void Add(T item) => throw new NotImplementedException("Validate the item, apply the rule, and audit successful additions.");

    public T? FindByKey(string key) => throw new NotImplementedException("Find an item by key, ignoring case.");

    public IReadOnlyList<TResult> Map<TResult>(Func<T, TResult> selector) =>
        throw new NotImplementedException("Project the stored items in insertion order.");

    public int RemoveWhere(Func<T, bool> predicate, Action<T>? onRemoved = null) =>
        throw new NotImplementedException("Remove matching items and return how many were removed.");
}
