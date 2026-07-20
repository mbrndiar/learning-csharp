# Authoritative sources

The course uses stable .NET 10 and C# 14 behavior. Microsoft documentation and
language specifications determine technical claims; examples in this
repository remain independently runnable evidence.

## Support and language

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [C# 14 overview](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14)
- [C# language reference](https://learn.microsoft.com/dotnet/csharp/language-reference/)
- [C# language specification](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/)
- [Configure the C# language version](https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version)

## Project and quality workflow

- [File-based apps](https://learn.microsoft.com/dotnet/csharp/fundamentals/tutorials/file-based-programs)
- [.NET CLI overview](https://learn.microsoft.com/dotnet/core/tools/)
- [SDK-style projects](https://learn.microsoft.com/dotnet/core/project-sdk/overview)
- [`global.json`](https://learn.microsoft.com/dotnet/core/tools/global-json)
- [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Checked and unchecked arithmetic](https://learn.microsoft.com/dotnet/csharp/language-reference/statements/checked-and-unchecked)
- [Method parameters](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/method-parameters)
- [XML documentation comments](https://learn.microsoft.com/dotnet/csharp/language-reference/xmldoc/)
- [.NET code analysis](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview)
- [`dotnet format`](https://learn.microsoft.com/dotnet/core/tools/dotnet-format)
- [Central package management](https://learn.microsoft.com/nuget/consume-packages/central-package-management)
- [Repeatable package restore](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#locking-dependencies)

## Testing and application boundaries

- [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
- [Testing with `dotnet test`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test)
- [xUnit.net v3 with MTP](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [Microsoft.Data.Sqlite overview](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [Microsoft.Data.Sqlite parameters](https://learn.microsoft.com/dotnet/standard/data/sqlite/parameters)
- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/dotnet/standard/data/sqlite/transactions)
- [Microsoft.Data.Sqlite ADO.NET limitations](https://learn.microsoft.com/dotnet/standard/data/sqlite/adonet-limitations)
- [`System.Text.Json`](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
- [`DateOnly` and `TimeOnly`](https://learn.microsoft.com/dotnet/standard/datetime/how-to-use-dateonly-timeonly)
- [Choose between `DateTime`, `DateOnly`, and `DateTimeOffset`](https://learn.microsoft.com/dotnet/standard/datetime/choosing-between-datetime)
- [`TimeProvider` overview](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview)
- [Asynchronous programming](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/)
- [Cancellation](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads)
- [`HttpClient` guidelines](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [`IHttpClientFactory`](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory)
- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
- [ASP.NET Core request and response operations](https://learn.microsoft.com/aspnet/core/fundamentals/use-http-context)
- [ASP.NET Core API approaches](https://learn.microsoft.com/aspnet/core/fundamentals/apis?view=aspnetcore-10.0)
- [Minimal API quick reference](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0)
- [OpenAPI support in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0)
- [Microsoft.OpenApi 3.9.0](https://www.nuget.org/packages/Microsoft.OpenApi/3.9.0)

## Deliberate boundaries

C# 14 contains advanced features that are stable but not automatically
beginner-appropriate. The course uses a feature when it clarifies the current
model, not merely because it is new. Legacy .NET Framework APIs, preview
features, and framework-specific UI or cloud stacks are not normal-path
examples.

For a compact reminder after completing the relevant unit, see
[CHEATSHEET.md](../CHEATSHEET.md).
