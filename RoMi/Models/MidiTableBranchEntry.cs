namespace RoMi.Models;

/// <summary>
/// Entry of a "Branch" or "Root" table. It contains a 3 or 4 byte start address. The description cell contains the linkname of the next child table which may be a "Branch" or "Child" table.
/// </summary>
public class MidiTableBranchEntry : MidiTableEntry
{
    public string LeafName { get; set; } = string.Empty;

    public MidiTableBranchEntry() : base ("00 00 00", string.Empty) { }

    public MidiTableBranchEntry(string startAddress, string description) : base(startAddress, description)
    {
        // Check if this is a reserved entry first
        if (GeneratedRegex.MidiTableLeafEntryReservedValueDescriptionRegex().IsMatch(description))
        {
            // For reserved entries, don't set a leaf name to avoid trying to link to non-existent tables
            Description = description;
            LeafName = string.Empty;
            return;
        }

        if (description.Contains('['))
        {
            // AX-Edge branch tables contain references to child tables in square brackets
            Description = description[..description.IndexOf('[')].Trim();
            LeafName = description.Substring(description.IndexOf('[') + 1, description.IndexOf(']') - description.IndexOf('[') - 1).Trim();
        }
        else
        {
            /*
             * RD2000 branch tables do not contain references to child tables. Take the description name and cut of brackets.
             * example: "Program (Temporary)" will link to "Program".
             */
            Description = description;
            int leafNameEndLength = Description.IndexOf(" (");

            if (leafNameEndLength == -1)
            {
                leafNameEndLength = Description.Length;
            }

            LeafName = Description.Substring(0, leafNameEndLength);
        }
    }

    public MidiTableBranchEntry(StartAddress startAddress, string leafName, string description) : base(startAddress, description)
    {
        LeafName = leafName;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(LeafName))
        {
            return base.ToString() + " " + LeafName;
        }

        return string.Empty;
    }
}
