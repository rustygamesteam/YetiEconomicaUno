using RustyDTO.Interfaces;
using RustyDTO;
using RustyDTO.Supports;

namespace RustyEngine;

public partial class User
{
    private EntityID ResolveNextID(IRustyEntity archetype, EntitySpecialMask mask)
    {
        EntityID next;
        if (mask.IsHas(EntitySpecialMask.HasUniqueID))
        {
            if (mask.IsHas(EntitySpecialMask.IsUserConent))
                next = Engine.Instance.GetNextUserEntityID();
            else
                next = new EntityID(EntityIndexType.m, ++_nextMultiIndex);
        }
        else
        {
            next = archetype.ID;
        }

        return next;
    }
}