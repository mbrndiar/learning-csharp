using System.Collections.Generic;

string[] planets = new string[] { "Mercury", "Venus", "Earth" };
Console.WriteLine($"Array index 1: {planets[1]}");

List<string> snacks = new List<string> { "apple", "pear" };
snacks.Add("banana");
Console.WriteLine($"List count: {snacks.Count}");

Dictionary<string, int> inventory = new Dictionary<string, int>
{
    ["apple"] = 2,
    ["pear"] = 1,
};
Console.WriteLine($"Dictionary lookup for apple: {inventory["apple"]}");

HashSet<string> tags = new HashSet<string> { "csharp", "beginner", "csharp" };
Console.WriteLine($"HashSet count after duplicates: {tags.Count}");
