# Troubleshooting

## `dotnet` is not found

Install the .NET 10 **SDK**, open a new terminal, and run:

```console
dotnet --list-sdks
```

If an SDK is listed but the command is missing in a new terminal, follow the
official installation page for your operating system and check its `PATH`
instructions.

## The selected SDK is incompatible

Typical messages mention that a compatible SDK or `global.json` version was not
found. Check:

```console
dotnet --version
dotnet --list-sdks
```

Install a stable .NET 10 SDK in the 10.0.300 feature band or later. Do not edit
`global.json` to bypass the course support boundary.

## Restore fails

First distinguish connectivity from a locked dependency mismatch:

```console
dotnet restore LearningCSharp.slnx
dotnet restore LearningCSharp.slnx --locked-mode
```

- A network or certificate error means NuGet could not reach its configured
  source. Check network, proxy, system time, and certificate settings.
- `NU1004` in locked mode means a project or central package declaration no
  longer matches its committed lock file. Do not regenerate locks unless you
  intentionally changed dependencies.

## A starter test fails

That is expected when the failure names unfinished behavior. Read:

1. the test name;
2. expected and actual values;
3. the first stack frame in your starter code;
4. the exercise contract in the lesson guide.

Unexpected compile errors usually mean an intentional method signature was
changed. Restore the required public shape, then change the method body.

## Tests run the solution instead of the starter

Pass the selector exactly:

```console
dotnet test --project exercises/09-linq-and-transformations/tests/LinqTransformationsPractice.Tests.csproj \
  -p:CourseImplementation=Starter
```

Only `Starter` and `Solution` are accepted, including capitalization.

## Formatting fails in CI

Apply formatting locally:

```console
dotnet format LearningCSharp.slnx
git diff
```

Review the diff and rerun:

```console
dotnet format LearningCSharp.slnx --verify-no-changes --no-restore
```

## The build treats a warning as an error

Warnings identify a correctness, maintainability, or portability risk under the
course configuration. Read the diagnostic ID, locate it in Microsoft Learn,
and correct the cause. Do not add a broad suppression. A narrowly justified
teaching exception belongs next to the relevant project configuration and in
the lesson explanation.

## A file or JSON test behaves differently on another OS

Use `Path.Combine` or `Path.Join`, keep test data inside a temporary directory,
and do not assume `/tmp`, drive letters, path casing, or a platform newline.
Use explicit UTF-8 and compare logical content when line endings are irrelevant.

## An async test hangs

Check that:

- every started task is awaited or deliberately owned;
- loops observe their `CancellationToken`;
- a fake dependency completes;
- no `.Result` or `.Wait()` blocks asynchronous work;
- HTTP and process-like operations have finite cancellation or timeout.

Stop the test, reduce it to one operation, and add an explicit short
test-controlled cancellation token.

## A local API address is unavailable

Course integration tests run in process and should not need a port. For manual
capstone use, make sure no earlier API process still owns the configured
loopback port, then select another local port through documented configuration.
Do not bind the learning API to a public interface.

## CourseVerifier reports a missing path

The course manifest and Markdown links are integrity contracts. Correct the
stale path or restore the missing artifact; do not weaken verification to make
an inconsistent course appear valid.

Return to [setup](SETUP.md) or the [course entry point](../README.md).
