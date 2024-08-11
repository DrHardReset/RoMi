using System.Data;
using System.Text.RegularExpressions;
using RoMi.Business.Converters;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RoMi.Business.Models;

public class MidiDocument
{
    private static readonly byte sysexStart = "0xF0".HexStringToByte();
    private static readonly byte sysexEnd = "0xF7".HexStringToByte();
    private static readonly byte rolandId = "0x41".HexStringToByte();
    private static readonly byte commandId = "0x12".HexStringToByte(); // command type: DT1 == 0x12 == Data transmit; RQ1 == 0x11 == Data request.
    public static readonly List<int> DeviceIds = Enumerable.Range(17, 32).ToList(); // From the RD2000 documentation: 10H–1FH, the initial value is 10H (17)

    internal MidiTables MidiTables = new MidiTables();
    internal byte[] ModelIdBytes { get; set; }
    internal string DeviceName { get; private set; }

    public MidiDocument(string deviceName, string midiDocumentationFileContent)
    {
        DeviceName = deviceName;
        List<MidiValueList> midiValueLists = new List<MidiValueList>();

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

        match = GeneratedRegex.MidiMapEndMarkerRegex().Match(midiDocumentationFileContent);

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
            case "GT-1000 / GT-1000CORE":
                // as it is getting harder to find a regex that works for all PDFs we just delete this content as it will match wrongly.
                midiDocumentationFileContent = midiDocumentationFileContent.Replace(
                    "* AIRD PREAMP (valeu 1018-1023) automatically change each target to the AIRD\nPREAMP 1 or 2 whether which one is active.",
                    "");
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

            if (startAddress1 == "0" || (!startAddress1.StartsWith("0") && !startAddress1.StartsWith("#")))
            {
                // TODO: Use the valuelist und leaftable
                midiValueLists.Add(MidiTable.ParseDescriptionTable(name, dataRows));
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

            MidiTable midiTable = new MidiTable(midiTableType, deviceName, name, dataRows);
            MidiTables.Add(midiTable);
        }

        MidiTables.FixTablesWithMissingSubTables(deviceName);
    }

    internal static byte[] CalculateSysex(byte[] modelIdBytes, byte deviceId, MidiTableBranchEntry root, MidiTableBranchEntry branch1, MidiTableBranchEntry branch2, MidiTableLeafEntry leafEntry, int value)
    {
        List<byte> sysexData = new List<byte>()
        {
            sysexStart,
            rolandId,
            deviceId,
        };

        sysexData.AddRange(modelIdBytes);
        sysexData.Add(commandId);

        byte[] accumulatedStartAddress = AccumulateStartAddreses(root, branch1, branch2, leafEntry);
        sysexData.AddRange(accumulatedStartAddress);

        List<byte> valueBytes = CalculateValueBytes(leafEntry, value);
        sysexData.AddRange(valueBytes);

        byte checkSum = CalculateCheckSum(accumulatedStartAddress, value);
        sysexData.Add(checkSum);

        sysexData.Add(sysexEnd);

        return sysexData.ToArray();
    }

    private static byte[] AccumulateStartAddreses(MidiTableBranchEntry root, MidiTableBranchEntry branch1, MidiTableBranchEntry branch2, MidiTableLeafEntry leaf)
    {
        byte[] accumulatedAddress = root.StartAddress.BytesCopy();
        accumulatedAddress.Add(branch1.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
        accumulatedAddress.Add(branch2.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
        accumulatedAddress.Add(leaf.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
        return accumulatedAddress;
    }

    private static List<byte> CalculateValueBytes(MidiTableLeafEntry leafEntry, int value)
    {
        /*
         * Calculate the value byte or multiple value bytes.
         * The value must be splitted by the bitmask provided in the documentation file.
         * Multi byte values must be splitted n times by the bitmask.
         * Multi byte values must be added to sysex message from LSB to MSB.
         */
        byte[] valueArray = new byte[leafEntry.ValueDataByteBitMasks.Count];
        int valueRest = value;

        for (int i = 0; i < leafEntry.ValueDataByteBitMasks.Count; i++)
        {
            uint bitmask = leafEntry.ValueDataByteBitMasks[i];
            valueArray[i] = (byte)(valueRest & bitmask);
            int setBitCount = System.Numerics.BitOperations.PopCount(bitmask);
            valueRest = valueRest / (int)Math.Pow(2, setBitCount);
        }

        List<byte> valueBytes = valueArray.Reverse().ToList();
        return valueBytes;
    }

    private static byte CalculateCheckSum(byte[] accumulatedStartAddress, int value)
    {
        int sum = accumulatedStartAddress[0] + accumulatedStartAddress[1] + accumulatedStartAddress[2] + accumulatedStartAddress[3] + value;
        int remainder = sum % 128;
        byte checkSum = (byte)(128 - remainder);

        if (checkSum == 128)
        {
            checkSum = 0;
        }

        return checkSum;
    }
}
