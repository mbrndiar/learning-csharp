namespace Tasks.Http;

/// <summary>One matched route and its optional captured identifier text.</summary>
public readonly record struct RouteMatch(string Route, string? IdText);
