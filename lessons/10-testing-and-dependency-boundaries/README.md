# 🧭 Lesson 10 · Testing and dependency boundaries

## 🎯 Objectives
- Write xUnit v3 facts and theories using the arrange-act-assert pattern.
- Test normal, boundary, and failure behavior.
- Use hand-written fakes instead of a mocking framework for simple boundaries.
- Explain why dependency injection makes code easier to test and replace.
- Interpret test failures and understand what coverage can and cannot tell you.

## ✅ Prerequisites
Before this lesson, you should be comfortable with:
- projects and builds from Lesson 06
- modeling and abstractions from Lessons 07 and 08
- reading LINQ-based business logic from Lesson 09

## 🧠 Causal mental model
A unit test isolates one behavior, gives it controlled inputs, and checks the observable outcome.

- **Facts** are single scenario tests.
- **Theories** reuse the same test logic with several data rows.
- **AAA** means arrange the state, act once, then assert the result.
- **Dependency injection** moves volatile work behind interfaces so tests can supply small fake implementations.

Coverage is a flashlight, not a proof. High coverage can still miss important assertions, and lower coverage can still protect the critical boundaries if the tests are well chosen.

## 🔤 Authentic minimal fragments
A fact:

```csharp
[Fact]
public void SendsReminderInsideTheWindow()
{
    var sink = new FakeReminderSink();
    var service = new ReminderService(sink);

    service.TrySendDueSoon("ada@example.com", 2);

    Assert.Single(sink.Messages);
}
```

A theory:

```csharp
[Theory]
[InlineData(4)]
[InlineData(7)]
public void DoesNotSendOutsideTheWindow(int daysUntilDue)
{
}
```

## ▶️ Demonstration project
The demonstration lives here:
- `lessons/10-testing-and-dependency-boundaries/ReminderBoundaryDemo/ReminderBoundaryDemo.csproj`
- `lessons/10-testing-and-dependency-boundaries/ReminderBoundaryDemo.Tests/ReminderBoundaryDemo.Tests.csproj`

It shows a tiny service with one injected dependency and xUnit v3 tests that use a hand-written fake.

### Commands from the repository root
Build the demonstration library:

```bash
dotnet build lessons/10-testing-and-dependency-boundaries/ReminderBoundaryDemo/ReminderBoundaryDemo.csproj
```

Run the demonstration tests:

```bash
dotnet test --project lessons/10-testing-and-dependency-boundaries/ReminderBoundaryDemo.Tests/ReminderBoundaryDemo.Tests.csproj
```

### Expected output
The final summary should show five passing tests and zero failures.

```text
Test run summary: Passed!
  total: 5
  failed: 0
  succeeded: 5
```

## 👀 What to notice
- The production code depends on `IReminderSink`, not on email, HTTP, or a database.
- The tests use a tiny fake object that records calls in memory.
- One theory covers multiple outside-the-window cases with almost no duplication.
- The tests assert outcomes, not implementation trivia.

## ⚠️ Debugging failures
When a test fails, read it in this order:
1. the test name
2. the assertion message
3. the actual and expected values
4. the stack trace line in the test
5. the stack trace line in the production code

Do not start by changing the assertion blindly. First decide whether the code is wrong, the test is wrong, or the scenario setup is wrong.

## 🧠 Coverage meaning
Coverage answers "what lines were executed?" It does **not** answer "were the right assertions made?" Use coverage to find untested regions, then add meaningful tests for risky behavior such as boundaries, failure paths, and important branching logic.

## 🧩 Experiment
Try one change at a time:
1. Break the reminder message text and watch the assertion fail.
2. Add another theory row and confirm the same test logic still works.
3. Remove dependency injection and feel how much harder the service is to test.
4. Run the exercise tests with coverage and inspect which code was exercised.

## ⚠️ Common mistakes and diagnosis
- **Mistake:** testing several unrelated behaviors in one test.
  - **Diagnosis:** a failure gives you poor signal about what actually broke.
