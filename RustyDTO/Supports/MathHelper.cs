namespace RustyDTO.Supports;

public static class MathHelper
{
    public static int ToIndexString(string value)
    {
        if (value.Length == 0)
            return 0;

        int index = 0;
        bool isNeg = value[0] == '-';

        for (int i = isNeg ? 1 : 0; i < value.Length; i++)
            index = index * 10 + (value[i] - '0');
        return isNeg ? -index : index;
    }

    public static int ToUnsignedIndexString(string value)
    {
        if (value.Length == 0)
            return 0;

        int result = 0;
        for (int i = 0; i < value.Length; i++)
            result = result * 10 + (value[i] - '0');
        return result;
    }

    public static int ToIndexString(string value, int startIndex)
    {
        if (value.Length == 0)
            return 0;

        int result = 0;
        bool isNeg = value[startIndex] == '-';
        if (isNeg)
            startIndex++;

        for (int i = startIndex; i < value.Length; i++)
            result = result * 10 + (value[i] - '0');
        return isNeg ? -result : result;
    }
}