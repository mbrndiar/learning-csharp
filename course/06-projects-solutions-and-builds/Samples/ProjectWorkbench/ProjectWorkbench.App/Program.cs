using ProjectWorkbench.Text;

namespace ProjectWorkbench.App;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("Project: ProjectWorkbench.App");
        Console.WriteLine($"Own assembly: {typeof(Program).Assembly.GetName().Name}");
        Console.WriteLine($"Referenced assembly: {typeof(GreetingComposer).Assembly.GetName().Name}");
        Console.WriteLine($"Message: {GreetingComposer.Create("Ada")}");
        Console.WriteLine($"Output folder pattern: bin/{BuildMetadata.Configuration}/net10.0/");
    }
}
