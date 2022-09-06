using RustyDTO;
using RustyDTO.Interfaces;

namespace RustyEngine.Internal;

internal record struct PropertyInfo(int Type, LazyDescProperty Property);
internal record struct OwnerInfo(int Owner, IRustyEntity Child);