using H.Generators;

namespace ReactiveUIGenerator.Models;

internal readonly record struct ClassData(
string Namespace,
string Name,
string FullName,
string Modifiers,
ViewForData ViewFor);
