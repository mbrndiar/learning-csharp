# 🧭 Lesson 04 · Collections and iteration

## 🎯 Measurable objectives
By the end of this lesson, you can:
- explain the difference between an array, a `List<T>`, a `Dictionary<TKey, TValue>`, and a `HashSet<T>`;
- read and write collection elements by index where indexing is available;
- iterate through a collection and summarize what happened;
- predict the difference between mutating an existing collection and creating a copy;
- explain aliasing: two variable names that refer to the same mutable collection object.

## ✅ Explicit prerequisites
- Lessons 01-03.
- Comfort reading loops, conditions, and string output.

## 🧠 Plain-language causal mental model
Collections are containers with different promises. An **array** has fixed slots. A **list** can grow or shrink. A **dictionary** maps keys to values for lookup. A **hash set** remembers uniqueness. Iteration is how you visit each stored item. Mutation changes an existing container; copying gives you a separate container so later edits do not change the original data source.

Authentic fragment:

```csharp
string[] rawItems = new string[] { " apple ", "", "Pear" };
List<string> cleaned = new List<string>();

foreach (string item in rawItems)
{
    if (!string.IsNullOrWhiteSpace(item))
    {
        cleaned.Add(item.Trim());
    }
}
```

The loop causes each array element to be inspected, and the list grows only when the condition passes.

Aliasing versus copying is a separate idea from mutation versus reading. When
you write `List<string> aliasList = copiedList;`, you do not create a new
list - you create a second name for the exact same mutable object. Changing an
item through either name is visible through both:

```csharp
List<string> copiedList = new List<string>(original);
List<string> aliasList = copiedList;

aliasList[0] = "amber";
Console.WriteLine(copiedList[0]); // "amber" - same underlying list
```

`new List<string>(original)` is different: it allocates a new list and copies
each element reference into it, so `copiedList` and `original` are separate
containers from that point on. That copy is **shallow** - if the elements
were mutable reference types, the copy would share the same element objects,
even though the two containers (the arrays/lists themselves) are distinct.
`List<T>` equality (`==` and `.Equals`) is reference identity: two lists with
identical contents are only "equal" if they are literally the same object.
Use `Enumerable.SequenceEqual` when you want to compare the elements of two
possibly-different list instances.

## ▶️ Demonstrations
- `01-CollectionsOverview.cs`
- `02-MutationVsCopying.cs`

## ▶️ Exact run commands from repository root
```bash
dotnet lessons/04-collections-and-iteration/01-CollectionsOverview.cs
dotnet lessons/04-collections-and-iteration/02-MutationVsCopying.cs
dotnet build exercises/04-collections-and-iteration/starter/CollectionsAndIteration.csproj
dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj
dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj -p:CourseImplementation=Solution
```

## 👀 Expected observable behavior
`01-CollectionsOverview.cs` prints an indexed array item, a list count, dictionary lookups, and the size of a set after duplicate values are added. `02-MutationVsCopying.cs` shows that changing a copied list does not rewrite the original array, and then shows an alias observation: a second name for the same list (`aliasList = copiedList`) reflects mutations made through either name, while `copiedList.SequenceEqual(original)` becomes `false` once the copy diverges from the source.

## 🧩 Learner experiment
1. Run `dotnet lessons/04-collections-and-iteration/02-MutationVsCopying.cs`.
2. Predict what will happen if you change `copiedList[0]` to a new value.
3. Make the edit and rerun.
4. Compare the printed array value with the list value.
5. Explain why only one changed.
6. Add `List<string> aliasList = copiedList;` and mutate `aliasList` instead. Predict whether `copiedList` changes too, then run and check `ReferenceEquals(copiedList, aliasList)`.
7. Explain in one sentence why aliasing is not the same risk as copying.

## ⚠️ Common mistakes with diagnosis
- **Mistake:** Assuming an array automatically grows like a list.
  **Diagnosis:** Fixed-size containers cannot add new slots after creation.
- **Mistake:** Forgetting to trim or skip blank values before counting.
  **Diagnosis:** Your dictionary ends up with misleading keys such as `""` or extra spaces.
- **Mistake:** Believing a copy and the original are the same object.
  **Diagnosis:** Mutating the copy does not affect the original collection if you created a separate container.
- **Mistake:** Assigning a list to a second variable and expecting an implicit copy.
  **Diagnosis:** `List<string> aliasList = copiedList;` copies only the reference; both names point at the same mutable list, so mutating either one is visible through both.
- **Mistake:** Comparing two lists with `==` and expecting element-by-element equality.
  **Diagnosis:** `List<T>` equality is reference identity; use `SequenceEqual` to compare elements between two distinct list instances.

## 🧪 Practice contract
Implement:
- `CollectionPractice.CopyWithoutBlanks(string[] items)`
- `CollectionPractice.CountItems(List<string> items)`
- `CollectionPractice.FindDuplicates(string[] items)`

Contract details:
- **Inputs:** arrays or lists of strings
- **Outputs:** a cleaned list, a count dictionary, and a duplicate set
- **Constraints:** trim each non-blank item; preserve order in the copied list; normalize dictionary and set keys to lower-case trimmed text
- **Edge cases:** empty input, all blanks, repeated values with different casing such as `Apple` and `apple`
- **Invalid case:** `null` input throws `ArgumentNullException`

See [exercises/04-collections-and-iteration/README.md](../../exercises/04-collections-and-iteration/README.md) for the complete task, edge cases, and Starter-first build/test/watch commands.

## 🔁 Feedback instructions
- Build the starter project: `dotnet build exercises/04-collections-and-iteration/starter/CollectionsAndIteration.csproj`
- Test your work: `dotnet test --project exercises/04-collections-and-iteration/tests/CollectionsAndIteration.Tests.csproj`
- If counts are wrong, print the normalized value for each loop pass.
- If duplicate tests fail, check whether you are adding the same normalized value more than once.

## 📝 Concise summary
Collections store groups of values, but each collection type makes different trade-offs. Iteration lets you inspect or transform each item, copying protects the original data from later mutations, and aliasing means a second variable name can silently share - and mutate - the same underlying container.

## ❓ Review questions
1. When is an array a better mental model than a list?
2. What problem does a dictionary solve?
3. Why is a hash set useful for duplicates?
4. What is the practical difference between mutation and copying?
5. Why can trimming input change dictionary counts?
6. What is the difference between `List<string> aliasList = copiedList;` and `List<string> copiedList = new List<string>(original);`?
7. Why does `SequenceEqual` sometimes report `true` when `==` reports `false` for two lists?

## 📚 Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/arrays>
- <https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1>
- <https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2>
- <https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset-1>
- <https://learn.microsoft.com/dotnet/api/system.linq.enumerable.sequenceequal>
