using System.Collections;

#if REACTIVE
using DynamicData;
#endif

namespace RustyDTO.Interfaces;

public interface IEntityService
{
    bool IsInitialize { get; }

    IRustyEntity GetEntity(int index);
    bool TryGetEntity(int index, out IRustyEntity? entity);

    IEnumerable<IRustyEntity> AllEntites();

    IEnumerable<IRustyEntity> EntitesWhere(Func<IRustyEntity, bool> where);
    IEnumerable<IRustyEntity> EntitesWhereType(RustyEntityType type);
    IEnumerable<IRustyEntity> EntitesWhereTypes(BitArray types);

    IEnumerable<IRustyEntity> GetItemsFor(int index);

#if REACTIVE
    IObservable<IChangeSet<IRustyEntity, int>> PreviewToEntity(Func<IRustyEntity, bool>? filter = null);
    IObservable<IChangeSet<IRustyEntity, int>> ConnectToEntity(Func<IRustyEntity, bool>? filter = null);
    IObservable<IChangeSet<IRustyEntity>> GetObservableEntitiesForOwner(int index);

    void ReplaceOwner(IRustyEntity entity, IRustyEntity newOwner);
    void Create(RustyEntityType type, string? displayName, EntityBuildOptions? options = null, int index = 0);
    void Remove(IRustyEntity entity);

    bool TryAttachProperty(IRustyEntity entity, EntityPropertyType propertyType);
    bool TryRemoveProperty(IRustyEntity entity, EntityPropertyType propertyType);
#endif
}