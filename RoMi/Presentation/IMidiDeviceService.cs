using System.ComponentModel;

namespace RoMi.Presentation;

public interface IMidiDeviceService : INotifyPropertyChanged
{
    bool IsMidiDeviceEnabled { get; }
    event EventHandler<bool> MidiDeviceEnabledChanged;
    RolandSysExClient SysExClient { get; }

    Task DisableMidiDeviceAsync();
}
