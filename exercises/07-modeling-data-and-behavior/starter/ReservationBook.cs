namespace ModelingDataBehaviorPractice;

public sealed class ReservationBook
{
    // TODO: Share a read-only view while retaining this book's ownership of its mutable reservation storage.
    public IReadOnlyList<Reservation> Reservations => throw new NotImplementedException("Expose a read-only view of the stored reservations.");

    // TODO: Reject a missing reservation and add only valid references to this book's owned collection.
    public void Add(Reservation reservation) => throw new NotImplementedException("Store a reservation.");

    // TODO: Validate and normalize the guest lookup, match addresses without casing differences, and avoid exposing backing storage.
    public IReadOnlyList<Reservation> FindByGuest(string email) =>
        throw new NotImplementedException("Find reservations by guest email.");

    // TODO: Derive the count from this book's current state without changing reservations or counting other states.
    public int CountConfirmedOn(DateOnly day) => throw new NotImplementedException("Count only confirmed reservations on the given day.");
}
