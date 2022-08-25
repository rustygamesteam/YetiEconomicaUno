using System.Runtime.InteropServices;
using RustyDTO.Interfaces;

namespace RustyDTO;

[StructLayout(LayoutKind.Sequential)]
public struct LazyRustyProperty
{
    private IRustyEntityProperty? _property;

    private readonly EntityPropertyType _type;
    private readonly ILazyRustyPropertyResolver _resolver;

    public EntityPropertyType Type => _type;
    public bool IsValid => _type != EntityPropertyType.None;

    public LazyRustyProperty(EntityPropertyType type, ILazyRustyPropertyResolver resolver)
    {
        _type = type;

        _resolver = resolver;
        _property = null;
    }

    public IRustyEntityProperty Value => _property ??= _resolver.Resolve();
}