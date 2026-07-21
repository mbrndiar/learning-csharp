# 🧪 Exercise 07 · Modeling data and behavior

## 🎯 Goal
Implement a small reservation domain model — a value-object struct, an
immutable record, two composed classes, and a strict date parser — so the
shared tests pass against the starter project.

## 📎 Related lesson
Read [Lesson 07 · Modeling data and behavior](../../lessons/07-modeling-data-and-behavior/README.md)
first, especially its 🧪 Practice contract section, before you start coding.

## 🗂️ Your task
`starter/ReservationState.cs` is already complete (the `Draft`, `Confirmed`,
`Cancelled` enum) — it needs no changes.

### `starter/PartySize.cs` — `PartySize` readonly record struct
- Constructor: reject negative `adults` or `children`.
- Constructor: reject an explicitly supplied all-zero pair.
- Remember that `default(PartySize)` bypasses this constructor; the
  `Reservation` constructor must reject that unvalidated value.
- Constructor: add `adults + children` with `checked` arithmetic so an
  overflow throws `OverflowException` instead of wrapping.
- Store the validated `Adults`, `Children`, and computed `Total`.

### `starter/GuestProfile.cs` — `GuestProfile` sealed record
- Constructor: reject missing/blank `name` or `email`.
- Constructor: store the trimmed values.
- Keep `Name` and `Email` get-only (already declared that way) — do not
  switch them to `init`, since that would let an object initializer bypass
  the constructor's validation.

### `starter/Table.cs` — `Table` sealed class
- Constructor: reject a non-positive table `number` or `seats`.
- `CanSeat(PartySize partySize)`: return whether `Seats` can accommodate the
  party's `Total` guest count, without changing either object.

### `starter/Reservation.cs` — `Reservation` sealed class
- Constructor: validate the composed `GuestProfile`, `DateOnly` day,
  `PartySize`, and `Table`.
- Constructor: reject an unvalidated `default(PartySize)` with
  `ArgumentException`.
- Constructor: reject a party that does not fit the given table with
  `InvalidOperationException`.
- Constructor: start every new reservation in `ReservationState.Draft`.
- `Summary`: return a readable string that includes at least the guest's
  name and the reservation's ISO calendar day, without mutating any state.
- `Confirm()`: move a `Draft` reservation to `Confirmed`.
- `Confirm()`: throw `InvalidOperationException` when the reservation cannot
  be confirmed (for example, it is already `Cancelled`).
- `Cancel()`: move the reservation to `Cancelled`.

### `starter/ReservationBook.cs` — `ReservationBook` sealed class
- `Reservations`: expose the stored reservations as a read-only view
  (`IReadOnlyList<Reservation>`) without exposing the mutable backing
  collection.
- `Add(Reservation reservation)`: reject `null` with `ArgumentNullException`.
- `Add(Reservation reservation)`: store valid reservations.
- `FindByGuest(string email)`: validate the `email` argument, then match
  stored reservations' guest email case-insensitively and return a read-only
  list.
- `CountConfirmedOn(DateOnly day)`: count only the reservations whose state
  is `Confirmed` and whose day equals the given day.

### `starter/ReservationDate.cs` — `ReservationDate` static class
- `ParseIso(string value)`: reject missing/blank text.
- `ParseIso(string value)`: parse only the strict `yyyy-MM-dd` shape under
  `CultureInfo.InvariantCulture`.
- `ParseIso(string value)`: throw `FormatException` for any other shape
  (for example `07/24/2026`).

### Edge cases the shared tests exercise
- A child-only party such as `Adults = 0, Children = 2`.
- `PartySize` overflow via `int.MaxValue` adults plus more children.
- Email lookups that differ only by casing.
- Repeated calls to `Cancel()` and confirming after a cancellation.
- Two reservations sharing the exact same `Table` instance (reference
  semantics), and one that shares a table too small for its party.
- A reservation built from an unvalidated `default(PartySize)`.

## 🏁 Done when
- `dotnet test` against `tests/` passes fully with the `starter/`
  implementation selected (the default), with no changes to the tests,
  member signatures, or exception messages.
- The same tests still pass when the reference `solution/` project is
  selected instead.

## ▶️ Feedback commands
Work against the starter first:

```bash
dotnet build exercises/07-modeling-data-and-behavior/starter/ModelingDataBehaviorPractice.csproj
dotnet test --project exercises/07-modeling-data-and-behavior/tests/ModelingDataBehaviorPractice.Tests.csproj
dotnet watch test --project exercises/07-modeling-data-and-behavior/tests/ModelingDataBehaviorPractice.Tests.csproj
```

Compare with the reference solution only after a genuine attempt:

```bash
dotnet build exercises/07-modeling-data-and-behavior/solution/ModelingDataBehaviorPractice.csproj
dotnet test --project exercises/07-modeling-data-and-behavior/tests/ModelingDataBehaviorPractice.Tests.csproj -p:CourseImplementation=Solution
```
