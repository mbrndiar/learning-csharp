using ReservationTracker.Domain;

namespace ReservationTracker;

internal static class Program
{
    private static void Main()
    {
        var partyA = new PartySize(2, 1);
        var partyB = new PartySize(2, 1);
        var guest = new GuestProfile("Ada Lovelace", "ada@example.com");
        var table = new Table(4, 4);
        var reservation = new Reservation(guest, new DateOnly(2026, 7, 22), partyA, table);
        reservation.Confirm();

        var secondReservation = new Reservation(guest, new DateOnly(2026, 7, 23), new PartySize(2, 0), table);

        Console.WriteLine($"Party sizes equal by value: {partyA == partyB}");
        Console.WriteLine($"Same table reference reused: {ReferenceEquals(table, secondReservation.Table)}");
        Console.WriteLine($"Guest name: {reservation.Guest.Name}");
        Console.WriteLine($"Reservation status: {reservation.State}");
        Console.WriteLine($"Seats required: {reservation.PartySize.Total}");
    }
}
