using System.Text.RegularExpressions;

namespace RoMi.Business.Models;

internal partial class GeneratedRegex
{
    [GeneratedRegex(@"\d+\. (Parameter Address Map|System Exclusive Address Map)")] // parameter address caption
    internal static partial Regex ParameterAddressMapCaption();

    /// <summary>
    /// Regex pattern for extraction of model id bytes.
    /// "H" is missing after third byte in AX Edge documentation.
    /// </summary>
    [GeneratedRegex(@"\(Model\s?ID [=\:] ([0-9a-fA-F]{2})H?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)\)")]
    internal static partial Regex ModelIdBytesRegex();

    /// <summary>
    /// Matches a table header row. Example:
    /// +------------------------------------------------------------------------------+
    /// </summary>
    [GeneratedRegex(@"\+-{78}\+")]
    internal static partial Regex MidiMapStartMarkerRegex();

    /// <summary>
    /// Matches a table header row that is not preceded by a newline. Example:
    /// +------------------------------------------------------------------------------+
    /// </summary>
    [GeneratedRegex(@"(?<!\n)(\+-{78}\+)")]
    internal static partial Regex MidiMapStartMarkerFixRegex();

    /// <summary>
    /// Matches a table footer row. Example:
    /// +------------------------------------------------------------------------------+
    /// </summary>
    [GeneratedRegex(@"\+-{78}\+", RegexOptions.RightToLeft)]
    internal static partial Regex MidiMapEndMarkerRegex();

    /// <summary>
    /// Matches a table name row. Examples:
    /// AX-Edge:
    /// * [Program]
    ///
    /// RD-2000:
    /// * Program
    /// </summary>
    ///
    /// GT-1000 value description table:
    /// *1 KNOB SETTING TABLE
    [GeneratedRegex(@"(?=^\*\s\[?.*?\]?\n|^\*\d.*\n)", RegexOptions.Multiline)]
    internal static partial Regex MidiTableNameRegex();

    /// <summary>
    /// Extracts a table name. Examples:
    /// AX-Edge:
    /// * [Program]
    ///
    /// RD-2000:
    /// * Program
    /// </summary>
    ///
    /// GT-1000 value description table:
    /// *1 KNOB SETTING TABLE
    [GeneratedRegex(@"\* \[?(.*?)\]?\n|(\*\d [\w\s]+)\n")]
    internal static partial Regex MidiTableNameExtractRegex();

    /// <summary>
    /// Matches rows that contain "content of interest" for parsing the MIDI sysex infos.
    /// The first collumn of a "content" row must contain an address. Examples:
    /// <![CDATA[
    /// | 00 00 00 | Program Common [Program Common] |
    /// | : | |
    /// |# 00 0E | 0000 aaaa | |
    /// : : : :
    /// ]]>
    /// </summary>
    [GeneratedRegex(@"^[\|:]#?\s*[0-9a-fA-F:\s]*?[\|:].*[\|:]", RegexOptions.Multiline)]
    internal static partial Regex MiditableContentRow();

    /// <summary>
    /// Matches rows that contain a leaf entry description. Examples:
    /// | 00 1A | 0000 bbbb | Mic Noise Supressor Threshold (-32 - 64) |
    /// | 00 03 | 0000 dddd | Master Tune (24 - 2024) |
    /// | 00 00 | 00aa aaaa | Manual Num1 Function (DOWN on GT-1000CORE)(0 - 59) |
    /// </summary>
    [GeneratedRegex(@"(.*)[ )]\((-?\d+) -\s?(\d+)\)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    internal static partial Regex MidiTableLeafEntryDescriptionRegex();

    /// <summary>
    /// Matches the relevant column and parts of a value description row. Examples:
    /// <![CDATA[
    /// | | | -3 - 3 |
    /// | | | -24 - +24 [dB] |
    /// | | | -24.0 - +24.0 [dB] |
    /// | | | L64 - 63R |
    /// ]]>
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
    /// <![CDATA[
    /// | 06 49 | 0aaa aaaa | <Reserved> |
    /// | 00 20 | 0000 aaaa | (Reserved) |
    /// | 00 07 | 0aaa aaaa | (reserve) <*> |
    /// | 00 02 | 0aaa aaaa | reserved(0 - 127) |
    /// | 00 1B | 0aaa aaaa | Reserved |
    /// ]]>
    /// </summary>
    [GeneratedRegex(@"(^[<\(]?[Rr]eserve|N/A\(fixed value\))")]
    internal static partial Regex MidiTableLeafEntryReservedValueDescriptionRegex();

    /// <summary>
    /// Matches rows that contain fill up markers. Examples:
    /// <![CDATA[
    /// | : | | |
    /// : : : :
    /// ]]>
    /// </summary>
    [GeneratedRegex(@"^[\|:]\s+:")]
    internal static partial Regex MidiTableLeafEntryFillUpRowRegex();

    /// <summary>
    /// Matches the number of a description that needs to be filled up. Examples:
    /// | 00 00 | 0aaa aaaa | volume 1 (14 - 64) |
    /// -> the 1 of 'volume 1'
    /// The first digit found in the description is taken.
    /// Another example:
    /// |       00 03 | 0000 dddd | Program Change#1                         (0 - 499) |
    /// </summary>
    [GeneratedRegex(@"[a-zA-z\s\[(#](\d+).*?$")]
    internal static partial Regex MidiTableEntryFillUpDescriptionNumberRegex();
}
