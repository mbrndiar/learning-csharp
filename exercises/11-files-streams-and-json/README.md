# 📄 Exercise 11 · Files, streams, and JSON

## 🎯 Goal

Make `RecipeStoragePractice` turn a `RecipeCatalog` into JSON text and UTF-8
bytes and back again, while keeping every file path safe and every save
atomic.

## 🧩 Your task

Implement the members of `RecipePersistence`
(`RecipeStoragePractice/RecipePersistence.cs`):

- **`GetSafePath(rootDirectory, fileName)`**
  - Reject a blank root directory or file name (`ArgumentException`).
  - Reject rooted paths, nested paths (subdirectories), and any file name
    that is not a simple `.json` file (`ArgumentException`).
  - Return an absolute path anchored inside `rootDirectory`.
- **`SerializeToJsonText(collection)`**
  - Reject a `null` catalog.
  - Produce JSON text whose shape lets `DeserializeFromJsonText` rebuild an
    equivalent `RecipeCatalog`.
- **`SerializeToUtf8(collection)`**
  - Produce UTF-8 bytes that decode back to exactly the same JSON text
    `SerializeToJsonText` would produce for the same catalog.
- **`DeserializeFromJsonText(jsonText)`**
  - Reject blank JSON text.
  - Rebuild the original `RecipeCatalog`/`Recipe` object graph from valid
    JSON.
  - Surface malformed JSON as `InvalidDataException`, not a raw parser
    exception.
- **`DeserializeFromUtf8(utf8Json)`**
  - Reject empty byte input.
  - Decode the UTF-8 bytes and rebuild the object graph exactly like the
    text path.
- **`Load(rootDirectory, fileName)`**
  - Resolve the path through `GetSafePath` first.
  - Return `RecipeCatalog.Empty` when the file is missing or zero bytes
    long.
  - Rebuild the catalog otherwise, surfacing malformed JSON as
    `InvalidDataException`.
- **`SaveAtomically(rootDirectory, fileName, collection)`**
  - Reject a `null` catalog and make sure the destination directory exists.
  - Write through a temporary file, then replace the destination file so a
    reader never observes a partially written file.
  - Leave no `.tmp` file behind, whether the save succeeds or fails.

## ✅ Done when

- All tests in `RecipeStoragePractice.Tests` pass against your starter
  implementation.
- No `.tmp` files remain in the workspace after `SaveAtomically`.
- Malformed JSON on disk raises `InvalidDataException`, never a raw parser
  exception.

## 🔗 Related lesson

[Lesson 11 · Files, streams, and JSON](../../lessons/11-files-streams-and-json/README.md)

## ▶️ Build, test, and watch

Build the starter first:

```bash
dotnet build exercises/11-files-streams-and-json/starter/RecipeStoragePractice/RecipeStoragePractice.csproj
```

Run the shared tests against your starter implementation (the default):

```bash
dotnet test --project exercises/11-files-streams-and-json/tests/RecipeStoragePractice.Tests/RecipeStoragePractice.Tests.csproj
```

Get continuous feedback while you edit:

```bash
dotnet watch test --project exercises/11-files-streams-and-json/tests/RecipeStoragePractice.Tests/RecipeStoragePractice.Tests.csproj
```

## 🆚 Compare with the solution

After a genuine attempt, run the same tests against the reference solution:

```bash
dotnet test --project exercises/11-files-streams-and-json/tests/RecipeStoragePractice.Tests/RecipeStoragePractice.Tests.csproj -p:CourseImplementation=Solution
```
