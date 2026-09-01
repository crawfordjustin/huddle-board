using System.Globalization;

namespace HuddleBoard.Playbook;

/// <summary>
/// A coordinate that remembers whether it was written as a whole number or a
/// decimal, so the exported JSON reads the same either way (2 stays 2, -2.0
/// stays -2.0). Numbers are yards; write them exactly as you mean them.
/// </summary>
public readonly struct Num : IEquatable<Num>
{
    public double Value { get; }
    private readonly bool _whole;

    private Num(double value, bool whole)
    {
        Value = value;
        _whole = whole;
    }

    public static implicit operator Num(int v) => new(v, true);
    public static implicit operator Num(double v) => new(v, false);
    public static implicit operator double(Num n) => n.Value;

    /// <summary>Formats the way the source literal was written.</summary>
    public override string ToString()
    {
        if (_whole)
            return ((long)Value).ToString(CultureInfo.InvariantCulture);
        var s = Value.ToString("R", CultureInfo.InvariantCulture);
        return s.Contains('.') || s.Contains('e') || s.Contains('E') ? s : s + ".0";
    }

    public bool Equals(Num other) => Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is Num n && Equals(n);
    public override int GetHashCode() => Value.GetHashCode();
}

/// <summary>
/// A point on the field, in yards. x = left/right of the snapper (negative is
/// the offense's left, the BLUE side), y = downfield from the line of
/// scrimmage (negative is the backfield).
/// </summary>
public readonly record struct Pt(Num X, Num Y)
{
    public double Dx(Pt other) => X.Value - other.X.Value;
    public double Dy(Pt other) => Y.Value - other.Y.Value;
    public double DistanceTo(Pt other) => double.Hypot(Dx(other), Dy(other));
}
