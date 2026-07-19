using System.Collections.Generic;

string[] original = new string[] { "red", "green", "blue" };
List<string> copiedList = new List<string>(original);

copiedList[0] = "purple";

Console.WriteLine($"Original array first item: {original[0]}");
Console.WriteLine($"Copied list first item: {copiedList[0]}");
