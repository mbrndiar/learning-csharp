namespace LearningCSharp.Course.Unit13.Practice;

/// <summary>
/// One row from the Books table, mapped explicitly by <see cref="BookRepository"/>.
/// <c>Rating</c> is nullable because the column allows NULL until a reader rates the book.
/// </summary>
public sealed record Book(int Id, string Title, string Author, string Isbn, int PublicationYear, int? Rating);
