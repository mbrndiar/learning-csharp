#:property NoWarn=CA1707

int pagesRead = 12;
int totalPages = 30;
double percent = (double)pagesRead / totalPages * 100;
decimal bookPrice = 19.95m;
string title = "Café ☕";

Console.WriteLine(title);
Console.WriteLine($"Pages: {pagesRead}/{totalPages}");
Console.WriteLine($"Percent: {percent:0.0}%");
Console.WriteLine($"Price: {bookPrice:0.00} (decimal)");

int largestInt = int.MaxValue;
try
{
    _ = checked(largestInt + 1);
}
catch (OverflowException)
{
    Console.WriteLine("Checked overflow: OverflowException");
}
