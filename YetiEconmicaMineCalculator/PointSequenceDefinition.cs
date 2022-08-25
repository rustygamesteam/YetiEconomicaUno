namespace YetiEconmicaMineCalculator;

internal readonly struct PointSequenceDefinition
{
    public readonly int LengthSum;
    public readonly Int2 Length;
    public readonly int RowLength;

    public PointSequenceDefinition(Int2 areaSize, int rowLength, int totalLength)
    {
        RowLength = rowLength;
        Length = new Int2(Math.Max(0, areaSize.X - rowLength + 1), Math.Max(0, areaSize.Y - rowLength + 1));
        LengthSum = Length.X * areaSize.Y + Length.Y * areaSize.X - totalLength;
    }
}