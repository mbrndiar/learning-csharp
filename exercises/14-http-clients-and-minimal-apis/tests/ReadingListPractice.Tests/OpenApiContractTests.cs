using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;
using LearningCSharp.Exercises.HttpClientsAndMinimalApis.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Tests;

/// <summary>
/// The checked-in OpenAPI 3.1 contract (<c>reading-list-api.openapi.json</c>) is
/// independent evidence: it is authored by hand, not generated from the running
/// server, so it cannot silently drift into agreement with a bug.
///
/// These tests draw a hard line between two different questions:
///  - "Is the contract itself a well-formed OpenAPI 3.1 description?" - answered
///    by parsing it with <c>Microsoft.OpenApi</c>, a maintained .NET parser. A
///    clean parse (no diagnostic errors) is semantic validation of the
///    document's structure, types, and references; it proves nothing about any
///    running server.
///  - "Does the live server actually behave the way the contract claims?" -
///    answered only by calling the running Minimal API and checking its status
///    codes. Parsing alone cannot execute anything, so it cannot catch a
///    behavioral mismatch, a concurrency bug, or a validation message that
///    drifted from the contract - that boundary limitation is exactly why this
///    exercise keeps both kinds of test.
/// </summary>
public sealed class OpenApiContractTests
{
    private static readonly string ContractPath = Path.Combine(AppContext.BaseDirectory, "TestData", "reading-list-api.openapi.json");

    [Fact]
    public async Task ContractParsesAsASemanticallyValidOpenApiThreeOneDocument()
    {
        Assert.True(File.Exists(ContractPath), $"Expected the checked-in contract at {ContractPath}.");

        ReadResult result = await OpenApiDocument.LoadAsync(ContractPath, token: TestContext.Current.CancellationToken);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.Empty(result.Diagnostic!.Errors);
        Assert.Equal(OpenApiSpecVersion.OpenApi3_1, result.Diagnostic.SpecificationVersion);
    }

    [Theory]
    [InlineData("/books", "GET")]
    [InlineData("/books", "POST")]
    [InlineData("/books/{id}", "GET")]
    public async Task ContractDeclaresEveryImplementedOperation(string path, string method)
    {
        ReadResult result = await OpenApiDocument.LoadAsync(ContractPath, token: TestContext.Current.CancellationToken);
        OpenApiDocument document = result.Document ?? throw new InvalidOperationException("The contract failed to parse.");

        Assert.True(document.Paths.TryGetValue(path, out IOpenApiPathItem? pathItem), $"Contract is missing path '{path}'.");
        bool hasOperation = pathItem?.Operations?.ContainsKey(new HttpMethod(method)) ?? false;
        Assert.True(hasOperation, $"Contract path '{path}' is missing operation '{method}'.");
    }

    [Fact]
    public async Task ContractBookSchemaMatchesTheRuntimeDtoShape()
    {
        ReadResult result = await OpenApiDocument.LoadAsync(ContractPath, token: TestContext.Current.CancellationToken);
        OpenApiDocument document = result.Document ?? throw new InvalidOperationException("The contract failed to parse.");

        IOpenApiSchema bookSchema = document.Components?.Schemas?["Book"]
            ?? throw new InvalidOperationException("Contract is missing the Book schema.");
        string[] declaredProperties = [.. (bookSchema.Properties ?? new Dictionary<string, IOpenApiSchema>()).Keys];

        string[] actualProperties = [.. typeof(BookDto).GetProperties().Select(property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..])];

        Assert.Equal(actualProperties.OrderBy(name => name, StringComparer.Ordinal), declaredProperties.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public async Task LiveApiBehavesTheWayTheContractClaimsForTheDocumentedStatusCodes()
    {
        // Semantic validation above only proves the contract is well-formed
        // OpenAPI - it never touched a server. This test closes the loop for
        // the slice of behavior a static document cannot execute.
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using WebApplicationFactory<Program> factory = new();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage list = await client.GetAsync("/books", cancellationToken);
        HttpResponseMessage badId = await client.GetAsync($"/books/{Guid.Empty:D}", cancellationToken);
        HttpResponseMessage missing = await client.GetAsync(
            $"/books/{Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"):D}",
            cancellationToken);
        HttpResponseMessage invalidPost = await client.PostAsJsonAsync("/books", new CreateBookRequest("", "", 1), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPost.StatusCode);
    }
}
