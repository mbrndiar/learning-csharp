namespace LearningCSharp.Exercises.HttpClientsAndMinimalApis.Client;

public sealed record BookDto(Guid Id, string Title, string Author, int YearPublished);
