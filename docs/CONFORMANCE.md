# Quality contract conformance

This record maps the finished learner experience to the governing learning
repository quality contract. It records target-repository evidence rather than
depending on another course.

## Course profile

- Audience: complete beginners with basic terminal and file-system skills.
- Platform: stable .NET 10 LTS, C# 14, `net10.0`.
- Environments: Linux, macOS, and Windows through the `dotnet` CLI.
- Scope: core C# and .NET application development through a local Minimal API
  and `HttpClient` client.
- Required project: personal book catalog and reading journal.
- Boundaries: no preview features, .NET Framework, UI framework, ORM, cloud
  deployment, authentication, or production operations.

The learner-facing profile and non-goals are in [README.md](../README.md).

## Contract evidence

| Contract area | Result | Inspectable evidence |
| --- | --- | --- |
| Authority and profile | Conforms | Root learner entry point; [setup](SETUP.md); [authoritative sources](SOURCES.md); .NET 10 selection in `global.json`; `net10.0` and C# 14 in `Directory.Build.props`. |
| Pedagogy and sequence | Conforms | Fourteen ordered `course/*/README.md` guides; [curriculum matrix](CURRICULUM_MATRIX.md); 19 manifest-run samples; paired starter/solution/test artifacts; dates, instants, durations, overflow, parameter passing, aliases, and cost reasoning now appear before later work depends on them. |
| Repository roles | Conforms | `README.md`, `docs/`, co-located `course/*/Practice`, `capstone/reading-log`, `.slnx` and MSBuild configuration, `CourseVerifier`, and GitHub Actions are linked from the entry point. All 17 tracked READMEs use the verified visual hierarchy. |
| Integrity and completeness | Conforms locally | `course-manifest.json` validates all role paths; CourseVerifier runs every declared sample, checks local links and anchors, and enforces README presentation; all 14 untouched module starters compile and report intentional failing feedback; the capstone starter smoke suite passes. |
| Idiomaticity and currency | Conforms | SDK-style projects and `.slnx`; nullable reference types; SDK analyzers; `.editorconfig`; central package versions and lock files; .NET packages at stable 10.0.10; xUnit v3 on native MTP v2; `System.Text.Json`; strict invariant date parsing; async/cancellation; Minimal APIs; finite, fully scoped HTTP timeouts. |
| Lifecycle and environments | Conforms locally | Locked restore, `dotnet format`, Release build, MTP tests, coverage, and CourseVerifier commands are documented and mirrored by `.github/workflows/validate.yml`. Its matrix builds and tests on Ubuntu, Windows, and macOS; format, link, and coverage gates run once on Ubuntu against the same configuration. |
| Projects and capstones | Conforms | `capstone/reading-log/{README.md,SPEC.md,ARCHITECTURE.md}`; matching Starter/Solution project graphs; normal, boundary, failure, timeout, cancellation, disposal, malformed-data, overlapping-file-access, integration, and end-to-end tests; branch coverage above the 85% gate. |
| Refinement and validation | Conforms locally | Exactly two independent initial full-course reviews challenged pedagogy and technical behavior. Verified findings were resolved in learner guides, samples, starters, solutions, tests, dependencies, CourseVerifier, and the reconciled matrix. The materially mixed delivered diff receives exactly one read-only mixed final review after integrated validation. |
| Git and delivery | Verifiable from Git | The focused refinement commit preserves unrelated local workflow changes, includes the required trailers, and is delivered to the tracked `origin/main` branch. The final response records the verified commit hash and branch. |
| Evidence transfer | Conforms | `learning-python` supplied transient candidate invariants only. All durable rationale is expressed in C#/.NET terms and verified in this repository; no sibling path, code, layout, or runtime dependency is required. |

## Validation baseline

The integrated local validation produced:

- locked dependency restore for 69 projects;
- formatting verification and a Release build with zero warnings and errors;
- 201 passing solution-selected MTP tests, including 71 capstone tests;
- 14 compileable module starters with intentional focused failures;
- 2 passing capstone starter smoke tests;
- 14 manifest entries, 19 independently executed samples, 17 formatted
  READMEs, and 47 local links with anchor validation;
- 88.3% capstone branch coverage against an 85% gate.

External documentation links were checked from the supported environment.

## Deliberate target-specific choices

- One supported LTS target keeps a complete beginner's environment and language
  behavior coherent. Multi-targeting and compatibility migration are non-goals.
- File-based C# 14 apps reduce initial project ceremony in Units 1-5. Unit 6
  explicitly replaces that teaching simplification with normal SDK projects.
- `DateOnly`, `DateTimeOffset`, UTC, `TimeSpan`, and the `TimeProvider` boundary
  are taught before the capstone; broader time-zone conversion remains outside
  this local reading-log scope.
- XML comments are taught for IDE-visible public contracts. Generated API sites
  are outside scope because the course does not publish a reusable package.
- Cost is taught at concrete beginner decisions: repeated enumeration,
  materialization, linear scans, keyed lookup, and bounded concurrency. Formal
  complexity analysis is not a promised outcome.
- Lesson solutions use behavioral tests but do not each impose a coverage
  percentage. Coverage is taught in Unit 10 and enforced independently on the
  integrated capstone, where the metric is meaningful.
- Full formatting, external-link, and coverage work runs once on Ubuntu in the
  workflow; build and behavior tests run on all three claimed operating-system
  families.

## Final risk-selected diff review

The resulting change materially mixes behavioral fixes, tests, dependencies,
learner-facing prose, source explanations, and presentation. Exactly one
read-only mixed diff review checks behavioral correctness, compatibility,
contract evidence, sequencing, starter/solution parity, milestone guidance, and
solution leakage. High-confidence findings are corrected and the affected
checks repeated without a second final review.

## Delivery evidence

The repository has a configured `origin`, and local `main` tracks
`origin/main`. Delivery is complete only when the focused refinement commit is
pushed and the remote branch is verified to contain it; the final response is
the authoritative record of that hash and branch.
