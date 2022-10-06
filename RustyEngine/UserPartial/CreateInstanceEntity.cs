using RustyDTO.Supports;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using RustyDTO.MutableProperties;
using RustyDTO.MutableProperties.Impl;

namespace RustyEngine;

public partial class User
{
    
    private void TryCreateInstance(IRustyEntity archetype)
    {
        TryCreateInstance(archetype, out _);
    }

    private bool TryCreateInstance(IRustyEntity archetype, out EntityID next)
    {
        var typeAsIndex = archetype.TypeAsIndex;
        var mask = EntityDependencies.GetMask(typeAsIndex);
        
        
        if (mask.IsHas(EntitySpecialMask.IsInstance))
        {
            if (archetype.TryGetProperty(out IHasOwner ownerInfo))
            {
                IMutableUsedInstance? usedInstance;
                if (_mutableBag.TryGetValue(ownerInfo.Owner.ID, out var ownerData))
                    usedInstance = ownerData.Get<IMutableUsedInstance>();
                else if (TryCreateInstance(ownerInfo.Owner, out var ownerID))
                    usedInstance = _mutableBag[ownerID].Get<IMutableUsedInstance>();
                else
                    usedInstance = null;

                if (usedInstance is not null)
                    usedInstance.Entity = archetype;
            }

            next = ResolveNextID(archetype, mask);

            var mutable = EntityDependencies.GetMutalbeProperties(typeAsIndex);
            if (mutable.Count > 0)
            {
                var data = new RustyUserEntity(archetype);

                if (mutable.Contains(MutablePropertyType.OwnerArchetype))
                    data.InjectProperty(new MutableOwnerArchetypeImpl
                    {
                        Entity = archetype
                    });
                
                if(mutable.Contains(MutablePropertyType.Author))
                    data.InjectProperty(new MutableAuthorImpl
                    {
                        ID = ID
                    });

                _mutableBag[next] = data;
            }
            else
                _instanceBag.Add(next);

            return true;
        }

        next = default;
        return false;
    }
}