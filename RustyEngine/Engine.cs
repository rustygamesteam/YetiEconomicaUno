using System.Collections.Concurrent;
using System.Text.Json;
using Lepracaun;
using RustyDTO;
using RustyDTO.CodeGen.Impl;
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

    private readonly ConcurrentDictionary<string, User> _entityInUsers;
    private readonly ConcurrentDictionary<string, User> _users;
    
    private WorkerThreadSynchronizationContext _userContext;

    internal IMutablePropertyResolver MutableResolver { get; }
    internal IDescPropertyResolver DescPropertyResolver { get; }

    public static Engine Instance { get; private set; } = null!;

    public Engine(ITimeService timeService, JsonDocument entityConfig)
    {
        Instance = this;

        MutableResolver = new SimpleMutablePropertyResolver();
        DescPropertyResolver = new SimpleDescPropertyResolver();

        EntityService = new EntityService();
        EntityService.Initialize(entityConfig);

        TimeService = timeService;
        timeService.Sync();

        _users = new ConcurrentDictionary<string, User>();
        _entityInUsers = new ConcurrentDictionary<string, User>();

        _userContext = new WorkerThreadSynchronizationContext();
        _userContext.Run();
    }

    public void InjectUser(string id, JsonDocument document, long timeExpiration)
    {
        _users[id] = new User(document, this);
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
    
    public bool TryGetUserEntity(string udid, EntityID id, out IRustyUserEntity entity)
    {
        return _entityInUsers[udid].TryGetUserEntity(id, out entity);
    }

    private record ExpirationTask(ConcurrentDictionary<string, User> users, string userId)
    {
        public void Complete()
        {
            users.TryRemove(userId, out var user);
        }
    }
}