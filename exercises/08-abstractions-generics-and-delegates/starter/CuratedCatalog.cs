namespace AbstractionsGenericsDelegatesPractice;

public sealed class CuratedCatalog<T> where T : class, IKeyedItem
{
    // TODO: Require the rule collaborator, retain optional auditing behavior, and keep catalog state owned here.
    public CuratedCatalog(IRule<T> rule, Action<string>? audit = null) =>
        throw new NotImplementedException("Store the abstraction-based collaborators.");

    // TODO: Report the current size of the catalog-owned item collection.
    public int Count => throw new NotImplementedException("Return the number of stored items.");

    // TODO: Validate the item, consult the injected rule, and audit only after an accepted item changes catalog state.
    public void Add(T item) => throw new NotImplementedException("Validate the item, apply the rule, and audit successful additions.");

    // TODO: Validate and normalize the requested key, then search the stored items without treating key casing as distinct.
    public T? FindByKey(string key) => throw new NotImplementedException("Find an item by key, ignoring case.");

    // TODO: Validate the selector and materialize its projections in the catalog's insertion order.
    public IReadOnlyList<TResult> Map<TResult>(Func<T, TResult> selector) =>
        throw new NotImplementedException("Project the stored items in insertion order.");

    // TODO: Validate the predicate, remove only its matches, invoke the optional callback for each removal, and report the total.
    public int RemoveWhere(Func<T, bool> predicate, Action<T>? onRemoved = null) =>
        throw new NotImplementedException("Remove matching items and return how many were removed.");
}
