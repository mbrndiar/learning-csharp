# 🧭 Unit 07 · Modeling data and behavior

## 🎯 Objectives
- Choose between a class, record, struct, and enum for a small domain model.
- Use properties and constructors to protect valid state.
- Explain value semantics versus reference semantics with examples.
- Prefer composition over giant procedural files.
- Use access boundaries so callers cannot casually break an object's invariants.
- Distinguish calendar dates, UTC instants, and durations, and parse a strict ISO date safely.

## ✅ Prerequisites
Before this unit, you should be comfortable with:
- the project/build workflow from Unit 06
- methods, parameters, and return values
- creating and using collections

## 🧠 Causal mental model
A type is a promise about **what data exists** and **what operations are allowed**.

- Use a **class** when identity and shared mutable state matter.
- Use a **record** when the main idea is the data value itself.
- Use a **struct** when the value is small, self-contained, and copied by value.
- Use an **enum** when the valid states come from a small closed set.
- Use **properties** and **constructors** to make invalid states hard to create.
- Use **composition** when one type should own or depend on other types rather than doing everything alone.

If Unit 06 taught you how code becomes an assembly, Unit 07 teaches you how to shape the code inside that assembly so the rules of your domain are visible.

## 🔤 Authentic minimal fragments
A record is good for immutable descriptive data. Prefer get-only properties
set from a validating constructor over a positional record or public `init`
setters, so every stored `GuestProfile` is already trimmed and non-blank -
there is no post-construction window where a caller could still assign it:

```csharp
public sealed record GuestProfile
{
    public GuestProfile(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Name = name.Trim();
        Email = email.Trim();
    }

    public string Name { get; }
    public string Email { get; }
}
```

A small value object can be a struct. A `readonly record struct` is still a
struct, and every struct has a compiler-generated **default value** -
`default(PartySize)` produces `Adults = 0, Children = 0, Total = 0` without
ever running the constructor. A consuming API must reject that default
explicitly instead of assuming "if it exists, the constructor already
validated it":

```csharp
public readonly record struct PartySize
{
    public PartySize(int adults, int children)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(adults);
        ArgumentOutOfRangeException.ThrowIfNegative(children);

        int total = checked(adults + children); // overflow-safe: throws OverflowException instead of wrapping
        if (total == 0)
        {
            throw new ArgumentException("A party must contain at least one guest.");
        }

        Adults = adults;
        Children = children;
        Total = total;
    }

    public int Adults { get; }
    public int Children { get; }
    public int Total { get; }
}
```

`Reservation`'s constructor is the caller that rejects the bypass: it treats
`partySize.Total <= 0` as invalid input, which also catches an unvalidated
`default(PartySize)` passed in by mistake.

A class can own behavior and internal state:

```csharp
public sealed class Reservation
{
    public ReservationState State { get; private set; } = ReservationState.Draft;

    public void Confirm() => State = ReservationState.Confirmed;
}
```

A strict ISO calendar date parse, using the invariant culture so the format
does not depend on the machine's regional settings:

```csharp
using System.Globalization;

public static class ReservationDate
{
    public static DateOnly ParseIso(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly day)
            ? day
            : throw new FormatException("Use an ISO calendar date in yyyy-MM-dd format.");
    }
}
```

## 🧠 Dates, instants, and durations
This unit's model needs three related but distinct time concepts:

- **`DateOnly`** is a calendar date with no time-of-day or time zone - "July
  24, 2026." It is the right shape for a reservation day.
- **`DateTimeOffset`** is a specific instant, anchored to an offset from UTC -
  "this exact moment, unambiguously." Use `DateTimeOffset.UtcNow` when you
  need to record when something happened.
- **`TimeSpan`** is a duration - an amount of elapsed time, not a point on the
  calendar or clock.

Reading the clock directly with `DateTimeOffset.UtcNow` is environment
input: the same code produces a different value every time it runs, which
makes it awkward to test deterministically. When exact clock behavior
matters to a test - "assert this happened within one second of now," or
"simulate the clock advancing" - the testable boundary is `TimeProvider`,
injected the same way any other collaborator would be, rather than every
type calling `DateTimeOffset.UtcNow` directly.

## ▶️ Sample project
The sample lives here:
- `course/07-modeling-data-and-behavior/Samples/ReservationTracker/ReservationTracker.csproj`

