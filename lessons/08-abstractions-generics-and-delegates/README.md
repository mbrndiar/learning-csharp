# 🧭 Lesson 08 · Abstractions, generics, and delegates

## 🎯 Objectives
- Use interfaces to depend on capabilities instead of concrete implementations.
- Use generic types and constraints to reuse behavior safely.
- Compose behavior with injected abstractions instead of hard-coded details.
- Use `Func`, `Action`, lambdas, and closures for small custom behavior.
- Explain when to substitute a dependency versus when to reuse a generic component.

## ✅ Prerequisites
Before this lesson, you should be comfortable with:
- classes, records, and constructors from Lesson 07
- collection basics
- reading a method signature that returns a generic type like `List<T>`

## 🧠 Causal mental model
An **abstraction** is a smaller promise than a concrete class.

- An **interface** says what a collaborator can do.
- A **generic type** says the algorithm is reusable across many compatible types.
- A **constraint** says what a generic parameter must be able to do.
- A **delegate** says "someone else will supply this bit of behavior later".

Use an interface when you want to swap collaborators. Use a generic type when the same structure should work for many data types. Use a delegate when the variation is tiny and local.

## 🔤 Authentic minimal fragments
An interface describes a capability:

```csharp
public interface IKeyedItem
{
    string Key { get; }
}
```

A generic class can reuse one algorithm for many item types:

```csharp
public sealed class CuratedCatalog<T> where T : class, IKeyedItem
{
}
```

A delegate injects a small custom step:

```csharp
Action<string> audit = message => Console.WriteLine(message);
```

## ▶️ Demonstration project
The demonstration lives here:
- `lessons/08-abstractions-generics-and-delegates/CurationConsole/CurationConsole.csproj`

It contains:
- `IKeyedItem` and `IRule<T>` interfaces
- `CuratedCatalog<T>` generic class
- `CourseCard` as a concrete type that satisfies the constraint
- lambdas for projection and auditing

### Commands from the repository root
Build the demonstration:

```bash
dotnet build lessons/08-abstractions-generics-and-delegates/CurationConsole/CurationConsole.csproj
```

Run it:

```bash
dotnet run --project lessons/08-abstractions-generics-and-delegates/CurationConsole/CurationConsole.csproj
```

### Expected output

```text
Approved count: 2
Keys: cs-basics, linq-lab
Titles: C# Basics | LINQ Lab
Audit entries: Added cs-basics; Added linq-lab
```

## 👀 What to notice
- The collection depends on `IRule<T>`, not on a specific rule class.
- The generic constraint guarantees every stored item has a `Key`.
- `Map` uses a `Func<T, TResult>` so callers choose the projection.
- The audit callback uses a closure: it captures a list defined outside the collection.

## 🧠 When to substitute and when to reuse
- **Substitute with an interface** when you need a different collaborator with the same job.
- **Reuse with generics** when the container or algorithm is the same but the data type changes.
- **Use a delegate** when variation is one tiny behavior and creating a whole type would be overkill.

A common mistake is using inheritance first. In this lesson, composition is usually simpler: the collection owns a rule and delegates small custom behaviors outward.

## 🔤 Documenting a public contract with XML comments
A public interface or class is a promise to every future caller, so it is worth
documenting directly in the source with an XML `/// <summary>` comment:

```csharp
/// <summary>
/// Decides whether an item is allowed into the catalog.
/// </summary>
public interface IRule<in T>
{
    bool Accepts(T item);
}
```

Your editor and IDE read these comments to show hover tooltips, parameter
help, and IntelliSense as soon as you type a member name - that tooling value
is the main beginner-relevant payoff. Generating a published, shareable API
reference document from these comments (for example with a documentation
generator) is out of scope for this course: nothing here builds or ships a
reusable package, so there is no external audience for generated reference
docs yet.

