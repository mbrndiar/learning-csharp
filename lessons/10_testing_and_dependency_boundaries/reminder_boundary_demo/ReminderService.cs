namespace ReminderBoundaryDemo;

public sealed class ReminderService
{
    private readonly IReminderSink sink;

    public ReminderService(IReminderSink sink) => this.sink = sink ?? throw new ArgumentNullException(nameof(sink));

    public bool TrySendDueSoon(string recipient, int daysUntilDue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient, nameof(recipient));
        ArgumentOutOfRangeException.ThrowIfNegative(daysUntilDue);

        if (daysUntilDue > 3)
        {
            return false;
        }

        sink.Send(recipient.Trim(), $"Reminder: due in {daysUntilDue} day(s).");
        return true;
    }
}
