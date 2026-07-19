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
| Pedagogy and sequence | Conforms | Fourteen ordered `course/*/README.md` guides; [curriculum matrix](CURRICULUM_MATRIX.md); 19 manifest-run samples; paired starter/solution/test artifacts. Unit 11 is synchronous so Unit 12 remains the first async prerequisite. |
| Repository roles | Conforms | `README.md`, `docs/`, co-located `course/*/Practice`, `capstone/reading-log`, `.slnx` and MSBuild configuration, `CourseVerifier`, and GitHub Actions are all linked from the entry point. |
| Integrity and completeness | Conforms locally | `course-manifest.json` validates all role paths; CourseVerifier runs every declared sample and local link; all 14 untouched module starters compile and report intentional failing feedback; the capstone starter smoke suite passes. |
| Idiomaticity and currency | Conforms | SDK-style projects and `.slnx`; nullable reference types; SDK analyzers; `.editorconfig`; central package versions and lock files; xUnit v3 on native MTP v2; `System.Text.Json`; async/cancellation; Minimal APIs; finite, fully scoped HTTP timeouts. |
| Lifecycle and environments | Conforms locally | Locked restore, `dotnet format`, Release build, MTP tests, coverage, and CourseVerifier commands are documented and mirrored by `.github/workflows/validate.yml`. CI builds and tests on Ubuntu, Windows, and macOS; format, link, and coverage gates run once on Ubuntu against the same configuration. |
| Projects and capstones | Conforms | `capstone/reading-log/{README.md,SPEC.md,ARCHITECTURE.md}`; matching Starter/Solution project graphs; 65 normal, boundary, failure, timeout, cancellation, disposal, integration, and end-to-end tests; 87.1% branch coverage. |
| Refinement and validation | Conforms locally | Independent refinement challenged prerequisites, command parity, starter automation, and exception guidance. Findings were resolved in Unit 5, Unit 6, the root feedback loop, CourseVerifier starter checks, and this final matrix. |
| Git and delivery | Blocked | Local commit `e45639560ec2623ec0160dfb1e2927eea5175126` contains the course. `https://github.com/mbrndiar/learning-csharp` does not exist and the configured GitHub CLI credential is invalid, so push and remote-branch verification remain required. |
| Evidence transfer | Conforms | All durable rationale is expressed in .NET/C# terms and cites official target sources. No sibling repository path, code, or runtime dependency is required to understand or validate this repository. |

## Validation baseline

The integrated local validation produced:

- locked dependency restore for 69 projects;
- formatting verification and a Release build with zero warnings and errors;
- 188 passing solution-selected MTP tests;
- 14 compileable module starters with intentional focused failures;
- 2 passing capstone starter smoke tests;
- 14 manifest entries, 19 independently executed samples, and 31 local links;
- 65 passing capstone solution tests;
- 87.1% capstone branch coverage against an 85% gate.

External links were checked. All reachable documentation links passed; the
future repository clone URL remains a known delivery blocker until the GitHub
repository is created.

## Deliberate target-specific choices

- One supported LTS target keeps a complete beginner's environment and language
  behavior coherent. Multi-targeting and compatibility migration are non-goals.
- File-based C# 14 apps reduce initial project ceremony in Units 1-5. Unit 6
  explicitly replaces that teaching simplification with normal SDK projects.
- Lesson solutions use behavioral tests but do not each impose a coverage
  percentage. Coverage is taught in Unit 10 and enforced independently on the
  integrated capstone, where the metric is meaningful.
- Full formatting, external-link, and coverage work runs once on Ubuntu in CI;
  build and behavior tests run on all three claimed operating-system families.

## Final risk-selected diff review

Exactly one final read-only review inspected the staged repository. Its focus
was the highest-risk behavior: JSON persistence, HTTP timeout/cancellation and
resource ownership, CLI exits, native MTP configuration, CI command integrity,
and learner-facing contract consistency.

The review found one issue: the real CLI entry point passed
`CancellationToken.None`, so Ctrl+C could not reach the already tested
cancellation path. Both Starter and Solution now translate
`Console.CancelKeyPress` into a token passed to `CliApplication`. The affected
Starter/Solution builds, 65-test solution suite, 2-test starter smoke suite, and
87.1% branch-coverage gate were rerun successfully. No second review pass was
introduced.

## Delivery state

The course content, local quality gates, and focused local commit are complete.
The configured `origin` is `git@github_ms:mbrndiar/learning-csharp`; its push
fails with `Repository not found`. Overall delivery is not complete until:

1. the GitHub repository is created;
2. the external clone link returns success;
3. the focused commit is pushed to `main`;
4. the remote branch is verified to contain that commit; and
5. the configured workflow completes successfully.