## 🧩 Experiment
Try one change at a time:
1. Write a stricter rule and see which items stop being accepted.
2. Change the `Map` projection from keys to title lengths.
3. Capture a local counter in the audit lambda and observe the closure.
4. Remove the `where T : IKeyedItem` constraint and notice what code breaks.

## ⚠️ Common mistakes and diagnosis
- **Mistake:** depending directly on a concrete helper class.
  - **Diagnosis:** swapping behavior becomes harder than it needs to be.
- **Mistake:** using generics without a constraint when the algorithm needs a member such as `Key`.
  - **Diagnosis:** the compiler cannot prove the generic item has the required shape.
- **Mistake:** overusing interfaces for one tiny custom expression.
  - **Diagnosis:** you create unnecessary types where a lambda would be clearer.
- **Mistake:** mutating shared state casually inside closures.
  - **Diagnosis:** debugging becomes confusing because the lambda remembers outside variables.
- **Mistake:** hard-coding selection logic inside the reusable component.
  - **Diagnosis:** callers must edit the class instead of supplying behavior.

## 🧪 Practice contract
Implement `CuratedCatalog<T>` and its supporting abstractions in `AbstractionsGenericsDelegatesPractice`.

### Required types
- `IKeyedItem` with `Key`
- `IRule<in T>` with `Accepts(T item)`
- `CourseCard` record implementing `IKeyedItem`
- `CuratedCatalog<T>` constrained to `class, IKeyedItem`

### Required behavior
- constructor requires a non-null rule and accepts an optional audit `Action<string>`
- `Add(T item)` rejects null items, throws `InvalidOperationException` when the rule rejects an item, stores accepted items, and writes `Added <key>` to the audit callback
- `Count` returns the number of stored items
- `FindByKey(key)` matches keys case-insensitively and returns `null` when absent
- `Map<TResult>(selector)` projects stored items in insertion order
- `RemoveWhere(predicate, onRemoved)` removes matching items, calls the optional callback once per removed item, and returns the number removed

### Constraints
- validate required text and delegates
- do not expose the mutable backing list
- preserve insertion order
- keep rule checking and callbacks deterministic

### Edge cases to handle
- duplicate keys with different casing
- removing zero items
- projecting to a different type such as `int`
- using a closure-backed audit log

### Feedback commands
Build the starter project:

```bash
dotnet build exercises/08-abstractions-generics-and-delegates/starter/AbstractionsGenericsDelegatesPractice.csproj
```

Build the reference solution:

```bash
dotnet build exercises/08-abstractions-generics-and-delegates/solution/AbstractionsGenericsDelegatesPractice.csproj
```

Run the tests against the starter implementation while you work:

```bash
dotnet test --project exercises/08-abstractions-generics-and-delegates/tests/AbstractionsGenericsDelegatesPractice.Tests.csproj
```

Run the tests against the finished solution:

```bash
dotnet test --project exercises/08-abstractions-generics-and-delegates/tests/AbstractionsGenericsDelegatesPractice.Tests.csproj -p:CourseImplementation=Solution
```

## 📝 Summary
Interfaces let you swap collaborators, generic constraints let you reuse code safely, and delegates let callers provide tiny pieces of behavior. Together they make code more flexible without forcing you into inheritance-heavy designs.

## ❓ Review questions
1. What problem does an interface solve that a concrete class does not?
2. Why is a generic constraint useful in a reusable component?
3. When is a lambda simpler than creating a whole new class?
4. What is a closure, and what should you be careful about when using one?
5. How does composition help testing and substitution?
6. What does an XML `/// <summary>` comment give you immediately in the editor, and why is generating a published API reference out of scope here?

## 📚 Microsoft Learn links
- https://learn.microsoft.com/dotnet/csharp/programming-guide/interfaces/
- https://learn.microsoft.com/dotnet/csharp/programming-guide/generics/
- https://learn.microsoft.com/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters
- https://learn.microsoft.com/dotnet/csharp/programming-guide/delegates/
- https://learn.microsoft.com/dotnet/csharp/language-reference/operators/lambda-expressions
- https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/
