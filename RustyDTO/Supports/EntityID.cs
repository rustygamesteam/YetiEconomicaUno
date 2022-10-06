namespace RustyDTO.Supports;

public struct EntityID : IEquatable<EntityID>
{ 
    public int Index { get; }
    public bool IsUserEntity { get; }

    private readonly int _hash;
    private string? _str;

    public EntityID(bool isUserEntity, int index, string? str = null)
    {
        Index = index;
        IsUserEntity = isUserEntity;

        _str = str;
        _hash = HashCode.Combine(isUserEntity, index);
    }

    public static bool operator ==(EntityID first, EntityID second)
    {
        return first.Index == second.Index && first.IsUserEntity == second.IsUserEntity;
    }

    public static bool operator !=(EntityID first, EntityID second)
    {
        return first.Index != second.Index || first.IsUserEntity != second.IsUserEntity;
    }

    public override string ToString()
    {
        if (_str is null)
        {
            Span<char> chars = stackalloc char[16];
            chars[0] = IsUserEntity ? 'u' : 'e';

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
        chars[1] = IsUserEntity ? 'u' : 'e';
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
        var isUserEntity = raw[0] == 'u';
        return new EntityID(isUserEntity, MathHelper.ToIndexString(raw, 1), raw);
    }

    public bool Equals(EntityID other)
    {
        return Index == other.Index && IsUserEntity == other.IsUserEntity;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityID other && Equals(other);
    }

    public static EntityID CreateByDB(int index)
    {
        return new EntityID(false, index);
    }
    
    public static EntityID CreateByUser(int index)
    {
        return new EntityID(true, index);
    }
}