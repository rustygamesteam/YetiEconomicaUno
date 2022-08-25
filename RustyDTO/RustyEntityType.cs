namespace RustyDTO;

public enum RustyEntityType : int
{
    None = 0,
    Resource = 1,
    ResourceGroup = 2,
    Tech = 3,
    UniqueBuild = 4,
    Build = 5,
    UniqueTool = 6,
    Tool = 7,
    Craft = 8,
    Plant = 9,
    // 10 is free
    Exchage = 11,
    FarmObstacleClearing = 12,
    PVE = 13,
}