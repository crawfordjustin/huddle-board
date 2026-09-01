using System.Globalization;
using System.Text;

namespace HuddleBoard.Playbook;

/// <summary>
/// A tiny JSON writer with exact control over separators and escaping.
/// </summary>
/// <remarks>
/// The shipped data is embedded in a single HTML file and diffed between
/// builds, so how it is spelled matters: ASCII-only escaping, no spaces in the
/// compact form, and keys in the order they are written. Nothing in
/// System.Text.Json lets you pin all three at once, and it is forty lines.
/// </remarks>
public sealed class JsonWriter
{
    private readonly StringBuilder _sb = new();
    private readonly int _indent;
    private int _depth;
    private bool _needComma;
    private bool _afterKey;

    /// <param name="indent">
    /// Spaces per level, or -1 for the compact form with no whitespace at all.
    /// </param>
    public JsonWriter(int indent = -1) => _indent = indent;

    private bool Pretty => _indent >= 0;

    private void Separate()
    {
        // a value written straight after "key:" needs no comma and no newline
        if (_afterKey)
        {
            _afterKey = false;
            return;
        }

        if (_needComma)
            _sb.Append(',');
        if (Pretty && _depth > 0)
            NewLine();
        _needComma = false;
    }

    private void NewLine()
    {
        if (!Pretty)
            return;
        _sb.Append('\n');
        _sb.Append(' ', _depth * _indent);
    }

    private void Open(char brace)
    {
        Separate();
        _sb.Append(brace);
        _depth++;
    }

    private void Close(char brace)
    {
        _depth--;
        if (Pretty && _needComma)
            NewLine();
        _sb.Append(brace);
        _needComma = true;
    }

    public JsonWriter StartObject()
    {
        Open('{');
        return this;
    }

    public JsonWriter EndObject()
    {
        Close('}');
        return this;
    }

    public JsonWriter StartArray()
    {
        Open('[');
        return this;
    }

    public JsonWriter EndArray()
    {
        Close(']');
        return this;
    }

    /// <summary>Writes a key. The next call writes its value.</summary>
    public JsonWriter Key(string name)
    {
        Separate();
        Escape(name);
        _sb.Append(Pretty ? ": " : ":");
        _afterKey = true;
        return this;
    }

    public JsonWriter Value(string value)
    {
        Separate();
        Escape(value);
        _needComma = true;
        return this;
    }

    public JsonWriter Value(int value)
    {
        Separate();
        _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        _needComma = true;
        return this;
    }

    public JsonWriter Value(Num value)
    {
        Separate();
        _sb.Append(value.ToString());
        _needComma = true;
        return this;
    }

    public JsonWriter Value(bool value)
    {
        Separate();
        _sb.Append(value ? "true" : "false");
        _needComma = true;
        return this;
    }

    public JsonWriter Pair(string key, string value) => Key(key).Value(value);

    public JsonWriter Pair(string key, int value) => Key(key).Value(value);

    public JsonWriter Pair(string key, Num value) => Key(key).Value(value);

    public JsonWriter Pair(string key, bool value) => Key(key).Value(value);

    /// <summary>An array of plain strings, written inline.</summary>
    public JsonWriter Pair(string key, IEnumerable<string> values)
    {
        Key(key).StartArray();
        foreach (var v in values)
            Value(v);
        return EndArray();
    }

    /// <summary>
    /// Escapes to pure ASCII: quote, backslash, the short control escapes, and
    /// \uXXXX for everything outside printable ASCII.
    /// </summary>
    private void Escape(string s)
    {
        _sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': _sb.Append("\\\""); break;
                case '\\': _sb.Append("\\\\"); break;
                case '\b': _sb.Append("\\b"); break;
                case '\f': _sb.Append("\\f"); break;
                case '\n': _sb.Append("\\n"); break;
                case '\r': _sb.Append("\\r"); break;
                case '\t': _sb.Append("\\t"); break;
                default:
                    if (c is >= ' ' and <= '~')
                        _sb.Append(c);
                    else
                        _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    break;
            }
        }

        _sb.Append('"');
    }

    public override string ToString() => _sb.ToString();
}
