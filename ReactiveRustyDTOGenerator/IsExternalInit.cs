#if !NET5_0_OR_GREATER

using System.ComponentModel;

#pragma warning disable

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}

#endif