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
        var reservationDay = ReservationDate.ParseIso("2026-07-22");
        var reservation = new Reservation(guest, reservationDay, partyA, table);
        reservation.Confirm();
        DateTimeOffset capturedAtUtc = DateTimeOffset.UtcNow;

        var secondReservation = new Reservation(guest, new DateOnly(2026, 7, 23), new PartySize(2, 0), table);

        Console.WriteLine($"Party sizes equal by value: {partyA == partyB}");
        Console.WriteLine($"Same table reference reused: {ReferenceEquals(table, secondReservation.Table)}");
        Console.WriteLine($"Guest name: {reservation.Guest.Name}");
        Console.WriteLine($"Calendar date: {reservation.Day:yyyy-MM-dd}");
        Console.WriteLine($"Captured instant uses UTC offset: {capturedAtUtc.Offset == TimeSpan.Zero}");
        Console.WriteLine($"Reservation status: {reservation.State}");
        Console.WriteLine($"Seats required: {reservation.PartySize.Total}");
    }
}
