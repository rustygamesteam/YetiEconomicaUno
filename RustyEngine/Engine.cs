using System.Collections.ObjectModel;
using System.Text.Json;
using RustyDTO.Interfaces;
using RustyEngine.Interfaces;

namespace RustyEngine;

public class Engine
{
    public IDiffWriter? Writter { get; private set; }

    public IEntityService EntityService { get; private set; } = null!;
    public ITimeService TimeService { get; private set; } = null!;

    private readonly Dictionary<string, User> _users = null!;
    private readonly TaskScheduler _context;

    public Engine(ITimeService timeService, JsonDocument entityConfig, int userCapacity)
    {
        EntityService = new EntityService();
        EntityService.Initialize(entityConfig);

        TimeService = timeService;
        timeService.Sync();

        _users = new Dictionary<string, User>(userCapacity);

        _context = TaskScheduler.FromCurrentSynchronizationContext();
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
        await Task.Factory.StartNew(static info => ((ExpirationTask)info!).Complete(), new ExpirationTask(_users, userId), default, TaskCreationOptions.None, _context).ConfigureAwait(false);
    }

    private record ExpirationTask(Dictionary<string, User> users, string userId)
    {
        public void Complete()
        {
            users.Remove(userId);
        }
    }
}