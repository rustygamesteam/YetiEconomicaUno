namespace YetiEconmicaMineCalculator;

public class MineCache
{
    private readonly Key _key;
    private (float Stone, float Ore) _multipliers;

    internal record struct Key(Int2 Size, (int Ground, int Stone, int Ore) Proportions)
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(Size.X, Size.Y, Proportions.Ground, Proportions.Stone, Proportions.Ore);
        }
    }

    private readonly static Dictionary<Key, MineCache> _cache = new(MaxCapacity);
    private readonly static LinkedList<MineCache> _list = new ();

    private const int MaxCapacity = 10;

    public static (float Stone, float Ore) GetCacheValue((int X, int Y) size, (int Ground, int Stone, int Ore) proportions, int testCount)
    {
        var key = new Key(new Int2(size.X, size.Y), proportions);
        if (!_cache.TryGetValue(key, out var cache))
        {
            while(_list.Count + 1 >= MaxCapacity)
            {
                var value = _list.First!.Value;
                _list.RemoveFirst();
                _cache.Remove(value._key);
            }

            _cache[key] = cache = new MineCache(key, testCount);
            _list.AddLast(cache);
        }

        return cache._multipliers;
    }

    internal MineCache(Key key, int testCount)
    {
        _key = key;
        _multipliers = GetValue(testCount);
    }

    internal unsafe (float Stone, float iron) GetValue(int testCount)
    {
        var proportions = _key.Proportions;
        var size = _key.Size;

        var mineProportionsSum = proportions.Ground + proportions.Stone + proportions.Ore;

        var chances = stackalloc int[mineProportionsSum];
        for (int i = 0, iMax = proportions.Ground; i < iMax; i++)
            chances[i] = 0;
        for (int i = proportions.Ground,
             iMax = proportions.Ground + proportions.Stone; i < iMax; i++)
            chances[i] = 1;
        for (int i = proportions.Ground + proportions.Stone,
             iMax = proportions.Ground + proportions.Stone + proportions.Ore; i < iMax; i++)
            chances[i] = 2;

        var chancesArray = new UnsafeArray<int>(chances, mineProportionsSum);


        const int rowMin = 3;
        var rowMax = Math.Max(size.X, size.Y);

        _multipliers = new((float)proportions.Stone / mineProportionsSum, (float)proportions.Ore / mineProportionsSum);

        if (rowMin > rowMax)
            return _multipliers;

        int length = 0;
        int pointLength = 0;
        var sequenceDefinitionRaw = stackalloc PointSequenceDefinition[rowMax - rowMin + 1];
        var sequenceDefinition = new UnsafeArray<PointSequenceDefinition>(sequenceDefinitionRaw, rowMax - rowMin + 1);
        for (int definitionIndex = 0; rowMax >= rowMin; definitionIndex++)
        {
            ref var definition = ref sequenceDefinitionRaw[definitionIndex];

            definition = new PointSequenceDefinition(size, rowMax--, length);
            length += definition.LengthSum;
            pointLength += definition.LengthSum * definition.RowLength;
        }

        var sequenceRaw = stackalloc Int2[pointLength];
        SequenceUtils.Generate(size, new ReadOnlySpan<PointSequenceDefinition>(sequenceDefinitionRaw, sequenceDefinition.Length), new Span<Int2>(sequenceRaw, pointLength));
        var sequence = new UnsafeArray<Int2>(sequenceRaw, pointLength);

        var result = SequenceUtils.Calculate(chancesArray, sequence, sequenceDefinition, size, testCount);

        for (var i = 0; i < result.Length; i++)
        {
            var tuple = result[i];

            _multipliers.Stone += (float)tuple.stone / testCount * (i + rowMin);
            _multipliers.Ore += (float)tuple.iron / testCount * (i + rowMin);
        }

        _multipliers.Stone /= result.Length;
        _multipliers.Ore /= result.Length;

        return _multipliers;
    }
}
