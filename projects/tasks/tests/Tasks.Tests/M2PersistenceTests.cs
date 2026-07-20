using System.Text;
using Tasks.Core;
using Tasks.Server.Persistence;
using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>Milestone 2: SQLite and Markdown repositories under one contract.</summary>
public sealed class M2PersistenceTests
{
    private static ITaskRepository Create(string backend, string path) => backend switch
    {
        "sqlite" => new SqliteTaskRepository(path),
        "markdown" => new MarkdownTaskRepository(path),
        _ => throw new ArgumentOutOfRangeException(nameof(backend)),
    };

    public static TheoryData<string, string> Backends => new()
    {
        { "sqlite", "tasks.db" },
        { "markdown", "tasks.md" },
    };

    [Theory]
    [MemberData(nameof(Backends))]
    public async Task RepositoryContractIsSharedByBothBackends(string backend, string fileName)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File(fileName);

        ITaskRepository repository = Create(backend, path);
        Assert.Empty(await repository.ListAsync(cancellationToken: token));

        TaskItem first = await repository.CreateAsync(new CreateTaskInput("Learn SQLite"), token);
        TaskItem second = await repository.CreateAsync(new CreateTaskInput("Build an API"), token);
        TaskItem third = await repository.CreateAsync(new CreateTaskInput("Write contracts"), token);
        Assert.Equal(new TaskItem(1, "Learn SQLite", false), first);
        Assert.Equal(new TaskItem(2, "Build an API", false), second);
        Assert.Equal(new TaskItem(3, "Write contracts", false), third);

        Assert.Equal([first, second, third], await repository.ListAsync(cancellationToken: token));
        Assert.Equal([first, second, third], await repository.ListAsync(false, token));
        Assert.Empty(await repository.ListAsync(true, token));
        Assert.Equal(second, await repository.GetAsync(2, token));

        TaskItem completed = await repository.UpdateAsync(2, new UpdateTaskInput(completed: true), token);
        Assert.Equal(new TaskItem(2, "Build an API", true), completed);
        TaskItem renamed = await repository.UpdateAsync(2, new UpdateTaskInput(title: "Ship the API"), token);
        Assert.Equal(new TaskItem(2, "Ship the API", true), renamed);
        TaskItem unchanged = await repository.UpdateAsync(2, new UpdateTaskInput(completed: true), token);
        Assert.Equal(renamed, unchanged);
        Assert.Equal([first, third], await repository.ListAsync(false, token));
        Assert.Equal([renamed], await repository.ListAsync(true, token));

        await Assert.ThrowsAsync<TaskNotFoundException>(() => repository.GetAsync(99, token));
        await Assert.ThrowsAsync<TaskNotFoundException>(
            () => repository.UpdateAsync(99, new UpdateTaskInput(completed: true), token));
        await Assert.ThrowsAsync<TaskNotFoundException>(() => repository.DeleteAsync(99, token));

        await repository.DeleteAsync(2, token);
        await Assert.ThrowsAsync<TaskNotFoundException>(() => repository.GetAsync(2, token));

        // A fresh instance distinguishes persisted state from an in-memory cache.
        ITaskRepository reopened = Create(backend, path);
        Assert.Equal([first, third], await reopened.ListAsync(cancellationToken: token));
        TaskItem fourth = await reopened.CreateAsync(new CreateTaskInput("Reopen safely"), token);
        Assert.Equal(new TaskItem(4, "Reopen safely", false), fourth);

        await reopened.DeleteAsync(1, token);
        await reopened.DeleteAsync(3, token);
        await reopened.DeleteAsync(4, token);
        Assert.Empty(await reopened.ListAsync(cancellationToken: token));

