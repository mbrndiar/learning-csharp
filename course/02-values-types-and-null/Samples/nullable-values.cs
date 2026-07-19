string? missingTitle = null;
double? missingRating = null;
string shownTitle = string.IsNullOrWhiteSpace(missingTitle) ? "(untitled)" : missingTitle;
string shownRating = missingRating is null ? "unrated" : $"{missingRating.Value:0.0}★";

Console.WriteLine($"{shownTitle}: {shownRating}");

string? realTitle = "Unicode 漢字";
double? realRating = 4.5;
Console.WriteLine($"{realTitle}: {realRating:0.0}★");
