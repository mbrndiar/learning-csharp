using System.Globalization;

namespace Tasks.Client;

/// <summary>Safe URL composition shared by every transport.</summary>
public static class TransportUrls
{
    /// <summary>Return an unambiguous HTTP(S) base URL safe for path composition.</summary>
    public static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrEmpty(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("base URL must be an absolute HTTP or HTTPS URL", nameof(value));
        }

        if (value.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("base URL must not contain whitespace", nameof(value));
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException("base URL must be an absolute HTTP or HTTPS URL", nameof(value));
        }

        string scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https")
            || uri.UserInfo.Length > 0
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0)
        {
            throw new ArgumentException(
                "base URL must use HTTP or HTTPS without credentials, query, or fragment",
                nameof(value));
        }

        string authority = uri.Authority.ToLowerInvariant();
        string path = uri.AbsolutePath.TrimEnd('/');
        return $"{scheme}://{authority}{path}";
    }

    /// <summary>Build an absolute request URL, encoding path and query safely.</summary>
    public static string BuildAbsoluteUrl(string baseUrl, TransportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return NormalizeBaseUrl(baseUrl) + BuildRelativeUrl(request, leadingSlash: true);
    }

    /// <summary>Build a relative request URL for use with a client base address.</summary>
    public static string BuildRelativeUrl(TransportRequest request)
        => BuildRelativeUrl(request, leadingSlash: false);

    private static string BuildRelativeUrl(TransportRequest request, bool leadingSlash)
    {
        ArgumentNullException.ThrowIfNull(request);
        IEnumerable<string> segments = request.Path.Split('/').Select(Uri.EscapeDataString);
        string path = string.Join('/', segments);
        if (!leadingSlash)
        {
            path = path.TrimStart('/');
        }

        if (request.Query.Count == 0)
        {
            return path;
        }

        string query = string.Join(
            '&',
            request.Query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return $"{path}?{query}";
    }

    internal static string FormatTimeout(TimeSpan timeout)
        => timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture);
}
