namespace ProjectWorkbench.Text;

public static class GreetingComposer
{
    public static string Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return $"Hello from a referenced class library, {name.Trim()}.";
    }
}
