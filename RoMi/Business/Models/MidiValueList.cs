namespace RoMi.Business.Models;

public class MidiValueList : List<MidiValue>
{
    // TODO: Properties or fields?
    private double? DescriptionMinValue { get; set; } = null;
    private double? DescriptionMaxValue { get; set; } = null;
    private string? Unit { get; set; } = null;
    public string? DescriptionTableRefName { get; set; } = null;

    public MidiValueList() { }

    public MidiValueList(List<int> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], null, null, null));
        }
    }

    public MidiValueList(List<int> values, List<string> descriptions, List<string> categories)
    {
        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], descriptions[i], categories[i], null));
        }
    }

    public MidiValueList(List<int> values, List<string> descriptions)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Value list must not be empty.", nameof(values));
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

            DescriptionMinValue = values[0];
            DescriptionMaxValue = values[^1];

            for (int i = 0; i < values.Count; i++)
            {
                Add(new MidiValue(values[i], null, null, null));
            }
        }
    }

    public MidiValueList(List<int> values, double descriptionMinValue, double descriptionMaxValue, string? unit)
    {
        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], null, null, unit));
        }

        DescriptionMinValue = descriptionMinValue;
        DescriptionMaxValue = descriptionMaxValue;
        Unit = unit;
    }

    public MidiValueList(List<int> values, string descriptionTableRefname)
    {
        DescriptionTableRefName = descriptionTableRefname;

        for (int i = 0; i < values.Count; i++)
        {
            Add(new MidiValue(values[i], null, null, null));
        }
    }

    // TODO: This Method is ugly as each call does the enumeration
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

        if (this[0].Description == null)
        {
            if (DescriptionMinValue != null && DescriptionMaxValue != null)
            {
                string unit = string.IsNullOrEmpty(Unit) ? "" : Unit;
                return MidiTableLeafEntry.AssembleDescriptionValues(DescriptionMinValue.Value, DescriptionMaxValue.Value, Count, unit);
            }

            return this.Select(x => x.Value.ToString()).ToList();
        }

        return this.Select(x => x.Category == null ? x.Description : x.Category + " - " + x.Description).ToList();
    }
}
