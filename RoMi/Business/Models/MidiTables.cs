namespace RoMi.Business.Models
{
    public class MidiTables : List<MidiTable>
    {
        public MidiTable GetTableByName(string name)
        {
            MidiTable? childTable = this.FirstOrDefault(x => x.Name == name);

            if (childTable == null)
            {
                throw new KeyNotFoundException("Table '" + name + "' could not be found in table list.");
            }

            return childTable;
        }

        public int GetTableIndexByName(string name)
        {
            /*
             * Use StartsWith as child table's name sometimes contains description as Postfix, eg:
             * Branch: [Tone PMT]
             * Leaf: [Tone PMT(Partial Mix Table)]
             */
            int index = FindIndex(x => x.Name.StartsWith(name));

            if (index < 0)
            {
                throw new Exception("Table index '" + name + "' could not be found in table list.");
            }

            return index;
        }

        /// <summary>
        /// Removes tables which's child tables do not exist. Example: Table "Editor" references table [Edit] which does not exist in AX-Edge documentation.
        /// </summary>
        public void RemoveTablesWithMissingSubTables()
        {
            for (int i = this[0].Count - 1; i > 0; i--)
            {
                try
                {
                    GetTableByName(((MidiTableBranchEntry)this[0][i]).LeafName);
                }
                catch (KeyNotFoundException)
                {
                    this[0].RemoveAt(i);
                }
            }
        }

        public override string ToString()
        {
            return string.Join("\n", this.Select(x => x.ToString()));
        }
    }
}
