using System.Data;
using System.Text.RegularExpressions;
using RoMi.Models.Converters;

namespace RoMi.Models;

public class MidiDocument
{
    private static readonly byte sysexStart = "0xF0".HexStringToByte();
    private static readonly byte sysexEnd = "0xF7".HexStringToByte();
    private static readonly byte rolandId = "0x41".HexStringToByte();
    public static readonly List<byte> DeviceIds = Enumerable.Range(0x10, (0x1F - 0x10 + 1)) // individual ids from 0x10 to 0x1F
                                                            .Select(i => (byte)i)
                                                            .Append((byte)0x7F) // Broadcast address
                                                            .ToList();

    internal MidiTables MidiTables = [];
    internal byte[] ModelIdBytes { get; set; }
    internal string DeviceName { get; private set; }

    public MidiDocument(string deviceName, string midiDocumentationFileContent)
    {
        DeviceName = deviceName;
        Dictionary<string, MidiValueList> midiValueDictionary = [];

        GroupCollection modelIdByteStrings = GeneratedRegex.ModelIdBytesRegex().Match(midiDocumentationFileContent).Groups;

        if (modelIdByteStrings.Count < 2)
        {
            throw new Exception("Error while parsing model bytes from MIDI implementation file.");
        }

        // Find the model bytes (4 for AX Edge, 3 for RD2000). Last match group ís empty for RD2000 -> check for not empty results
        ModelIdBytes = modelIdByteStrings.Cast<Group>().Skip(1).Where(o => o.Value != string.Empty).Select(o => o.Value.HexStringToByte()).ToArray();

        Match match = GeneratedRegex.MidiMapStartMarkerRegex().Match(midiDocumentationFileContent);

        if (!match.Success)
        {
            throw new NotSupportedException("The MIDI PDF does not seem to contain a 'Parameter Address Map' section.");
        }

        int startIndex = match.Index;

        match = GeneratedRegex.MidiMapEndMarkerRegex().Matches(midiDocumentationFileContent).Last();

        if (!match.Success)
        {
            throw new NotSupportedException("The end of the 'Parameter Address Map' section could not be found.");
        }

        int endIndex = match.Index;
        midiDocumentationFileContent = midiDocumentationFileContent[startIndex..endIndex];

        #region Workaround for unexpected table formatting

        // Insert missing new lines:
        midiDocumentationFileContent = GeneratedRegex.MidiMapStartMarkerFixRegex().Replace(midiDocumentationFileContent, "\n$1");

        // Fix more PDF issues:
        switch (DeviceName)
        {
            case "JUPITER-X/Xm":
                midiDocumentationFileContent = midiDocumentationFileContent.Replace(
                    "* [Setup]\n9\nJUPITER-X/Xm MIDI Implementation",
                    "* [Setup]");
                midiDocumentationFileContent = midiDocumentationFileContent.Replace(
                    "* [User Pattern]\n16\nJUPITER-X/Xm MIDI Implementation",
                    "* [User Pattern]");
                break;
        }

        #endregion

        string[] tablesRawSplit = GeneratedRegex.MidiTableNameRegex().Split(midiDocumentationFileContent);

        if (tablesRawSplit.Length <= 0)
        {
            throw new Exception("The tables could not be parsed.");
        }

        for (int i = 0; i < tablesRawSplit.Length; i++)
        {
            // Get the name of the table. Use the device name for root table as for some devices (e.g. RD2000) no header for first table is provided.
            string name;

            if (i == 0)
            {
                name = deviceName;
            }
            else
            {
                match = GeneratedRegex.MidiTableNameExtractRegex().Match(tablesRawSplit[i]);

                if (!match.Success)
                {
                    throw new NotSupportedException("Failed to parse table name header.");
                }

                if (match.Groups[1].Success)
                {
                    name = match.Groups[1].Value.Trim();
                }
                else if (match.Groups[2].Success)
                {
                    name = match.Groups[2].Value.Trim();
                }
                else
                {
                    throw new NotImplementedException("Failed to parse table name header although regex matched.");
                }
            }

            // split single Rows and keep only relevant rows that contain whether start addresses or value descriptions:
            List<string> dataRows = GeneratedRegex.MiditableContentRow().Matches(tablesRawSplit[i]).Select(x => x.Value.Trim()).ToList();

            if (deviceName == "GT-1000 / GT-1000CORE")
            {
                if (name == "PatchEfct")
                {
                    // Table contains empty rows which are currently not filtered by GeneratedRegex.MiditableContentRow.
                    dataRows.Remove(x => x == "|             |           |                                                    |");
                }

                if (dataRows.Count == 0)
                {
                    // Table "*4 CHAIN ELEMENT TABLE" is followed by multiple comment rows which need to be ignored, e.g.:
                    continue;
                }
            }

            if (dataRows.Count == 0)
            {
                throw new Exception($"No useful rows could be found in table '{name}'.");
            }

            string startAddress1 = dataRows[0].Split("|")[1].Replace(" ", "");

            if (startAddress1 == "0" || (!startAddress1.StartsWith('0') && !startAddress1.StartsWith('#')))
            {
                midiValueDictionary.Add(name, MidiTable.ParseDescriptionTable(name, dataRows));
                continue;
            }

            MidiTableType midiTableType;

            switch (startAddress1.Length)
            {
                case 8: // 00000000
                    midiTableType = MidiTableType.RootTable;
                    break;
                case 6: // 000000
                    midiTableType = MidiTableType.BranchTable;
                    break;
                default:
                case 4: // 0000
                case 5: // #0000
                    midiTableType = MidiTableType.LeafTable;
                    break;
            }

            MidiTable midiTable = new(midiTableType, deviceName, name, dataRows);
            MidiTables.Add(midiTable);
        }

        MidiTables.FixTablesWithMissingSubTables(deviceName);
        MidiTables.LinkValueDescriptionTables(midiValueDictionary);
    }

