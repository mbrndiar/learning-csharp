using System.Text.Json;
using Tasks.Core;
using Tasks.Http;
using Tasks.Server.Configuration;

namespace Tasks.Tests;

/// <summary>
/// Direct unit coverage of the provided core contracts: the <see cref="Maybe{T}"/>
/// value, the launcher settings parser, and the framework-neutral HTTP boundary
/// helpers. These types are shared infrastructure present in both trees.
/// </summary>
public sealed class CoreContractTests
{
    [Fact]
    public void MaybeDistinguishesSetUnsetAndSupportsEquality()
    {
        Maybe<string> unset = default;
        Assert.False(unset.HasValue);
        Assert.False(unset.TryGet(out _));
        Assert.Throws<InvalidOperationException>(() => unset.Value);

        Maybe<string> set = "x";
        Assert.True(set.HasValue);
        Assert.True(set.TryGet(out string? value));
        Assert.Equal("x", value);
        Assert.Equal("x", set.Value);

        Assert.True(set == MaybeFactory.Of("x"));
        Assert.True(set != unset);
        Assert.False(set.Equals(unset));
        Assert.True(set.Equals((object)MaybeFactory.Of("x")));
        Assert.False(set.Equals("not a maybe"));
        Assert.Equal(MaybeFactory.Of("x").GetHashCode(), set.GetHashCode());
        Assert.Equal(MaybeFactory.None<string>(), unset);
    }

    [Theory]
    [InlineData("/health", "health", null)]
    [InlineData("/tasks", "tasks", null)]
    [InlineData("/tasks/5", "task", "5")]
    [InlineData("/tasks/007", "task", "007")]
    public void MatchResolvesKnownRoutes(string path, string route, string? idText)
    {
        RouteMatch? match = TaskHttpContract.Match(path);
        Assert.NotNull(match);
        Assert.Equal(route, match!.Value.Route);
        Assert.Equal(idText, match.Value.IdText);
    }

    [Theory]
    [InlineData("/nope")]
    [InlineData("/tasks/")]
    [InlineData("/tasks/5/extra")]
    [InlineData("/health/x")]
    public void MatchRejectsUnknownRoutes(string path) => Assert.Null(TaskHttpContract.Match(path));

    [Fact]
    public void AllowedMethodsAreStableAndRejectUnknownRoutes()
    {
        Assert.Equal(["GET"], TaskHttpContract.AllowedMethods(TaskHttpContract.HealthRoute));
        Assert.Equal(["GET", "POST"], TaskHttpContract.AllowedMethods(TaskHttpContract.TasksRoute));
        Assert.Equal(["GET", "PATCH", "DELETE"], TaskHttpContract.AllowedMethods(TaskHttpContract.TaskRoute));
        Assert.Throws<ArgumentException>(() => TaskHttpContract.AllowedMethods("mystery"));
    }

