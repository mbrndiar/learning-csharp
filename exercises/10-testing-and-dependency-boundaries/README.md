# 🧪 Exercise 10 · Testing and dependency boundaries

## 🎯 Goal
Implement `OrderService` behind its two injected boundary interfaces, and
write your own xUnit v3 fact and theory in the learner-owned test scaffold,
so the shared tests pass against the starter project.

## 📎 Related lesson
Read [Lesson 10 · Testing and dependency boundaries](../../lessons/10-testing-and-dependency-boundaries/README.md)
first, especially its 🧪 Practice contract section, before you start coding.

## 🗂️ Your task
`starter/IInventoryGateway.cs`, `starter/IReceiptStore.cs`, and
`starter/OrderReceipt.cs` are already complete — they need no changes.

### `starter/OrderService.cs` — `OrderService` sealed class
- Constructor: reject a `null` `IInventoryGateway` or `IReceiptStore` with
  `ArgumentNullException`.
- Constructor: retain the injected boundaries as this instance's
  collaborators, rather than opening or owning any external resource itself.
- `PlaceOrder(string orderId, string sku, int quantity)`: reject a
  missing/blank `orderId` or `sku` with `ArgumentException`.
- `PlaceOrder(...)`: reject `quantity` below `1` with
  `ArgumentOutOfRangeException`.
- `PlaceOrder(...)`: trim `orderId` and `sku` before using them.
- `PlaceOrder(...)`: read available inventory through
  `IInventoryGateway.GetAvailable`.
- `PlaceOrder(...)`: when available inventory is less than the requested
  quantity, throw `InvalidOperationException` **without** calling `Reserve`
  and **without** calling `IReceiptStore.Save`.
- `PlaceOrder(...)`: otherwise call `Reserve(sku, quantity)`, build an
  `OrderReceipt` from the trimmed `orderId`, `sku`, and `quantity`, save it
  through `IReceiptStore.Save`, and return it.

### `starter/OrderServiceLearnerTests.cs` — your own tests (in `starter/`, not `tests/`)
This file's placeholder comments already describe the shape you must
produce; replace the two scaffold methods to satisfy it:
- Replace `AddFactScenario()` with one enabled `[Fact]` that exercises one
  observable `OrderService` behavior.
- Replace `AddTheoryScenarios(int value)` with one enabled `[Theory]` that
  has at least two `[InlineData]` rows covering a boundary or failure
  behavior.
- Use Arrange-Act-Assert and small hand-written fakes for
  `IInventoryGateway`/`IReceiptStore` (no mocking package) where they help
  keep each test focused on one observable behavior.
- Choose the concrete scenarios, fake behavior, and assertions yourself —
  `tests/OrderServiceLearnerTestRequirements.cs` only checks that your suite
  contains an enabled `[Fact]` and an enabled `[Theory]` with at least two
  `[InlineData]` rows; it does not check which scenario, fake, or assertion
  you pick.

### Edge cases the shared tests exercise (and your own tests should consider)
- `quantity == 0` (and other non-positive quantities).
- Blank `orderId` or `sku` values.
- Available inventory exactly equal to the requested quantity (must
  succeed).
- Insufficient inventory (must avoid both reservation and receipt saving).

## 🏁 Done when
- `dotnet test` against `tests/` passes fully with the `starter/`
  implementation selected (the default) — this includes both
  `OrderServiceTests` and the `OrderServiceLearnerTestRequirements`
  meta-feedback for your own test file.
- No changes to the shared tests, member signatures, or exception messages.
- The same tests still pass when the reference `solution/` project is
  selected instead.

## ▶️ Feedback commands
Work against the starter first. `Starter` is the default, so no
`-p:CourseImplementation` property is needed:

```bash
dotnet build exercises/10-testing-and-dependency-boundaries/starter/TestingDependencyBoundariesPractice.csproj
dotnet test --project exercises/10-testing-and-dependency-boundaries/tests/TestingDependencyBoundariesPractice.Tests.csproj
```

Keep the suite running while you write `OrderServiceLearnerTests` and
implement `OrderService`, so every save reruns it automatically:

```bash
dotnet watch test --project exercises/10-testing-and-dependency-boundaries/tests/TestingDependencyBoundariesPractice.Tests.csproj
```

Compare with the reference solution only after a genuine attempt, with an
explicit `-p:CourseImplementation=Solution`:

```bash
dotnet build exercises/10-testing-and-dependency-boundaries/solution/TestingDependencyBoundariesPractice.csproj
dotnet test --project exercises/10-testing-and-dependency-boundaries/tests/TestingDependencyBoundariesPractice.Tests.csproj -p:CourseImplementation=Solution
```
