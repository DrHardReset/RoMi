namespace RoMi.Business.Models;

/// <summary>
/// Entry of a "Leaf" table. It contains a 2 byte start address. The description cell contains the possible values. In the documentation this row is followed by the desciptive strings for the values.
/// </summary>
public class MidiTableLeafEntry : MidiTableEntry
{
    /// <summary>
    /// Each letter in the documentation symbolizes 1 bit of the data range for the SysEx message.
    /// For most Parameters 1 data byte needs to be sent. Parameters marked with # require values with dataranges that exceed 7 bits. These parameters need multiple data bytes to be sent.
    /// </summary>
    public List<uint> ValueDataByteBitMasks { get; set; }
    public List<int> Values { get; set; } = new List<int>();
    public List<string> ValueDescriptions { get; set; } = new List<string>();
    /// <summary>Returns a bitmask with "bitCount" bits set to 1.</summary>
    private Func<int, uint> Bitmask = (bitCount) =>  (uint)((1 << bitCount) - 1);

    public MidiTableLeafEntry(StartAddress startAddress, string description, List<uint> valueDataBitsPerByte, List<int> values, List<string> valueDescriptions)
        : base(startAddress, description)
    {
        CheckEmptyLists(values, valueDescriptions, valueDataBitsPerByte);

        ValueDataByteBitMasks = valueDataBitsPerByte;
        Values = values;
        ValueDescriptions = valueDescriptions;
    }

    public MidiTableLeafEntry(string startAddress, string description, List<string> valueDataBitsPerByte, List<int> values, List<string> valueDescriptions)
        : base(startAddress, description)
    {
        CheckEmptyLists(values, valueDescriptions, valueDataBitsPerByte);

        ValueDataByteBitMasks = valueDataBitsPerByte.Select(x => Bitmask(x.Count(y => y != ' ' && y != '0'))).ToList();

        if (ValueDataByteBitMasks.Count(x => x == 0 || x > 127) > 0)
        {
            throw new NotSupportedException($"At least one of the value bytes for {description} contains less then 1 or more than 7 bits.");
        }

        uint completeBitmask = Bitmask(ValueDataByteBitMasks.Sum(x => (int)x));

        if (values.Count > 0 && completeBitmask < values.Last())
        {
            /*
             * There are table entries with wrong bitmasks:
             * | 00 09 | 0000 000a | Click Type (0 - 3) |
             * Try to correct them at least for single byte entries by analyzing the value list.
             */

            if (ValueDataByteBitMasks.Count > 1)
            {
                throw new Exception($"The highest entry of the value list '{values.Last()}' for {description} does not fit to bitmask {completeBitmask}. As thsi is a multi nyte entry no correction is implemented.");
            }

            uint newBitmask = Bitmask((int)Math.Log(values.Last(), 2) + 1);

            if (newBitmask < values.Last())
            {
                throw new NotSupportedException($"The highest entry of the value list '{values.Last()}' for {description} does not fit to bitmask {completeBitmask}.");
            }

            ValueDataByteBitMasks[0] = newBitmask;
        }

        Values = values;
        ValueDescriptions = valueDescriptions;
    }

    private void CheckEmptyLists<T, U>(List<int> values, List<T> valueDescriptions, List<U> valueDataBitsPerByte)
    {
        if (values.Count > 0 && valueDescriptions.Count == 0)
        {
            throw new ArgumentException($"The value description list for {Description} must not be empty.", nameof(valueDescriptions));
        }

        if (valueDataBitsPerByte.Count == 0)
        {
            throw new ArgumentException($"The value data bits list for {Description} must not be empty.", nameof(valueDataBitsPerByte));
        }
    }

    public static List<int> AssembleValueList(int valueLow, int valueHigh)
    {
        return Enumerable.Range(valueLow, valueHigh - valueLow + 1).ToList();
    }

    /// <summary>
    /// Assembles the list of description values by the given low and high value and the amount of steps between them.
    /// </summary>
    /// <param name="valueLow">lowest description value.</param>
    /// <param name="valueHigh">Highest description value.</param>
    /// <param name="valueCount">The amount of values in the range of <paramref name="valueLow"/> and <paramref name="valueHigh"/>.</param>
    /// <param name="unit">The unit that gets postfxed to each description value. (May be an empty string).</param>
    public static List<string> AssembleDescriptionValues(double valueLow, double valueHigh, int valueCount, string unit)
    {
        return Linspace(valueLow, valueHigh, valueCount).Select(x => Math.Round(x, 4) + unit).ToList();
    }

    static List<double> Linspace(double StartValue, double EndValue, int numberofpoints)
    {
        double[] parameterVals = new double[numberofpoints];
        double increment = Math.Abs(StartValue - EndValue) / Convert.ToDouble(numberofpoints - 1);
        int count = 0;
        double nextValue = StartValue;

        for (int i = 0; i < numberofpoints; i++)
        {
            parameterVals.SetValue(Math.Round(nextValue, 10), count); // Round values because of floatingpoint precision. (Amount of digits is fictively set to 10)
            count++;

            if (count > numberofpoints)
            {
                throw new IndexOutOfRangeException();
            }

            nextValue += increment;
        }

        return parameterVals.ToList();
    }

    public override string ToString()
    {
        string divisionTag = ValueDataByteBitMasks.Count > 1 ? "#" : " ";
        string valueBitMask = string.Join("_", ValueDataByteBitMasks);
        return $"{divisionTag} {base.ToString(),-50} {valueBitMask} {Values[0]} - {Values.Last()} ({string.Join(",", ValueDescriptions)})";
    }
}
