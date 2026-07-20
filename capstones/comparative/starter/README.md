# 🧭 Comparative KV starter

This tree deliberately compiles before it conforms. Its public Core, Application,
SQLite, and CLI surfaces match [`../solution/`](../solution/), but operations stop with an explicit
`MilestoneIncompleteException` (the executable emits one `incomplete` envelope)
and never opens or creates a database.

Work in milestone order:

1. 🔤 implement keys, expectations, and restricted JSON in `ComparativeKv.Core`;
2. 🧩 make the exact CLI call an injected application/store seam;
3. 🗄️ add literal-path SQLite initialization, validation, and v0 migration;
4. 🔢 add atomic revisions, CAS, deletion, and ordering;
5. 🧪 pass the shared real-process fixtures under [`../tests/`](../tests/).

Keep the public types and method signatures intact so
`-p:ComparativeImplementation=Starter` continues to select this tree.
