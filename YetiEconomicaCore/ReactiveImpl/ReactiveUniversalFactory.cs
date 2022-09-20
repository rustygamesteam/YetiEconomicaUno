using System.Linq.Expressions;
using LiteDB;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;

namespace YetiEconomicaCore.ReactiveImpl;

[FactoryForProperty<ICraftSpeed>]
[FactoryForProperty<ITechSpeed>]
[FactoryForProperty<IFactor>]
[FactoryForProperty<ITakeSpace>]
[FactoryForProperty<ILink>(makePartial: true)]
[FactoryForProperty<IHasPrestige>]
[FactoryForProperty<IFarmExpansion>]
[FactoryForProperty<IHexMask>]
[FactoryForProperty<IHasCraftingQueue>]
[FactoryForProperty<IMineSize>]
[FactoryForProperty<IInBuildProcess>]
[FactoryForProperty<IToolSettings>]
[FactoryForProperty<ILongExecution>]
[FactoryForProperty<IHasDependents>]
[FactoryForProperty<ICitySize>]
[FactoryForProperty<ISubGroup>]
internal static partial class ReactiveUniversalFactory
{
    internal class ReactiveFactory<TProperty> : IPropertyResolver
        where TProperty : IDescProperty
    {
        private readonly Func<int, BsonValue, TProperty> _factory;
        private readonly BsonValue _defaultValue;

        private Dictionary<string, string>? _serialize;
        private Dictionary<string, string>? _deserialize;

        internal ReactiveFactory(BsonValue @default, Func<int, BsonValue, TProperty> factory)
        {
            _defaultValue = @default;
            _factory = factory;

        }

        public ReactiveFactory<TProperty> With<TRet>(string jsonKey, Expression<Func<TProperty, TRet>> property)
        {
            _serialize ??= new Dictionary<string, string>();
            _deserialize ??= new Dictionary<string, string>();

            var memberName = ((MemberExpression)property.Body).Member.Name;

            _serialize[memberName] = jsonKey;
            _deserialize[jsonKey] = memberName;

            return this;
        }

        public ReactiveFactory<TProperty> With<TRet>(params (string jsonKey, Expression<Func<TProperty, TRet>> property)[] config)
        {
            _serialize ??= new Dictionary<string, string>(config.Length);
            _deserialize ??= new Dictionary<string, string>(config.Length);

            foreach (var tuple in config)
            {
                var memberName = ((MemberExpression)tuple.property.Body).Member.Name;

                _serialize[memberName] = tuple.jsonKey;
                _deserialize[tuple.jsonKey] = memberName;
            }

            return this;
        }

        public BsonValue? SerializeDefault()
        {
            return _defaultValue;
        }

        public BsonValue Serialize(IDescProperty @base)
        {
            var property = (TProperty)@base;
            var result = BsonMapper.Global.Serialize(typeof(TProperty), property);
            if (result.IsDocument)
            {
                var document = result.AsDocument;
                document.Remove(nameof(IDescProperty.Index));

                if (document.Count == 1)
                    return document.First().Value;

                if (_serialize is not null)
                {
                    var keys = document.Keys.ToArray();

                    foreach (var key in keys)
                    {
                        if(!_serialize.TryGetValue(key, out var fixedKey))
                            continue;

                        var value = document[key];
                        document.Remove(key);
                        document[fixedKey] = value;
                    }
                }
            }

            return result;
        }

        public IDescProperty Deserialize(int index, BsonValue data)
        {
            if (data.IsDocument && _deserialize is not null)
            {
                var document = data.AsDocument;
                var keys = document.Keys.ToArray();
                foreach (var key in keys)
                {
                    if(!_deserialize.TryGetValue(key, out var fixedKey))
                        continue;

                    var value = document[key];
                    document.Remove(key);
                    document[fixedKey] = value;
                }
            }

            return _factory.Invoke(index, data);
        }
    }
}