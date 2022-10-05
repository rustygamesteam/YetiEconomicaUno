using RustyDTO.Supports;
using RustyDTO;
using RustyDTO.DescPropertyModels;
using RustyDTO.Interfaces;
using RustyDTO.MutableProperties;

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

            var mutalbe = EntityDependencies.GetMutalbeProperties(typeAsIndex);
            if (mutalbe.Count > 0)
            {
                var data = new MutableData(archetype.TypeAsIndex);

                if (mutalbe.Contains(MutablePropertyType.OwnerArchetype))
                    data.Get<IMutableOwnerArchetype>().Entity = archetype;

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