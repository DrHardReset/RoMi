using System.Text.RegularExpressions;

namespace RoMi.Business.Models
{
    internal partial class GeneratedRegex
    {
        [GeneratedRegex(@"\d+\. (Parameter Address Map|System Exclusive Address Map)")] // parameter address caption
        internal static partial Regex ParameterAddressMapCaption();

        [GeneratedRegex(@"\(Model\s?ID [=\:] ([0-9a-fA-F]{2})H?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?\)")] // "H" is missing after third byte in AX Edge documentation.
        internal static partial Regex ModelIdBytesRegex();

        [GeneratedRegex(@"^\* \[?(.*?)\]?(?:[\r\n])", RegexOptions.Multiline)]
        internal static partial Regex MidiTableNameRegex();

        /// <summary>
        /// Matches rows that contain "content of interest" for parsing the MIDI sysex infos.
        /// The first collumn of a "content" row must contain an address. Examples:
        /// | 00 00 00 | Program Common [Program Common] |
        /// | : | |
        /// |# 00 0E | 0000 aaaa | |
        /// </summary>
        [GeneratedRegex(@"^\|\s*#?\s*[0-9a-fA-F:\s]*\|")]
        internal static partial Regex MiditableContentRow();

        /// <summary>
        /// Matches rows that contain a leaf entry description. Examples:
        /// | 00 1A | 0000 bbbb | Mic Noise Supressor Threshold (-32 - 64) |
        /// | 00 03 | 0000 dddd | Master Tune (24 - 2024) |
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex(@"(.*) \((-?\d+) -\s?(\d+)\)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex MidiTableLeafEntryDescriptionRegex();
    }
}