    internal static byte[] AccumulateStartAddreses(MidiTableBranchEntry root, MidiTableBranchEntry branch1, MidiTableBranchEntry? branch2, MidiTableLeafEntry leaf)
    {
        byte[] accumulatedAddress = root.StartAddress.BytesCopy();
        accumulatedAddress.Add(branch1.StartAddress.Bytes, StartAddress.MaxAddressByteCount);

        if (branch2 != null)
        {
            accumulatedAddress.Add(branch2.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
        }

        accumulatedAddress.Add(leaf.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
        return accumulatedAddress;
    }

    internal static byte[] CalculateValueBytes(List<uint> valueDataByteBitMasks, int value)
    {
        /*
         * Calculate the value byte or multiple value bytes.
         * The value must be splitted by the bitmask provided in the documentation file.
         * Multi byte values must be splitted n times by the bitmask.
         * Multi byte values must be added to sysex message from LSB to MSB.
         */
        byte[] valueArray = new byte[valueDataByteBitMasks.Count];
        int valueRest = value;

        for (int i = 0; i < valueDataByteBitMasks.Count; i++)
        {
            uint bitmask = valueDataByteBitMasks[i];
            valueArray[i] = (byte)(valueRest & bitmask);
            int setBitCount = System.Numerics.BitOperations.PopCount(bitmask);
            valueRest /= (int)Math.Pow(2, setBitCount);
        }

        byte[] valueBytes = valueArray.Reverse().ToArray();
        return valueBytes;
    }

    internal static int CalculateValueFromBytes(List<uint> valueDataByteBitMasks, byte[] valueBytes)
    {
        /*
         * Reconstruct the integer value from a list of value bytes and corresponding bitmasks.
         * The bitmasks define how many bits are relevant in each byte (e.g., 0x7F for 7 bits).
         * Value bytes are expected in MSB to LSB order, so we reverse them before processing.
         */

        if (valueBytes.Length != valueDataByteBitMasks.Count)
        {
            throw new ArgumentException("Length of valueBytes must match valueDataByteBitMasks count");
        }

        valueBytes = valueBytes.Reverse().ToArray();

        int value = 0;
        int shift = 0;

        for (int i = 0; i < valueBytes.Length; i++)
        {
            uint mask = valueDataByteBitMasks[i];
            int setBitCount = System.Numerics.BitOperations.PopCount(mask);

            // Ensure the byte value only includes valid bits
            byte maskedValue = (byte)(valueBytes[i] & mask);

            value |= maskedValue << shift;
            shift += setBitCount;
        }

        return value;
    }

    public byte[] CalculateDt1(byte deviceId, byte[] address, List<uint> valueDataByteBitMasks, int value)
    {
        byte[] valueBytes = CalculateValueBytes(valueDataByteBitMasks, value);

        // SysExStart + RolandId + DevíceId + nxModelId + MessageType + 4xAddress + 4xValue + Checksum + SysExEnd
        byte[] message = new byte[1 + 1 + 1 + ModelIdBytes.Length + 1 + address.Length + valueBytes.Length + 1 + 1];
        //byte[] message = new byte[10 + address.Length + valueBytes.Length];
        int i = 0;

        message[i++] = sysexStart;
        message[i++] = rolandId;
        message[i++] = deviceId;
        Array.Copy(ModelIdBytes, 0, message, i, ModelIdBytes.Length);
        i += ModelIdBytes.Length;
        message[i++] = 0x12; // DT1

        Array.Copy(address, 0, message, i, address.Length);
        i += address.Length;

        Array.Copy(valueBytes, 0, message, i, valueBytes.Length);
        i += valueBytes.Length;

        byte[] checksumData = address.Concat(valueBytes).ToArray();
        message[i++] = CalculateChecksum(checksumData);
        message[i++] = sysexEnd;

        return message;
    }

    public byte[] CalculateRq1(byte deviceId, byte[] address, int size)
    {
        byte[] sizeBytes =
        [
            (byte)((size >> 24) & 0x7F),
            (byte)((size >> 16) & 0x7F),
            (byte)((size >> 8) & 0x7F),
            (byte)(size & 0x7F)
        ];

        // SysExStart + RolandId + DevíceId + ModelId + MessageType + 4xAddress + 4xSize + Checksum + SysExEnd
        int messageLength = 1 + 1 + 1 + ModelIdBytes.Length + 1 + address.Length + sizeBytes.Length + 1 + 1;
        byte[] message = new byte[messageLength];
        int i = 0;

        message[i++] = sysexStart;
        message[i++] = rolandId;
        message[i++] = deviceId;

        Array.Copy(ModelIdBytes, 0, message, i, ModelIdBytes.Length);
        i += ModelIdBytes.Length;

        message[i++] = 0x11; // RQ1

        Array.Copy(address, 0, message, i, 4);
        i += 4;

        Array.Copy(sizeBytes, 0, message, i, 4);
        i += 4;

        byte checksum = CalculateChecksum(address.Concat(sizeBytes).ToArray());
        message[i++] = checksum;
        message[i++] = sysexEnd;

        return message;
    }

    private static byte CalculateChecksum(byte[] data)
    {
        int sum = data.Sum(x => x);
        return (byte)((128 - (sum % 128)) & 0x7F);
    }
}
