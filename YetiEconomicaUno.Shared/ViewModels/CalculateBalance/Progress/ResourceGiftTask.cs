using System;
using LiteDB;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using YetiEconomicaCore.Database;
using DynamicData.Binding;
using DynamicData;
using System.Reactive.Linq;
using RustyDTO;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public class ResourceGiftTask : ProgressTask, IDisposable
{
    public ObservableCollectionExtended<ResourceStack> Resources { get; }
    private readonly Dictionary<IRustyEntity, IDisposable> _disposables = new();

    [BsonCtor]
    public ResourceGiftTask(IEnumerable<ResourceStackRecord> resources) : this(resources.Select(static x => new ResourceStack(x.Resource, x.Value)))
    {
    }

    public ResourceGiftTask(IEnumerable<ResourceStack> resources) : base(ProgressType.ResourceGift)
    {
        Resources = new ObservableCollectionExtended<ResourceStack>(resources);
        Resources.CollectionChanged += Resources_CollectionChanged;

        foreach (var resourceStack in Resources)
            _disposables[resourceStack.Resource] = resourceStack.Changed.Subscribe(_ => OnResourcesChanged());
    }

    private void OnResourcesChanged()
    {
        this.RaisePropertyChanged(nameof(Resources));
    }

    private void Resources_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        ResourceStack item;
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                item = (ResourceStack)e.NewItems[0];
                _disposables[item.Resource] = item.Changed.Subscribe(_ => OnResourcesChanged());
                break;
            case NotifyCollectionChangedAction.Remove:
                item = (ResourceStack)e.OldItems[0];
                if (_disposables.TryGetValue(item.Resource, out var disposable))
                {
                    disposable?.Dispose();
                    _disposables.Remove(item.Resource);
                }
                break;
        }
        this.RaisePropertyChanged(nameof(Resources));
    }

    public override void Evalute(ref UserData userData, bool updateStatistics = false)
    {
        foreach (var exchange in Resources)
            userData.Wallet.IncrimentWallet(exchange);
    }

    internal override bool OnYetiObjectRemove(IRustyEntity rustyEntity)
    {
        for (var i = Resources.Count - 1; i >= 0; i--)
        {
            if (Resources[i].Resource == rustyEntity)
                Resources.RemoveAt(i);
        }

        return false;
    }

    [BsonIgnore]
    internal override IEnumerable<ResourceStackRecord> Price => Enumerable.Empty<ResourceStackRecord>();

    void IDisposable.Dispose()
    {
        foreach (var disposable in _disposables.Values)
            disposable.Dispose();
        _disposables.Clear();
    }
}
