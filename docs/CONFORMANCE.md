# Quality contract conformance

This record maps the finished learner experience to the governing learning
repository quality contract. Evidence comes from this repository and its
supported environment, not from sibling implementations.

## Course profile

- Audience: complete beginners with basic terminal and file-system skills.
- Platform: stable .NET 10 LTS, C# 14, `net10.0`.
- Environments: Linux, macOS, and Windows through the `dotnet` CLI.
- Scope: core C#/.NET through relational SQLite, local HTTP APIs/clients, an
  applied Tasks project, and comparative plus C#-idiomatic capstones.
- Required applied work: Tasks REST API/clients, comparative versioned
  configuration store, and idiomatic Reading Log.
- Boundaries: no preview features, .NET Framework, UI framework, ORM,
  authentication, cloud deployment, or production operations.

The learner-facing profile and non-goals are in [README.md](../README.md).

## Contract evidence

| Contract area | Result | Inspectable evidence |
| --- | --- | --- |
| Authority and profile | Conforms | Root entry point; [setup](SETUP.md); [authoritative sources](SOURCES.md); .NET 10 selection in `global.json`; `net10.0`/C# 14 in `Directory.Build.props`; central stable package pins. |
| Pedagogy and sequence | Conforms | Fifteen ordered `lessons/*/README.md` guides; [curriculum matrix](CURRICULUM_MATRIX.md); 21 manifest-run lesson artifacts; top-level test-driven exercises; SQL and low-level HTTP prerequisites before applied work. |
| Repository roles | Conforms | Shared `lessons/`, `exercises/`, `projects/tasks/`, and `capstones/{comparative,idiomatic}` roles with indexes; [migration guide](STRUCTURE_MIGRATION.md); no legacy `course/`, `Practice/`, `Samples/`, singular `capstone/`, or top-level `examples/` role. |
| Integrity and completeness | Conforms locally | `course-manifest.json` validates lesson/exercise/destination paths; CourseVerifier runs every declared lesson artifact, checks links/anchors and README presentation, rejects legacy roles, verifies 15 intentionally-red lesson starters, and runs three destination starter smokes. |
| Idiomaticity and currency | Conforms | SDK-style projects and `.slnx`; nullable analysis/analyzers; central package management and locks; .NET packages at 10.0.10; `Microsoft.Data.Sqlite` 10.0.10 with centrally pinned patched SQLitePCLRaw 2.1.12; Microsoft.OpenApi 3.9.0; xUnit v3/MTP v2. |
| Lifecycle and environments | Conforms locally; remote matrix pending | Locked restore, format verification, Release build, 465 solution-selected tests, lesson execution, starter checks, three coverage gates, OpenAPI/spec checks, links, and audit configuration share the manual `.github/workflows/course.yml` commands. The final three-OS manual run remains delivery evidence to obtain. |
| Projects and capstones | Conforms locally | Tasks has SQLite/Markdown stores, middleware/Minimal API servers, raw/typed clients, 2x2 interoperability, red milestones, and independent coverage. Comparative preserves frozen `comparative-kv` fixtures plus real processes. Idiomatic preserves Reading Log's JSON/HTTP/CLI behavior. |
| Refinement and validation | Conforms locally | The role migration is evidence-backed across all sibling repositories, adapted to .NET rather than copied mechanically, and validated by change class. One final mixed read-only review remains required after the complete diff is staged. |
| Git and delivery | Pending final delivery | Validated milestone commits use required trailers and push to `origin/main`. The final structural migration commit/range and remote manual workflow conclusion remain to be recorded. |
| Evidence transfer | Conforms | Sibling repositories established the shared role invariant; C# paths, projects, packages, adapter comparison, SQLite APIs, test selection, and capstone implementations are target-specific and self-contained. |

## Local validation baseline

- locked restore for 101 projects;
- formatting verification and Release build with zero warnings/errors;
- 465 passing solution-selected MTP tests, zero failures/skips;
- 15 lesson starters compile and report focused intentional failures;
- 3 project/capstone starter smoke suites pass;
- 15 lessons, 21 independently executed runnables, 3 applied destinations,
  25 formatted READMEs, 133 local links/anchors, and 102 external links;
- Tasks: 182 tests and 87.2% branch coverage;
- comparative capstone: 38 tests, frozen manifest/fixtures, real-process checks,
  and 91.2% branch coverage;
- idiomatic capstone: 71 tests and 88.3% branch coverage.

## Deliberate target-specific choices

- Complete runnable demonstrations stay with `lessons/`; incomplete learner work
  stays under `exercises/`. No top-level `examples/` role is invented.
- Most exercises supply tests for production-code work. Lesson 10 also requires
  learner-authored Fact/Theory scenarios with non-leaking meta-feedback.
- File-based C# apps reduce early ceremony and omit SDK/RID-specific virtual
  project lockfiles. Lesson 6 transitions to normal locked SDK projects.
- Microsoft.Data.Sqlite's async ADO.NET methods execute synchronously because
  SQLite has no asynchronous I/O. Lesson 13 teaches honest synchronous ownership
  rather than `Task.Run` wrappers.
- Tasks compares low-level ASP.NET Core middleware with Minimal APIs and raw
  HttpClient ownership with typed `IHttpClientFactory` clients. Minimal APIs and
  typed clients remain the preferred normal application defaults.
- OpenAPI documents are independent checked-in evidence parsed with maintained
  Microsoft.OpenApi; runtime tests remain necessary because schemas cannot prove
  behavior, lifecycle, concurrency, or cleanup.
- `projects/tasks` is a bounded applied bridge, not a third capstone.
- Comparative preserves a frozen cross-language contract; idiomatic Reading Log
  remains free to demonstrate C#-specific JSON, Minimal API, and typed-client
  design.
- Every applied destination has its own 85% branch gate so mature code elsewhere
  cannot hide gaps.

## Final review and delivery gate

Exactly one read-only mixed review inspected the complete staged migration for
correctness, prerequisite safety, starter/solution boundaries, solution
leakage, unsupported conformance claims, and generated artifacts. It found no
blocker. Its two consistency findings are resolved: Tasks now explains why its
shared async-shaped repository interface does not make SQLite I/O asynchronous,
and the learner entry point no longer claims the pending three-OS manual run has
already completed. Stale pre-migration learner-ignore paths were also removed.
Affected documentation and link checks were repeated without a second final
review; the executable baseline above was unchanged.
