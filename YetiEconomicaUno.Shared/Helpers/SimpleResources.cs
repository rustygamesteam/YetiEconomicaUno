using DynamicData;
using RustyDTO.Interfaces;
using System;
using System.Collections.Generic;
using RustyDTO.DescPropertyModels;
using YetiEconomicaCore;
using YetiEconomicaCore.Services;

namespace YetiEconomicaUno.Helpers;

internal class SimpleResources
{
    private static Lazy<SimpleResources> _instance = new Lazy<SimpleResources>(() => new SimpleResources(ResourceService.Instance));
    public static SimpleResources Instance => _instance.Value;

    private SimpleResources(ResourceService service)
    {
        service.ObservableResources.Connect(static resource => resource.GetIndex() < 0 && resource.TryGetProperty<IHasOwner>(out var ownerInfo) && ownerInfo.Tear == 0 && ownerInfo.Owner.DisplayName == "Main")
            .Subscribe(OnUpdateResources);
    }

    public IRustyEntity Wood { get; private set; }
    public IRustyEntity Stone { get; private set; }
    public IRustyEntity Ore { get; private set; }

    public void Reset()
    {
        Wood = Stone = Ore = null;
    }

    public IEnumerable<IRustyEntity> AllSimplesSafe()
    {
        if (Wood != null)
            yield return Wood;
        if (Stone != null)
            yield return Stone;
        if (Ore != null)
            yield return Ore;
    }

    public IEnumerable<IRustyEntity> AllSimples()
    {
        yield return Wood;
        yield return Stone;
        yield return Ore;
    }

    public bool IsSimple(IRustyEntity resource)
    {
        if (resource is null)
            return false;

        var index = resource.ID;
        foreach (var simple in AllSimplesSafe())
        {
            if (simple.ID == index)
                return true;
        }
        return false;
    }

    private void Inject(string name, IRustyEntity entity)
    {
        switch (name)
        {
            case "Wood":
                Wood = entity;
                break;
            case "Stone":
                Stone = entity;
                break;
            case "Ore":
                Ore = entity;
                break;
        }
    }

    private void OnUpdateResources(IChangeSet<IRustyEntity> diffs)
    {
        foreach(var changes in diffs)
        {
            switch (changes.Reason)
            {
                case ListChangeReason.AddRange:
                    foreach (var change in changes.Range)
                        Inject(change.DisplayName, change);
                    break;
                case ListChangeReason.RemoveRange:
                    foreach (var change in changes.Range)
                        Inject(change.DisplayName, null);
                    break;
                case ListChangeReason.Add:
                    Inject(changes.Item.Current.DisplayName, changes.Item.Current);
                    break;
                case ListChangeReason.Remove:
                    Inject(changes.Item.Current.DisplayName, null);
                    break;
                case ListChangeReason.Clear:
                    Reset();
                    break;
                default:
                    continue;
            }
        }
    }
}
