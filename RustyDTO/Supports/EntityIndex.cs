using System.Text;

namespace RustyDTO.Supports;

public struct EntityID : IEquatable<EntityID>
{
    private static readonly char[] _keys = new char[] { 'd', 'm', 'u' };

    public int Index { get; }
    public EntityIndexType Type { get; }

    public EntityID(EntityIndexType type, int index)
    {
        Index = index;
        Type = type;
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
        return EntityIndexHelper.ToString(_keys[(int) Type], Index);
    }

    public string ToJsonRaw()
    {
        return EntityIndexHelper.ToJsonRaw(_keys[(int)Type], Index);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Index);
    }

    public static EntityID Parse(string raw)
    {
        EntityIndexType indexType;
        switch (raw[0])
        {
            case 'd':
                indexType = EntityIndexType.d;
                break;
            case 'm':
                indexType = EntityIndexType.m;
                break;
            case 'u':
                indexType = EntityIndexType.u;
                break;
            default:
                throw new NotImplementedException($"EntityIndexType invalid try parse: {raw}");
        }

        return new EntityID(indexType, MathHelper.ToIndexString(raw, 1));
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

internal static class EntityIndexHelper
{
    private static readonly ThreadLocal<StringBuilder> _toStringBuilder = new (() => new StringBuilder(8));

    public static string ToString(char type, int index)
    {
        var sb = _toStringBuilder.Value!;

        sb.Length = 0;
        sb.Append(type);
        sb.Append(index);

        return sb.ToString();
    }

    public static string ToJsonRaw(char type, int index)
    {
        var sb = _toStringBuilder.Value!;

        sb.Length = 0;
        sb.Append('[');
        sb.Append(type);
        sb.Append(',');
        sb.Append(index);
        sb.Append(']');

        return sb.ToString();
    }
}

public enum EntityIndexType : byte
{
    /// <summary>
    /// Default
    /// </summary>
    d = 0,
    /// <summary>
    /// Multi instance
    /// </summary>
    m = 1,
    /// <summary>
    /// User or game generated
    /// </summary>
    u = 2
}