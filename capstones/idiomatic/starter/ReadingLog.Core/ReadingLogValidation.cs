namespace ReadingLog.Core;

public static class ReadingLogValidation
{
    public static CreateBookRequest ValidateCreateBookRequest(CreateBookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var title = NormalizeRequired(request.Title, "title", 200, errors);
        var author = NormalizeRequired(request.Author, "author", 120, errors);
        var isbn = NormalizeOptional(request.Isbn, 32, errors, "isbn");

        if (request.PublicationYear is < 1450 or > 2100)
        {
            AddError(errors, "publicationYear", "Publication year must be between 1450 and 2100.");
        }

        ThrowIfAny(errors);

        // TODO(m1): Add a stricter ISBN rule once milestone 1 is complete.
        return new CreateBookRequest(title, author, request.PublicationYear, isbn);
    }

    public static CreateReadingEntryRequest ValidateCreateReadingEntryRequest(CreateReadingEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var notes = NormalizeOptional(request.Notes, 2000, errors, "notes");

        if (request.BookId == Guid.Empty)
        {
            AddError(errors, "bookId", "BookId is required.");
        }

        if (request.PagesRead <= 0)
        {
            AddError(errors, "pagesRead", "PagesRead must be greater than zero.");
        }

        if (request.Rating is < 1 or > 5)
        {
            AddError(errors, "rating", "Rating must be between 1 and 5.");
        }

        if (request.FinishedOn is not null && request.FinishedOn.Value < request.StartedOn)
        {
            AddError(errors, "finishedOn", "FinishedOn cannot be earlier than StartedOn.");
        }

        ThrowIfAny(errors);
        return new CreateReadingEntryRequest(
            request.BookId,
            request.StartedOn,
            request.FinishedOn,
            request.PagesRead,
            request.Rating,
            notes);
    }

    public static void ValidateSnapshot(ReadingLogSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var bookIds = new HashSet<Guid>();
        var entryIds = new HashSet<Guid>();

        for (var index = 0; index < snapshot.Books.Count; index++)
        {
            Book? book = snapshot.Books[index];
            if (book is null)
            {
                AddError(errors, $"books[{index}]", "Book must not be null.");
                continue;
            }

            ValidateBook(book, index, errors);
            if (!bookIds.Add(book.Id))
            {
                AddError(errors, $"books[{index}].id", "Book ids must be unique.");
            }
        }

        for (var index = 0; index < snapshot.Entries.Count; index++)
        {
            ReadingEntry? entry = snapshot.Entries[index];
            if (entry is null)
            {
                AddError(errors, $"entries[{index}]", "Entry must not be null.");
                continue;
            }

            ValidateEntry(entry, index, errors);
            if (!entryIds.Add(entry.Id))
            {
                AddError(errors, $"entries[{index}].id", "Entry ids must be unique.");
            }

            if (!bookIds.Contains(entry.BookId))
            {
                AddError(errors, $"entries[{index}].bookId", "Entry must reference an existing book.");
            }
        }

        ThrowIfAny(errors);
    }

    private static void ValidateBook(Book book, int index, Dictionary<string, List<string>> errors)
    {
        if (book.Id == Guid.Empty)
        {
            AddError(errors, $"books[{index}].id", "Book id is required.");
        }

        _ = NormalizeRequired(book.Title, $"books[{index}].title", 200, errors);
        _ = NormalizeRequired(book.Author, $"books[{index}].author", 120, errors);
        _ = NormalizeOptional(book.Isbn, 32, errors, $"books[{index}].isbn");

        if (book.PublicationYear is < 1450 or > 2100)
        {
            AddError(errors, $"books[{index}].publicationYear", "Publication year must be between 1450 and 2100.");
        }
    }

    private static void ValidateEntry(ReadingEntry entry, int index, Dictionary<string, List<string>> errors)
    {
        if (entry.Id == Guid.Empty)
        {
            AddError(errors, $"entries[{index}].id", "Entry id is required.");
        }

        if (entry.BookId == Guid.Empty)
        {
            AddError(errors, $"entries[{index}].bookId", "Book id is required.");
        }

        if (entry.PagesRead <= 0)
        {
            AddError(errors, $"entries[{index}].pagesRead", "PagesRead must be greater than zero.");
        }

        if (entry.Rating is < 1 or > 5)
        {
            AddError(errors, $"entries[{index}].rating", "Rating must be between 1 and 5.");
        }

        if (entry.FinishedOn is not null && entry.FinishedOn.Value < entry.StartedOn)
        {
            AddError(errors, $"entries[{index}].finishedOn", "FinishedOn cannot be earlier than StartedOn.");
        }

        _ = NormalizeOptional(entry.Notes, 2000, errors, $"entries[{index}].notes");
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength, Dictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            AddError(errors, fieldName, "A value is required.");
            return string.Empty;
        }

        if (normalized.Length > maxLength)
        {
            AddError(errors, fieldName, $"Value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, Dictionary<string, List<string>> errors, string fieldName)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            AddError(errors, fieldName, "Optional values cannot be blank when provided.");
            return null;
        }

        if (normalized.Length > maxLength)
        {
            AddError(errors, fieldName, $"Value must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    private static void ThrowIfAny(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        throw new DomainValidationException(errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
    }
}
