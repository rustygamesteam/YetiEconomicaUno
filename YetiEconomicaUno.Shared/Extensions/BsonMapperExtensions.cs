using LiteDB;
using ReactiveUI;

namespace YetiEconomicaUno.Extensions;

public static class BsonMapperExtensions
{
    public static EntityBuilder<T> IgnoreReactive<T>(this EntityBuilder<T> builder) where T : ReactiveObject
    {
        return builder
            .Ignore(static x => x.Changed)
            .Ignore(static x => x.Changing)
            .Ignore(static x => x.ThrownExceptions);
    }
}