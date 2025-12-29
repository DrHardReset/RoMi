namespace RoMi.Presentation;

public class DeviceIdDisplayItem(byte value)
{
    public byte Value { get; set; } = value;
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
}
