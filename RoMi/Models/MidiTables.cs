using System.Text.RegularExpressions;

namespace RoMi.Models;

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
        /*
         * Single dictionary for all device-specific actions
         * If the Func returns true, the loop shall break and the next parent table shall be processed.
         */
        Dictionary<string, Func<MidiTable, MidiTableBranchEntry, int, int, bool>> deviceActions = new()
        {
            ["FANTOM-06/07/08"] = HandleFantomCase,
            ["FANTOM-6/7/8"] = HandleFantomCase,
            ["AX-Edge"] = (_, childTable, tableIndex, entryIndex) => {
                // Root table entry "Setup" references table 'Edit' which does not exist
                if (childTable.LeafName == "Edit")
                {
                    this[tableIndex].RemoveAt(entryIndex);
                    return true;
                }
                return false;
            },
            ["JUPITER-X/Xm"] = (_, childTable, tableIndex, entryIndex) => {
                // Root table entry "Editor" does not reference a table
                if (childTable.LeafName == "Editor")
                {
                    this[tableIndex].RemoveAt(entryIndex);
                    return true;
                }
                return false;
            },
            ["JD-Xi"] = (parentTable, childTable, tableIndex, entryIndex) => {
                /*
                 * JD-Xi root table has hard to follow references for Temporary tones.
                 * Fix the child reference tables and the startaddresses
                 */
                if (parentTable.Name == "Temporary Tone")
                {
                    this[tableIndex].RemoveAt(tableIndex);
                    return true;
                }

                switch (childTable.Description)
                {
                    case "Temporary Tone (Digital Synth Part 1)":
                        childTable.LeafName = "SuperNATURAL Synth Tone";
                        childTable.StartAddress.Increment([00, 0x01, 00, 00]);
                        break;
                    case "Temporary Tone (Digital Synth Part 2)":
                        childTable.LeafName = "SuperNATURAL Synth Tone";
                        childTable.StartAddress.Increment([00, 0x01, 00, 00]);
                        break;
                    case "Temporary Tone (Analog Synth Part)":
                        childTable.LeafName = "Analog Synth Part";
                        childTable.StartAddress.Increment([00, 0x02, 00, 00]);
                        break;
                    case "Temporary Tone (Drums Part)":
                        childTable.LeafName = "Drum Kit";
                        childTable.StartAddress.Increment([00, 0x10, 00, 00]);
                        break;
                    case "Analog Synth Tone" when childTable.LeafName == "Analog Synth Tone":
                        this[tableIndex].Name = "Analog Synth Part";
                        break;
                }
                return false;
            }
        };

        for (int midiTableIndex = 0; midiTableIndex < Count; midiTableIndex++)
        {
            MidiTable parentTable = this[midiTableIndex];

            for (int midiTableEntryIndex = 0; midiTableEntryIndex < parentTable.Count; midiTableEntryIndex++)
            {
                if (!(parentTable[midiTableEntryIndex] is MidiTableBranchEntry childTable))
                {
                    break;
                }

                // Apply device-specific processing first
                if (deviceActions.TryGetValue(deviceName, out var action))
                {
                    bool handled = action(parentTable, childTable, midiTableIndex, midiTableEntryIndex);

                    if (handled || midiTableIndex >= Count || midiTableEntryIndex >= parentTable.Count)
                    {
                        break;
                    }
                }

                string leafName = childTable.LeafName;

                try
                {
                    GetTableIndexByName(leafName);
                }
                catch (KeyNotFoundException)
                {
                    try
                    {
                        /*
                         * PDF for INTEGRA-7 root table contains multiple entries that reference child tables whose names should start with "Temporary". The actual child table's names do not have this prefix. Example: root entry 'Temporary Studio Set' must reference "Studio Set"
                         * If that is the case rename the child table on first check.
                         */
                        if (leafName.StartsWith("Temporary"))
                        {
                            int temporaryEntryNameIndex = GetTableIndexByName(leafName.Replace("Temporary ", ""));
                            this[temporaryEntryNameIndex].Name = leafName;
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new Exception($"Table '{childTable.Description}' references a table named '{leafName}' which could not be found in the table list.");
                    }
                }
            }
        }
    }

    private bool HandleFantomCase(MidiTable parentTable, MidiTableBranchEntry childTable, int tableIndex, int entryIndex)
    {
        // Entry "Setup" of table "System Controller" references table 'System Controller' which is named "System Control" -> rename
        if (childTable.LeafName == "System Controller")
        {
            childTable.LeafName = "System Control";
            GetTableIndexByName(childTable.LeafName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Some leaf table entries link to a separate value description table. This method links the entry with the description list.
    /// </summary>
    public void LinkValueDescriptionTables(Dictionary<string, MidiValueList> midiValueDictionary)
    {
        for (int midiTableIndex = 0; midiTableIndex < Count; midiTableIndex++)
        {
            for (int midiTableEntryIndex = 0; midiTableEntryIndex < this[midiTableIndex].Count; midiTableEntryIndex++)
            {
                if (this[midiTableIndex][midiTableEntryIndex] is not MidiTableLeafEntry)
                {
                    continue;
                }

                MidiTableLeafEntry midiTableLeafEntry = (MidiTableLeafEntry)this[midiTableIndex][midiTableEntryIndex];

                if (midiTableLeafEntry.MidiValueList.DescriptionTableRefName != null)
                {
                    List<string> keyList = midiValueDictionary.Keys.Where(x => x.StartsWith(midiTableLeafEntry.MidiValueList.DescriptionTableRefName)).ToList();

                    if (keyList.Count == 0)
                    {
                        throw new Exception($"Leaf table '{this[midiTableIndex].Name}' entry '{midiTableLeafEntry.Description}' references a value table '{midiTableLeafEntry.MidiValueList.DescriptionTableRefName}' which could not be found.");
                    }

                    if (keyList.Count > 1)
                    {
                        throw new Exception($"Leaf table '{this[midiTableIndex].Name}' entry '{midiTableLeafEntry.Description}' references a value table '{midiTableLeafEntry.MidiValueList.DescriptionTableRefName}' which could not clearly be referenced as multiple tables match the criteria:\n{string.Join('\n', keyList)}");
                    }

                    midiTableLeafEntry.MidiValueList = midiValueDictionary[keyList[0]]; ;
                }
            }
        }
    }

    public override string ToString()
    {
        return string.Join("\n", this.Select(x => x.ToString()));
    }
}
