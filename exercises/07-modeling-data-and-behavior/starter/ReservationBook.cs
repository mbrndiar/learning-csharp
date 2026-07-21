namespace ModelingDataBehaviorPractice;

public sealed class ReservationBook
{
    // TODO: Implement Reservations. Expose a read-only view of the stored reservations without exposing this book's mutable backing collection.
    public IReadOnlyList<Reservation> Reservations => throw new NotImplementedException("Expose a read-only view of the stored reservations.");

    // TODO: Implement Add. Reject a missing reservation, then store only valid references in this book's owned collection.
    public void Add(Reservation reservation) => throw new NotImplementedException("Store a reservation.");

    // TODO: Implement FindByGuest. Reject a missing email, then match stored reservations' guest email case-insensitively and return a read-only list.
    public IReadOnlyList<Reservation> FindByGuest(string email) =>
        throw new NotImplementedException("Find reservations by guest email.");

    // TODO: Implement CountConfirmedOn. Count only the stored reservations that are confirmed and scheduled on the given day.
    public int CountConfirmedOn(DateOnly day) => throw new NotImplementedException("Count only confirmed reservations on the given day.");
}
