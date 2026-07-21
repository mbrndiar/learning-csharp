namespace AbstractionsGenericsDelegatesPractice;

public sealed class CuratedCatalog<T> where T : class, IKeyedItem
{
    // TODO: Implement this constructor. Reject a missing rule, then store the rule and optional audit callback and initialize this catalog's owned item storage.
    public CuratedCatalog(IRule<T> rule, Action<string>? audit = null) =>
        throw new NotImplementedException("Store the abstraction-based collaborators.");

    // TODO: Implement Count. Report the number of items currently stored in the catalog.
    public int Count => throw new NotImplementedException("Return the number of stored items.");

    // TODO: Implement Add. Reject a missing item, throw InvalidOperationException when the injected rule rejects it, otherwise store the item and invoke the optional audit callback once with "Added <key>".
    public void Add(T item) => throw new NotImplementedException("Validate the item, apply the rule, and audit successful additions.");

    // TODO: Implement FindByKey. Reject a missing key, then return the stored item whose key matches case-insensitively, or null when none matches.
    public T? FindByKey(string key) => throw new NotImplementedException("Find an item by key, ignoring case.");

    // TODO: Implement Map. Reject a missing selector, then return the projected results of the stored items in their original insertion order.
    public IReadOnlyList<TResult> Map<TResult>(Func<T, TResult> selector) =>
        throw new NotImplementedException("Project the stored items in insertion order.");

    // TODO: Implement RemoveWhere. Reject a missing predicate, remove only the items it matches, invoke the optional callback once per removed item, and return the count removed.
    public int RemoveWhere(Func<T, bool> predicate, Action<T>? onRemoved = null) =>
        throw new NotImplementedException("Remove matching items and return how many were removed.");
}
