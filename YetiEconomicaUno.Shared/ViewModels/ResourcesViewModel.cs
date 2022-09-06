using System;
using System.Collections;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Nito.Comparers;
using YetiEconomicaCore.Services;
using System.Reactive;
using System.Collections.Generic;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using YetiEconomicaCore;

namespace YetiEconomicaUno.ViewModels;

public class ResourcesViewModel : BaseViewModel
{
    [Reactive]
    public string NewGroupName { get; set; }

    [Reactive]
    public string NewResourceName { get; set; }

    [Reactive]
    public IRustyEntity NewResourceGroup { get; set; }


    internal IList Groups { get; }
    internal ObservableCollectionExtended<IRustyEntity> Resources { get; } = new();

    internal ObservableCollectionExtended<IRustyEntity> GroupsFilter { get; } = new();

    [Reactive]
    public string FilterContent { get; set; } = "All";


    [ObservableAsProperty]
    public IRustyEntity SelectedGroup { get; }
    [ObservableAsProperty]
    public IRustyEntity SelectedResource { get; }


    public ReactiveCommand<Unit, Unit> RemoveResourceCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveResourceGroupCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CreateGroupCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateResourceCommand { get; }

    public ResourcesViewModel()
    {
        var canRemoveGroup = this.WhenAnyValue(static x => x.SelectedGroup)
            .Select(static x => x is not null);
        RemoveResourceGroupCommand = ReactiveCommand.Create(OnRemoveGroup, canRemoveGroup);

        var canRemoveResource = this.WhenAnyValue(static x => x.SelectedResource)
            .Select(static x => x is not null);

        var canAddResource = this.WhenAnyValue(static model => model.NewResourceGroup)
            .Select(static entity => entity is not null);

        RemoveResourceCommand = ReactiveCommand.Create(OnRemoveResource, canRemoveResource);

        CreateGroupCommand = ReactiveCommand.Create(() =>
        {
            if (ResourceService.Instance.CreateGroup(NewGroupName))
                NewGroupName = string.Empty;
        });

        CreateResourceCommand = ReactiveCommand.Create(() =>
        {
            if (ResourceService.Instance.CreateResource(NewResourceName, NewResourceGroup))
                NewResourceName = string.Empty;
        }, canAddResource);

        Groups = (IList)ResourceService.Instance.Groups;
    }

    private void OnRemoveGroup()
    {
        ResourceService.Instance.Remove(SelectedGroup);
    }

    private void OnRemoveResource()
    {
        ResourceService.Instance.Remove(SelectedResource);
    }

    public void OnFilterUpdate(IList<object> list)
    {
        FilterContent = list.Count switch
        {
            0 => "All",
            < 4 => string.Join(", ", list.Select(item => ((IRustyEntity)item).DisplayName)),
            _ => list.Count == Groups.Count ? "All" : "Mixed...",
        };

        GroupsFilter.Load(list.Cast<IRustyEntity>());
    }

    public void Activate(CompositeDisposable disposable)
    {
        var filter = GroupsFilter
            .WhenValueChanged(static x => x.Count)
            .Select(BuildFilter);

        var resourceComparer = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static x => x.GetDescUnsafe<IHasOwner>().Owner.DisplayName, StringComparer.Ordinal)
            .ThenBy(static x => x.GetDescUnsafe<IHasOwner>().Tear)
            .ThenBy(static x => x.DisplayName, StringComparer.Ordinal);

        ResourceService.Instance.ObservableResources.Connect().Filter(filter).Sort(resourceComparer).Bind(Resources).Subscribe().DisposeWith(disposable);

        NewResourceGroup = ResourceService.Instance.Groups.FirstOrDefault();
        ResourceService.Instance.ObservableGroups.ObserveOn(RxApp.MainThreadScheduler).Subscribe(diffs =>
        {
            NewResourceGroup ??= ResourceService.Instance.Groups.FirstOrDefault();
        }).DisposeWith(disposable);
    }

    public Func<IRustyEntity, bool> BuildFilter(int count)
    {
        if (count == 0)
            return static _ => true;

        return resource => GroupsFilter.Any(group => group.GetIndex() == resource.GetDescUnsafe<IHasOwner>().Owner.GetIndex());
    }
}