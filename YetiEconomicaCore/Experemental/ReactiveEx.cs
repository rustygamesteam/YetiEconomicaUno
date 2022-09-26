using LiteDB;
using ReactiveUI;
using System.Reflection;

namespace YetiEconomicaCore.Experemental;

public static class ReactiveEx
{
    public readonly static HashSet<string> ReactiveMembers = new HashSet<string>
    {
        nameof(ReactiveObject.Changed),
        nameof(ReactiveObject.Changing),
        nameof(ReactiveObject.ThrownExceptions)
    };

    private readonly static MethodInfo GetEntityMapperFunc = typeof(BsonMapper).GetMethod("GetEntityMapper", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)!;

    public static void IgnoreForType(this BsonMapper mapper, Type type)
    {
        var entity = (EntityMapper)GetEntityMapperFunc.Invoke(mapper, new object[] { type })!;
        var members = entity.Members;
        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (ReactiveMembers.Contains(members[i].MemberName))
                members.RemoveAt(i);
        }
    }
}