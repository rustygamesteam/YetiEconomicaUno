using System.Runtime.CompilerServices;

namespace YetiEconmicaMineCalculator;

internal static class SequenceUtils
{
    public static void Generate(Int2 size, ReadOnlySpan<PointSequenceDefinition> sequenceDefinitions, Span<Int2> sequence)
    {
        int index = 0;
        Span<Int2> tempSequence = stackalloc Int2[sequenceDefinitions[0].RowLength];

        for (var definitionIndex = 0; definitionIndex < sequenceDefinitions.Length; definitionIndex++)
        {
            var definition = sequenceDefinitions[definitionIndex];
            tempSequence = tempSequence[..definition.RowLength];
            var tmpIndex = 0;

            for (int i = 0; i < definition.Length.X; i++)
                for (int y = 0; y < size.Y; y++)
                {
                    tmpIndex = 0;
                    for (int x = 0; x < definition.RowLength; x++)
                        tempSequence[tmpIndex++] = new(x + i, y);

                    if (HasSequence(definitionIndex, sequenceDefinitions, sequence, tempSequence))
                        continue;

                    tempSequence.CopyTo(sequence.Slice(index, definition.RowLength));
                    index += definition.RowLength;

                }

            for (int i = 0; i < definition.Length.Y; i++)
                for (int x = 0; x < size.X; x++)
                {
                    tmpIndex = 0;
                    for (int y = 0; y < definition.RowLength; y++)
                        tempSequence[tmpIndex++] = new(x, y + i);

                    if (HasSequence(definitionIndex, sequenceDefinitions, sequence, tempSequence[..definition.RowLength]))
                        continue;

                    tempSequence.CopyTo(sequence.Slice(index, definition.RowLength));
                    index += definition.RowLength;
                }
        }
    }

    private static bool HasSequence(int maxIndex, ReadOnlySpan<PointSequenceDefinition> sequenceDefinitions, ReadOnlySpan<Int2> sequence, ReadOnlySpan<Int2> test)
    {
        int start = 0;
        for (var index = 0; index < maxIndex; index++)
        {
            var definition = sequenceDefinitions[index];

            for (int i = 0; i < definition.LengthSum; i++)
            {
                var slice = sequence.Slice(start + i * definition.RowLength, test.Length);
                if (slice.SequenceEqual(test))
                    return true;
            }

            start += definition.LengthSum * definition.RowLength;
        }

        return false;
    }

    public static (int stone, int iron)[] Calculate(UnsafeArray<int> chances, UnsafeArray<Int2> testPoints,
        UnsafeArray<PointSequenceDefinition> sequenceDefinition, Int2 size, int count)
    {
        var maxSequence = Math.Max(size.X, size.Y);

        var result = new (int stone, int iron)[maxSequence - 2];

        ThreadPool.GetAvailableThreads(out var workers, out _);

        var tasks = new Task[Math.Min(workers, Environment.ProcessorCount)];

        var threadCount = count / tasks.Length;

        for (int i = 0; i < tasks.Length; i++)
            tasks[i] = Task.Factory.StartNew(() => CalculateUnsafe(chances, testPoints, sequenceDefinition, size, threadCount, result));

        Task.WaitAll(tasks);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int GetValue(int* range, Int2 point, int sizeX)
    {
        return *(range + (point.Y * sizeX + point.X));
    }

    private static unsafe void CalculateUnsafe(UnsafeArray<int> chances, UnsafeArray<Int2> testPoints,
        UnsafeArray<PointSequenceDefinition> sequenceDefinition, Int2 size, int count,
        (int stone, int iron)[] result)
    {
        var random = Random.Shared;

        var maxSize = size.X * size.Y;
        var range = stackalloc int[maxSize];
        int sizeX = size.X;


        var maxSequence = Math.Max(size.X, size.Y);

        Span<int> stone = stackalloc int[maxSequence];
        Span<int> iron = stackalloc int[maxSequence];
        Span<int> writeSpan = iron;

        var chancesLength = chances.Length;
        int index = 0;
        while (index++ < count)
        {
            for (int i = 0; i < maxSize; i++)
            {
                var changeIndex = random.Next(chancesLength);
                range[i] = chances[changeIndex];
            }

            int start = 0;
            foreach (var definition in sequenceDefinition.AsSpan())
            {
                for (int i = 0; i < definition.LengthSum; i++)
                {
                    var line = testPoints.Slice(start + i * definition.RowLength, definition.RowLength);
                    var target = GetValue(range, line[0], sizeX);
                    if (target == 0)
                        continue;


                    switch (target)
                    {
                        case 1:
                            writeSpan = stone;
                            break;
                        case 2:
                            writeSpan = iron;
                            break;
                    }

                    for (var sequenceIndex = 0; sequenceIndex < line.Length; sequenceIndex++)
                    {
                        var point = line[sequenceIndex];
                        if (GetValue(range, point, sizeX) != target)
                            break;

                        writeSpan[sequenceIndex]++;
                    }
                }
                start += definition.LengthSum * definition.RowLength;
            }
        }

        for (int i = 2, iMax = result.Length; i < iMax; i++)
        {
            ref var tuple = ref result[i - 2];
            Interlocked.Add(ref tuple.iron, iron[i]);
            Interlocked.Add(ref tuple.stone, stone[i]);
        }
    }
}