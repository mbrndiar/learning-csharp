#:property NoWarn=CA1707

using System.Collections.Generic;

string[] original = new string[] { "red", "green", "blue" };
List<string> copiedList = new List<string>(original);
List<string> aliasList = copiedList;

aliasList[0] = "purple";
List<string> sameContents = new List<string>(copiedList);

Console.WriteLine($"Original array first item: {original[0]}");
Console.WriteLine($"Copied list first item: {copiedList[0]}");
Console.WriteLine($"Alias and copied list are the same object: {ReferenceEquals(aliasList, copiedList)}");
Console.WriteLine($"Copied list still matches original elements: {copiedList.SequenceEqual(original)}");
Console.WriteLine($"Different lists are the same object: {ReferenceEquals(copiedList, sameContents)}");
Console.WriteLine($"Different lists contain equal elements: {copiedList.SequenceEqual(sameContents)}");
