namespace RoMi.Models;

public class MidiTableNavigator
{
    private readonly MidiTables midiTables;

    public MidiTableNavigator(MidiTables midiTables)
    {
        this.midiTables = midiTables;
    }

    public MidiTable GetRootTable()
    {
        return midiTables.FirstOrDefault(t => t.MidiTableType == MidiTableType.RootTable)
            ?? throw new InvalidOperationException("Root table not found.");
    }

    /// <summary>
    /// Returns the child table of the selected index of the parent table.
    /// </summary>
    /// <param name="parentTable">The parent table to search the child table in.</param>
    /// <param name="selectedIndex">The child table's index in parent table.</param>
    /// <returns>The child table that is referenced at <paramref name="selectedIndex"/> in <paramref name="parentTable"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the index is not found in parent table.</exception>
    /// <exception cref="InvalidOperationException">If the </exception>
    public MidiTable GetChildTable(MidiTable parentTable, int selectedIndex)
    {
        if (parentTable == null || selectedIndex < 0 || selectedIndex >= parentTable.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedIndex), "Invalid index for parent table.");
        }

        var branchEntry = parentTable[selectedIndex] as MidiTableBranchEntry;

        if (branchEntry == null)
        {
            throw new InvalidOperationException("Selected entry is not a branch entry.");
        }

        int targetTableIndex = midiTables.GetTableIndexByName(branchEntry.LeafName);
        return midiTables[targetTableIndex];
    }

    public MidiTable GetLeafTable(MidiTable parentTable, int selectedIndex)
    {
        var childTable = GetChildTable(parentTable, selectedIndex);

        if (childTable.MidiTableType != MidiTableType.LeafTable)
        {
            throw new InvalidOperationException("Child table is not a leaf table.");
        }

        return childTable;
    }

    public bool IsLeafTable(MidiTable table)
    {
        return table.MidiTableType == MidiTableType.LeafTable;
    }

    public bool IsBranchTable(MidiTable table)
    {
        return table.MidiTableType == MidiTableType.BranchTable;
    }
}
