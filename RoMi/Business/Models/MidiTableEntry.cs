using RoMi.Business.Converters;

namespace RoMi.Business.Models
{
    /// <summary>
    /// Base class for all table entries.
    /// </summary>
    public class MidiTableEntry
    {
        public StartAddress StartAddress { get; set; }
        public string Description { get; set; }

        public MidiTableEntry(string startAddress, string description)
        {
            StartAddress = new StartAddress(startAddress);
            Description = description;
        }

        public MidiTableEntry(StartAddress startAddress, string description)
        {
            StartAddress = startAddress;
            Description = description;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Description))
            {
                return StartAddress.Bytes.ByteArrayToHexString() + " " + Description;
            }

            return string.Empty;
        }
    }
}
