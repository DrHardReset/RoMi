namespace RoMi.Models;
public static class SupportedDevices
{
    public static ImmutableDictionary<string, string> Devices = ImmutableDictionary.CreateRange(
        new KeyValuePair<string, string>[] {
            KeyValuePair.Create("AX-Edge", "https://static.roland.com/assets/media/pdf/AX-Edge_MIDI_Imple_eng02_W.pdf"),
            KeyValuePair.Create("FANTOM-06/07/08", "https://static.roland.com/assets/media/pdf/FANTOM-06_07_08_MIDI_Imple_eng01_W.pdf"),
            KeyValuePair.Create("FANTOM-6/7/8", "https://static.roland.com/assets/media/pdf/FANTOM-6_7_8_MIDI_Imple_eng01_W.pdf"),
            KeyValuePair.Create("INTEGRA-7", "https://static.roland.com/assets/media/pdf/INTEGRA-7_MI.pdf"),
            KeyValuePair.Create("JUNO-X", "https://static.roland.com/assets/media/pdf/JUNO-X_MIDI_imple_eng01_W.pdf"),
            KeyValuePair.Create("JUPITER-X/Xm", "https://static.roland.com/assets/media/pdf/JUPITER-X_Xm_MIDI_imple_eng06_W.pdf"),
            KeyValuePair.Create("RD-2000", "https://static.roland.com/assets/media/pdf/RD-2000_MIDI_Imple_eng02_W.pdf"),
            KeyValuePair.Create("RD-88", "https://static.roland.com/assets/media/pdf/RD-88_MIDI_Imple_eng03_W.pdf"),
            KeyValuePair.Create("GT-1000", "https://static.roland.com/assets/media/pdf/GT-1000_GT-1000CORE_MIDI_Imple_eng10_W.pdf"),
    });
}
