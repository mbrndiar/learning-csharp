# 🧪 Exercises

Every lesson has a matching test-driven exercise. Most exercises ask you to
implement production code while supplied tests provide focused feedback. The
testing lesson also asks you to complete tests so you practice choosing normal,
boundary, and failure scenarios yourself.

## 🗂️ Exercise roles

```text
exercises/<lesson>/
├── starter/   # learner-owned implementation
├── solution/  # one complete reference approach
└── tests/     # shared behavioral feedback
```

The untouched starter must restore and compile. Its selected behavioral tests
fail intentionally and name unfinished work; accidental wiring or compile
failures are not accepted feedback. The solution passes the same public
contract. Tests assert observable behavior rather than requiring identical
implementation syntax.

## 🔁 Working loop

1. Read the matching lesson and exercise contract.
2. Build the starter.
3. Run the shared tests with `-p:CourseImplementation=Starter`.
4. Fix one failing behavior at a time.
5. Add your own boundary checks where the lesson asks for them.
6. Compare with `solution/` only after a genuine attempt.

## 🗺️ Exercise index

| Exercise | Focus |
| --- | --- |
| [01 · First program](01_first_program/) | Input validation and exact observable output |
| [02 · Values, types, and null](02_values_types_and_null/) | Numeric/null representation and formatting |
| [03 · Decisions and repetition](03_decisions_and_repetition/) | Branches, loops, and termination |
| [04 · Collections and iteration](04_collections_and_iteration/) | Cleaning, counting, lookup, and duplicates |
| [05 · Methods, errors, and debugging](05_methods_errors_and_debugging/) | Method contracts and specific exceptions |
| [06 · Projects, solutions, and builds](06_projects_solutions_and_builds/) | Project/build metadata and deterministic commands |
| [07 · Modeling data and behavior](07_modeling_data_and_behavior/) | Types, invariants, equality, and strict dates |
| [08 · Abstractions, generics, and delegates](08_abstractions_generics_and_delegates/) | Interfaces, constraints, callbacks, and closures |
| [09 · LINQ and transformations](09_linq_and_transformations/) | Deferred pipelines and materialized reports |
| [10 · Testing and dependency boundaries](10_testing_and_dependency_boundaries/) | Production behavior plus learner-written tests |
| [11 · Files, streams, and JSON](11_files_streams_and_json/) | Safe paths, encoding, malformed data, and atomic save |
| [12 · Async, cancellation, and concurrency](12_async_cancellation_and_concurrency/) | Owned tasks, cancellation, and bounded work |
| [13 · SQL and SQLite](13_sql_and_sqlite/) | Schema, parameters, row mapping, and transactions |
| [14 · HTTP clients and Minimal APIs](14_http_clients_and_minimal_apis/) | Middleware/Minimal API and raw/typed client contracts |
| [15 · Application composition](15_application_composition/) | Boundaries, configuration, CLI exits, and adapter wiring |
