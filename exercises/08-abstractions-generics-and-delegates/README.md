# 🧪 Exercise 08 · Abstractions, generics, and delegates

## 🎯 Goal
Implement a small generic catalog that depends on an injected rule
abstraction and an optional audit delegate, so the shared tests pass against
the starter project.

## 📎 Related lesson
Read [Lesson 08 · Abstractions, generics, and delegates](../../lessons/08-abstractions-generics-and-delegates/README.md)
first, especially its 🧪 Practice contract section, before you start coding.

## 🗂️ Your task
`starter/IKeyedItem.cs` and `starter/IRule.cs` are already complete
interfaces (with XML doc comments) — they need no changes.

### `starter/CourseCard.cs` — `CourseCard` sealed record implementing `IKeyedItem`
- Constructor: reject missing/blank `key` or `title`.
- Constructor: store the trimmed values.

### `starter/CuratedCatalog.cs` — `CuratedCatalog<T> where T : class, IKeyedItem`
- Constructor: require a non-null `IRule<T> rule`, throwing
  `ArgumentNullException` when it is missing.
- Constructor: accept the optional `Action<string>? audit` callback and own
  the catalog's item storage.
- `Count`: report how many items are currently stored.
- `Add(T item)`: reject a `null` item with `ArgumentNullException`.
- `Add(T item)`: consult the injected rule and throw
  `InvalidOperationException` when it rejects the item.
- `Add(T item)`: otherwise store the item and, only for that accepted item,
  invoke `audit` (when supplied) with `Added <key>`.
- `FindByKey(string key)`: validate `key`, match stored items'
  keys case-insensitively, and return `null` when no item matches.
- `Map<TResult>(Func<T, TResult> selector)`: validate `selector` and return
  the projected results in the catalog's insertion order.
- `RemoveWhere(Func<T, bool> predicate, Action<T>? onRemoved = null)`:
  validate `predicate`, remove only the items it matches, invoke
  `onRemoved` (when supplied) once per removed item, and return the number
  removed.

### Edge cases the shared tests exercise
- Duplicate keys that differ only by casing.
- `RemoveWhere` matching zero items.
- Projecting to a different result type (for example `string` lengths or a
  `bool` flag).
- An audit or `Map` callback that is a closure capturing outside state.

## 🏁 Done when
- `dotnet test` against `tests/` passes fully with the `starter/`
  implementation selected (the default), with no changes to the tests,
  member signatures, or exception messages.
- The same tests still pass when the reference `solution/` project is
  selected instead.

## ▶️ Feedback commands
Work against the starter first:

```bash
dotnet build exercises/08-abstractions-generics-and-delegates/starter/AbstractionsGenericsDelegatesPractice.csproj
dotnet test --project exercises/08-abstractions-generics-and-delegates/tests/AbstractionsGenericsDelegatesPractice.Tests.csproj
dotnet watch test --project exercises/08-abstractions-generics-and-delegates/tests/AbstractionsGenericsDelegatesPractice.Tests.csproj
```

Compare with the reference solution only after a genuine attempt:

```bash
dotnet build exercises/08-abstractions-generics-and-delegates/solution/AbstractionsGenericsDelegatesPractice.csproj
dotnet test --project exercises/08-abstractions-generics-and-delegates/tests/AbstractionsGenericsDelegatesPractice.Tests.csproj -p:CourseImplementation=Solution
```
