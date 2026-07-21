# 🧭 Lesson 11 · Files, streams, and JSON

## 🎯 Objectives

By the end of this lesson you will be able to:

- build safe file paths without allowing accidental directory traversal;
- explain `using`, `IDisposable`, and `using var` in plain English;
- describe the chain **object -> JSON text -> UTF-8 bytes -> stream -> file**;
- read and write text and bytes intentionally instead of mixing them by accident;
- serialize and deserialize with `System.Text.Json`;
- detect malformed JSON and report it clearly;
- persist data atomically so a half-written file does not become the new truth.

## ✅ Prerequisites

Complete earlier C# basics first: variables, methods, collections, records/classes, exceptions, and arrays.
If `string`, arrays, or records still feel new, slow down before this lesson.

## 🧠 Causal mental model

Think about persistence as a pipeline:

1. **Object** - your in-memory C# values.
2. **JSON text** - characters like `{`, `"title"`, and `[`.
3. **UTF-8 bytes** - the numeric byte representation of that text.
4. **Stream** - a pipe that moves those bytes.
5. **File** - durable storage at a path.

When you load data, the flow goes backwards:

`file -> stream -> UTF-8 bytes -> JSON text -> object`

If you are confused while debugging, ask: **Which layer is wrong right now?**

- Wrong path? File layer.
- Wrong encoding? Byte/text boundary.
- Wrong JSON shape? Text/object boundary.
- File left half-written? Persistence strategy.

## 🔤 Authentic fragments

Safe path building:

```csharp
string fullRoot = Path.GetFullPath(rootDirectory);
string candidate = Path.GetFileName(fileName);

if (candidate != fileName || Path.IsPathRooted(fileName))
{
    throw new ArgumentException("Use a simple file name only.", nameof(fileName));
}

string fullPath = Path.Combine(fullRoot, candidate);
```

Object -> JSON text -> UTF-8 bytes:

```csharp
string jsonText = JsonSerializer.Serialize(collection, options);
byte[] utf8Bytes = Encoding.UTF8.GetBytes(jsonText);
```

UTF-8 bytes -> JSON text -> object:

```csharp
string jsonText = Encoding.UTF8.GetString(utf8Bytes);
RecipeCatalog collection = JsonSerializer.Deserialize<RecipeCatalog>(jsonText, options)
    ?? throw new InvalidDataException("JSON did not produce a collection.");
```

Atomic save in the same directory:

```csharp
string tempPath = destinationPath + ".tmp";
using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
{
    stream.Write(utf8Bytes);
    stream.Flush();
}

File.Move(tempPath, destinationPath, overwrite: true);
```

The braces matter here: the stream must be disposed - which releases its file
handle and flushes any buffered bytes - before `File.Move` runs. A `using`
*declaration* without braces would not be disposed until the end of the
enclosing method, so `File.Move` could otherwise run while the temporary
file is still open.

`FileStream` also reads and writes through an internal buffer, 4096 bytes by
default. That buffer size is a transfer/performance detail about how many
bytes move between the operating system and your program per underlying I/O
call; it has nothing to do with the file's total size or the shape of the
JSON text or object it is carrying.

## ▶️ Demonstration project

Run the demonstration from the repository root:

```bash
dotnet run --project lessons/11-files-streams-and-json/RecipeJournalSample/RecipeJournalSample.csproj
```

Expected behavior:

- prints the JSON text it is about to save;
- prints the UTF-8 byte count;
- writes a file under the demonstration's output folder;
- loads the file back into objects;
- shows a clear message for intentionally malformed JSON.

## 🧪 Practice contract

The matching exercise lives in
[`exercises/11-files-streams-and-json/`](../../exercises/11-files-streams-and-json/README.md).

Starter feedback (the default):

```bash
dotnet test --project exercises/11-files-streams-and-json/tests/RecipeStoragePractice.Tests/RecipeStoragePractice.Tests.csproj
```

Reference solution:

```bash
dotnet test --project exercises/11-files-streams-and-json/tests/RecipeStoragePractice.Tests/RecipeStoragePractice.Tests.csproj -p:CourseImplementation=Solution
```

Your job in `RecipeStoragePractice` is to make these behaviors true:

1. `GetSafePath` accepts only a simple `.json` file name and rejects rooted or nested paths.
2. `SerializeToJsonText` produces readable JSON for a `RecipeCatalog`.
3. `SerializeToUtf8` returns UTF-8 bytes for that JSON text.
4. `DeserializeFromJsonText` and `DeserializeFromUtf8` rebuild the original object graph.
5. `Load` returns `RecipeCatalog.Empty` when the file does not exist or is zero bytes long.
6. `Load` throws `InvalidDataException` for malformed JSON.
7. `SaveAtomically` writes to a temporary file and then replaces the destination without leaving `.tmp` files behind.

Deterministic feedback:

- If path validation is wrong, the path tests fail.
- If representation conversion is wrong, the serialization tests fail.
- If file I/O is wrong, the load/save tests fail.
- If malformed input is swallowed, the invalid-data test fails.

## 🧩 Experiment

After the demonstration works, change one thing at a time:

1. Change the saved file extension to `.txt` and notice the safe-path guard.
2. Replace one byte in the saved JSON file and run again.
3. Remove `File.Move(... overwrite: true)` and observe why replacement behavior matters.
4. Save the JSON text directly with a `StreamWriter` and compare that mental model to writing UTF-8 bytes yourself.

## ⚠️ Common mistakes and diagnosis

- **Mistake:** treating a file path as trustworthy user input.
  **Diagnosis:** paths like `../secrets.json` or `/etc/passwd` should be rejected by `GetSafePath`.

- **Mistake:** forgetting disposal.
  **Diagnosis:** files stay locked or buffered data does not flush when expected.

- **Mistake:** calling `File.Move` in the same scope as an unbraced `using` declaration for the source stream.
  **Diagnosis:** disposal timing becomes unclear; wrap the write in a braced `using (...) { }` block so the stream is closed and flushed before the move runs.

- **Mistake:** mixing text and bytes mentally.
  **Diagnosis:** you cannot explain whether you currently hold `string`, `byte[]`, or a `Stream`.

- **Mistake:** catching every exception and returning an empty result.
  **Diagnosis:** malformed JSON looks like success, which hides real bugs.

- **Mistake:** writing directly into the final file.
  **Diagnosis:** a crash in the middle can leave corrupted JSON as the only copy.

## 📝 Summary

Files are durable, streams move bytes, UTF-8 turns text into bytes, and JSON turns objects into text.
Safe persistence is about respecting every boundary deliberately.

## ❓ Review questions

1. What problem does `using` solve?
2. Why is UTF-8 a separate idea from JSON?
3. When would you choose bytes over text APIs?
4. Why should a malformed JSON file not silently become an empty object?
5. Why is writing a temp file and moving it safer than overwriting directly?
6. Why must the temporary stream be disposed before `File.Move` runs, and how does a braced `using` block guarantee that ordering?
7. What does `FileStream`'s default 4096-byte buffer actually describe, and what does it not describe?

## 📚 Official Microsoft Learn links

- [Use `using` and `IDisposable`](https://learn.microsoft.com/dotnet/csharp/language-reference/statements/using)
- [Work with file and stream I/O](https://learn.microsoft.com/dotnet/standard/io/)
- [Serialize and deserialize JSON with `System.Text.Json`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
- [Character encoding in .NET](https://learn.microsoft.com/dotnet/api/system.text.encoding.utf8)
