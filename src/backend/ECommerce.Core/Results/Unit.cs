namespace ECommerce.Core.Results;

/// <summary>
/// Represents a void-like type for results that don't return data.
/// Use with Result&lt;Unit&gt; instead of throwing exceptions for void operations.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The single Unit value.
    /// </summary>
    public static readonly Unit Value;

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
