namespace RustyEngine.Interfaces;

public interface ITimeService
{
    void Sync();

    bool IsReady { get; }
    long CurrentTime { get; }
}