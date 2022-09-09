namespace RustyDTO;

[Flags]
public enum GameScopes : int
{
    None = 0,
    EntityExecute = 1 << 0,
    EntityLongExecute = 1 << 1,
    ToolUsage = 1 << 2,
    ToolUpdate = 1 << 3,
    IncreaseResources = 1 << 4,
    DecreaseResources = 1 << 5,
}