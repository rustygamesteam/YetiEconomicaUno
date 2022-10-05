using System.Text.Json;
using Lepracaun;
using RustyDTO.Interfaces;
using RustyDTO.Supports;
using RustyEngine.Interfaces;
using RustyEngine.Internal;

namespace RustyEngine;

public class Engine
{
    public IDiffWriter? Writter { get; private set; }

    public IEntityService EntityService { get; private set; } = null!;
    public ITimeService TimeService { get; private set; } = null!;

    private readonly Dictionary<string, User> _users = null!;
    private WorkerThreadSynchronizationContext _userContext;

    internal PropertyFactory PropertyFactory { get; }
    internal IMutablePropertyResolver MutableResolver { get; }

    public static Engine Instance { get; private set; } = null!;

    public Engine(ITimeService timeService, JsonDocument entityConfig, int userCapacity)
    {
        Instance = this;

        MutableResolver = new SimpleMutablePropertyResolver();

        EntityService = new EntityService();
        EntityService.Initialize(entityConfig);

        TimeService = timeService;
        timeService.Sync();

        _users = new Dictionary<string, User>(userCapacity);

        _userContext = new WorkerThreadSynchronizationContext();
        _userContext.Run();
    }

    public void InjectUser(string id, JsonDocument document, long timeExpiration)
    {
        _users[id] = new User(document);
        if (timeExpiration > 0)
            UserExpiration(timeExpiration - TimeService.CurrentTime, id);
    }

    private async void UserExpiration(long awaitMilisecond, string userId)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(awaitMilisecond)).ConfigureAwait(false);
        _userContext.Post(state => ((ExpirationTask)state!).Complete(), new ExpirationTask(_users, userId));
    }

    public void InvokeInUserContext(Action action)
    {
        _userContext.Post(_ => action?.Invoke(), null);
    }

    public void InvokeInUserContext(SendOrPostCallback callback, object? state)
    {
        _userContext.Post(callback, state);
    }

    private record ExpirationTask(Dictionary<string, User> users, string userId)
    {
        public void Complete()
        {
            users.Remove(userId);
        }
    }

    public EntityID GetNextUserEntityID()
    {
        throw new NotImplementedException();
    }
}