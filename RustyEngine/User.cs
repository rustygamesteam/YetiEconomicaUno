using RustyDTO;
using RustyDTO.Interfaces;
using System.Text.Json;
using RustyDTO.Supports;
using RustyEngine.Enums;

namespace RustyEngine;

public class User
{
    private HashSet<EntityID> _instanceBag;
    private Dictionary<EntityID, MutableData> _mutableBag;

    private Dictionary<EntityID, TaskInfo> _exucutingTask;

    public User(JsonDocument jsonDocument)
    {
        var json = jsonDocument.RootElement;

        if (json.TryGetProperty("bag", out var bagNode))
        {
            _instanceBag = new HashSet<EntityID>(bagNode.GetArrayLength() + 3);
            foreach (var value in bagNode.EnumerateArray())
                _instanceBag.Add(EntityID.Parse(value.GetString()!));
        }
        else
            _instanceBag = new HashSet<EntityID>(8);

        if (json.TryGetProperty("mutalbeBag", out var mutableBagNode))
        {
            var values = mutableBagNode.EnumerateObject();

            _mutableBag = new Dictionary<EntityID, MutableData>(values.Count() + 3);
            foreach (var value in values)
            {
                var index = EntityID.Parse(value.Name);
                _mutableBag[index] = new MutableData(value.Value);
            }
        }
        else
            _mutableBag = new Dictionary<EntityID, MutableData>(8);
    }

    public bool HasEntity(IRustyEntity entity)
    {
        if (entity.MutablePropertyTypes.Count > 0)
            return _mutableBag.ContainsKey(entity.ID);
        else
            return _instanceBag.Contains(entity.ID);
    }

    public bool CanExecute(IRustyEntity rustyEntity)
    {
        throw new NotImplementedException();
    }

    public void Execute(IRustyEntity entity)
    {
        throw new NotImplementedException();
    }

    public void ExecuteBatch(IEnumerable<IRustyEntity> batches)
    {
        foreach (var batch in batches)
            Execute(batch);
    }

    private void OnTaskResult(RustyTaskResult status, EntityID index)
    {

    }
}

public record struct TaskInfo (long TimeComplete, ResourceStack[] ResourcesOnSuccess, ResourceStack[] ResourcesOnCancel);