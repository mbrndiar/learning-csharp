namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed record CreateBookRequest(string? Title, string? Author, int YearPublished);
