using System.Reactive.Linq;
using DynamicData;
using Nito.Comparers;
using ReactiveUI;
using RustyDTO;
using RustyDTO.Interfaces;

namespace YetiEconomicaCore.Services;

public class ResourceService
{
    private static readonly Lazy<ResourceService> _instacne = new(() => new ResourceService());
    public static ResourceService Instance => _instacne.Value;

    private ResourceService()
    {
        var service = RustyEntityService.Instance;

        var sortGroup = ComparerBuilder.For<IRustyEntity>()
            .OrderBy(static entity => entity.DisplayName);

        var observable = service.ConnectToEntity(static entity => entity.Type is RustyEntityType.ResourceGroup).RemoveKey().Sort(sortGroup);
        ObservableGroups = observable;
        observable.Bind(out var list).Subscribe();
        Groups = list;

        observable = service.ConnectToEntity(static entity => entity.Type is RustyEntityType.Resource).RemoveKey().Sort(sortGroup);
        ObservableResources = observable.AsObservableList();
    }

    public IObservableList<IRustyEntity> ObservableResources { get; }

    public IObservable<IChangeSet<IRustyEntity>> ObservableGroups { get; }
    public IReadOnlyCollection<IRustyEntity> Groups { get; }

    public bool CreateGroup(string newGroupName)
    {
        RustyEntityService.Instance.Create(RustyEntityType.ResourceGroup, newGroupName);
        return true;
    }

    public bool CreateResource(string newResourceName, IRustyEntity newResourceGroup)
    {
        RustyEntityService.Instance.Create(RustyEntityType.Resource, newResourceName, EntityBuildOptions.CreateWithOwner(newResourceGroup.ID.Index));
        return true;
    }

    public void Remove(IRustyEntity entity)
    {
        RustyEntityService.Instance.Remove(entity);
    }
}