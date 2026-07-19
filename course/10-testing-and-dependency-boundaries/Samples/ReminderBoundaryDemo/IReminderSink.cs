namespace ReminderBoundaryDemo;

public interface IReminderSink
{
    void Send(string recipient, string message);
}
