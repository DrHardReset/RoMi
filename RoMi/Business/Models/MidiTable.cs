using System.Text.RegularExpressions;
using System.Data;
using RoMi.Business.Converters;

namespace RoMi.Business.Models;

public class MidiTable : List<MidiTableEntry>
{
    public MidiTableType MidiTableType { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Constructor for dummy entries in the viewmodel.
    /// </summary>
    public MidiTable() { }

    public MidiTable(MidiTableType midiTableType, string deviceName, string tableName, List<string> data)
    {
        Name = tableName;

        switch (midiTableType)
        {
            case MidiTableType.RootTable:
                ParseBranchTableRows(data);
                break;
            case MidiTableType.BranchTable:
                ParseBranchTableRows(data);
                break;
            default:
            case MidiTableType.LeafTable:
                ParseLeafTableRows(deviceName, data);
                break;
        }
    }

    public static MidiValueList ParseDescriptionTable(string tableName, List<string> tableRows)
    {
        MidiValueList midiValueList = new();

        for (int rowIter = 0; rowIter < tableRows.Count; rowIter++)
        {
            string currentRow = tableRows[rowIter];

            try
            {
                string[] currentRowParts = SplitDataRowParts(currentRow);
                string description;
                string? category = null;

                switch (currentRowParts.Length)
                {
                    case 2:
                        description = currentRowParts[1];
                        break;
                    case 3:
                        description = currentRowParts[2];
                        category = currentRowParts[1];
                        break;
                    default:
                        throw new Exception("MIDI value row does not consist of at 2 or 3 columns.");
                }

                if (!int.TryParse(currentRowParts[0], out int value))
                {
                    throw new Exception($"First column does not contain an integer: {currentRowParts[0]}");
                }

                MidiValue midiValue = new MidiValue(value, description, category);
                midiValueList.Add(midiValue);
            }
            catch (Exception ex)
            {
                throw new Exception($"Parsing of value table row '{currentRow}' failed.", ex);
            }
        }

        if (midiValueList.Count == 0)
        {
            throw new Exception($"Value description table '{tableName}' could not be parsed.");
        }

        return midiValueList;
    }

    private static string[] SplitDataRowParts(string dataRow)
    {
        try
        {
            string dataRowCleaned = dataRow;

            if (dataRow.StartsWith("|") && dataRow.EndsWith("|"))
            {
                // Remove leading and trailing "|"
                dataRowCleaned = dataRowCleaned[1..^1];
            }

            string[] rowParts = dataRowCleaned.Split("|").Select(x => x.Trim(' ')).ToArray();

            if (rowParts.Length < 2)
            {
                throw new NotSupportedException($"Splitted row should consist of at least 2 parts but conists of {rowParts.Length}");
            }

            return rowParts;
        }
        catch (Exception ex)
        {
            throw new Exception($"Table row could not be splitted: {dataRow}", ex);
        }
    }

    private void ParseBranchTableRows(List<string> data)
    {
        bool needEntryFillUp = false;

        foreach (string dataRow in data)
        {
            try
            {
                string[] rowParts = SplitDataRowParts(dataRow);

                if (rowParts.Length != 2)
                {
                    throw new Exception("The splitted table row must consist of 2 items but it consists of " + rowParts.Length + ".");
                }

                if (rowParts[0].Contains(':'))
                {
                    /*
                     * There are placeholders for repetitive rows. If we find them, we start creating rows until we find the next proper entry.
                     * 
                     * | 00 2D 00 | Partial Amp Env 2 [Partial Amp Env] |
                     * | : | |
                     * | 00 2F 00 | Partial Amp Env 4 [Partial Amp Env] |
                     * 
                     * | 20 02 00 00 | User Tone (002) [Tone] |
                     * | : | |
                     * | 23 7E 00 00 | User Tone (256) [Tone] |
                     */
                    needEntryFillUp = true;
                    continue;
                }

                //| 00 00 00 00 | System [System] |
                MidiTableBranchEntry newBranchEntry = new MidiTableBranchEntry(rowParts[0], rowParts[1]);

                if (needEntryFillUp)
                {
                    /*
                     * Calculate the fillup number for the description of the fillup entry.
                     * number can be in brackets or without
                     * | 00 10 00 | Program Tone (01)
                     * | 00 21 00 | Tone Partial 1
                     */

                    if (this.Count < 2)
                    {
                        throw new Exception($"The table should countain at least 2 entries, but it's count is {this.Count}");
                    }

                    if (this.Last().GetType() != typeof(MidiTableBranchEntry))
                    {
                        throw new Exception("The previous table entry should be of type " + nameof(MidiTableBranchEntry) + ".");
                    }

                    MidiTableBranchEntry? lastEntry = this[Count - 1] as MidiTableBranchEntry;
                    MidiTableBranchEntry? secondLastEntry = this[Count - 2] as MidiTableBranchEntry;


                    int highAddress = newBranchEntry.StartAddress.ToIntegerRepresentation();
                    byte[] addressOffset = StartAddress.CalculateOffset(secondLastEntry!.StartAddress, lastEntry!.StartAddress);
                    int lastFillUpAddress = highAddress - addressOffset.ToIntegerRepresentation(StartAddress.MaxAddressByteCount);

                    string newDescription = lastEntry.Description;
                    string lastEntryDescription = lastEntry.Description;
                    Match match = GeneratedRegex.MidiTableEntryFillUpDescriptionNumberRegex().Match(lastEntryDescription);

                    if (!match.Success)
                    {
                        throw new Exception("The previous table entry should contain a number which should be incremented for the fill up entry.");
                    }

                    string currentNumberString = match.Groups[^1].Value;
                    int currentNumber = Convert.ToInt32(currentNumberString);

                    while (this.Last().StartAddress.ToIntegerRepresentation() < lastFillUpAddress)
                    {
                        string replacement = (++currentNumber).ToString("D" + currentNumberString.Length);
                        newDescription = Regex.Replace(lastEntryDescription, currentNumberString, replacement);

                        StartAddress newStartAddress = new StartAddress(this.Last().StartAddress.BytesCopy());
                        newStartAddress.Increment(addressOffset);
                        MidiTableBranchEntry newFillUpBranchEntry = new MidiTableBranchEntry(newStartAddress, lastEntry.LeafName, newDescription);
                        Add(newFillUpBranchEntry);
                    }

                    needEntryFillUp = false;
                }

                Add(newBranchEntry);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while parsing branch row '" + dataRow + "'.", ex);
            }
        }
    }        

    private void ParseLeafTableRows(string deviceName, List<string> tableRows)
    {
        int ignoredTableRows = 0;

        for (int rowIter = 0; rowIter < tableRows.Count; rowIter++)
        {
            string currentRow = tableRows[rowIter];

            try
            {
                string[] currentRowParts = SplitDataRowParts(currentRow);

                if (IsTotalSizeRow(currentRowParts))
                {
                    int expectedEntries = new StartAddress(currentRowParts[0]).ToIntegerRepresentation();
                    expectedEntries -= ignoredTableRows;
                    expectedEntries -= GetAmountOfMissingTableRows(deviceName);

                    int totalDatabyteCount = this.Select(x => ((MidiTableLeafEntry)x).ValueDataByteBitMasks.Count).Sum();

                    if (expectedEntries != totalDatabyteCount)
                    {
                        throw new Exception("The number of parsed rows of table '" + Name + "' is not equal to the number in the documentation.\nExpected: " + expectedEntries + "\nActual: " + totalDatabyteCount);
                    }

                    continue;
                }
                else if (currentRowParts.Length != 3)
                {
                    throw new NotSupportedException("The splitted row should consist of 3 parts, but it contains " + currentRowParts.Length + ".");
                }

                if (currentRowParts[0] == string.Empty)
                {
                    if (deviceName == "GT-1000 / GT-1000CORE")
                    {
                        switch (Name)
                        {
                            case "SystemControl2":
                                // there are hint rows that must be skipped
                                continue;
                            case "PcmapPc":
                                // there are 2 separate value description rows for the two devices which are currently ignored
                                continue;
                            case "PatchEfct":
                                // there are hint rows that must be skipped
                                continue;
                        }
                    }

                    throw new Exception("First row part must not be empty.");
                }

                /*
                 * Example for regular entry:
                 * | 00 09 | 0000 000a | EQ Mode (0 - 1) |
                 *  | | PROGRAM, REMAIN |
                 *  
                 * Example for big value entry:
                 * |# 00 00 | 0000 aaaa | |
                 * | | 0000 bbbb | |
                 * | | 0000 cccc | |
                 * | | 0000 dddd | Master Tune (24 - 2024) |
                 * | | | -100.0 - 100.0 [cent] |
                 * Multiple data bytes need to be sent with one sysex message for these entries.
                 */

                string startAddress = currentRowParts[0];
                List<string> valueDataBitsPerByte = new List<string>() { currentRowParts[1] };

                if (currentRowParts[0].StartsWith("#"))
                {
                    // This is the first row of a "big value" entry -> data byte count needs to be determined
                    startAddress = currentRowParts[0][2..];

                    while (rowIter < tableRows.Count)
                    {
                        rowIter++;

                        if (rowIter >= tableRows.Count)
                        {
                            // reached end of table
                            break;
                        }

                        string nextRow = tableRows[rowIter];
                        string[] nextRowParts = SplitDataRowParts(nextRow);

                        valueDataBitsPerByte.Add(nextRowParts[1]);

                        // Last row of big value entry must contain the description in column 3.
                        if (IsLeafTableMultiByteCommandLastRow(nextRowParts))
                        {
                            currentRow = nextRow;
                            currentRowParts = nextRowParts;
                            break;
                        }
                    }
                }

                // Description column contains the general description, the value range and for some entries the unit. All of them need to be separated.
                string descriptionColumnRaw = currentRowParts[2].Trim();

                if (IsReservedValueDescriptionEntry(descriptionColumnRaw))
                {
                    ignoredTableRows += valueDataBitsPerByte.Count;

                    if (rowIter < tableRows.Count)
                    {
                        string followUpRow = tableRows[rowIter + 1];

                        if (IsFillUpRow(followUpRow))
                        {
                            /*
                             * Fill up reserved addresses. Example:
                             * | 00 1A | 0aaa aaaa | Reserved (0 - 127) |
                             * : : : :
                             * | 00 29 | 0aaa aaaa | Noise Level (0 - 127) |
                             */
                            rowIter++;
                            followUpRow = tableRows[rowIter + 1];
                            string[] nextFillUpRowPartsReserve = SplitDataRowParts(followUpRow);
                            string nextStartAddressReserve = nextFillUpRowPartsReserve[0];
                            StartAddress startAddressReservedFillUpEnd = new StartAddress(nextStartAddressReserve);
                            int reservedHighAddress = startAddressReservedFillUpEnd.ToIntegerRepresentation();
                            int reservedLowAddress = new StartAddress(startAddress).ToIntegerRepresentation();
                            int rows = reservedHighAddress - reservedLowAddress - 1; // -1 as the actual reserved row was already added to row counter.
                            ignoredTableRows += rows;
                        }
                    }

                    /*
                     * Some reserved fill up entries end with a useless value description row -> skip it. Example FA-06/07/08 [Studio Set Part]:
                     * | 00 1D | 0aaa aaaa | (reserve) <*> |
                     * | : | | |
                     * | 00 20 | 0aaa aaaa | (reserve) <*> |
                     * | | | 0 - 127 |
                     */
                    if (rowIter < tableRows.Count)
                    {
                        string[] nextRowParts = SplitDataRowParts(tableRows[rowIter + 1]);

                        if (IsLeafTableValueDescriptionRow(nextRowParts))
                        {
                            rowIter++;
                        }
                    }

                    continue;
                }


                Match match = GeneratedRegex.MidiTableLeafEntryDescriptionRegex().Match(descriptionColumnRaw);
                string description;
                List<int> values;

                if (match.Success && match.Groups.Count == 4)
                {
                    // regular row with low and high value
                    description = match.Groups[1].Value.Trim();
                    int valueLow = Convert.ToInt32(match.Groups[2].Value);
                    int valueHigh = Convert.ToInt32(match.Groups[3].Value);

                    if (valueHigh >= 2147483591 - 1)
                    {
                        // TODO: MidiTableLeafEntry.AssembleValueList needs to be redesigned as all value lists get completely initialized in RAM regardless if used later or not. Maybe just store min and max
                        // TODO: So far only "GT-1000 / GT-1000CORE" table "PatchLed" contains a value that is bigger than 2147483590.
                        valueHigh = 100;
                    }

                    values = MidiTableLeafEntry.AssembleValueList(valueLow, valueHigh);
                }
                else
                {
                    // treatment of extraordinary rows
                    if (deviceName == "RD-88" && Name == "Sympathetic Resonance" && descriptionColumnRaw == "Rev HF Damp")
                    {
                        // RD-88 table "Sympathetic Resonance" entry "Rev HF Damp" contains no value Range
                        description = descriptionColumnRaw;
                        values = MidiTableLeafEntry.AssembleValueList(0, 31);
                    }
                    else if (deviceName == "FA-06/07/08")
                    {
                        if (Name == "System Common" && descriptionColumnRaw == "(0 - 1)")
                        {
                            // FA-06/07/08 table "System Common" contains an entry without a Name
                            ignoredTableRows++;
                            rowIter++;
                            continue;
                        }

                        if (Name == "TFX" && descriptionColumnRaw == "(0 - 127)")
                        {
                            // FA-06/07/08 table "TFX" contains multiple entries without a Name
                            ignoredTableRows += 4;
                            rowIter += 7;
                            continue;
                        }

                        if (Name == "Studio Set Common" && descriptionColumnRaw == "(0 - 15)")
                        {
                            // FA-06/07/08 table "Studio Set Common" contains an entry without a Name
                            ignoredTableRows++;
                            rowIter++;
                            continue;
                        }

                        if (Name == "Studio Set Controller" && descriptionColumnRaw == "(0 - 15)")
                        {
                            // FA-06/07/08 table "Studio Set Controller" contains an entry without a Name
                            ignoredTableRows++;
                            rowIter++;
                            continue;
                        }

                        if (Name == "PCM Drum Kit Common" && startAddress == "00 0D")
                        {
                            // FA-06/07/08 table "PCM Drum Kit Common" contains multiple entries without a Name
                            ignoredTableRows += 4;
                            rowIter += 3;
                            continue;
                        }

                        if (Name == "PCM Drum Kit Partial" && startAddress == "01 42")
                        {
                            // FA-06/07/08 table "PCM Drum Kit Partial" contains wrong formatted reserved entry
                            ignoredTableRows++;
                            rowIter++;
                            continue;
                        }

                        throw new Exception("This leaf table row falls through the workarounds for table rows not containing a value range: " + descriptionColumnRaw);
                    }
                    else if (deviceName == "INTEGRA-7")
                    {
                        if (Name == "PCM Synth Tone Common 2" && descriptionColumnRaw == "(0 - 255)")
                        {
                            // INTEGRA-7 table "PCM Synth Tone Common 2" contains a multibyte entry without a Name
                            ignoredTableRows += 2;
                            continue;
                        }

                        if (Name == "PCM Drum Kit Common")
                        {
                            if (startAddress == "00 0D" || startAddress == "00 10")
                            {
                                // INTEGRA-7 table "PCM Drum Kit Common" contains entries without a Name
                                ignoredTableRows++;
                                continue;
                            }

                            if (startAddress == "00 0E")
                            {
                                // INTEGRA-7 table "PCM Drum Kit Common" contains a multibyte entry without a Name
                                ignoredTableRows += 2;
                                continue;
                            }
                        }

                        throw new Exception("This leaf table row falls through the workarounds for table rows not containing a value range: " + descriptionColumnRaw);
                    }
                    else
                    {
                        throw new Exception("Leaf table row does not contain a value range: " + descriptionColumnRaw);
                    }
                }

                // Check and parse separate value description rows if they exist:
                List<string> valueDescriptions = new List<string>();

                if (rowIter < tableRows.Count)
                {
                    rowIter++;
                    currentRow = tableRows[rowIter];

                    if (IsFillUpRow(currentRow))
                    {
                        /*
                         * The next row is a fill up row for reserved addresses.
                         * reset the iterator as fill up rows are parsed later.
                         */
                        rowIter--;
                    }
                    else
                    {
                        currentRowParts = SplitDataRowParts(currentRow);

                        if (!IsLeafTableValueDescriptionRow(currentRowParts))
                        {
                            /*
                             * There is no extra row with value descriptions
                             * reset the iterator for the next for-loop-iteration
                             */
                            rowIter--;
                        }
                        else
                        {
                            // There is at least one extra row with value description to be parsed
                            string valueDescriptionColumnRaw = currentRowParts[2];

                            if (valueDescriptionColumnRaw.Contains(','))
                            {
                                // | | | OFF, ON, TONE |
                                valueDescriptions = new List<string>();

                                while (rowIter < tableRows.Count)
                                {
                                    // iterate over to following rows as long as the descriptoin column contains comas.
                                    string[] descriptionParts = valueDescriptionColumnRaw.Trim(',').Split(',');
                                    valueDescriptions.AddRange(descriptionParts);
                                    rowIter++;

                                    if (rowIter >= tableRows.Count)
                                    {
                                        // reached end of table
                                        break;
                                    }

                                    currentRow = tableRows[rowIter];
                                    currentRowParts = SplitDataRowParts(currentRow);

                                    if (!IsLeafTableValueDescriptionRow(currentRowParts))
                                    {
                                        // We found the next normal row and must reset the iterator for the next for-loop-iteration
                                        rowIter--;
                                        break;
                                    }

                                    valueDescriptionColumnRaw = currentRowParts[2];
                                }

                                // Some description rows end with a unit
                                if (valueDescriptions.Count > 0 && GeneratedRegex.MidiTableLeafEntryValueUnitRegex().IsMatch(valueDescriptions.Last()))
                                {
                                    match = GeneratedRegex.MidiTableLeafEntryValueUnitRegex().Match(valueDescriptionColumnRaw);

                                    if (match.Success && match.Groups.Count == 2)
                                    {
                                        string unit = match.Groups[1].Value;
                                        string lastElement = valueDescriptions[^1]; // Keep last element as it already contains the unit
                                        valueDescriptions = valueDescriptions.Take(valueDescriptions.Count - 1).Select(x => x += " " + unit).ToList();
                                        valueDescriptions.Add(lastElement);
                                    }
                                }
                            }
                            else
                            {
                                // | | | -3 - 3 |
                                // | | | -24 - +24 [dB] |
                                // | | | -24.0 - +24.0 [dB] |
                                // | | | L64 - 63R |
                                match = GeneratedRegex.MidiTableLeafEntryDescriptionRowPartsRegex().Match(valueDescriptionColumnRaw);

                                if (match.Success && (match.Groups.Count == 3 || match.Groups.Count == 4) && match.Groups[2].Value != string.Empty)
                                {
                                    // Treat L(eft) and R(ight) letters as negative and positive numbers for more easy use
                                    string lowerMatch = match.Groups[1].Value;

                                    if (lowerMatch.StartsWith('L'))
                                    {
                                        lowerMatch = lowerMatch.Replace('L', '-');
                                    }

                                    string higherMatch = match.Groups[2].Value;

                                    if (higherMatch.StartsWith('R'))
                                    {
                                        higherMatch = higherMatch.Replace("R", "");
                                    }

                                    if (deviceName == "FA-06/07/08")
                                    {
                                        // Multiple entries contain string values that cannot be automatically interpreted  -> ignore, e.g. Velocity Range Lower/Upper
                                        if (higherMatch == "") // UPPER
                                        {
                                            higherMatch = "127";
                                        }

                                        if (lowerMatch == "") // LOWER
                                        {
                                            lowerMatch = "1";
                                        }
                                    }

                                    if (deviceName == "INTEGRA-7")
                                    {
                                        // Multiple entries contain string values that cannot be automatically interpreted  -> ignore, e.g. Velocity Range Lower/Upper
                                        if (higherMatch == "") // UPPER
                                        {
                                            higherMatch = "127";
                                        }

                                        if (lowerMatch == "") // LOWER
                                        {
                                            lowerMatch = "1";
                                        }
                                    }

                                    double valueDescriptionLow = Convert.ToDouble(lowerMatch);
                                    double valueDescriptionHigh = Convert.ToDouble(higherMatch);
                                    string unit = string.Empty;

                                    if (match.Groups.Count == 4)
                                    {
                                        unit = " " + match.Groups[3].Value;
                                    }

                                    valueDescriptions = MidiTableLeafEntry.AssembleDescriptionValues(valueDescriptionLow, valueDescriptionHigh, values.Count, unit);
                                }
                            }
                        }
                    }
                }

                if (valueDescriptions.Count == 0)
                {
                    // There is no extra row with value descriptions or description values could not be parsed -> take the plain values
                    valueDescriptions = values.Select(x => x.ToString()).ToList();
                }

                /*
                 * Sometimes there are mistakes in the documentation like missing comas. so that the number of value descriptions does not match the number of values.
                 * In this case we use the values as value descriptions.
                 */

                if (values.Count != valueDescriptions.Count)
                {
                    valueDescriptions = values.Select(x => x.ToString()).ToList();
                }

                if (deviceName == "AX-Edge" && Name == "Tone Synth Partial" && description == "Click Type")
                {
                    /*
                    * The table entry has a wrong bitmask:
                    * | 00 09 | 0000 000a | Click Type (0 - 3) |
                    * Correct it.
                    */
                    valueDataBitsPerByte = ["0000 00aa"];
                }

                MidiTableLeafEntry leafEntry = new MidiTableLeafEntry(startAddress, description, valueDataBitsPerByte, values, valueDescriptions);

                if (!this.Any(x => x.StartAddress.Equals(leafEntry.StartAddress)))
                {
                    // Documentation mistakenly contains duplicate entries, e.g.: [System Control] Startaddress 00 09 -> skip them
                    Add(leafEntry);
                }

                if (rowIter >= tableRows.Count)
                {
                    // reached end of table
                    break;
                }

                #region Entry fill up

                /* 
                 * At this point rows may occure that only contain ':'. In this case we need to fill up repetitive entries (example from RD2000 documentation)
                 * | 00 11 | 0aaa aaaa | Voice Reserve 2 (0 - 64) |
                 * | | | 0 - 63, FULL |
                 * | : | | |                                            <--- fill up from 11 to 1F
                 * | 00 1F | 0aaa aaaa | Voice Reserve 16 (0 - 64) |
                 * | | | 0 - 63, FULL |
                 */

                string nextFillUpRow = tableRows[++rowIter];

                if (!IsFillUpRow(nextFillUpRow))
                {
                    // No fill up needed -> Reset the iterator for the next for-loop-iteration
                    rowIter--;
                    continue;
                }

                nextFillUpRow = tableRows[++rowIter];

                if (IsFillUpRow(nextFillUpRow))
                {
                    // GT-1000 / GT-1000CORE uses double fill up rows in table "PcmapPc"
                    nextFillUpRow = tableRows[++rowIter];
                }

                string[] nextFillUpRowParts = SplitDataRowParts(nextFillUpRow);

                if (rowIter >= tableRows.Count)
                {
                    // reached end of table
                    throw new Exception($"A fill up row was found at the end of table {description}.");
                }

                string nextStartAddress = nextFillUpRowParts[0];

                if (nextFillUpRowParts[0].StartsWith("#"))
                {
                    // This is the first row of a "big value" entry -> data byte count needs to be determined
                    nextStartAddress = nextFillUpRowParts[0][2..];
                }

                if (string.IsNullOrWhiteSpace(nextStartAddress))
                {
                    throw new Exception("No valid startaddress for fill up entry could be parsed off row: " + nextFillUpRow);
                }

                // do the actual fill up:
                /*
                 * Most fill up entries contain an integer that should incremented from first to last entry:
                 * | 05 3E | 0000 aaaa | Individual Note Voicing Character 1 (59 - 69) |
                 * | 05 3F | 0000 aaaa | Individual Note Voicing Character 2 (59 - 69) |
                 * | : | | |
                 * | 06 3D | 0000 aaaa | Individual Note Voicing Character 128 (59 - 69) |
                 */
                byte[] addressOffset;

                if (this.Count == 0)
                {
                    throw new Exception($"There must be at least 1 leaf table row before the fill up row in table {Name} for entry '{leafEntry.Description}' to calculate the offset.");
                }
                else if (this.Count == 1)
                {
                    // Calculate the start address offset
                    addressOffset = Enumerable.Repeat(0, StartAddress.MaxAddressByteCount).Select(x => (byte)x).ToArray();
                    addressOffset[addressOffset.Length - 1] = (byte)leafEntry.ValueDataByteBitMasks.Count;
                }
                else
                {
                    addressOffset = StartAddress.CalculateOffset(this[Count - 2].StartAddress, leafEntry.StartAddress);
                }

                StartAddress startAddressFillUpEnd = new StartAddress(nextStartAddress);
                int highAddress = startAddressFillUpEnd.ToIntegerRepresentation();
                int lastFillUpAddress = highAddress - addressOffset.ToIntegerRepresentation(StartAddress.MaxAddressByteCount);

                match = GeneratedRegex.MidiTableEntryFillUpDescriptionNumberRegex().Match(leafEntry.Description);

                if (!match.Success)
                {
                    throw new Exception("The description of the previous table entry should contain a number which should be incremented for the fill up entry.");
                }

                string currentNumberString = match.Groups[^1].Value;
                int currentNumber = Convert.ToInt32(currentNumberString);

                while (this.Last().StartAddress.ToIntegerRepresentation() <= lastFillUpAddress)
                {
                    string replacement = (++currentNumber).ToString("D" + currentNumberString.Length);
                    string newDescription = Regex.Replace(leafEntry.Description, currentNumberString, replacement);

                    StartAddress newStartAddress = new StartAddress(this.Last().StartAddress.BytesCopy());
                    newStartAddress.Increment(addressOffset);
                    MidiTableLeafEntry newFillUpBranchEntry = new MidiTableLeafEntry(newStartAddress, newDescription, leafEntry.ValueDataByteBitMasks, leafEntry.Values, leafEntry.ValueDescriptions);
                    Add(newFillUpBranchEntry);
                }

                // RowIter now points to description row of fill up End entry -> Skip it.
                // If current entry is a multi byte entry, rowIter must skip rows with data byte definitions
                rowIter += leafEntry.ValueDataByteBitMasks.Count;

                #endregion Entry fill up
            }
            catch (Exception ex)
            {
                throw new Exception("Parsing leaf table row '" + currentRow + "' failed.", ex);
            }
        }
    }

    private bool IsTotalSizeRow(string[] rowParts)
    {
        if (rowParts.Length == 2 && rowParts[1] == "Total Size")
        {
            // Leaf tables end with a 'Total Size' row which is used here to verify the parsed data.
            // | 00 00 00 03 |Total Size |
            return true;
        }

        return false;
    }

    private bool IsLeafTableMultiByteCommandLastRow(string[] rowParts)
    {
        if (rowParts.Length == 3 && !string.IsNullOrWhiteSpace(rowParts[1]) && !string.IsNullOrWhiteSpace(rowParts[2]))
        {
            /*
             * |# 00 00 | 0000 aaaa | |
             * | 00 01 | 0000 bbbb | |
             * | 00 02 | 0000 cccc | |
             * | 00 03 | 0000 dddd | Master Tune (24 - 2024) | <--- this is last row with command
             */
                return true;
        }

        return false;
    }

    private bool IsLeafTableValueDescriptionRow(string[] rowParts)
    {
        if (rowParts.Length == 3 && rowParts[0] == string.Empty && rowParts[1] == string.Empty)
        {
            // Example for a leaf table description row:
            // | | | PROGRAM, REMAIN |
            return true;
        }

        return false;
    }

    private bool IsFillUpRow(string dataRow)
    {
        return GeneratedRegex.MidiTableLeafEntryFillUpRowRegex().IsMatch(dataRow);
    }

    private bool IsReservedValueDescriptionEntry(string valueDescription)
    {
        return GeneratedRegex.MidiTableLeafEntryReservedValueDescriptionRegex().IsMatch(valueDescription);
    }

    private int GetAmountOfMissingTableRows(string deviceName)
    {
        /*
         * In some tables there are missing rows - more or less undocumented.
         * In those cases the total size does not match the actual entry count.
         * The comparison of entry count is useful to determine parsing errors. -> manipulate the expected total size for the affected tables
         */

        if (deviceName == "RD-2000")
        {
            switch (Name)
            {
                case "System Common":
                    // Entry "00 0B" is missing
                    return 1;
                case "Program Common":
                    /*
                     * Missing entries:
                     * 00 2F - 00 34    = 6
                     * 00 62            = 1
                     * 00 7F - 01 02    = 4
                     * 01 1A - 01 1B    = 2
                     */
                    return 13;
                case "Program Modulation FX, Program Tremolo/Amp Simulator":
                    // Entries 00 05 and 00 06 are missing
                    return 2;
                case "Program Internal Zone":
                    /*
                     * Missing entries:
                     * 00 16            = 1
                     * 00 17            = 1
                     * 00 1D - 00 1F    = 3
                     * 06 52 - 06 5E    = 13
                     * 06 71 - 06 73    = 3
                     */
                    return 21;
                case "Program External Zone":
                    /*
                     * Missing entries:
                     * 00 0C - 00 0D     = 2
                     * 00 41 - 00 45     = 5
                     */
                    return 7;
            }
        }
        if (deviceName == "GT-1000 / GT-1000CORE" && Name == "PatchStompBox2")
        {
            return 3;
        }

        return 0;
    }

    public override string ToString()
    {
        return string.Join("\n", this.Select(x => x.ToString()));
    }
}
