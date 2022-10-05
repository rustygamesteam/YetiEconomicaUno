using LiteDB;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using RustyDTO.Supports;
using YetiEconomicaCore.Database;
using YetiEconomicaCore.Services;

namespace YetiEconomicaCore.ReactiveImpl;

internal class ReactiveOwnerViewModel : ReactiveObject, IHasOwner
{
    public ReactiveOwnerViewModel(int index, int ownerIndex, int tear = 0)
    {
        Index = index;
        Owner = RustyEntityService.Instance.GetEntity(new EntityID(EntityIndexType.d, ownerIndex));
        Tear = tear;
    }

    public int Index { get; }

    [Reactive]
    public IRustyEntity Owner { get; set; }
    [Reactive]
    public int Tear { get; set; }
}

internal class ReactiveHasOwnerFactory : IDatabaseConvertable<IHasOwner, ItemOfGroupInfo>, IPropertyResolver
{
    public static ReactiveHasOwnerFactory Instance { get; } = new();

    public bool TryToModel(ItemOfGroupInfo data, out IHasOwner? model)
    {
        model = new ReactiveOwnerViewModel(data.Index, data.Owner, data.Tear);
        return true;
    }

    public ItemOfGroupInfo ToData(IHasOwner data)
    {
        return new ItemOfGroupInfo(data.Index, data.Owner.ID.Index, data.Tear);
    }

    public int GetID(IHasOwner data)
    {
        return data.Index;
    }

    public BsonValue? SerializeDefault()
    {
        return null;
    }

    public BsonValue Serialize(IDescProperty @base)
    {
        var property = (IHasOwner)@base;
        return new BsonDocument
        {
            { nameof(IDescProperty.Index), property.Index },
            { "OwnerIndex", property.Owner.ID.Index },
            { nameof(IHasOwner.Tear), property.Tear },
        };
    }

    public IDescProperty Deserialize(int index, BsonValue data)
    {
        return new ReactiveOwnerViewModel(index, data["OwnerIndex"].AsInt32, data[nameof(IHasOwner.Tear)].AsInt32);
    }
}