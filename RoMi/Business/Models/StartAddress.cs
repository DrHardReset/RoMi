using RoMi.Business.Converters;

namespace RoMi.Business.Models;

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
    public static byte[] HexStringToBytes(string hexString)
    {
        hexString = hexString.Replace(" ", "");

        int charCount = 2 * MaxAddressByteCount;

        if (hexString.Length > charCount)
        {
            throw new ArgumentException("String value must have " + charCount + " chars max!", nameof(hexString));
        }

        int NumberChars = hexString.Length;
        byte[] bytes = new byte[MaxAddressByteCount];
        int arrayIterator = charCount - NumberChars;

        for (int i = 0; i < NumberChars; i += 2)
        {
            bytes[(arrayIterator + i) / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
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
