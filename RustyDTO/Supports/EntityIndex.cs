namespace RustyDTO.Supports;

public struct EntityID : IEquatable<EntityID>
{ 
    public int Index { get; }
    public EntityIndexType Type { get; }

    private readonly int _hash;
    private string? _str;

    public EntityID(EntityIndexType type, int index, string? str = null)
    {
        Index = index;
        Type = type;

        _str = str;
        _hash = HashCode.Combine(type, index);
    }

    public static bool operator ==(EntityID first, EntityID second)
    {
        return first.Index == second.Index && first.Type == second.Type;
    }

    public static bool operator !=(EntityID first, EntityID second)
    {
        return first.Index != second.Index || first.Type != second.Type;
    }

    public override string ToString()
    {
        if (_str is null)
        {
            Span<char> chars = stackalloc char[16];
            chars[0] = (char)Type;

            if (Index.TryFormat(chars.Slice(1), out var length))
            {
                _str = new string(chars.Slice(0, length + 1));
                return _str;
            }

            _str = string.Concat(chars[0], Index);
        }

        return _str;
    }

    public string ToJsonRaw()
    {
        Span<char> chars = stackalloc char[19];
        chars[0] = '[';
        chars[1] = (char)Type;
        chars[2] = ',';

        if (Index.TryFormat(chars.Slice(3), out var length))
        {
            chars[length + 3] = ']';
            return new string(chars.Slice(0, length + 4));
        }

        return $"[{chars[1]},{Index}]";
    }

    public override int GetHashCode()
    {
        return _hash;
    }

    public static EntityID Parse(string raw)
    {
        EntityIndexType indexType = (EntityIndexType)raw[0];
        return new EntityID(indexType, MathHelper.ToIndexString(raw, 1), raw);
    }

    public bool Equals(EntityID other)
    {
        return Index == other.Index && Type == other.Type;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityID other && Equals(other);
    }
}

public enum EntityIndexType : byte
{
    /// <summary>
    /// Default
    /// </summary>
    d = (byte)'d',
    /// <summary>
    /// Multi instance
    /// </summary>
    m = (byte)'m',
    /// <summary>
    /// User or game generated
    /// </summary>
    u = (byte)'u'
}