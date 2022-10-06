using System.Runtime.InteropServices;
using RustyDTO.Interfaces;

namespace RustyDTO;

[StructLayout(LayoutKind.Sequential)]
public struct LazyDescProperty
{
    private IDescProperty? _property;
    private readonly ILazyDescPropertyResolver? _resolver;

    private readonly DescPropertyType _type;
    private int _propertyPrt;

    public DescPropertyType Type => _type;
    public bool IsValid => _type != DescPropertyType.None;

    public LazyDescProperty(DescPropertyType type, ILazyDescPropertyResolver resolver)
    {
        _type = type;
        _resolver = resolver;
        _property = null;
    }
    
    public IDescProperty Value => _property ??= _resolver!.Resolve();
}