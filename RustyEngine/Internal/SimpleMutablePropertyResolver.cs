using System.Text.Json;
using RustyDTO;
using RustyDTO.Interfaces;
using RustyDTO.MutableProperties;
using RustyDTO.Supports;


namespace RustyEngine.Internal;

public partial class SimpleMutablePropertyResolver : IMutablePropertyResolver
{
    private Func<IMutableProperty>[] _factory;
    private Func<JsonElement, IMutableProperty>[] _factoryWithData;

    public SimpleMutablePropertyResolver()
    {
        int count = EntityDependencies.MutablePropertiesCount;
        _factory = new Func<IMutableProperty>[count];
        _factoryWithData = new Func<JsonElement, IMutableProperty>[count];

        _factory[0] = () => new MutableCount(0);
        _factoryWithData[0] = static d => new MutableCount(d.GetInt32());

        _factory[1] = () => new MutableManager(null);
        _factoryWithData[1] = static d => new MutableManager(EntityID.Parse(d.GetRawText()));

        _factory[2] = () => new MutalbePosition2D();
        _factoryWithData[2] = static d => new MutalbePosition2D(d);

        _factory[3] = () => new MutablePositionInsideHex();
        _factoryWithData[3] = static d => new MutablePositionInsideHex(d);

        _factory[4] = () => new OwnerArchetype();
        _factoryWithData[4] = static d => new OwnerArchetype(d);

        _factory[5] = () => new MutableUsedInstance();
        _factoryWithData[5] = static d => new MutableUsedInstance(d);

    }

    public IMutableProperty Resolve(int type)
    {
        return _factory[type].Invoke();
    }

    public IMutableProperty Resolve(int type, JsonElement dataElement)
    {
        return _factoryWithData[type].Invoke(dataElement);
    }

    internal class MutableCount : IMutableCount
    {
        public MutableCount(int value)
        {
            Count = value;
        }

        public int Count { get; set; }
    }

    internal class MutableManager : IMutableManager
    {
        public MutableManager(EntityID managerID)
        {
            if (managerID.Type == EntityIndexType.d)
                Manager = Engine.Instance.EntityService.GetEntity(managerID);
        }

        public MutableManager(IRustyEntity? manager)
        {
            Manager = manager;
        }

        public IRustyEntity? Manager { get; set; }
    }

    internal class MutalbePosition2D : IMutablePosition2D
    {
        public MutalbePosition2D(JsonElement data)
        {
            X = data.GetProperty("X").GetInt32();
            Y = data.GetProperty("Y").GetInt32();
        }

        public MutalbePosition2D()
        {

        }

        public int X { get; set; }
        public int Y { get; set; }
    }

    internal class MutablePositionInsideHex : IMutablePositionInsideHex
    {
        public MutablePositionInsideHex()
        {

        }

        public MutablePositionInsideHex(JsonElement data)
        {
            Position = (HexPosition)data.GetInt32();
        }

        public HexPosition Position { get; set; }
    }

    internal class OwnerArchetype : IMutableOwnerArchetype
    {
        public OwnerArchetype()
        {

        }

        public OwnerArchetype(JsonElement data)
        {
            Entity = Engine.Instance.EntityService.GetEntity(EntityID.Parse(data.GetRawText()));
        }

        public IRustyEntity Entity { get; set; }
    }

    internal class MutableUsedInstance : IMutableUsedInstance
    {
        public MutableUsedInstance()
        {

        }

        public MutableUsedInstance(JsonElement data)
        {
            Entity = Engine.Instance.EntityService.GetEntity(EntityID.Parse(data.GetRawText()));
        }

        public IRustyEntity Entity { get; set; }
    }
}