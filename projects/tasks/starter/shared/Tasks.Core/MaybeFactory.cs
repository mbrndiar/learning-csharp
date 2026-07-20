namespace Tasks.Core;

/// <summary>Non-generic factory helpers for <see cref="Maybe{T}"/> values.</summary>
public static class MaybeFactory
{
    /// <summary>Create a set value.</summary>
    public static Maybe<T> Of<T>(T value) => new(value);

    /// <summary>Create an unset value.</summary>
    public static Maybe<T> None<T>() => default;
}