- **Mistake:** skipping failure-path tests.
  - **Diagnosis:** exceptions and boundary conditions break in production first.
- **Mistake:** mocking every dependency automatically.
  - **Diagnosis:** your tests become harder to read than the production code.
- **Mistake:** asserting internal implementation details.
  - **Diagnosis:** harmless refactors break tests even when behavior is still correct.
- **Mistake:** treating coverage as a quality score by itself.
  - **Diagnosis:** the percentage goes up while important cases remain untested.

## 🧪 Practice contract
Complete `OrderService` and the learner-owned `OrderServiceLearnerTests` in `TestingDependencyBoundariesPractice`.

### Required types
- `IInventoryGateway` with `GetAvailable(string sku)` and `Reserve(string sku, int quantity)`
- `IReceiptStore` with `Save(OrderReceipt receipt)`
- `OrderReceipt` record with `OrderId`, `Sku`, and `Quantity`
- `OrderService` with constructor-injected dependencies

### Learner-authored tests
- In `exercises/10-testing-and-dependency-boundaries/starter/OrderServiceLearnerTests.cs`, replace the two scaffold methods with one enabled `[Fact]` and one enabled `[Theory]`.
- Give the theory at least two `[InlineData]` rows, and keep each test focused on one observable `OrderService` behavior using hand-written fakes.
- The supplied meta-feedback fails clearly when either scenario shape is missing, but intentionally does not prescribe the exact inputs, fake implementation, or assertions.

### Required behavior
- constructor rejects null dependencies
- `PlaceOrder(orderId, sku, quantity)` trims text inputs and rejects blank values
- quantity must be at least 1
- read available inventory through `IInventoryGateway`
- throw `InvalidOperationException` when available inventory is insufficient
- when inventory is sufficient, call `Reserve`, create an `OrderReceipt`, save it through `IReceiptStore`, and return it

### Constraints
- no mocking package
- keep side effects behind the two injected interfaces
- do not save a receipt when reservation fails
- keep the method deterministic for the same fake inputs

### Edge cases to handle
- quantity `0`
- blank order IDs or SKUs
- inventory exactly equal to requested quantity
- insufficient inventory that should avoid both reservation and receipt saving

### Feedback commands
Build the starter project:

```bash
dotnet build exercises/10-testing-and-dependency-boundaries/starter/TestingDependencyBoundariesPractice.csproj
```

Build the reference solution:

```bash
dotnet build exercises/10-testing-and-dependency-boundaries/solution/TestingDependencyBoundariesPractice.csproj
```

Run the tests against the starter implementation while you work:

```bash
dotnet test --project exercises/10-testing-and-dependency-boundaries/tests/TestingDependencyBoundariesPractice.Tests.csproj -p:CourseImplementation=Starter
```

Run the tests against the finished solution:

```bash
dotnet test --project exercises/10-testing-and-dependency-boundaries/tests/TestingDependencyBoundariesPractice.Tests.csproj
```

Run the tests with coverage enabled:

```bash
dotnet test --project exercises/10-testing-and-dependency-boundaries/tests/TestingDependencyBoundariesPractice.Tests.csproj --coverage
```

## 📝 Summary
Testing gets easier when behavior is small, deterministic, and separated from external work. Facts and theories help you express scenarios clearly, hand-written fakes keep the boundary visible, and dependency injection lets you replace volatile collaborators with simple test doubles.

## ❓ Review questions
1. What is the difference between a fact and a theory?
2. Why does dependency injection make a service easier to test?
3. What kinds of cases belong in boundary and failure tests?
4. Why is a simple fake often enough for a unit test?
5. What can coverage tell you, and what can it not tell you?

## 📚 Microsoft Learn links
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-csharp-with-xunit
- https://learn.microsoft.com/dotnet/core/extensions/dependency-injection
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices
- https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage
