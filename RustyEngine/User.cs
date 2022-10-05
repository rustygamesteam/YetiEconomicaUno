using RustyDTO;
using RustyDTO.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using RustyDTO.DescPropertyModels;
using RustyDTO.MutableProperties;
using RustyDTO.Supports;
using RustyEngine.Enums;

namespace RustyEngine;

public partial class User
{
    private HashSet<EntityID> _instanceBag;
    private Dictionary<EntityID, MutableData> _mutableBag;

    private Dictionary<EntityID, TaskInfo> _exucutingTask;

    private JsonObject _stats;

    private Random _random;
    private int _nextMultiIndex;

    private Dictionary<string, int> _intStats;
    private Dictionary<string, string> _stringStats;
    private Dictionary<string, double> _doubleStats;

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

        if (json.TryGetProperty("tasks", out var tasks))
        {
            _exucutingTask = new Dictionary<EntityID, TaskInfo>(tasks.GetArrayLength() + 8);
            foreach (var taskData in tasks.EnumerateObject())
            {
                //TODO парсим задачу
            }
        }
        else
            _exucutingTask = new Dictionary<EntityID, TaskInfo>(8);

        if (json.TryGetProperty("stats", out var statsJsonElement))
            _stats = JsonObject.Create(statsJsonElement)!;
        else
            _stats = new JsonObject();
    }

    public bool OptionalHasEntity(IRustyEntity? entity)
    {
        if (entity is null)
            return true;
        return HasEntity(entity);
    }

    public bool HasEntity(IRustyEntity entity)
    {
        if (entity.MutablePropertyTypes.Count > 0)
            return _mutableBag.ContainsKey(entity.ID);
        else
            return _instanceBag.Contains(entity.ID);
    }

    private bool CanPayInternal(IPayable payable)
    {
        var bag = _mutableBag;
        foreach (var resourceStack in payable.Price)
        {
            if (!bag.TryGetValue(resourceStack.Resource.ID, out var data) || data.Get<IMutableCount>().Count < (int)resourceStack.Value)
                return false;
        }

        return true;
    }

    private void UncheckedPayInternal(IPayable payable)
    {
        var bag = _mutableBag;
        foreach (var resourceStack in payable.Price)
            bag[resourceStack.Resource.ID].Get<IMutableCount>().Count -= (int)resourceStack.Value;

        //TODO: Сообщить серверу о изменениях
    }

    private void UnchekedSinglePay(IRustyEntity entity, int count)
    {
        _mutableBag[entity.ID].Get<IMutableCount>().Count -= count;
        //TODO: Сообщить серверу о изменениях
    }

    private void InternalIncrement(IRustyEntity resource, int count)
    {
        ResolveMutableData(resource).Get<IMutableCount>().Count += count;
        //TODO: Сообщить серверу о изменениях
    }

    private MutableData ResolveMutableData(IRustyEntity entity)
    {
        if (!_mutableBag.TryGetValue(entity.ID, out var result))
            _mutableBag.Add(entity.ID, result = new MutableData(entity.TypeAsIndex));

        return result;
    }

    public bool CanExecute(IRustyEntity entity, int multiplayFactor = 1)
    {
        if (!entity.HasSpecialMask(EntitySpecialMask.Executable))
            return false;

        if (entity.TryGetProperty(out IHasDependents dependents) && !(OptionalHasEntity(dependents.Required) && OptionalHasEntity(dependents.VisibleAfter)))
            return false;

        if (entity.TryGetProperty(out IPayable payable) && !CanPayInternal(payable))
            return false;

        if (entity.TryGetProperty(out IHasExchange exchange))
        {
            if (!_mutableBag.TryGetValue(exchange.FromEntity.ID, out var data) ||
                data.Get<IMutableCount>().Count < exchange.FromRate * multiplayFactor)
                return false;
        }

        if (entity.HasSpecialMask(EntitySpecialMask.IsInstance))
        {
            var mutalbeTypes = EntityDependencies.GetMutalbeProperties(entity.Type);
            if (mutalbeTypes.Count == 0 && _instanceBag.Contains(entity.ID))
                return false;

            if (mutalbeTypes.Count > 0 && _mutableBag.ContainsKey(entity.ID))
                return false;
        }

        return true;
    }

    private int CalculateDuration(IRustyEntity entity, int duration)
    {
        if (!entity.TryGetProperty(out IInBuildProcess inBuildProcess) || inBuildProcess.Build is null)
            return duration;

        if (_mutableBag.TryGetValue(inBuildProcess.Build.ID, out var mutableBuildData) && 
            mutableBuildData.TryGet(out IMutableUsedInstance usedInstance) && 
            usedInstance.Entity is not null)
        {
            var build = usedInstance.Entity;
            switch (entity.Type)
            {
                case RustyEntityType.CraftTask:
                    if (build.TryGetProperty(out ICraftSpeed craftSpeed))
                        duration = (int)Math.Ceiling(duration / craftSpeed.Factor);
                    break;
                case RustyEntityType.Tech:
                    if (build.TryGetProperty(out ITechSpeed techSpeed))
                        duration = (int)Math.Ceiling(duration / techSpeed.Factor);
                    break;
            }
        }
        
        //TODO Добавляем модификации от менеджеров баффы

        return duration;
    }

    public void Execute(IRustyEntity entity, int multiplayFactor = 1)
    {
        if(!CanExecute(entity, multiplayFactor))
            return;

        //TODO начинаем запись

        IPayable payable;
        if (entity.TryGetProperty(out payable))
            UncheckedPayInternal(payable);

        if (entity.TryGetProperty(out IHasExchange exchange))
            UnchekedSinglePay(exchange.FromEntity, (int) (exchange.FromRate * multiplayFactor));

        if (entity.TryGetProperty(out ILongExecution longExecution))
        {
            ResourceStack[] onCancell = ReferenceEquals(payable, null)
                ? Array.Empty<ResourceStack>()
                : payable.Price.ToArray();

            int duration = CalculateDuration(entity, longExecution.Duration);
            var endTime = Engine.Instance.TimeService.CurrentTime + duration;
            _exucutingTask.Add(entity.ID, new TaskInfo(endTime, multiplayFactor, entity, onCancell));

            //TODO: Сообщить серверу о задаче
            //TODO: запустить задачу

            return;
        }

        ExecuteAfterPay(entity, multiplayFactor);
    }

    private void ExecuteAfterPay(IRustyEntity entity, int multiplayFactor)
    {
        if (entity.TryGetProperty(out IHasRewards rewards))
        {
            foreach (var resourceStack in rewards.Rewards)
                InternalIncrement(resourceStack.Resource, (int) resourceStack.Value);
        }
        else if (entity.TryGetProperty(out IHasSingleReward singleReward))
            InternalIncrement(singleReward.Entity, singleReward.Count);

        if (entity.TryGetProperty(out IHasExchange exchange))
            InternalIncrement(exchange.ToEntity, (int)(multiplayFactor / exchange.FromRate));

        TryCreateInstance(entity);

        //TODO завершаем запись
    }

    public void ExecuteBatch(IEnumerable<(IRustyEntity entity, int multiplayFactor)> batches)
    {
        foreach (var batch in batches)
            Execute(batch.entity, batch.multiplayFactor);
    }

    private void OnTaskResult(RustyTaskResult status, EntityID index)
    {
        var info = _exucutingTask[index];
        switch (status)
        {
            case RustyTaskResult.Cancel:
                //TODO Начинаем запись
                foreach (var resourceStack in info.ResourcesOnCancel)
                    InternalIncrement(resourceStack.Resource, (int)resourceStack.Value);
                //TODO завершаем запись
                throw new NotImplementedException(); //TODO: Result for client
                break;
            case RustyTaskResult.Success:
                //TODO Начинаем запись
                ExecuteAfterPay(info.ExecuteOnSuccess, info.MultiplayFactor);
                throw new NotImplementedException(); //TODO: Result for client
                break;
            case RustyTaskResult.Failure:
                throw new NotImplementedException(); //TODO: Error for client
            case RustyTaskResult.None:
                throw new NotImplementedException(); //TODO: Error for server
        }
    }
}

public record struct TaskInfo (long TimeComplete, int MultiplayFactor, IRustyEntity ExecuteOnSuccess, ResourceStack[] ResourcesOnCancel);