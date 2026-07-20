namespace Tasks.Core;

/// <summary>
/// Distinguishes an omitted partial-update field from an explicitly supplied
/// value. In particular, <c>completed = false</c> must remain a real update
/// rather than being mistaken for a missing value, and <c>null</c> is never
/// used to mean "omitted". An unset value is <c>default</c>.
/// </summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
public readonly struct Maybe<T> : IEquatable<Maybe<T>>
{
    private readonly T _value;

    /// <summary>Create a set value carrying a payload.</summary>
    public Maybe(T value)
    {
        _value = value;
        HasValue = true;
    }

    /// <summary>Whether a value was supplied.</summary>
    public bool HasValue { get; }

    /// <summary>The supplied value; throws when the value is unset.</summary>
    public T Value => HasValue
        ? _value
        : throw new InvalidOperationException("value is unset");

    /// <summary>Try to read the value without throwing.</summary>
    public bool TryGet(out T value)
    {
        value = _value;
        return HasValue;
    }

    /// <summary>Implicitly wrap a value so callers can pass it directly.</summary>
    public static implicit operator Maybe<T>(T value) => new(value);

    /// <inheritdoc />
    public bool Equals(Maybe<T> other)
        => HasValue == other.HasValue
           && (!HasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Maybe<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HasValue ? HashCode.Combine(true, _value) : HashCode.Combine(false);

    /// <summary>Structural equality operator.</summary>
    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    /// <summary>Structural inequality operator.</summary>
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);
}

/// <summary>Non-generic factory helpers for <see cref="Maybe{T}"/> values.</summary>
public static class Maybe
{
    /// <summary>Create a set value.</summary>
    public static Maybe<T> Of<T>(T value) => new(value);

    /// <summary>Create an unset value.</summary>
    public static Maybe<T> None<T>() => default;
}
