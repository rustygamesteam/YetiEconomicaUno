#if REACTIVE
using ReactiveUI;
#endif

namespace RustyDTO.Interfaces;

public interface IDescProperty
#if REACTIVE
    : IReactiveObject
#endif
{
    int Index { get; }
}