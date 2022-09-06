namespace RustyEngine.Interfaces;

public interface IDiffWriter
{
    IDiffWriter MakeDeferred(long endTime);

    IDiffWriter Increment(int entity, int diffCount);
    IDiffWriter PushToBag(int entity);

    IDiffWriter SetGroupInstance(int entityGroup, int entityChild);

    void Complete();
    void Error(int code, string reason, string[] arguments);

    public void Error(int code, string reason)
    {
        Error(code, reason, Array.Empty<string>());
    }
}