using System.Reflection;
using System.Text.Json.Serialization;
using LiteDB;
using ReactiveUI;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Helper;

public static class BsonEx
{
    private static readonly Dictionary<Type, BsonCacheConverter> _converters = new();
    private static Dictionary<Type, Func<int, BsonValue, BsonCacheConverter, IDescProperty>> _factory = new();
    
    private static Dictionary<Type, Func<object, BsonValue>> _mapTo = new();
    private static Dictionary<Type, Func<BsonValue, object>> _mapFrom = new();

    static BsonEx()
    {
    }

    public static void Initialize()
    {
        _converters.Clear();
        var baseType = typeof(IDescProperty);

        var ignore = new HashSet<string>
        {
            nameof(IDescProperty.Index),
            nameof(ReactiveObject.Changed),
            nameof(ReactiveObject.Changing),
            nameof(ReactiveObject.ThrownExceptions),
        };

        var cacheMembers = new List<BsonCacheMember>();
        var constructorTypes = new Type[]
        {
            typeof(int)
        };

        foreach (var typeInfo in typeof(IDescProperty).Assembly.DefinedTypes)
        {
            if(!typeInfo.IsClass)
                continue;

            if (typeInfo.ImplementedInterfaces.All(type => type != baseType))
                continue;

            var info = typeInfo.CustomAttributes.First(static data => data.AttributeType.Name.StartsWith("RustyDTO.Supports.PropertyImplInfo", StringComparison.Ordinal));
            
            cacheMembers.Clear();

            bool isValue = true;
            
            foreach (var member in typeInfo.DeclaredProperties)
            {
                if(ignore.Contains(member.Name) || member.IsStatic())
                    continue;

                var jsonCustomName = member.GetCustomAttribute<JsonPropertyNameAttribute>();
                var name = jsonCustomName?.Name ?? member.Name;
                var type = member.PropertyType;

                if (jsonCustomName is not null)
                    isValue = false;
                
                cacheMembers.Add(new BsonCacheMember(member, type, name));
            }

            if (cacheMembers.Count > 1)
                isValue = false;

            var constructor = typeInfo.GetConstructor(BindingFlags.Public | BindingFlags.Instance, constructorTypes);
            _converters[info.AttributeType.GenericTypeArguments[1]] = new BsonCacheConverter(typeInfo, isValue, cacheMembers.ToArray(), constructor!);
        }

        foreach (var pair in _converters)
        {
            _factory[pair.Key] = static (index, value, cache) =>
            {
                var instance = cache.Constructor.Invoke(BindingFlags.Instance, null, new object?[]{index}, null);
                if (cache.IsValue)
                {
                    var first = cache.Members.First();
                    var propertyValue = _mapFrom.TryGetValue(first.Type, out var factory) ? 
                        factory.Invoke(value) :  
                        BsonMapper.Global.Deserialize(first.Type, value);
                    first.Property.SetValue(instance, propertyValue);

                    return (IDescProperty)instance;
                }
                
                foreach (var member in cache.Members)
                {
                    var propertyValue = _mapFrom.TryGetValue(member.Type, out var factory) ? 
                        factory.Invoke(value[member.AsName]) :
                        BsonMapper.Global.Deserialize(member.Type, value[member.AsName]);
                    member.Property.SetValue(instance, propertyValue);
                }
                return (IDescProperty)instance;
            };
        }
    }

    public static BsonValue ToBson(this IDescProperty value, Type type)
    {
        var info = _converters[type];
        if (info.IsValue)
        {
            var propertyValue = info.Members[0].Property.GetValue(value);
            return _mapTo.TryGetValue(info.Members[0].Type, out var factory) ? 
                factory.Invoke(propertyValue) :
                new BsonValue(propertyValue);
        }
        
        var result = new BsonDocument(new Dictionary<string, BsonValue>(info.Members.Length));
        foreach (var infoMember in info.Members)
        {
            var propertyValue = infoMember.Property.GetValue(value);
            result[infoMember.AsName] = _mapTo.TryGetValue(infoMember.Type, out var factory) ? 
                factory.Invoke(propertyValue) :
                new BsonValue(propertyValue);
        }
        
        return result;
    }
    
    public static BsonValue ToBson<T>(this T value) where T : IDescProperty
    {
        return ToBson(value, value.GetType());
    }

    public static IDescProperty FromBson(Type type, int index, BsonValue value)
    {
        return _factory[type].Invoke(index, value, _converters[type]);
    }

    private record struct BsonCacheMember(PropertyInfo Property, Type Type, string AsName);
    private record BsonCacheConverter(TypeInfo Type, bool IsValue, BsonCacheMember[] Members, ConstructorInfo Constructor);
}