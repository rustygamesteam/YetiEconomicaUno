using System.Collections;
using System.Text.Json;
using RustyDTO.Supports;


/*
#if REACTIVE
using DynamicData;
#endif
*/

namespace RustyDTO.Interfaces;

public interface IEntityService
{
    bool IsInitialize { get; }

    void Initialize(JsonDocument? database);

    IRustyEntity GetEntity(EntityID id);
    bool TryGetEntity(EntityID id, out IRustyEntity? entity);

    IEnumerable<IRustyEntity> AllEntites();
    IEnumerable<IRustyEntity> GetItemsFor(EntityID ownerID);

    IEnumerable<IRustyEntity> EntitesWhere(Func<IRustyEntity, bool> where);
    IEnumerable<IRustyEntity> EntitesWhereType(RustyEntityType type);
    IEnumerable<IRustyEntity> EntitesWhereTypes(BitArray types);

/*
#if REACTIVE
IObservable<IChangeSet<IRustyEntity, int>> PreviewToEntity(Func<IRustyEntity, bool>? filter = null);
IObservable<IChangeSet<IRustyEntity, int>> ConnectToEntity(Func<IRustyEntity, bool>? filter = null);
IObservable<IChangeSet<IRustyEntity>> GetObservableEntitiesForOwner(EntityID id);

void ReplaceOwner(IRustyEntity entity, IRustyEntity newOwner);
void Create(RustyEntityType type, string? displayName, EntityBuildOptions? options = null, int index = 0);
void Remove(IRustyEntity entity);

bool TryAttachProperty(IRustyEntity entity, DescPropertyType propertyType);
bool TryRemoveProperty(IRustyEntity entity, DescPropertyType propertyType);
#endif*/
}