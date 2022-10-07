using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Xml.Linq;
using DynamicData;
using DynamicData.Binding;
using LiteDB;
using RustyDTO;
using RustyDTO.CodeGen.Impl;
using RustyDTO.Interfaces;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Descriptions;
using YetiEconomicaCore.Helper;

namespace YetiEconomicaCore.Experemental;

internal class DynamicLazyPropertiesDatabase : IDisposable
{
    private readonly ILiteCollection<BsonDocument> _collection;
    private readonly SourceCache<Properties, int> _data = new(property => property.Index);

    private readonly Dictionary<DescPropertyType, IPropertyResolver> _resolvers;
    private readonly IDescPropertyResolver _resolver;

    private CompositeDisposable _disposable = new();

    private static string GetKey(DescPropertyType type)
    {
        return EntityDependencies.IntAsString((int)type);
    }

    public IDescProperty ResolveProperty(int index, DescPropertyType type)
    {
        return _data.Lookup(index).Value.Get(type);
    }

    public IPropertiesAccess GetProperties(int index)
    {
        return _data.Lookup(index).Value;
    }

    public DynamicLazyPropertiesDatabase(ILiteDatabase database, string table, IEnumerable<KeyValuePair<DescPropertyType, IPropertyResolver>> resolvers)
    {
        _resolver = new SimpleDescPropertyResolver();
        _collection = database.GetCollection(table, BsonAutoId.Int32);
        foreach (var document in _collection.FindAll())
        {
            var index = document["_id"].AsInt32;
            _data.AddOrUpdate(new Properties(this, index, document));
        }

        _resolvers = new Dictionary<DescPropertyType, IPropertyResolver>(resolvers);
    }

    public bool HasResolve(DescPropertyType type)
    {
        return _resolver.HasResolve(type.AsIndex());
    }

    public bool TryResolve(int index, DescPropertyType propertyType, out LazyDescProperty property)
    {
        if (HasResolve(propertyType))
        {
            property = new (propertyType, new ResolveByIndex(index, propertyType));
            return true;
        }

        property = default;
        return false;
    }

    public void OnEntityAdd(int index, Dictionary<DescPropertyType, IDescProperty?> properties)
    {
        var dic = new Dictionary<string, BsonValue>(properties.Count + 1);
        dic["_id"] = index;
        foreach (var pair in properties)
        {
            if (_resolvers.TryGetValue(pair.Key, out var resolver))
            {
                if (pair.Value is null)
                {
                    var @default = resolver.SerializeDefault();
                    if (@default is not null)
                        dic[GetKey(pair.Key)] = @default;
                }
                else
                    dic[GetKey(pair.Key)] = resolver.Serialize(pair.Value);
            }
        }

        var document = new BsonDocument(dic);
        var propertiesAccess = new Properties(this, index, document);
        propertiesAccess.Fetch(properties);
        UpdateDocument(index, document);
        _data.AddOrUpdate(propertiesAccess);
    }

    public void OnEntityRemove(int index)
    {
        if (_collection.Delete(index))
        {
            _data.Lookup(index).Value.Dispose();
            _data.Remove(index);
        }
    }

    private IDescProperty ResolveProperty(int index, BsonValue data, DescPropertyType type)
    {
        return BsonEx.FromBson(EntityDependencies.EnumToType(type), index, data);
    }

    private bool TryPropertyUpdate(BsonDocument document, int index, DescPropertyType type, IDescProperty? property)
    {
        BsonValue? value;
        if (property is null)
        {
            value = _resolver.Resolve(index, type.AsIndex()).ToBson(EntityDependencies.EnumToType(type));
            if (value is null)
                return false;
        }
        else
            value = property.ToBson(EntityDependencies.EnumToType(type));

        var key = GetKey(type);
        document[key] = value;
        UpdateDocument(index, document);
        return true;
    }

    private void UpdateProperty((BsonDocument document, int Index, DescPropertyType Type, IDescProperty? Property) args)
    {
        TryPropertyUpdate(args.document, args.Index, args.Type, args.Property);
    }

    private void UpdateDocument(int index, BsonDocument document)
    {
        _collection.Upsert(index, document);
    }

    internal class Properties : IPropertiesAccess, IDisposable
    {
        public int Index { get; }

        private BsonDocument _document;
        private Dictionary<DescPropertyType, IDescProperty> _properties;
        private Dictionary<DescPropertyType, IDisposable> _disposable = new();

        private readonly DynamicLazyPropertiesDatabase _owner;

        public Properties(DynamicLazyPropertiesDatabase owner, int index, BsonDocument document)
        {
            _owner = owner;
            Index = index;
            _document = document;
            _properties = new Dictionary<DescPropertyType, IDescProperty>(Math.Max(4, _document.Count - 1));
        }

        public void Attach(DescPropertyType type, IDescProperty property)
        {
            AddProperty(type, property);
            _owner.UpdateProperty((_document, Index, type, property));
        }

        public void Detach(DescPropertyType type)
        {
            if (_document.Remove(GetKey(type)))
            {
                _properties.Remove(type);
                _owner.UpdateDocument(Index, _document);
            }

            if (_disposable.TryGetValue(type, out var disposable))
            {
                _disposable.Remove(type);
                disposable.Dispose();
            }
        }

        public bool TryDefaultAttach(DescPropertyType propertyType)
        {
            return _owner.TryPropertyUpdate(_document, Index, propertyType, null);
        }

        public IDescProperty Get(DescPropertyType type)
        {
            IDescProperty result;
            if (!_properties.TryGetValue(type, out result!))
            {
                result = TryResolve(type, true)!;
                _properties[type] = result;
            }

            return result;
        }

        public bool TryGet(DescPropertyType type, out IDescProperty property)
        {
            if (_properties.TryGetValue(type, out property!))
                return true;

            property = TryResolve(type, false)!;
            return property is not null;
        }

        private IDescProperty? TryResolve(DescPropertyType type, bool throwException)
        {
            if (_document.TryGetValue(GetKey(type), out var data))
            {
                var property = _owner.ResolveProperty(Index, data, type);
                AddProperty(type, property);
                return property;
            }

            if (throwException)
                throw new ArgumentNullException($"DynamicLazyPropertiesDatabase not containt {type} for entity by index {Index}");

            return null;
        }

        private void AddProperty(DescPropertyType type, IDescProperty property)
        {
            var disposable = property.WhenAnyPropertyChanged()
                .Select(property => (_document, Index, type, property))
                .Subscribe(_owner.UpdateProperty);
            
            _disposable[type] = disposable!;
            _properties[type] = property;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposable.Values)
                disposable.Dispose();
            _disposable.Clear();
        }

        public void Fetch(IReadOnlyDictionary<DescPropertyType, IDescProperty?> properties)
        {
            foreach (var pair in properties)
            {
                if(pair.Value is null)
                    continue;

                _properties[pair.Key] = pair.Value;
            }
        }
    }

    public void Dispose()
    {
        _data.Dispose();
        _disposable.Dispose();
    }
}