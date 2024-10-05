namespace RoMi.Models;

public class MidiValue
{
    /// <summary>The actual MIDI value.</summary>
    public int Value { get; set; }
    /// <summary>The description for the MIDI value.</summary>
    public string? Description { get; set; }
    /// <summary>The category of the MIDI value (only available if separate description tables exist).</summary>
    public string? Category { get; set; } = null;
    /// <summary>The unit for the desciption value.</summary>
    public string? Unit { get; set; } = null;

    public MidiValue(int value, string? description, string? category, string? unit)
    {
        Value = value;
        Description = description;
        Category = category;
        Unit = unit;
    }
}
