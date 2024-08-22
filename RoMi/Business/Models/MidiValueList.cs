namespace RoMi.Business.Models;

/// <summary>
/// This class holds all the possible values for a MIDI leaf table entry. It may contain descriptions and/or categories for all values <see cref="MidiValue"/>.
/// </summary>
public class MidiValueList : List<MidiValue>
{
    private readonly double? descriptionMinValue = null;
    private readonly double? descriptionMaxValue = null;
    private readonly string? unit = null;
    public string? DescriptionTableRefName { get; set; } = null;

    public MidiValueList() { }

    public MidiValueList(List<int> values)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Value list must not be empty.", nameof(values));
        }

        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], null, null, null));
        }
    }

    public MidiValueList(List<int> values, List<string> descriptions, List<string> categories)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Value list must not be empty.", nameof(values));
        }

        if (descriptions.Count == 0)
        {
            throw new ArgumentException("Description list must not be empty.", nameof(descriptions));
        }

        if (values.Count != descriptions.Count)
        {
            throw new ArgumentException("Value, description and category lists must be the same length.");
        }

        if (descriptions.Count > 0 && values.Count != descriptions.Count)
        {
            throw new ArgumentException("Value and category lists must be the same length.");
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (categories.Count > 0)
            {
                Add(new MidiValue(values[i], descriptions[i], categories[i], null));
            }
            else
            {
                Add(new MidiValue(values[i], descriptions[i], null, null));
            }
        }
    }

    public MidiValueList(List<int> values, List<string> descriptions)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Value list must not be empty.", nameof(values));
        }

        if (descriptions.Count == 0)
        {
            throw new ArgumentException("Description list must not be empty.", nameof(descriptions));
        }

        // Sometimes there are mistakes in the documentation like missing comas so that the number of value descriptions does not match the number of values.
        if (values.Count == descriptions.Count)
        {
            for (int i = 0; i < values.Count; i++)
            {
                Add(new MidiValue(values[i], descriptions[i], null, null));
            }
        }
        else
        {
            if (values.Count == 1)
            {
                Add(new MidiValue(values[1], values[1].ToString(), null, null));
                return;
            }

            for (int i = 0; i < values.Count; i++)
            {
                Add(new MidiValue(values[i], null, null, null));
            }

            descriptionMinValue = values[0];
            descriptionMaxValue = values[^1];
        }
    }

    public MidiValueList(List<int> values, double descriptionMinValue, double descriptionMaxValue, string? unit)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Value list must not be empty.", nameof(values));
        }

        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], null, null, unit));
        }

        this.descriptionMinValue = descriptionMinValue;
        this.descriptionMaxValue = descriptionMaxValue;
        this.unit = unit;
    }

    public MidiValueList(List<int> values, string descriptionTableRefname)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Value list must not be empty.", nameof(values));
        }

        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], null, null, null));
        }

        DescriptionTableRefName = descriptionTableRefname;
    }

    public List<int> GetValues()
    {
        return this.Select(x => x.Value).ToList();
    }

    public List<string> GetDescriptions()
    {
        if (Count == 0)
        {
            return [];
        }

        if (this[0].Description != null)
        {
            return this.Select(x => x.Category == null ? x.Description! : x.Category + " - " + x.Description).ToList();
        }

        if (descriptionMinValue != null && descriptionMaxValue != null)
        {
            string unit = string.IsNullOrEmpty(this.unit) ? "" : this.unit;
            return MidiTableLeafEntry.AssembleDescriptionValues(descriptionMinValue.Value, descriptionMaxValue.Value, Count, unit);
        }

        return this.Select(x => x.Value.ToString()).ToList();
    }
}
