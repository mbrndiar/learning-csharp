int score = 95;
string label = score switch
{
    >= 90 => "excellent",
    >= 70 => "good",
    >= 50 => "pass",
    _ => "needs work",
};
Console.WriteLine($"{score}: {label}");

score = 72;
label = score switch
{
    >= 90 => "excellent",
    >= 70 => "good",
    >= 50 => "pass",
    _ => "needs work",
};
Console.WriteLine($"{score}: {label}");

score = 44;
label = score switch
{
    >= 90 => "excellent",
    >= 70 => "good",
    >= 50 => "pass",
    _ => "needs work",
};
Console.WriteLine($"{score}: {label}");
