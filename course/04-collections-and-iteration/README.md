# Unit 04: Collections and iteration

## Measurable objectives
By the end of this unit, you can:
- explain the difference between an array, a `List<T>`, a `Dictionary<TKey, TValue>`, and a `HashSet<T>`;
- read and write collection elements by index where indexing is available;
- iterate through a collection and summarize what happened;
- predict the difference between mutating an existing collection and creating a copy.

## Explicit prerequisites
- Units 01-03.
- Comfort reading loops, conditions, and string output.

## Plain-language causal mental model
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

## Samples
- `Samples/collections-overview.cs`
- `Samples/mutation-vs-copying.cs`

## Exact run commands from repository root
```bash
dotnet course/04-collections-and-iteration/Samples/collections-overview.cs
dotnet course/04-collections-and-iteration/Samples/mutation-vs-copying.cs
dotnet build course/04-collections-and-iteration/Practice/Starter/CollectionsAndIteration.csproj
dotnet test --project course/04-collections-and-iteration/Practice/Tests/CollectionsAndIteration.Tests.csproj
dotnet test --project course/04-collections-and-iteration/Practice/Tests/CollectionsAndIteration.Tests.csproj -p:CourseImplementation=Starter
```

## Expected observable behavior
`collections-overview.cs` prints an indexed array item, a list count, dictionary lookups, and the size of a set after duplicate values are added. `mutation-vs-copying.cs` shows that changing a copied list does not rewrite the original array.

## Learner experiment
1. Run `dotnet course/04-collections-and-iteration/Samples/mutation-vs-copying.cs`.
2. Predict what will happen if you change `copiedList[0]` to a new value.
3. Make the edit and rerun.
4. Compare the printed array value with the list value.
5. Explain why only one changed.

## Common mistakes with diagnosis
- **Mistake:** Assuming an array automatically grows like a list.
  **Diagnosis:** Fixed-size containers cannot add new slots after creation.
- **Mistake:** Forgetting to trim or skip blank values before counting.
  **Diagnosis:** Your dictionary ends up with misleading keys such as `""` or extra spaces.
- **Mistake:** Believing a copy and the original are the same object.
  **Diagnosis:** Mutating the copy does not affect the original collection if you created a separate container.

## Practice contract
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

## Feedback instructions
- Build the starter project: `dotnet build course/04-collections-and-iteration/Practice/Starter/CollectionsAndIteration.csproj`
- Test your work: `dotnet test --project course/04-collections-and-iteration/Practice/Tests/CollectionsAndIteration.Tests.csproj -p:CourseImplementation=Starter`
- If counts are wrong, print the normalized value for each loop pass.
- If duplicate tests fail, check whether you are adding the same normalized value more than once.

## Concise summary
Collections store groups of values, but each collection type makes different trade-offs. Iteration lets you inspect or transform each item, and copying protects the original data from later mutations.

## Review questions
1. When is an array a better mental model than a list?
2. What problem does a dictionary solve?
3. Why is a hash set useful for duplicates?
4. What is the practical difference between mutation and copying?
5. Why can trimming input change dictionary counts?

## Authoritative Microsoft Learn references
- <https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/arrays>
- <https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1>
- <https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2>
- <https://learn.microsoft.com/dotnet/api/system.collections.generic.hashset-1>
