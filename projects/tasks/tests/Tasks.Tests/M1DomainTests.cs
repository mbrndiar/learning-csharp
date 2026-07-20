using Tasks.Core;
using Tasks.Tests.Support;

namespace Tasks.Tests;

/// <summary>Milestone 1: task validation, values, inputs, errors, and the service.</summary>
public sealed class M1DomainTests
{
    [Theory]
    [InlineData("Learn REST", "Learn REST")]
    [InlineData("  Learn REST  ", "Learn REST")]
    [InlineData("A", "A")]
    public void TitleValidationTrimsAndAcceptsBoundaries(string input, string expected)
    {
        Assert.Equal(expected, TaskValidation.ValidateTitle(input));
    }

    [Fact]
    public void TitleValidationAcceptsExactLengthBoundaries()
    {
        Assert.Equal(new string('x', 1), TaskValidation.ValidateTitle("x"));
        Assert.Equal(new string('x', 120), TaskValidation.ValidateTitle(new string('x', 120)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TitleValidationRejectsEmptyAfterTrim(string input)
    {
        TaskValidationException error =
            Assert.Throws<TaskValidationException>(() => TaskValidation.ValidateTitle(input));
        Assert.Equal("title must contain between 1 and 120 characters", error.Message);
        Assert.Equal("title", error.Field);
    }

    [Fact]
    public void TitleValidationRejectsTooLong()
    {
        Assert.Throws<TaskValidationException>(() => TaskValidation.ValidateTitle(new string('x', 121)));
    }

    [Theory]
    [InlineData("line one\nline two")]
    [InlineData("carriage\rreturn")]
    public void TitleValidationRejectsMultipleLines(string input)
    {
        TaskValidationException error =
            Assert.Throws<TaskValidationException>(() => TaskValidation.ValidateTitle(input));
        Assert.Equal("title must occupy one physical line", error.Message);
    }

    [Fact]
    public void TitleValidationRejectsControlCharacters()
    {
        TaskValidationException error =
            Assert.Throws<TaskValidationException>(() => TaskValidation.ValidateTitle("bell\u0007here"));
        Assert.Equal("title must not contain control characters", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TaskIdValidationRejectsNonPositive(long id)
    {
        TaskValidationException error =
            Assert.Throws<TaskValidationException>(() => TaskValidation.ValidateTaskId(id));
        Assert.Equal("task ID must be a positive integer", error.Message);
        Assert.Equal("id", error.Field);
    }

    [Fact]
    public void TaskIsValidatedNormalizedAndImmutable()
    {
        var task = new TaskItem(1, "  Trimmed  ", true);
        Assert.Equal(1, task.Id);
        Assert.Equal("Trimmed", task.Title);
        Assert.True(task.Completed);
        Assert.Equal(new TaskItem(1, "Trimmed", true), task);
        Assert.Throws<TaskValidationException>(() => new TaskItem(0, "x", false));
    }

    [Fact]
    public void UpdateInputRejectsEmptyPatchAndPreservesExplicitFalse()
    {
        TaskValidationException error = Assert.Throws<TaskValidationException>(() => new UpdateTaskInput());
        Assert.Equal("update must include title or completed", error.Message);
        Assert.Equal("update", error.Field);

        var update = new UpdateTaskInput(completed: false);
        Assert.True(update.Completed.HasValue);
        Assert.False(update.Completed.Value);
        Assert.False(update.Title.HasValue);
    }

    [Fact]
    public void UpdateInputApplyToPreservesOmittedFields()
    {
        var current = new TaskItem(3, "Original", false);

        Assert.Equal(new TaskItem(3, "Original", true), new UpdateTaskInput(completed: true).ApplyTo(current));
        Assert.Equal(new TaskItem(3, "Renamed", false), new UpdateTaskInput(title: "Renamed").ApplyTo(current));
        Assert.Equal(
            new TaskItem(3, "Both", true),
            new UpdateTaskInput(title: "Both", completed: true).ApplyTo(current));
    }

    [Fact]
    public void MaybeDistinguishesSetFromUnset()
    {
        Maybe<bool> unset = default;
        Assert.False(unset.HasValue);

        Maybe<bool> set = false;
        Assert.True(set.HasValue);
        Assert.False(set.Value);
        Assert.Equal(Maybe.Of(false), set);
        Assert.NotEqual(unset, set);
    }

    [Fact]
    public void NarrowErrorsExposeStableFields()
    {
        var notFound = new TaskNotFoundException(99);
        Assert.Equal(ErrorCodes.NotFound, notFound.Code);
        Assert.Equal("task 99 was not found", notFound.Message);
        Assert.Equal(99, notFound.TaskId);

        var storage = new TaskStorageException("disk gone", "read");
        Assert.Equal(ErrorCodes.InternalError, storage.Code);
        Assert.Equal("read", storage.Operation);
        Assert.Equal("read", storage.Details!["operation"]);

        var storageWithoutOperation = new TaskStorageException("disk gone");
        Assert.Null(storageWithoutOperation.Operation);
        Assert.Null(storageWithoutOperation.Details);
    }

    [Fact]
    public async Task ServiceNormalizesBoundariesAndDelegatesToRepository()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var service = new TaskService(new InMemoryTaskRepository());

        TaskItem created = await service.CreateTaskAsync("  Ship it  ", token);
        Assert.Equal(new TaskItem(1, "Ship it", false), created);

        TaskItem completed = await service.UpdateTaskAsync(1, new UpdateTaskInput(completed: true), token);
        Assert.True(completed.Completed);

        Assert.Equal([completed], await service.ListTasksAsync(true, token));
        Assert.Empty(await service.ListTasksAsync(false, token));
        Assert.Equal(completed, await service.GetTaskAsync(1, token));

        await service.DeleteTaskAsync(1, token);
        Assert.Empty(await service.ListTasksAsync(null, token));
    }

    [Fact]
    public async Task ServiceRejectsInvalidIdBeforeRepositoryAccess()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var service = new TaskService(new ThrowingTaskRepository());

        // A validation failure must surface before the throwing repository runs.
        await Assert.ThrowsAsync<TaskValidationException>(() => service.GetTaskAsync(0, token));
        await Assert.ThrowsAsync<TaskValidationException>(() => service.DeleteTaskAsync(-1, token));
    }

    [Fact]
    public async Task ServicePreservesNotFoundAndStorageFailures()
    {
        CancellationToken token = TestContext.Current.CancellationToken;

        var missing = new TaskService(new InMemoryTaskRepository());
        await Assert.ThrowsAsync<TaskNotFoundException>(() => missing.GetTaskAsync(42, token));

        var failing = new TaskService(new ThrowingTaskRepository());
        await Assert.ThrowsAsync<TaskStorageException>(() => failing.CreateTaskAsync("valid title", token));
    }
}
