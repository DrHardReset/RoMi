using System.Data;
using System.Text.RegularExpressions;
using RoMi.Business.Converters;
using Windows.Foundation.Collections;

namespace RoMi.Business.Models
{
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

            GroupCollection modelIdByteStrings = GeneratedRegex.ModelIdBytesRegex().Match(midiDocumentationFileContent).Groups;

            if (modelIdByteStrings.Count < 2)
            {
                throw new Exception("Error while parsing model bytes from MIDI implementation file.");
            }

            // Find the model bytes (4 for AX Edge, 3 for RD2000). Last match group ís empty for RD2000 -> check for not empty results
            ModelIdBytes = modelIdByteStrings.Cast<Group>().Skip(1).Where(o => o.Value != string.Empty).Select(o => o.Value.HexStringToByte()).ToArray();

            Match match = GeneratedRegex.ModelIdBytesRegex().Match(midiDocumentationFileContent);

            if (!match.Success)
            {
                throw new NotSupportedException("The MIDI PDF does not contain a 'Parameter Address Map' section.");
            }

            int startIndex = match.Index;
            midiDocumentationFileContent = midiDocumentationFileContent[startIndex..];

            // Find relevant pages with tables
            // first needed info is the model id which is followed by the first upper table border
            Match matchStart = Regex.Match(midiDocumentationFileContent, @".*\n\+------------------------------------------------------------------------------\+");
            startIndex = matchStart.Index;

            Match matchEnd = Regex.Match(midiDocumentationFileContent, "Total Size.*", RegexOptions.RightToLeft);
            int endIndex = matchEnd.Index;
            midiDocumentationFileContent = midiDocumentationFileContent.Substring(startIndex, endIndex - startIndex + matchEnd.Value.Length);

            string[] rawTableParts = GeneratedRegex.MidiTableNameRegex().Split(midiDocumentationFileContent); // table header always starts with the name of the table prefixed by "* "

            for (int i = 0; i < rawTableParts.Length; i++)
            {
                // Get the name of the table. Use the device name for root table as for some devices (e.g. RD2000) no header for first table is provided.
                string name;
                
                if (i == 0)
                {
                    name = deviceName;
                }
                else
                {
                    name = rawTableParts[i].Trim();
                    i++;
                }

                // split single Rows
                List<string> dataRows = rawTableParts[i].Split("\n").ToList();

                // keep only relevant rows that contain whether start addresses or value descriptions:
                dataRows.RemoveAll(x => !GeneratedRegex.MiditableContentRow().IsMatch(x));

                // PDF parser randomly throws page number at end of line -> remove
                dataRows = dataRows.Select(x =>
                {
                    return x[..(x.LastIndexOf('|') + 1)];
                }).ToList();

                MidiTable midiTable = new MidiTable(deviceName, name, dataRows);
                MidiTables.Add(midiTable);
            }

            MidiTables.RemoveTablesWithMissingSubTables();
        }

        internal static byte[] CalculateSysex(byte[] modelIdBytes, byte deviceId, MidiTableBranchEntry firstBranch, MidiTableBranchEntry secondBranch, MidiTableLeafEntry leafEntry, int value)
        {
            List<byte> sysexData = new List<byte>()
            {
                sysexStart,
                rolandId,
                deviceId,
            };

            sysexData.AddRange(modelIdBytes);
            sysexData.Add(commandId);

            byte[] accumulatedStartAddress = AccumulateStartAddreses(firstBranch, secondBranch, leafEntry);
            sysexData.AddRange(accumulatedStartAddress);

            List<byte> valueBytes = CalculateValueBytes(leafEntry, value);
            sysexData.AddRange(valueBytes);

            byte checkSum = CalculateCheckSum(accumulatedStartAddress, value);
            sysexData.Add(checkSum);

            sysexData.Add(sysexEnd);

            return sysexData.ToArray();
        }

        private static byte[] AccumulateStartAddreses(MidiTableBranchEntry firstBranch, MidiTableBranchEntry secondBranch, MidiTableLeafEntry leafEntry)
        {
            byte[] accumulatedAddress = firstBranch.StartAddress.BytesCopy();
            accumulatedAddress.Add(secondBranch.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
            accumulatedAddress.Add(leafEntry.StartAddress.Bytes, StartAddress.MaxAddressByteCount);
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
}
