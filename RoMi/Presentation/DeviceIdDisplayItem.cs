namespace RoMi.Presentation;

public class DeviceIdDisplayItem
{
    public byte Value { get; set; }
    public string Display => $"0x{Value:X2} ({Value + 1})";

    public DeviceIdDisplayItem(byte value)
    {
        this.Value = value;
    }
}
