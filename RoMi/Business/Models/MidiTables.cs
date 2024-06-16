using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RoMi.Business.Models;

public class MidiTables : List<MidiTable>
{
    public int GetTableIndexByName(string name)
    {
        /*
         * Child table's name sometimes contains description as Postfix, eg:
         * Branch: [Tone PMT]
         * Leaf: [Tone PMT(Partial Mix Table)]
         *
         * Some child table's names are coma separated and valid for multiple parents:
         * Branch 1: Program Modulation FX (Zone 1)
         * Branch 2: Program Tremolo/Amp Simulator (Zone 1)
         * Child: Program Modulation FX, Program Tremolo/Amp Simulator
         *
         *  [Partial Pitch Env] / [Inst Pitch Env]
         */
        int index = FindIndex(x => Regex.IsMatch(x.Name, @$"^{name}\]?|, {name}|/ \[{name}\]?"));

        if (index < 0)
        {
            throw new KeyNotFoundException("Table index for '" + name + "' could not be found in table list.");
        }

        return index;
    }

    /// <summary>
    /// Removes tables which's child tables do not exist. Example: AX-Edge table "Editor" references table [Edit] which does not exist in documentation.
    /// </summary>
    public void FixTablesWithMissingSubTables(string deviceName)
    {
        for (int midiTableIndex = 0; midiTableIndex < Count; midiTableIndex++)
        {
            for (int midiTableEntryIndex = 0; midiTableEntryIndex < this[midiTableIndex].Count; midiTableEntryIndex++)
            {
                if (this[midiTableIndex][midiTableEntryIndex] is MidiTableLeafEntry)
                {
                    break;
                }

                MidiTableBranchEntry midiTableBranchEntry = ((MidiTableBranchEntry)this[midiTableIndex][midiTableEntryIndex]);
                string leafName = midiTableBranchEntry.LeafName;

                try
                {
                    GetTableIndexByName(leafName);
                }
                catch (KeyNotFoundException)
                {
                    if (deviceName == "AX-Edge" && leafName == "Edit")
                    {
                        // Root table entry "Setup" references table 'Edit' which does not exist
                        this[midiTableIndex].RemoveAt(midiTableEntryIndex);
                        continue;
                    }

                    if (deviceName == "JUPITER-X/Xm" && leafName == "Editor")
                    {
                        // Root table entry "Editor" does not reference a table
                        this[midiTableIndex].RemoveAt(midiTableEntryIndex);
                        continue;
                    }

                    if ((deviceName == "FANTOM-06/07/08" || deviceName == "FANTOM-6/7/8") && leafName == "System Controller")
                    {
                        // Entry "Setup" of table "System Controller" references table 'System Controller' which is named "System Control" -> rename
                        midiTableBranchEntry.LeafName = "System Control";
                        GetTableIndexByName(midiTableBranchEntry.LeafName);
                    }

                    try
                    {
                        /*
                         * PDF for INTEGRA-7 root table contains multiple entries that reference child tables whose names should strat with "Temporary". The actual child table's names do not have this prefix. Example: root entry 'Temporary Studio Set' must reference "Studio Set"
                         * If that is the case rename the child table on first check.
                         */
                        if (leafName.StartsWith("Temporary"))
                        {
                            int temporaryEntryNameIndex = GetTableIndexByName(leafName.Replace("Temporary ", ""));
                            this[temporaryEntryNameIndex].Name = leafName;
                        }
                    } catch (KeyNotFoundException)
                    {
                        throw new Exception($"Table '{midiTableBranchEntry.Description}' references a table named '{leafName}' which could not be found in the table list.");
                    }
                }
            }
        }
    }

    public override string ToString()
    {
        return string.Join("\n", this.Select(x => x.ToString()));
    }
}
