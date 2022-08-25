

namespace YetiEconmicaMineCalculator;

internal unsafe struct UnsafeArray<T> where T : unmanaged
{
    public T* Array;
    public int Length;

    public UnsafeArray(T* array, int count)
    {
        Array = array;
        Length = count;
    }

    public Span<T> AsSpan() => new(Array, Length);

    public Span<T> Slice(int start, int length) => new(Array + start, length);

    public T this[int index] => Array[index];
}