# 🧪 Exercise 04 · Collections and iteration

## 🎯 Goal
Implement the three `CollectionPractice` methods so cleaning, counting, and
duplicate-finding all normalize text the same way while respecting the
original data.

## 📌 Your task

### `CollectionPractice.CopyWithoutBlanks(string[] items)`
- **Input:** `items`, an array of strings.
- **Validation:** throw `ArgumentNullException` when `items` is `null`.
- **Output:** a new `List<string>` containing the trimmed, non-blank items
  from `items`, in their original order.
- **Constraints:** the source array itself must not be mutated; the returned
  list must be a separate, independently mutable collection.
- **Edge cases:** an empty array returns an empty list; an array of only
  blank/whitespace entries returns an empty list.

### `CollectionPractice.CountItems(List<string> items)`
- **Input:** `items`, a list of strings.
- **Validation:** throw `ArgumentNullException` when `items` is `null`.
- **Output:** a `Dictionary<string, int>` counting each trimmed, lower-cased,
  non-blank item in `items`.
- **Edge cases:** blank/whitespace-only entries are skipped, not counted;
  entries that only differ by case or surrounding whitespace count toward the
  same key; an empty list returns an empty dictionary.

### `CollectionPractice.FindDuplicates(string[] items)`
- **Input:** `items`, an array of strings.
- **Validation:** throw `ArgumentNullException` when `items` is `null`.
- **Output:** a `HashSet<string>` containing each trimmed, lower-cased value
  from `items` that appears more than once, with each duplicate value
  appearing only once in the result.
- **Edge cases:** an empty array returns an empty set; values that only
  differ by case or surrounding whitespace count as the same value for
  duplicate detection.

## ✅ Done when
- `dotnet build exercises/04-collections-and-iteration/starter/CollectionsAndIteration.csproj`
  succeeds.
- `dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj`
  passes every test with no `-p:CourseImplementation` property.

## 📖 Matching lesson
Read [Lesson 04 · Collections and iteration](../../lessons/04-collections-and-iteration/README.md)
first, especially its Practice contract section, before you start coding.

## ▶️ Build, test, and watch (starter first)
```bash
dotnet build exercises/04-collections-and-iteration/starter/CollectionsAndIteration.csproj
dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj
dotnet watch test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj
```
`dotnet watch` reruns the shared tests every time you save the starter file,
so keep it running while you iterate. Stop it with `Ctrl+C` when you are done.

## 🔍 Comparing with the solution
Only after a genuine attempt, compare your work against the reference
implementation by passing an explicit `-p:CourseImplementation=Solution`, or
read `solution/CollectionsAndIteration.cs` directly:
```bash
dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj -p:CourseImplementation=Solution
```
