namespace RoMi.Business.Converters;

public static class Converter
{
    public static int IndexOfNth(this string str, string value, int nth = 0)
    {
        if (nth < 0)
        {
            throw new ArgumentException("A negative index of substring in string could not be found. Must start with 0", nameof(nth));
        }

        int offset = str.IndexOf(value);

        for (int i = 0; i < nth; i++)
        {
            if (offset == -1) return -1;
            {
                offset = str.IndexOf(value, offset + 1);
            }
        }

        return offset;
    }

    /// <summary>
    /// Converts a single hex value string to byte.
    /// </summary>
    /// <param name="hex">e.g. 'A1'</param>
    public static byte HexStringToByte(this string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }

        int NumberChars = hex.Length;

        if (NumberChars > 2)
        {
            throw new ArgumentException("String value must ony have 2 chars!", nameof(hex));
        }

        return Convert.ToByte(hex, 16);
    }

    /// <summary>
    /// Converts the start address to it's integer value.
    /// Logic:
    /// sum  = Bytes[0] * 128 * 128 * 128;
    /// sum += Bytes[1] * 128 * 128;
    /// sum += Bytes[2] * 128;
    /// sum += Bytes[3];
    /// </summary>
    public static int ToIntegerRepresentation(this byte[] givenBytes, int MaxAddressByteCount)
    {
        int sum = 0;

        for (int i = 0; i < MaxAddressByteCount; i++)
        {
            if (i == MaxAddressByteCount - 1)
            {
                sum += givenBytes[i];
                break;
            }

            sum += givenBytes[i] * Convert.ToInt32(Math.Pow(128, MaxAddressByteCount - 1 - i));
        }

        return sum;
    }

    /// <summary>
    /// Converts an integer value to the corresponding start address byte array.
    /// Logic:
    /// bytes[0] = value / 128 / 128 / 128;
    /// bytes[1] = value / 128 / 128;
    /// bytes[2] = value / 128;
    /// bytes[3] = value % 128;
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    public static byte[] From7bitIntegerRepresentation(this int value, int MaxAddressByteCount)
    {
        byte[] bytes = new byte[MaxAddressByteCount];

        for (int i = 0; i < MaxAddressByteCount; i++)
        {
            if (i == MaxAddressByteCount - 1)
            {
                bytes[i] = (byte)(value % 128);
                break;
            }

            bytes[i] = Convert.ToByte(value / Math.Pow(128, MaxAddressByteCount - 1 - i));
        }

        return bytes;
    }

    public static void Add(this byte[] summands1, byte[] summands2, int MaxAddressByteCount)
    {
        for (int i = 0; i < MaxAddressByteCount; i++)
        {
            summands1[i] += summands2[i];

            bool overflow = summands1[i] / 128 > 0;

            if (overflow)
            {
                if (i == 0)
                {
                    throw new OverflowException("First byte of " + nameof(StartAddress) + " is bigger than 127: " + summands1[i]);
                }

                summands1[i - 1] += (byte)(summands1[i] / 128);
                summands1[i] = (byte)(summands1[i] % 128);
            }
        }
    }

    public static string ByteArrayToHexString(this byte[] array)
    {
        if (array == null)
        {
            return string.Empty;
        }

        return string.Join(" ", array.Select(x => x.ByteToHexString()));
    }


    public static string ByteToHexString(this byte value)
    {
        return string.Format("{0:X2}", value);
    }
}
