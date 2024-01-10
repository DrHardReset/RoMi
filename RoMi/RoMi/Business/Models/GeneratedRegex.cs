using System.Text.RegularExpressions;

namespace RoMi.Business.Models
{
    internal partial class GeneratedRegex
    {
        [GeneratedRegex(@"\(Model\s?ID = ([0-9a-fA-F]{2})H?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?(?: ([0-9a-fA-F]{2})H?)?\)")] // "H" is missing after third byte in AX Edge documentation.
        internal static partial Regex ModelIdBytesRegex();

        [GeneratedRegex(@"[\[\]]")]
        internal static partial Regex MidiTableNameRegex();

        [GeneratedRegex(@"^\|\s*#?\s*[0-9\|:\s]")]
        internal static partial Regex MiditableContentRow();

        [GeneratedRegex(@"(.*) \((\d+) -\s?(\d+)\)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
        internal static partial Regex MidiTableLeafEntryDescriptionRegex();
    }
}
