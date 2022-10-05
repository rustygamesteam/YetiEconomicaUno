#if REACTIVE
using ReactiveUI;
#endif

using System.Text.Json;

namespace RustyDTO.Interfaces;

public interface IDescProperty
#if REACTIVE
    : IReactiveObject
#endif
{
    int Index { get; }
}

public interface IDescPropertyResolver
{
    public bool HasResolve(int type);
    public bool HasDefaultResolve(int type);

    public IDescProperty Resolve(int index, int type);
    public IDescProperty Resolve(int index, int type, JsonElement dataElement);
}

public interface ILazyDescPropertyResolver
{
    IDescProperty Resolve();
}