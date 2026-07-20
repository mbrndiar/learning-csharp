#:property NoWarn=CA1707

int current = 3;
while (current > 0)
{
    Console.WriteLine(current);
    current--;
}

Console.WriteLine("Lift off!");

foreach (char letter in "GO")
{
    Console.WriteLine(letter);
}
