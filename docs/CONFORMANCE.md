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
| Integrity and completeness | Conforms locally | `course-manifest.json` validates lesson/exercise/destination paths; CourseVerifier runs every declared lesson artifact, checks links/anchors and README presentation, enforces the instructional naming boundary, rejects legacy roles, verifies 15 intentionally-red lesson starters, and runs three destination starter smokes. |
| Idiomaticity and currency | Conforms | Ordered instructional slugs use `NN-kebab-case`; ordered file apps use `NN-PascalCase.cs`; buildable lesson directories match PascalCase project identities; SDK-style projects and `.slnx`; nullable analysis/analyzers; central package management and locks; .NET packages at 10.0.10, Microsoft.OpenApi 3.9.0, patched SQLitePCLRaw 2.1.12, and xUnit v3/MTP v2; applied trees enforce one top-level type per matching file; Tasks separates Core, HTTP protocol, server infrastructure/adapters, client, and CLI. |
| Lifecycle and environments | Conforms locally; remote matrix deliberately deferred | Locked restore, format verification, Release build, 465 tests run with an explicit `-p:CourseImplementation=Solution` selector (exercise test projects default to the learner starter), lesson execution, starter checks, three coverage gates, OpenAPI/spec checks, links, and audit configuration share the manual `.github/workflows/course.yml` commands. Per user instruction, the workflow remains manual-only and undispatched. |
| Projects and capstones | Conforms locally | Tasks uses seven inward-pointing projects: Core + HTTP contract, shared server persistence/configuration, two leaf server adapters, reusable client, and CLI host. Comparative preserves frozen `comparative-kv` fixtures/processes. Idiomatic preserves Reading Log behavior. All three applied trees use one top-level type per file. |
| Refinement and validation | Conforms locally | The role and naming migrations are evidence-backed, adapted to .NET rather than copied mechanically, validated by change class, and independently reviewed once after the complete diff was staged. |
| Git and delivery | Conforms | Validated milestone and naming-migration commits use required trailers and push to `origin/main`; the GitHub Actions workflow remains manual-only and undispatched. |
| Evidence transfer | Conforms | Sibling repositories established the shared role invariant; C# paths, projects, packages, adapter comparison, SQLite APIs, test selection, and capstone implementations are target-specific and self-contained. |

## Local validation baseline

- locked restore for 103 projects;
- formatting verification and Release build with zero warnings/errors;
- 465 passing MTP tests, zero failures/skips, run with an explicit
  `-p:CourseImplementation=Solution` selector because exercise test projects
  default to the learner starter;
- 15 lesson starters compile and report focused intentional failures;
- 3 project/capstone starter smoke suites pass;
- 15 lessons, 21 independently executed runnables, 3 applied destinations,
  251 structurally checked applied source files, 25 formatted READMEs,
  133 local links/anchors, and 102 external links;
- Tasks: 182 tests and 87.2% branch coverage;
- comparative capstone: 38 tests, frozen manifest/fixtures, real-process checks,
  and 91.2% branch coverage;
- idiomatic capstone: 71 tests and 88.3% branch coverage.

## Deliberate target-specific choices

- Complete runnable demonstrations stay with `lessons/`; incomplete learner work
  stays under `exercises/`. No top-level `examples/` role is invented.
- Numeric order remains visible in `NN-kebab-case` lesson/exercise slugs and
  `NN-PascalCase.cs` file apps. Buildable lesson project directories instead
  match their PascalCase `.csproj` identity; lowercase role directories remain
  structural rather than .NET identities.
- Most exercises supply tests for production-code work. Lesson 10 also requires
  learner-authored Fact/Theory scenarios with non-leaking meta-feedback.
- Exercise test projects default `CourseImplementation` to `Starter` so the
  shortest no-property command exercises learner work; the reference solution,
  full-suite runs, and the manual workflow select it with an explicit
  `-p:CourseImplementation=Solution`. Selector-specific `bin/obj` paths prevent
  Starter and Solution dependency metadata from contaminating each other.
- File-based C# apps reduce early ceremony and omit SDK/RID-specific virtual
  project lockfiles. Lesson 6 transitions to normal locked SDK projects.
- Microsoft.Data.Sqlite's async ADO.NET methods execute synchronously because
  SQLite has no asynchronous I/O. Lesson 13 teaches honest synchronous ownership
  rather than `Task.Run` wrappers.
- Tasks compares low-level ASP.NET Core middleware with Minimal APIs and raw
  HttpClient ownership with typed `IHttpClientFactory` clients. Minimal APIs and
  typed clients remain the preferred normal application defaults.
- Tasks keeps wire/routing/serialization behavior in `Tasks.Http`, domain and
  application rules in `Tasks.Core`, persistence/configuration in shared
  `Tasks.Server`, HTTP adapters in leaf server executables, and process concerns
  in `Tasks.Cli`.
- One top-level type per matching file is enforced for applied project/capstone
  starter and solution code. Lessons/exercises retain a pedagogical exception
  when co-locating tiny types materially improves comprehension.
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

The naming migration's single final review confirmed the instructional slugs,
file/project identities, 103-project inventory, manifest, and naming verifier.
The subsequent learner-first test-default change also received exactly one final
read-only review. It found no blocker and independently confirmed all 15 Starter
defaults, selector-isolated `bin/obj` paths, dual locked restores, explicit
Solution build/test selection, default-red and explicit-green behavior, Lesson
10's comment-only test scaffold, and the unchanged manual-only workflow.
Affected documentation and link checks were repeated without a second review;
the 465-test executable baseline remained green.