        // Deletion must not rewind allocator state: IDs are never reused.
        ITaskRepository again = Create(backend, path);
        Assert.Equal(new TaskItem(5, "Never reuse IDs", false), await again.CreateAsync(new CreateTaskInput("Never reuse IDs"), token));
    }

    [Fact]
    public async Task SqliteSchemaIsIdempotentCheckedAndAutoincrementing()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.db");

        var repository = new SqliteTaskRepository(path);
        _ = new SqliteTaskRepository(path); // Re-initialization is idempotent.

        await repository.CreateAsync(new CreateTaskInput("First"), token);
        await repository.CreateAsync(new CreateTaskInput("Second"), token);
        await repository.DeleteAsync(2, token);

        TaskItem next = await repository.CreateAsync(new CreateTaskInput("Third"), token);
        Assert.Equal(3, next.Id);
    }

    [Fact]
    public async Task SqliteSerializesPartialReadModifyWriteUpdates()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        var repository = new SqliteTaskRepository(directory.File("tasks.db"));
        await repository.CreateAsync(new CreateTaskInput("Concurrent"), token);

        Task<TaskItem> completing = repository.UpdateAsync(1, new UpdateTaskInput(completed: true), token);
        Task<TaskItem> renaming = repository.UpdateAsync(1, new UpdateTaskInput(title: "Renamed"), token);
        await Task.WhenAll(completing, renaming);

        // Whichever ran second must have preserved the other's field.
        TaskItem final = await repository.GetAsync(1, token);
        Assert.Equal(new TaskItem(1, "Renamed", true), final);
    }

    [Fact]
    public async Task MarkdownUsesTheExactVersionedDeterministicFormat()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.md");
        var repository = new MarkdownTaskRepository(path);

        await repository.CreateAsync(new CreateTaskInput("Learn SQLite"), token);
        await repository.CreateAsync(new CreateTaskInput("Build an API"), token);
        await repository.UpdateAsync(2, new UpdateTaskInput(completed: true), token);

        string expected =
            "<!-- rest-task-api:v1 next-id=3 -->\n# Tasks\n\n- [ ] 1: Learn SQLite\n- [x] 2: Build an API\n";
        Assert.Equal(expected, await File.ReadAllTextAsync(path, token));
    }

    [Fact]
    public async Task MarkdownInitializesMissingFileAsCanonicalEmptyDocument()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.md");
        var repository = new MarkdownTaskRepository(path);

        Assert.Empty(await repository.ListAsync(cancellationToken: token));
        Assert.Equal("<!-- rest-task-api:v1 next-id=1 -->\n# Tasks\n", await File.ReadAllTextAsync(path, token));
    }

    [Theory]
    [InlineData("<!-- rest-task-api:v2 next-id=1 -->\n# Tasks\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=nope -->\n# Tasks\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=2 -->\n# Tasks\n\n- [ ] 1: A\n- [ ] 1: B\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=3 -->\n# Tasks\n\n- [ ] 2: A\n- [ ] 1: B\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=2 -->\n# Tasks\n\n- [ ] 1:  padded title \n")]
    [InlineData("<!-- rest-task-api:v1 next-id=1 -->\n# Tasks\n\n- [ ] 1: Present\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=1 -->\n# Tasks")]
    [InlineData("<!-- rest-task-api:v1 next-id=1 -->\n# Tasks\r\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=1 -->\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=1 -->\n## Wrong\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=2 -->\n# Tasks\n- [ ] 1: A\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=1 -->\n# Tasks\n\n")]
    [InlineData("<!-- rest-task-api:v1 next-id=2 -->\n# Tasks\n\nnot a checklist row\n")]
    public async Task MarkdownRejectsMalformedOrNoncanonicalDocuments(string content)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.md");
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes(content), token);

        var repository = new MarkdownTaskRepository(path);
        TaskStorageException error =
            await Assert.ThrowsAsync<TaskStorageException>(() => repository.ListAsync(cancellationToken: token));
        Assert.Equal(ErrorCodes.InternalError, error.Code);
    }

    [Fact]
    public async Task MarkdownRejectsRowsWithInvalidTitles()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.md");
        string longTitle = new('x', 121);
        await File.WriteAllBytesAsync(
            path,
            Encoding.UTF8.GetBytes($"<!-- rest-task-api:v1 next-id=2 -->\n# Tasks\n\n- [ ] 1: {longTitle}\n"),
            token);

        var repository = new MarkdownTaskRepository(path);
        await Assert.ThrowsAsync<TaskStorageException>(() => repository.ListAsync(cancellationToken: token));
    }

    [Fact]
    public async Task MarkdownRejectsInvalidUtf8()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.md");
        await File.WriteAllBytesAsync(path, [0xff, 0xfe, 0x00, 0x0a], token);

        var repository = new MarkdownTaskRepository(path);
        await Assert.ThrowsAsync<TaskStorageException>(() => repository.ListAsync(cancellationToken: token));
    }

    [Fact]
    public async Task MarkdownSerializesConcurrentWritesAndCleansTemporaryFiles()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.md");
        var repository = new MarkdownTaskRepository(path);

        IEnumerable<Task<TaskItem>> creations = Enumerable
            .Range(0, 20)
            .Select(index => repository.CreateAsync(new CreateTaskInput($"Task {index}"), token));
        TaskItem[] created = await Task.WhenAll(creations);

        Assert.Equal(20, created.Select(task => task.Id).Distinct().Count());
        Assert.Equal(20, (await repository.ListAsync(cancellationToken: token)).Count);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void SqliteReportsStorageErrorsForUnusablePaths()
    {
        string directory = Path.GetFullPath("no-such-dir-" + Guid.NewGuid().ToString("N"));
        string databasePath = Path.Combine(directory, "tasks.db");
        Assert.Throws<TaskStorageException>(() => new SqliteTaskRepository(databasePath));
    }

    [Fact]
    public async Task MarkdownReportsStorageErrorsWhenItCannotPublish()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string directory = Path.GetFullPath("no-such-dir-" + Guid.NewGuid().ToString("N"));
        var repository = new MarkdownTaskRepository(Path.Combine(directory, "tasks.md"));
        await Assert.ThrowsAsync<TaskStorageException>(() => repository.ListAsync(cancellationToken: token));
    }

    [Fact]
    public async Task SqliteReportsStorageErrorForCorruptPersistedRows()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string path = directory.File("tasks.db");

        // Seed a row whose title violates the domain invariant (a control
        // character), which the schema's CHECK does not prevent.
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"))
        {
            await connection.OpenAsync(token);
            await using SqliteCommandScope schema = SqliteCommandScope.Create(
                connection,
                "CREATE TABLE tasks (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT NOT NULL, completed INTEGER NOT NULL DEFAULT 0 CHECK (completed IN (0, 1)))");
            await schema.Command.ExecuteNonQueryAsync(token);
            await using SqliteCommandScope insert = SqliteCommandScope.Create(
                connection,
                "INSERT INTO tasks (title, completed) VALUES ('line one\nline two', 0)");
            await insert.Command.ExecuteNonQueryAsync(token);
        }

        var repository = new SqliteTaskRepository(path);
        await Assert.ThrowsAsync<TaskStorageException>(() => repository.GetAsync(1, token));
    }

    private readonly struct SqliteCommandScope : IAsyncDisposable
    {
        private SqliteCommandScope(Microsoft.Data.Sqlite.SqliteCommand command) => Command = command;

        public Microsoft.Data.Sqlite.SqliteCommand Command { get; }

        public static SqliteCommandScope Create(Microsoft.Data.Sqlite.SqliteConnection connection, string sql)
        {
            Microsoft.Data.Sqlite.SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            return new SqliteCommandScope(command);
        }

        public ValueTask DisposeAsync() => Command.DisposeAsync();
    }
}
