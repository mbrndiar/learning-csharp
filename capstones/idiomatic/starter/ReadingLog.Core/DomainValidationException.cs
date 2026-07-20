namespace ReadingLog.Core;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(CreateMessage(errors))
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private static string CreateMessage(IReadOnlyDictionary<string, string[]> errors)
    {
        return string.Join(
            Environment.NewLine,
            errors.SelectMany(pair => pair.Value.Select(message => $"{pair.Key}: {message}")));
    }
}
