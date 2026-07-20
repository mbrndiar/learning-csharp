# ✅ Task REST API and clients (C# / .NET 10)

Build one small **Task** application behind **two** ASP.NET Core server styles and
use it through **two** `HttpClient` transports. The point is not three unrelated
apps — it is to keep **one domain** and **one HTTP contract** stable while
observing what each .NET hosting and client style makes explicit or convenient.

The project ships matching typed [`starter/`](starter) and [`solution/`](solution)
graphs with the **same public API**, plus one shared [`tests/`](tests) suite that
targets either tree. The starter keeps every milestone intentionally guided and
incomplete (`IncompleteProjectException`); the solution implements both
repositories, both servers, both clients, the shared contracts, and the
client × server interoperability matrix.

## 📚 Start with the contract

- 📄 [`docs/SPEC.md`](docs/SPEC.md) — behavior, persistence, errors, client
  commands, and the explicit project boundaries.
- 🧾 [`docs/openapi.yaml`](docs/openapi.yaml) — the compact OpenAPI 3.1 HTTP
  contract (the machine-readable source of truth).
- 🗺️ [`docs/PLAN.md`](docs/PLAN.md) — a reusable, language-independent build plan.

Read the specification before the tests or source. The tests give fast feedback
but do not replace the written contract.

## 🧰 Prerequisites

- A **.NET 10 SDK** (see [`global.json`](../../global.json); `10.0.3xx`).
- No network services: everything runs on **loopback**, and tests use
  **ephemeral ports** and **project-local temporary storage**.
- Central package versions are pinned by the repository
  ([`Directory.Packages.props`](../../Directory.Packages.props)); this project
  references `Microsoft.Data.Sqlite` and `Microsoft.Extensions.Http` **without**
  versions.

## 🏗️ Architecture and data flow

Both source roots expose the same seven projects; dependencies point inward toward
the framework-neutral core.

```text
projects/tasks/{starter,solution}/
├── shared/
│   ├── Tasks.Core              # Task value, validation, service, ITaskRepository,
│   │                           # Maybe<T> sentinel, and domain errors
│   └── Tasks.Http              # shared, framework-neutral HTTP boundary policy
│                               # (routing, decoding, error → envelope, serialization)
├── server/
│   ├── Tasks.Server            # launcher settings + SQLite & Markdown repositories + factory
│   ├── Tasks.Server.Middleware # low-level RequestDelegate/middleware server (host)
│   └── Tasks.Server.MinimalApi # Minimal API server (host)
└── client/
    ├── Tasks.Client            # transport contract + raw & typed transports + CLI app
    └── Tasks.Cli               # command-line host (selects a transport)
```

```text
CLI args ─▶ ClientApplication ─▶ ITaskTransport ═══ HTTP/JSON ═══▶ server adapter
                                 (raw | typed)                     (low-level | minimal)
                                                                        │
                                                             TaskService (validation)
                                                                        │
                                                       ITaskRepository (SQLite | Markdown)
```

- `Tasks.Core` owns the `TaskItem` value, validation, the application service, the
  repository interface, the `Maybe<T>` sentinel, and the domain errors. `Tasks.Http`
  owns the shared HTTP contract helpers (routing, strict request decoding,
  error → envelope mapping, serialization). `Tasks.Server` owns the launcher
  settings and both repositories. The `Tasks.Core` and `Tasks.Http` libraries never
  reference an HTTP server or client library, and the servers and clients depend
  inward on these shared projects rather than on each other.
- `ITaskRepository` is asynchronous because the Markdown adapter performs real
  async file I/O and the HTTP hosts compose asynchronous request pipelines. The
  SQLite adapter keeps that common async-shaped interface and calls
  Microsoft.Data.Sqlite's async-shaped methods, but—as Lesson 13 explains—those
  methods still execute SQLite work synchronously. They do not make SQLite
  nonblocking or more scalable, and the adapter never hides them behind
  `Task.Run`; transactions remain short and explicit.
- Partial updates use the public `Maybe<T>` sentinel through `UpdateTaskInput`.
  Omitted fields stay unset while `completed = false` is a real update; `null` is
  never used to mean "omitted".
- Every client works with every server. The two transports and two servers are
  **comparisons, not pairings**.
- Starter and solution source use one top-level public/internal type per matching
  `.cs` file. Entry-point `Program.cs`, deliberate partial files, and short
  private nested implementation details are the only exceptions. This keeps the
  reference application searchable without forcing the same file-hopping rule
  onto small pedagogical lesson fragments.

## 🚀 Exact commands

Run these from the repository root.

Build both trees (or use the local solution files):

```bash
dotnet build projects/tasks/Tasks.Solution.slnx
dotnet build projects/tasks/Tasks.Starter.slnx
```

Select `Starter` or `Solution` for the shared test suite with an MSBuild property
(default is `Solution`). Selection is done at compile time, so both runs report
**0 skipped**: the default `Solution` run compiles and runs every substantive
milestone test, and the default `Starter` run compiles and runs only the starter
smoke, infrastructure, and public-surface parity checks.

```bash
dotnet test projects/tasks/tests/Tasks.Tests -p:CourseImplementation=Solution
dotnet test projects/tasks/tests/Tasks.Tests -p:CourseImplementation=Starter
```

Get focused red feedback for one milestone by compiling that milestone's shared
test classes against the starter — they fail with a stable
`IncompleteProjectException` until you implement the milestone. `TasksMilestone`
accepts `1`–`5` and is only valid with `CourseImplementation=Starter`:

```bash
dotnet test projects/tasks/tests/Tasks.Tests \
  -p:CourseImplementation=Starter -p:TasksMilestone=1
```

Measure and gate solution branch coverage. The collector is scoped by
`coverage.runsettings` to the six behavior assemblies; CourseVerifier enforces
the **≥ 85 %** threshold independently:

```bash
dotnet test --project projects/tasks/tests/Tasks.Tests/Tasks.Tests.csproj \
  --configuration Release -p:CourseImplementation=Solution \
  --results-directory projects/tasks/tests/Tasks.Tests/TestResults -- \
  --coverage --coverage-settings projects/tasks/tests/Tasks.Tests/coverage.runsettings \
  --coverage-output-format cobertura --coverage-output tasks.cobertura.xml
dotnet run --project tools/CourseVerifier -- coverage \
  projects/tasks/tests/Tasks.Tests/TestResults 0.85
```

Start either server with either persistence backend (loopback only):

```bash
mkdir -p projects/tasks/.test-data/run
dotnet run --project projects/tasks/solution/server/Tasks.Server.Middleware -- \
  --host 127.0.0.1 --port 8000 --backend sqlite \
  --data projects/tasks/.test-data/run/tasks.db

dotnet run --project projects/tasks/solution/server/Tasks.Server.MinimalApi -- \
  --host 127.0.0.1 --port 8000 --backend markdown \
  --data projects/tasks/.test-data/run/tasks.md
```

In PowerShell, create the ignored directory with
`New-Item -ItemType Directory -Force projects/tasks/.test-data/run`.

Then use any client, regardless of which server is running (`--transport`
selects `raw` or `typed`, default `raw`):

```bash
dotnet run --project projects/tasks/solution/client/Tasks.Cli -- \
  --transport raw   --base-url http://127.0.0.1:8000 add "Learn REST"

dotnet run --project projects/tasks/solution/client/Tasks.Cli -- \
  --transport typed --base-url http://127.0.0.1:8000 list --completed false

dotnet run --project projects/tasks/solution/client/Tasks.Cli -- \
  --transport typed --base-url http://127.0.0.1:8000 complete 1
```

The launchers are loopback learning servers, **not** production deployment
instructions.

## 🪜 Five milestones

1. **Domain and contracts** — `TaskItem` validation, domain errors, the
   repository interface, the application service, and the client transport
   contract.
2. **Persistence** — SQLite and one-file Markdown repositories under one shared
   repository contract (restart persistence, corruption, monotonic IDs).
3. **Low-level HTTP** — a single `RequestDelegate`/middleware server that makes
   routing, byte reading, content length, JSON, headers, status, and cleanup
   visible, plus the **raw `HttpClient`/`HttpRequestMessage`** transport.
4. **Minimal API** — `MapGet`/`MapPost`/`MapPatch`/`MapDelete`, dependency
   injection, typed results, exception middleware and status-code pages, plus the
   **typed `IHttpClientFactory`** transport.
5. **Interoperability & contract** — the client × server matrix, semantic
   agreement with the checked-in OpenAPI document, lifecycle/cleanup, and CLI
   process behavior.

Attempt each starter milestone before reading the corresponding solution.

The `starter/` and `solution/` trees expose the **same public API**, so the
shared tests compile against either. The one deliberate exception is the
scaffold-only `Tasks.Core.IncompleteProjectException` (and its `Incomplete`
helper): it exists **only in the starter** to signal unfinished milestones with a
stable error, and is intentionally absent from the solution, which contains no
`TODO`, `NotImplemented`, or placeholder. The public-surface parity check
whitelists that one type.

## 🔀 What changes between adapters?

| Boundary | Makes explicit | Provides |
| --- | --- | --- |
| `Tasks.Server.Middleware` (low-level) | Routing, byte decoding, content length, JSON serialization, headers, status selection, and lifecycle | One terminal `RequestDelegate` over the shared service |
| `Tasks.Server.MinimalApi` (Minimal API) | Typed results and DI at the endpoint boundary | `MapX` routing, endpoint handlers, exception middleware, status-code pages, and OpenAPI-friendly metadata |
| Raw transport | Request construction, encoding, response ownership, and status handling | An owned `HttpClient` + `SocketsHttpHandler` (no redirects, no proxy, finite timeout) |
| Typed transport | Client lifetime, timeout, and default headers | A client resolved from `IHttpClientFactory` via `AddHttpClient<T>` |

The shared core deliberately does **not** hide these differences behind a
home-grown universal framework.

## ⚠️ Failure behavior

Every JSON error uses one envelope: `{"error":{"code","message","details?}}`.
Clients classify by **status** and **`code`**, never by parsing English messages.

| Status | Code | Used for |
| --- | --- | --- |
| `400` | `invalid_json` | Missing/unsupported JSON content type, invalid UTF-8, or malformed JSON on a body endpoint |
| `404` | `not_found` | A missing task or unknown route |
| `405` | `method_not_allowed` | A method a known path does not support (with an `Allow` header) |
| `422` | `validation_error` | A valid JSON value or URL component that violates request or domain rules |
| `500` | `internal_error` | Unexpected server or persistence failure (logged internally, sanitized on the wire) |

The CLI reserves stdout for success and stderr for a concise failure line:

| Exit code | Meaning |
| --- | --- |
| `0` | Success |
| `2` | Command usage error (missing argument or non-positive ID) |
| `3` | The server returned a documented API error |
| `4` | The response had an unexpected status, content type, or JSON shape |
| `5` | Connection, DNS, TLS, or timeout failure |

## 🚫 Non-production scope

This project intentionally omits authentication, timestamps/priorities/tags,
pagination/search/bulk operations, browser UI/CORS/WebSockets/streaming, public
deployment/TLS/containers, ORMs/migrations/pooling/distributed transactions,
cross-process Markdown locking, automatic retries, and generated client SDKs.
These omissions keep attention on domain boundaries, persistence adapters,
HTTP/JSON behavior, client interoperability, and idiomatic library comparison.
