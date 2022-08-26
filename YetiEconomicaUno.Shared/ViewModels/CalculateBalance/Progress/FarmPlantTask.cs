using System;
using LiteDB;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using RustyDTO;
using YetiEconomicaCore.Database;
using RustyDTO.Interfaces;

namespace YetiEconomicaUno.ViewModels.CalculateBalance.Progress;

public class FarmPlantTask : ProgressTask, IDisposable
{
    public ObservableCollectionExtended<ResourceStack> Targets { get; }
    private readonly Dictionary<IRustyEntity, IDisposable> _disposables = new();

    [BsonCtor]
    public FarmPlantTask(IEnumerable<ResourceStackRecord> targets) : this(targets.Select(static x => new ResourceStack(x.Resource, x.Value)))
    {

    }

    public FarmPlantTask(IEnumerable<ResourceStack> targets) : base(ProgressType.FarmPlant)
    {
        Targets = new ObservableCollectionExtended<ResourceStack>(targets);
        Targets.CollectionChanged += Targets_CollectionChanged;

        foreach (var resourceStack in Targets)
            _disposables[resourceStack.Resource] = resourceStack.Changed.Subscribe(_ => OnResourcesChanged());
    }

    private void OnResourcesChanged()
    {
        this.RaisePropertyChanged(nameof(Targets));
    }

    private void OnTargetsChanged(IChangeSet<ResourceStack> obj)
    {
        this.RaisePropertyChanged(nameof(Targets));
    }

    private void Targets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        this.RaisePropertyChanged(nameof(Targets));
    }

    public override void Evalute(ref UserData userData, bool updateStatistics = false)
    {
        if (updateStatistics)
            Statistics.Clear();

        CopyBag(ref userData);

        FarmResources(ref userData, Targets.Select(static x => (ResourceStackRecord)x), updateStatistics ? Statistics : null);

        EvaluteObservable.OnNext(Unit.Default);
    }

    internal override bool OnYetiObjectRemove(IRustyEntity rustyEntity)
    {
        for (var i = Targets.Count - 1; i >= 0; i--)
        {
            if (Targets[i].Resource == rustyEntity)
                Targets.RemoveAt(i);
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
