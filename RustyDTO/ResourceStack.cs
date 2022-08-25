using RustyDTO.Interfaces;

#if REACTIVE
using ReactiveUI;
#endif

namespace RustyDTO;

#if REACTIVE
public class ResourceStack : ReactiveObject, IEquatable<ResourceStack>
{
    public ResourceStack(IRustyEntity resource, double value)
    {
        Resource = resource;
        Value = value;
    }

    public ResourceStack(IRustyEntity resource, int value)
    {
        Resource = resource;
        Value = value;
    }

    private double _value;

    public IRustyEntity Resource { get; }
    public double Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }

    public bool Equals(ResourceStack? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Resource.Equals(other.Resource);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ResourceStack) obj);
    }
}
#else
public record struct ResourceStack(IRustyEntity Resource, double Value) : IEquatable<ResourceStack>
{
    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }

    public bool Equals(ResourceStack other)
    {
        return Resource.Equals(other.Resource);
    }
}
#endif