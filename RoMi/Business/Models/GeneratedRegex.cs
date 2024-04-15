using System.Text.RegularExpressions;

namespace RoMi.Business.Models
{
    internal partial class GeneratedRegex
    {
        [GeneratedRegex(@"\d+\. (Parameter Address Map|System Exclusive Address Map)")] // parameter address caption
        internal static partial Regex ParameterAddressMapCaption();

        [GeneratedRegex(@"\(Model\s?ID [=\:] ([0-9a-fA-F]{2})H?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?\)")] // "H" is missing after third byte in AX Edge documentation.
        internal static partial Regex ModelIdBytesRegex();

        /// <summary>
        /// Matches a table including it's table name row. First group is the name, second group is the table. Examples:
        /// AX-Edge (ModelID = 00H 00H 00 52H)
        /// +------------------------------------------------------------------------------+
        ///
        /// AX-Edge:
        /// * [Program]
        /// +------------------------------------------------------------------------------+
        ///
        /// RD-2000:
        /// * Program
        /// +-------------+----------------------------------------------------------------|
        /// </summary>
        [GeneratedRegex(@"[\*\s\[]*(.*?)\]?\n(\+[-\+]{78}[\+\|][\s\S]*?\+-{78}\+)", RegexOptions.Multiline)]
        internal static partial Regex MidiTableNameAndRowsRegex();

        /// <summary>
        /// Matches rows that contain "content of interest" for parsing the MIDI sysex infos.
        /// The first collumn of a "content" row must contain an address. Examples:
        /// | 00 00 00 | Program Common [Program Common] |
        /// | : | |
        /// |# 00 0E | 0000 aaaa | |
        /// : : : :
        /// </summary>
        [GeneratedRegex(@"^[\|:]\s*#?\s*[0-9a-fA-F:\s]*[\|:]")]
        internal static partial Regex MiditableContentRow();

        /// <summary>
        /// Matches rows that contain a leaf entry description. Examples:
        /// | 00 1A | 0000 bbbb | Mic Noise Supressor Threshold (-32 - 64) |
        /// | 00 03 | 0000 dddd | Master Tune (24 - 2024) |
        /// </summary>
        [GeneratedRegex(@"(.*) \((-?\d+) -\s?(\d+)\)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex MidiTableLeafEntryDescriptionRegex();

        /// <summary>
        /// Matches the relevant column and parts of a value description row. Examples:
        /// | | | -3 - 3 |
        /// | | | -24 - +24 [dB] |
        /// | | | -24.0 - +24.0 [dB] |
        /// | | | L64 - 63R |
        /// </summary>
        [GeneratedRegex(@"(L?[-0-9\.]*) - (R?[\+0-9\.]*)\s?(\[.*\])?")]
        internal static partial Regex MidiTableLeafEntryDescriptionRowPartsRegex();

        /// <summary>
        /// Matches the unit of a value row. Example:
        /// 3150,4000,5000,6300,8000,10000,12500,16000 [Hz]
        /// </summary>
        [GeneratedRegex(@"(\[.*\])$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex MidiTableLeafEntryValueUnitRegex();

        /// <summary>
        /// Matches rows that contain reserved entry addresses. Examples:
        /// | 06 49 | 0aaa aaaa | <Reserved> |
        /// | 00 20 | 0000 aaaa | (Reserved) |
        /// | 00 07 | 0aaa aaaa | (reserve) <*> |
        /// | 00 02 | 0aaa aaaa | reserved(0 - 127) |
        /// | 00 1B | 0aaa aaaa | Reserved |
        /// </summary>
        [GeneratedRegex(@"^[<\(]?[Rr]eserve")]
        internal static partial Regex MidiTableLeafEntryReservedValueDescriptionRegex();

        /// <summary>
        /// Matches rows that contain fill up markers. Examples:
        /// | : | | |
        /// : : : :
        /// </summary>
        [GeneratedRegex(@"^[\|:]\s+:")]
        internal static partial Regex MidiTableLeafEntryFillUpRowRegex();

        /// <summary>
        /// Matches the number of a description that needs to be filled up. Examples:
        /// | 00 00 | 0aaa aaaa | volume 1 (14 - 64) |
        /// -> the 1 of 'volume 1'
        /// The first digit found in the description is taken.
        /// </summary>
        [GeneratedRegex(@"[a-zA-z\s\[(](\d+).*?$")]
        internal static partial Regex MidiTableEntryFillUpDescriptionNumberRegex();
    }
}
