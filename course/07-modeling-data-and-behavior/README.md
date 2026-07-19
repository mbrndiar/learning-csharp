# Unit 07 - Modeling data and behavior

## Objectives
- Choose between a class, record, struct, and enum for a small domain model.
- Use properties and constructors to protect valid state.
- Explain value semantics versus reference semantics with examples.
- Prefer composition over giant procedural files.
- Use access boundaries so callers cannot casually break an object's invariants.

## Prerequisites
Before this unit, you should be comfortable with:
- the project/build workflow from Unit 06
- methods, parameters, and return values
- creating and using collections

## Causal mental model
A type is a promise about **what data exists** and **what operations are allowed**.

- Use a **class** when identity and shared mutable state matter.
- Use a **record** when the main idea is the data value itself.
- Use a **struct** when the value is small, self-contained, and copied by value.
- Use an **enum** when the valid states come from a small closed set.
- Use **properties** and **constructors** to make invalid states hard to create.
- Use **composition** when one type should own or depend on other types rather than doing everything alone.

If Unit 06 taught you how code becomes an assembly, Unit 07 teaches you how to shape the code inside that assembly so the rules of your domain are visible.

## Authentic minimal fragments
A record is good for immutable descriptive data:

```csharp
public sealed record GuestProfile(string Name, string Email);
```

A small value object can be a struct:

```csharp
public readonly record struct PartySize(int Adults, int Children)
{
    public int Total => Adults + Children;
}
```

A class can own behavior and internal state:

```csharp
public sealed class Reservation
{
    public ReservationState State { get; private set; } = ReservationState.Draft;

    public void Confirm() => State = ReservationState.Confirmed;
}
```

## Sample project
The sample lives here:
- `course/07-modeling-data-and-behavior/Samples/ReservationTracker/ReservationTracker.csproj`

It models a tiny reservation domain with:
- `GuestProfile` as an immutable record
- `PartySize` as a value object struct
- `ReservationState` as an enum
- `Reservation` and `Table` as composed classes

### Commands from the repository root
Build the sample:

```bash
dotnet build course/07-modeling-data-and-behavior/Samples/ReservationTracker/ReservationTracker.csproj
```

Run it:

```bash
dotnet run --project course/07-modeling-data-and-behavior/Samples/ReservationTracker/ReservationTracker.csproj
```

### Expected output

```text
Party sizes equal by value: True
Same table reference reused: True
Guest name: Ada Lovelace
Reservation status: Confirmed
Seats required: 3
```

## What to notice
- Two `PartySize` values with the same numbers compare equal because they are value-like.
- Two reservations can share the same `Table` instance because classes use reference semantics.
- The constructor decides whether a reservation is valid; callers do not bypass it.
- `Reservation.State` can be read by everyone, but only the reservation itself can change it.

## Immutability and access boundaries
Immutability reduces surprises. If `GuestProfile` is immutable, other code cannot quietly change a guest after you stored it. For state that must change, keep the mutation in a small number of methods such as `Confirm` or `Cancel`.

A beginner-friendly rule:
- start immutable unless you have a concrete reason not to
- if mutation is necessary, hide it behind methods that preserve invariants
- expose `IReadOnlyList<T>` instead of a writable `List<T>` when sharing collections

## Experiment
Try one change at a time:
1. Make the sample party size too large for the table and observe the constructor failure.
2. Create a second `Table` instance with the same values and compare it to the first one.
3. Add another reservation state such as `Seated` and decide where behavior should move.
4. Replace the shared `Table` with two separate instances and compare the reference result.

## Common mistakes and diagnosis
- **Mistake:** using public writable properties for everything.
  - **Diagnosis:** unrelated code can put the object into impossible states.
- **Mistake:** choosing a class for every tiny value.
  - **Diagnosis:** equality becomes about identity when the real question was whether the data matches.
- **Mistake:** putting validation only in UI or caller code.
  - **Diagnosis:** invalid objects still leak in from tests, scripts, or future callers.
- **Mistake:** exposing `List<T>` directly.
  - **Diagnosis:** callers can add or remove items behind the owner's back.
- **Mistake:** assuming folders create behavior boundaries automatically.
  - **Diagnosis:** boundaries come from types, constructors, properties, and access modifiers.

## Practice contract
Implement the reservation model in `ModelingDataBehaviorPractice`.

### Required types
- `ReservationState` enum with `Draft`, `Confirmed`, and `Cancelled`
- `PartySize` readonly record struct with `Adults`, `Children`, and `Total`
- `GuestProfile` immutable record with trimmed `Name` and `Email`
- `Table` class with `Number`, `Seats`, and `CanSeat(PartySize)`
- `Reservation` class composed from `GuestProfile`, `PartySize`, `Table`, and `DateOnly`
- `ReservationBook` class that stores reservations and exposes a read-only view

### Required behavior
- constructors reject invalid values
- `Reservation` starts in `Draft`
- `Confirm()` changes draft reservations to `Confirmed`
- `Cancel()` changes reservations to `Cancelled`
- confirming a cancelled reservation throws `InvalidOperationException`
- creating a reservation for a table that is too small throws `InvalidOperationException`
- `ReservationBook.FindByGuest(email)` matches email case-insensitively
- `ReservationBook.CountConfirmedOn(day)` counts only confirmed reservations on that day

### Constraints
- reject `null`, empty, or whitespace-only text where it matters
- reject negative counts and zero-seat tables
- do not expose a writable collection to callers
- keep output and equality behavior deterministic

### Edge cases to handle
- child-only parties such as `Adults = 0, Children = 2`
- email lookups with different casing
- repeated calls to `Cancel()`
- a reservation that shares the same `Table` object as another reservation

### Feedback commands
Build the starter project:

```bash
dotnet build course/07-modeling-data-and-behavior/Practice/Starter/ModelingDataBehaviorPractice.csproj
```

Build the reference solution:

```bash
dotnet build course/07-modeling-data-and-behavior/Practice/Solution/ModelingDataBehaviorPractice.csproj
```

Run the tests against the starter implementation while you work:

```bash
dotnet test --project course/07-modeling-data-and-behavior/Practice/Tests/ModelingDataBehaviorPractice.Tests.csproj -p:CourseImplementation=Starter
```

Run the tests against the finished solution:

```bash
dotnet test --project course/07-modeling-data-and-behavior/Practice/Tests/ModelingDataBehaviorPractice.Tests.csproj
```

## Summary
Good models make the legal states of your program obvious. Records and structs are great for value-like data, classes are great for coordinated behavior and identity, enums make state sets explicit, and constructors plus access boundaries keep invalid states from spreading.

## Review questions
1. When is a small struct a better fit than a class?
2. What does value equality mean, and which C# type shapes encourage it?
3. Why is `private set` or method-based mutation safer than public mutable properties?
4. What does composition buy you in a domain model?
5. Why should validation live inside the model as well as at the edges?

## Microsoft Learn links
- https://learn.microsoft.com/dotnet/csharp/fundamentals/object-oriented/
- https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/
- https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/enum
- https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record
- https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/properties
