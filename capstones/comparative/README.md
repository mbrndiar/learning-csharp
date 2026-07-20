# 🗃️ Comparative KV — C# capstone

An idiomatic .NET 10 / C# 14 implementation of the frozen, language-neutral
`comparative-kv` **1.0.0** contract. It is a local SQLite-backed versioned
configuration store with exactly `set`, `get`, `delete`, and `list`.

## 🧭 Index

| Path | Purpose |
| --- | --- |
| [`spec/`](spec/) | Byte-identical frozen specification, scenarios, fixtures, and SHA-256 manifest. |
| [`starter/`](starter/) | Compilable guided surface with explicit milestone-incomplete behavior and no storage side effects. |
| [`solution/`](solution/) | Complete Core → Application → SQLite → CLI implementation. |
| [`tests/`](tests/) | Shared selectable fixture runner, independent SQLite checks, real process helpers, and coverage gate. |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Responsibility boundaries and data flow. |

## ✅ Prerequisites

- .NET SDK `10.0.300` or compatible .NET 10 SDK selected by the repository
  `global.json`;
- the repository central package catalog must pin
  `Microsoft.Data.Sqlite` to `10.0.10` (the capstone's `PackageReference` has
  **no version**);
- familiarity with [Lesson 13 — SQL and SQLite](../../lessons/13_sql_and_sqlite/):
  tables, constraints, transactions, SQLite locking, and WAL.

No server, network, HTTP, environment-variable configuration, or interactive
mode is part of this capstone.

## 🚀 Exact commands

From the repository root:

```bash
dotnet build capstones/comparative/solution/ComparativeKv.Solution.slnx
dotnet build capstones/comparative/starter/ComparativeKv.Starter.slnx

dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj \
  -p:ComparativeImplementation=Solution \
  -p:UseMicrosoftTestingPlatformRunner=true -- --minimum-expected-tests 1
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj \
  -p:ComparativeImplementation=Starter \
  -p:UseMicrosoftTestingPlatformRunner=true -- --minimum-expected-tests 2

mkdir -p capstones/comparative/.test-data
dotnet run --project capstones/comparative/solution/ComparativeKv.Cli/ComparativeKv.Cli.csproj \
  -- --db capstones/comparative/.test-data/comparative.db set theme --value-json '"dark"' --expect absent
dotnet run --project capstones/comparative/solution/ComparativeKv.Cli/ComparativeKv.Cli.csproj \
  -- --db capstones/comparative/.test-data/comparative.db list
```

In PowerShell, create the ignored directory with
`New-Item -ItemType Directory -Force capstones/comparative/.test-data`.

### 🔴 Starter milestone feedback

The default starter suite above is intentionally green: it checks only the
manifest and the explicit no-storage starter boundary. Select exactly one
milestone to run its shared fixture against the untouched starter. Each command
is intentionally red until that milestone is implemented and reports a stable
`COMPARATIVE_STARTER_MILESTONE_N_INCOMPLETE` diagnostic.

```bash
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj -p:ComparativeImplementation=Starter -p:ComparativeMilestone=1 -p:UseMicrosoftTestingPlatformRunner=true
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj -p:ComparativeImplementation=Starter -p:ComparativeMilestone=2 -p:UseMicrosoftTestingPlatformRunner=true
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj -p:ComparativeImplementation=Starter -p:ComparativeMilestone=3 -p:UseMicrosoftTestingPlatformRunner=true
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj -p:ComparativeImplementation=Starter -p:ComparativeMilestone=4 -p:UseMicrosoftTestingPlatformRunner=true
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj -p:ComparativeImplementation=Starter -p:ComparativeMilestone=5 -p:UseMicrosoftTestingPlatformRunner=true
```

Verify the copied evidence independently:

```bash
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj \
  -p:ComparativeImplementation=Solution -- \
  --filter-class ComparativeKv.Tests.Spec.SpecManifestTests
```

Run the independent branch-coverage gate after a Release test build:

```bash
dotnet test --project capstones/comparative/tests/ComparativeKv.Tests.csproj \
  --configuration Release --results-directory capstones/comparative/tests/TestResults \
  -p:ComparativeImplementation=Solution \
  -p:UseMicrosoftTestingPlatformRunner=true -- \
  --coverage --coverage-output comparative.cobertura.xml \
  --coverage-output-format cobertura \
  --coverage-settings capstones/comparative/tests/coverage.runsettings
dotnet run --project tools/CourseVerifier -- coverage \
  capstones/comparative/tests/TestResults 0.85
```

## 🪜 Milestones

1. 🔤 **Values** — ASCII keys, expectations, safe revisions, restricted JSON,
   duplicate-member handling, Unicode scalars, and canonical numbers.
2. 🧩 **Application + CLI** — exact grammar/precedence, injected store boundary,
   one-line envelopes, stderr discipline, and exit mapping.
3. 🗄️ **Schema + migration** — literal paths, WAL/FK/busy setup, exact v1,
   validation, fresh initialization, and atomic v0 migration.
4. 🔢 **Mutations + revisions** — immediate transactions, global revisions,
   set/delete CAS precedence, exhaustion, and BINARY ordering.
5. 🧪 **Real-process integration** — initialization/migration races, competing
   mutations, lock release/timeout, finite waits, and sidecar cleanup.

## 🧱 Implementation notes

The [architecture guide](ARCHITECTURE.md) describes the four small projects.
`tests` selects one tree with `ComparativeImplementation=Starter|Solution`; it
never uses an in-process mock to replace required process scenarios. Fixture
setup and schema assertions use an independent `Microsoft.Data.Sqlite`
connection, while the owned helper executable supplies a barrier and an OS
process that holds `BEGIN IMMEDIATE`.

## 🛟 Safety and non-production boundary

This is a learning capstone, not a production database service. It intentionally
supports only writable same-host local files and excludes remote filesystems,
symlink-dependent layouts, crash/power-loss simulation, encryption, access
control, secrets, networking, background servers, watches, batch APIs, and
cross-command transactions. Do not point it at shared or important data.

The CLI treats `--db` as literal path data, rejects SQLite URI and `:memory:`
forms, creates no parent directories, and never expands shell-like syntax.
