# 🧱 Comparative KV architecture

```text
CLI process → Core validation → Application (injected IConfigurationStore)
       │                                  │
       └── one compact JSON envelope      └── SQLite store
                                                ├── open/configure WAL + FK
                                                ├── classify fresh/v0/v1
                                                └── immediate transactions
```

| Layer | Responsibility |
| --- | --- |
| `ComparativeKv.Core` | Restricted RFC 8259 parser, normalized values, keys, expectations, result records, and structured contract errors. |
| `ComparativeKv.Application` | Small injected store seam; no SQLite dependency. |
| `ComparativeKv.Storage.Sqlite` | Literal local files, exact v1 schema, atomic v0 migration, validation, revisions, and SQLite error mapping. |
| `ComparativeKv.Cli` | Exact argument grammar, validation order, process exit codes, and one-line UTF-8 envelopes. |
| `tests` | Fixture manifest check, selectable implementation tests, independent SQLite setup, barriers, lock helper, finite subprocess waits, and cleanup. |

The store owns exactly one connection per process invocation. It disables pooling,
sets SQLite's 10-second busy timeout before contended work, requests WAL, enables
foreign keys, and closes its connection before emitting no further process work.

`JsonValue` is a small semantic tree rather than `JsonDocument`: it preserves the
contract's last-member-wins object behavior and lets persistence/output use one
compact normalized writer.