It models a tiny reservation domain with:
- `GuestProfile` as an immutable record
- `PartySize` as a value object struct
- `ReservationState` as an enum
- `Reservation` and `Table` as composed classes
- `ReservationDate.ParseIso` for strict ISO calendar date parsing

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
Calendar date: 2026-07-22
Captured instant uses UTC offset: True
Reservation status: Confirmed
Seats required: 3
```

## 👀 What to notice
- Two `PartySize` values with the same numbers compare equal because they are value-like.
- Two reservations can share the same `Table` instance because classes use reference semantics.
- The constructor decides whether a reservation is valid; callers do not bypass it.
- `Reservation.State` can be read by everyone, but only the reservation itself can change it.
- `default(PartySize)` bypasses `PartySize`'s constructor entirely and produces `Total == 0`; `Reservation` treats that as invalid input instead of trusting that every `PartySize` it receives was already validated.
- `ReservationDate.ParseIso` accepts only the exact `yyyy-MM-dd` shape under the invariant culture; `DateTimeOffset.UtcNow` captures a UTC instant, which is a different concept from the reservation's calendar `Day`.

## 🧠 Immutability and access boundaries
Immutability reduces surprises. If `GuestProfile` is immutable, other code cannot quietly change a guest after you stored it. For state that must change, keep the mutation in a small number of methods such as `Confirm` or `Cancel`.

A beginner-friendly rule:
- start immutable unless you have a concrete reason not to
- if mutation is necessary, hide it behind methods that preserve invariants
- expose `IReadOnlyList<T>` instead of a writable `List<T>` when sharing collections
- once a record's constructor validates its data, keep its properties get-only (`{ get; }`) rather than `init`-settable; a public `init` setter still lets an object initializer assign an unvalidated value after the constructor already ran, through a syntax path that never calls your validation code

## 🧩 Experiment
Try one change at a time:
1. Make the sample party size too large for the table and observe the constructor failure.
2. Create a second `Table` instance with the same values and compare it to the first one.
3. Add another reservation state such as `Seated` and decide where behavior should move.
4. Replace the shared `Table` with two separate instances and compare the reference result.
5. Try `ReservationDate.ParseIso("07/22/2026")` (a non-ISO shape) and confirm it throws `FormatException`. Then try `default(PartySize)` and confirm the reservation constructor rejects it.

## ⚠️ Common mistakes and diagnosis
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
- **Mistake:** trusting that any struct value must have passed through its constructor.
  - **Diagnosis:** `default(SomeStruct)` always exists and skips the constructor; consuming code must validate the value it receives, not just the value the constructor could have produced.
- **Mistake:** using `init` on a validated record property "to be safe."
  - **Diagnosis:** `init` still allows an object initializer to set the property directly after construction, bypassing the constructor's validation; use a get-only property instead once validation matters.

## 🧪 Practice contract
Implement the reservation model in `ModelingDataBehaviorPractice`.

### Required types
- `ReservationState` enum with `Draft`, `Confirmed`, and `Cancelled`
- `PartySize` readonly record struct with `Adults`, `Children`, and an overflow-safe `Total`
- `GuestProfile` immutable record with trimmed, get-only `Name` and `Email` (not `init`-settable)
- `Table` class with `Number`, `Seats`, and `CanSeat(PartySize)`
- `Reservation` class composed from `GuestProfile`, `PartySize`, `Table`, and `DateOnly`
- `ReservationBook` class that stores reservations and exposes a read-only view
- `ReservationDate` static helper with `ParseIso(string value)`

### Required behavior
- constructors reject invalid values
- `PartySize`'s constructor rejects negative counts and explicit all-zero input, and computes `Total` with `checked` arithmetic so an overflow throws `OverflowException` instead of wrapping
- `Reservation` starts in `Draft`
- `Confirm()` changes draft reservations to `Confirmed`
- `Cancel()` changes reservations to `Cancelled`
- confirming a cancelled reservation throws `InvalidOperationException`
- creating a reservation for a table that is too small throws `InvalidOperationException`
- creating a reservation with an unvalidated `default(PartySize)` throws `ArgumentException`
- `ReservationBook.FindByGuest(email)` matches email case-insensitively
- `ReservationBook.CountConfirmedOn(day)` counts only confirmed reservations on that day
- `ReservationDate.ParseIso(value)` parses only the strict `yyyy-MM-dd` shape under the invariant culture and throws `FormatException` for anything else

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

## 📝 Summary
Good models make the legal states of your program obvious. Records and structs are great for value-like data, classes are great for coordinated behavior and identity, enums make state sets explicit, and constructors plus access boundaries keep invalid states from spreading - including a struct's own unvalidated `default` value. `DateOnly`, `DateTimeOffset`, and `TimeSpan` model calendar dates, UTC instants, and durations as three distinct concepts, and `TimeProvider` is the seam to reach for when a test needs to control the clock.

## ❓ Review questions
1. When is a small struct a better fit than a class?
2. What does value equality mean, and which C# type shapes encourage it?
3. Why is `private set` or method-based mutation safer than public mutable properties?
4. What does composition buy you in a domain model?
5. Why should validation live inside the model as well as at the edges?
6. Why can `default(PartySize)` exist even though its constructor always rejects an all-zero party, and whose job is it to reject that default value?
7. Why is `init` not sufficient to protect a validated record property from being set to an invalid value?
8. What is the practical difference between `DateOnly`, `DateTimeOffset`, and `TimeSpan`, and when would a test need `TimeProvider` instead of calling `DateTimeOffset.UtcNow` directly?

## 📚 Microsoft Learn links
- https://learn.microsoft.com/dotnet/csharp/fundamentals/object-oriented/
- https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/
- https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/enum
- https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record
- https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/properties
- https://learn.microsoft.com/dotnet/api/system.dateonly
- https://learn.microsoft.com/dotnet/api/system.datetimeoffset
- https://learn.microsoft.com/dotnet/api/system.timespan
- https://learn.microsoft.com/dotnet/api/system.timeprovider
