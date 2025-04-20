namespace RoMi.Models;

public class MidiValue(int value, string? description, string? category, string? unit)
{
    /// <summary>The actual MIDI value.</summary>
    public int Value { get; set; } = value;
    /// <summary>The description for the MIDI value.</summary>
    public string? Description { get; set; } = description;
    /// <summary>The category of the MIDI value (only available if separate description tables exist).</summary>
    public string? Category { get; set; } = category;
    /// <summary>The unit for the desciption value.</summary>
    public string? Unit { get; set; } = unit;
}