    [Theory]
    [InlineData("1", 1L)]
    [InlineData("42", 42L)]
    [InlineData("007", 7L)]
    public void ParseTaskIdAcceptsPositiveDecimals(string text, long expected)
        => Assert.Equal(expected, TaskHttpContract.ParseTaskId(text));

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("99999999999999999999999999")]
    public void ParseTaskIdRejectsInvalidText(string text)
    {
        TaskValidationException error = Assert.Throws<TaskValidationException>(() => TaskHttpContract.ParseTaskId(text));
        Assert.Equal("id", error.Field);
    }

    [Fact]
    public void ParseCompletedFilterHandlesEveryCase()
    {
        Assert.Null(TaskHttpContract.ParseCompletedFilter([]));
        Assert.True(TaskHttpContract.ParseCompletedFilter(["true"]));
        Assert.False(TaskHttpContract.ParseCompletedFilter(["false"]));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.ParseCompletedFilter(["maybe"]));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.ParseCompletedFilter(["true", "false"]));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.ParseCompletedFilter([null]));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("application/json;charset=\"utf-8\"")]
    [InlineData("APPLICATION/JSON")]
    public void ValidateJsonContentTypeAcceptsJson(string contentType)
        => TaskHttpContract.ValidateJsonContentType(contentType);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("text/plain")]
    public void ValidateJsonContentTypeRejectsNonJson(string? contentType)
    {
        ApiErrorException error =
            Assert.Throws<ApiErrorException>(() => TaskHttpContract.ValidateJsonContentType(contentType));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("request Content-Type must be application/json", error.Message);
    }

    [Fact]
    public void ValidateJsonContentTypeRejectsNonUtf8Charset()
    {
        ApiErrorException error = Assert.Throws<ApiErrorException>(
            () => TaskHttpContract.ValidateJsonContentType("application/json; charset=ascii"));
        Assert.Equal("request JSON charset must be UTF-8", error.Message);
    }

    [Fact]
    public void RejectUnexpectedQueryHandlesAllowedAndUnknownKeys()
    {
        TaskHttpContract.RejectUnexpectedQuery([], TaskHttpContract.NoQueryKeys);
        TaskHttpContract.RejectUnexpectedQuery(["completed"], TaskHttpContract.CompletedQueryKeys);
        TaskValidationException error = Assert.Throws<TaskValidationException>(
            () => TaskHttpContract.RejectUnexpectedQuery(["zeta", "alpha"], TaskHttpContract.CompletedQueryKeys));
        Assert.Equal("alpha", error.Field);
    }

    [Fact]
    public void DecodeCreateTitleReturnsRawTitleAndRejectsBadShapes()
    {
        Assert.Equal("  spaced  ", TaskHttpContract.DecodeCreateTitle("""{"title":"  spaced  "}"""u8.ToArray()));

        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeCreateTitle("""{"nope":1}"""u8.ToArray()));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeCreateTitle("{}"u8.ToArray()));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeCreateTitle("""{"title":5}"""u8.ToArray()));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeCreateTitle("[]"u8.ToArray()));
        Assert.Throws<ApiErrorException>(() => TaskHttpContract.DecodeCreateTitle("{bad"u8.ToArray()));
        Assert.Throws<ApiErrorException>(() => TaskHttpContract.DecodeCreateTitle("""{"title":"a","title":"b"}"""u8.ToArray()));
    }

    [Fact]
    public void DecodeUpdateRejectsUnknownAndWrongTypedFields()
    {
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeUpdate("""{"nope":1}"""u8.ToArray()));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeUpdate("""{"title":5}"""u8.ToArray()));
        Assert.Throws<TaskValidationException>(() => TaskHttpContract.DecodeUpdate("""{"completed":"yes"}"""u8.ToArray()));
    }

    [Fact]
    public void SerializersProduceTheDocumentedShapes()
    {
        var task = new TaskItem(1, "Learn REST", true);
        JsonElement single = JsonDocument.Parse(TaskHttpContract.SerializeTask(task)).RootElement;
        Assert.Equal(1, single.GetProperty("id").GetInt64());
        Assert.Equal("Learn REST", single.GetProperty("title").GetString());
        Assert.True(single.GetProperty("completed").GetBoolean());

        JsonElement list = JsonDocument.Parse(TaskHttpContract.SerializeTasks([task])).RootElement;
        Assert.Equal(1, list.GetArrayLength());

        Assert.Equal("ok", JsonDocument.Parse(TaskHttpContract.SerializeHealth()).RootElement.GetProperty("status").GetString());

        var envelope = new ErrorResponse(new ErrorBody("not_found", "gone", null));
        JsonElement error = JsonDocument.Parse(TaskHttpContract.SerializeError(envelope)).RootElement;
        Assert.Equal("not_found", error.GetProperty("error").GetProperty("code").GetString());
        Assert.False(error.GetProperty("error").TryGetProperty("details", out _));
    }

    [Fact]
    public void DescribeMapsEveryFailureCategory()
    {
        MappedError invalidJson = TaskHttpContract.Describe(ApiErrorException.InvalidJson("bad"));
        Assert.Equal(400, invalidJson.StatusCode);
        Assert.Equal(ErrorCodes.InvalidJson, invalidJson.Body.Error.Code);

        MappedError routeNotFound = TaskHttpContract.Describe(ApiErrorException.RouteNotFound());
        Assert.Equal(404, routeNotFound.StatusCode);

        MappedError methodNotAllowed = TaskHttpContract.Describe(ApiErrorException.MethodNotAllowed(["GET", "POST"]));
        Assert.Equal(405, methodNotAllowed.StatusCode);
        Assert.Equal("GET, POST", methodNotAllowed.Allow);

        MappedError payloadTooLarge = TaskHttpContract.Describe(ApiErrorException.PayloadTooLarge());
        Assert.Equal(400, payloadTooLarge.StatusCode);

        MappedError validation = TaskHttpContract.Describe(new TaskValidationException("bad title", "title"));
        Assert.Equal(422, validation.StatusCode);
        Assert.Equal("title", validation.Body.Error.Details!["field"]);

        MappedError notFound = TaskHttpContract.Describe(new TaskNotFoundException(7));
        Assert.Equal(404, notFound.StatusCode);
        Assert.Equal("task 7 was not found", notFound.Body.Error.Message);

        MappedError storage = TaskHttpContract.Describe(new TaskStorageException("disk gone", "read"));
        Assert.Equal(500, storage.StatusCode);
        Assert.Equal("the server could not complete the request", storage.Body.Error.Message);
        Assert.Null(storage.Body.Error.Details);

        MappedError unexpected = TaskHttpContract.Describe(new InvalidOperationException("boom"));
        Assert.Equal(500, unexpected.StatusCode);
        Assert.Equal(ErrorCodes.InternalError, unexpected.Body.Error.Code);
    }

    [Theory]
    [InlineData("--backend=sqlite", "--data=tasks.db")]
    [InlineData("--backend", "markdown", "--data", "tasks.md", "--host", "::1")]
    public void ServerSettingsAcceptsAlternateArgumentForms(params string[] args)
    {
        ServerSettings settings = ServerSettings.Parse(args);
        Assert.False(string.IsNullOrEmpty(settings.DataPath));
        Assert.Contains("http://", settings.BaseUrl, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--port")]
    [InlineData("--zebra", "1")]
    [InlineData("stray")]
    [InlineData("--port", "abc", "--backend", "sqlite", "--data", "t.db")]
    [InlineData("--data", "", "--backend", "sqlite")]
    public void ServerSettingsRejectsMalformedArguments(params string[] args)
        => Assert.Throws<ServerConfigurationException>(() => ServerSettings.Parse(args));
}
