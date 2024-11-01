using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RoMi.Models.Converters;

namespace RoMi.Models;

/// <summary>
/// As MIDI uses the 8th (MSB) bit to distinguish between status and data byte, a class is needed to represent the "usable" 7 bits of a MIDI data byte.
/// </summary>
public class StartAddress
{
    public const int MaxAddressByteCount = 4;
    /// <summary>Index 1 = MSB; Index 4 = LSB</summary>
    public byte[] Bytes { get; private set; }

    public StartAddress(byte[] bytes)
    {
        Bytes = bytes;
    }

    public StartAddress(string hexString)
    {
        Bytes = HexStringToBytes(hexString);
    }

    /// <summary>
    /// Converts a hex string containing max <see cref="MaxAddressByteCount"/> values to byte array.
    /// </summary>
    /// <param name="hexString">e.g. '001020A1'</param>
    private static byte[] HexStringToBytes(string hexString)
    {
        hexString = hexString.Trim();

        if (!GeneratedRegex.HexStringRegex().IsMatch(hexString))
        {
            throw new ArgumentException("Malformed hex string.", nameof(hexString));
        }

        string[] byteStrings = hexString.Split(' ');

        if (byteStrings.Length < 2)
        {
            throw new ArgumentException($"String value must consist of at least {MaxAddressByteCount} two digit numbers.", nameof(hexString));
        }

        if (byteStrings.Length > MaxAddressByteCount)
        {
            throw new ArgumentException("String value must consist of " + MaxAddressByteCount + " two digit numbers.", nameof(hexString));
        }

        byte[] bytes = new byte[MaxAddressByteCount];
        int arrayIterator = MaxAddressByteCount - byteStrings.Length;

        for (int i = 0; i < byteStrings.Length; i++)
        {
            bytes[arrayIterator + i] = Convert.ToByte(byteStrings[i], 16);
        }

        if (bytes.Any(x => x < 0 || x > 127))
        {
            throw new ArgumentException("Each single byte of the array must be in the range between 0 (0x00) and 127 (0x7F): " + hexString, nameof(hexString));
        }

        return bytes;
    }

    public byte[] BytesCopy()
    {
        return (byte[])Bytes.Clone();
    }

    /// <summary>
    /// Converts the start address to it's integer value.
    /// </summary>
    public int ToIntegerRepresentation()
    {
        return Bytes.ToIntegerRepresentation(MaxAddressByteCount);
    }

    public static byte[] CalculateOffset(StartAddress lowerStartAddress, StartAddress higherStartAddress)
    {
        int lower = lowerStartAddress.ToIntegerRepresentation();
        int higher = higherStartAddress.ToIntegerRepresentation();

        if (lower > higher)
        {
            throw new ArgumentException("The first argument must contain a lower value than the second.");
        }

        int offset = higher - lower;
        return offset.From7bitIntegerRepresentation(MaxAddressByteCount);
    }

    public void Increment(byte[] value)
    {
        Bytes.Add(value, MaxAddressByteCount);
    }

    public bool Equals(StartAddress comparableStartAddress)
    {
        return Bytes.SequenceEqual(comparableStartAddress.Bytes);
    }
}
