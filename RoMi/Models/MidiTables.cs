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
    /// This method now recursively checks all levels of child tables.
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
                    this.RemoveAt(tableIndex);
                    return true;
                }

                // First "Analog Synth Tone" table is not needed as it is just a link to the second one.
                if (parentTable.Name == "Analog Synth Tone" && parentTable.Count == 1)
                {
                    this.RemoveAt(tableIndex);
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
                        childTable.LeafName = "Analog Synth Tone";
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
            },
            ["JD-XA"] = (parentTable, childTable, tableIndex, entryIndex) => {
                /*
                 * JD-XA root table has hard to follow references for Temporary tones.
                 * Fix the child reference tables and the startaddresses
                 */
                if (parentTable.Name == "Temporary Tone")
                {
                    this.RemoveAt(tableIndex);
                    return true;
                }

                switch (childTable.Description)
                {
                    case "Temporary Tone (Analog Part 1)":
                        childTable.LeafName = "Analog Synth Tone";
                        childTable.StartAddress.Increment([00, 0x02, 00, 00]);
                        break;
                    case "Temporary Tone (Analog Part 2)":
                        childTable.LeafName = "Analog Synth Tone";
                        childTable.StartAddress.Increment([00, 0x02, 00, 00]);
                        break;
                    case "Temporary Tone (Analog Part 3)":
                        childTable.LeafName = "Analog Synth Tone";
                        childTable.StartAddress.Increment([00, 0x02, 00, 00]);
                        break;
                    case "Temporary Tone (Analog Part 4)":
                        childTable.LeafName = "Analog Synth Tone";
                        childTable.StartAddress.Increment([00, 0x02, 00, 00]);
                        break;

                    case "Temporary Tone (Digital Part 1)":
                        childTable.LeafName = "SuperNATURAL Synth Tone";
                        childTable.StartAddress.Increment([00, 0x01, 00, 00]);
                        break;
                    case "Temporary Tone (Digital Part 2)":
                        childTable.LeafName = "SuperNATURAL Synth Tone";
                        childTable.StartAddress.Increment([00, 0x01, 00, 00]);
                        break;
                    case "Temporary Tone (Digital Part 3)":
                        childTable.LeafName = "SuperNATURAL Synth Tone";
                        childTable.StartAddress.Increment([00, 0x01, 00, 00]);
                        break;
                    case "Temporary Tone (Digital Part 4)":
                        childTable.LeafName = "SuperNATURAL Synth Tone";
                        childTable.StartAddress.Increment([00, 0x01, 00, 00]);
                        break;
                }
                return false;
            }
        };

        // HashSet to track which tables have already been processed to prevent infinite loops
        HashSet<string> processedTables = new();

        // Start recursive checking with ALL tables (not just root and branch)
        for (int midiTableIndex = 0; midiTableIndex < Count; midiTableIndex++)
        {
            MidiTable table = this[midiTableIndex];
            // Check all table types, not just root and branch
            CheckTableRecursively(table, deviceName, deviceActions, processedTables);
        }
    }

    /// <summary>
    /// Recursively checks a table and all its child tables for missing references.
    /// </summary>
    private void CheckTableRecursively(MidiTable parentTable, string deviceName,
        Dictionary<string, Func<MidiTable, MidiTableBranchEntry, int, int, bool>> deviceActions,
        HashSet<string> processedTables)
    {
        // Prevent infinite loops by tracking processed tables
        if (processedTables.Contains(parentTable.Name))
        {
            return;
        }

        processedTables.Add(parentTable.Name);

        int parentTableIndex = FindIndex(t => t.Name == parentTable.Name);

        if (parentTableIndex < 0)
        {
            return;
        }

        // Only process tables that can have child references (Root and Branch tables)
        // Leaf tables don't have child table references, only data entries
        if (parentTable.MidiTableType != MidiTableType.RootTable && 
            parentTable.MidiTableType != MidiTableType.BranchTable)
        {
            return;
        }

        for (int midiTableEntryIndex = 0; midiTableEntryIndex < parentTable.Count; midiTableEntryIndex++)
        {
            if (!(parentTable[midiTableEntryIndex] is MidiTableBranchEntry childTable))
            {
                break;
            }

            // Apply device-specific processing first
            if (deviceActions.TryGetValue(deviceName, out var action))
            {
                bool handled = action(parentTable, childTable, parentTableIndex, midiTableEntryIndex);

                if (handled || parentTableIndex >= Count || midiTableEntryIndex >= parentTable.Count)
                {
                    break;
                }
            }

            string leafName = childTable.LeafName;

            // Skip entries with empty LeafName (e.g., reserved entries)
            if (string.IsNullOrEmpty(leafName))
            {
                continue;
            }

            try
            {
                int childTableIndex = GetTableIndexByName(leafName);
                MidiTable foundChildTable = this[childTableIndex];

                // Recursively check ALL found child tables (regardless of type)
                // This ensures we check the entire hierarchy
                CheckTableRecursively(foundChildTable, deviceName, deviceActions, processedTables);
            }
            catch (KeyNotFoundException)
            {
                string errorMessage = $"Table '{parentTable.Name}' references child table '{leafName}' which does not exist in the documentation.";

                /*
                 * PDF for INTEGRA-7 root table contains multiple entries that reference child tables whose names should start with "Temporary". The actual child table's names do not have this prefix. Example: root entry 'Temporary Studio Set' must reference "Studio Set"
                 * If that is the case rename the child table on first check.
                 */
                if (leafName.StartsWith("Temporary"))
                {
                    try
                    {
                        int temporaryEntryNameIndex = GetTableIndexByName(leafName.Replace("Temporary ", ""));
                        this[temporaryEntryNameIndex].Name = leafName;

                        // After renaming, recursively check this table (regardless of type)
                        MidiTable renamedTable = this[temporaryEntryNameIndex];
                        CheckTableRecursively(renamedTable, deviceName, deviceActions, processedTables);
                    }
                    catch(KeyNotFoundException)
                    {
                        throw new KeyNotFoundException(errorMessage);
                    }
                }
                else if (GeneratedRegex.MidiTableLeafEntryReservedValueDescriptionRegex().IsMatch(leafName))
                {
                    continue; // Skip reserved entries, they are not relevant for the table structure
                }
                else
                {
                    throw new KeyNotFoundException(errorMessage);
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
