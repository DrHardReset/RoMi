namespace RoMi.Presentation;

public class DeviceIdDisplayItem
{
    public byte Value { get; set; }
    public string Display
    {
        get
        {
            string valueString = $"0x{Value:X2} ({Value + 1})"; ;

            if (Value == 127)
            {
                valueString += " [Broadcast]";
            }

            return valueString;
        }
    }

    public DeviceIdDisplayItem(byte value)
    {
        this.Value = value;
    }
}
