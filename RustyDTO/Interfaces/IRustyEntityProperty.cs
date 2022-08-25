#if REACTIVE
using ReactiveUI;
#endif

namespace RustyDTO.Interfaces;

public interface IRustyEntityProperty
#if REACTIVE
    : IReactiveObject
#endif
{
    int Index { get; }
}