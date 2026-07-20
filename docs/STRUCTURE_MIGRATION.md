# Shared role structure migration

The course now uses the same learner-facing roles as the sibling learning
repositories:

```text
lessons/    complete explanations and runnable demonstrations
exercises/  learner starters, reference solutions, and shared tests
projects/   required applied bridges
capstones/  comparative and C#-idiomatic final destinations
```

## Path mapping

| Previous role | Current role |
| --- | --- |
| `course/<unit>/README.md` and `Samples/` | `lessons/<lesson>/` |
| `course/<unit>/Practice/Starter/` | `exercises/<lesson>/starter/` |
| `course/<unit>/Practice/Solution/` | `exercises/<lesson>/solution/` |
| `course/<unit>/Practice/Tests/` | `exercises/<lesson>/tests/` |
| no applied-project role | `projects/tasks/` |
| no comparative capstone | `capstones/comparative/` |
| `capstone/reading-log/` | `capstones/idiomatic/` |

There is no top-level `examples/` directory. Runnable lesson programs are
complete demonstrations and stay with the lesson that explains them. Incomplete
test-driven work always lives under `exercises/`.

## Numbering change

The original HTTP and composition units move from 13 and 14 to lessons 14 and
15. New lesson 13 teaches SQL and SQLite before the Tasks project and
comparative capstone depend on those concepts.

## Git history

No compatibility copies or symlinks preserve the old paths. Use Git history
before the structural migration commit when following an old link or comparing
the previous co-located layout. Current commands, manifests, solution folders,
and CI use only the shared role structure.
