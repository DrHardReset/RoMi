namespace RoMi.Models.Converters;

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
    /// <param name="bytes">The byte array to calculate its integer representation for.</param>
    /// <param name="MaxAddressByteCount">The max number of bytes a <see cref="StartAddress"/> may consist of.</param>
    public static int ToIntegerRepresentation(this byte[] bytes, int MaxAddressByteCount)
    {
        if (bytes.Any(x => x < 0 || x > 127))
        {
            string byteString = string.Join(' ', Array.ConvertAll(bytes, b => "0x" + b.ToString("X2")));
            throw new ArgumentException("Each single byte of the array must be in the range between 0 (0x00) and 127 (0x7F): " + byteString, nameof(bytes));
        }

        int result = 0;

        for (int i = 0; i < MaxAddressByteCount; i++)
        {
            result += bytes[i] << (7 * (MaxAddressByteCount - 1 - i));
        }

        return result;
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
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The decimal value must not be negative.");
        }

        var requiredLength = (int)Math.Ceiling(Math.Log(value + 1, 128));

        if (requiredLength > MaxAddressByteCount)
        {
            throw new ArgumentException($"The decimal value can not be converted to byte array as the {MaxAddressByteCount} byte array would be too small.");
        }

        var bytes = new byte[MaxAddressByteCount];
        int index = MaxAddressByteCount - 1;

        while (value > 0 && index >= 0)
        {
            bytes[index--] = (byte)(value & 0x7F); // 0x7F = 127, masks the lower 7 bits
            value >>= 7;
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
