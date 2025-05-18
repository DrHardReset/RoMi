using System.Collections.ObjectModel;
using RoMi.Presentation.Controls;

namespace RoMi.Presentation;

// class shall be partial for trimming and AOT compatibility
public partial class MidiViewModel : ObservableObject, IMidiDeviceService, IDisposable
{
    public class ControlOption
    {
        public string Name { get; set; }
        public Type ControlType { get; set; }

        public ControlOption(string name, Type controlType)
        {
            Name = name;
            ControlType = controlType;
        }
    }

    private readonly INavigator navigator;
    private readonly MidiDocument midiDocument;
    public string DeviceName { get; set; }

    public IAsyncRelayCommand ToggleMidiEnableCommand { get; }
    public ICommand OnNavigatedFrom { get; }

    public ObservableCollection<ControlOption> AvailableControls { get; } =
    [
        new ControlOption("Combobox View", typeof(MidiIoComboboxControl)),
        new ControlOption("Tree View", typeof(MidiIoTreeControl))
    ];

    private ControlOption? selectedControl;
    public ControlOption? SelectedControl
    {
        get => selectedControl;
        set
        {
            if (value == null)
            {
                return;
            }

            if (SetProperty(ref selectedControl, value))
            {
                if (value.ControlType == typeof(MidiIoComboboxControl))
                {
                    var viewModel = new MidiIoComboboxControlViewModel(navigator, midiDocument, this);
                    SelectedControlInstance = new MidiIoComboboxControl(viewModel);
                }
                else if (value.ControlType == typeof(MidiIoTreeControl))
                {
                    var viewModel = new MidiIoTreeControlViewModel(navigator, midiDocument, this);
                    SelectedControlInstance = new MidiIoTreeControl(viewModel);
                }
                else
                {
                    // Fallback for other control types
                    SelectedControlInstance = Activator.CreateInstance(value.ControlType) as UserControl;
                }
            }
        }
    }

    private UserControl? selectedControlInstance;
    public UserControl? SelectedControlInstance
    {
        get => selectedControlInstance;
        private set
        {
            // Dispose the old control if it's disposable
            if (selectedControlInstance is IDisposable disposableControl)
            {
                disposableControl.Dispose();
            }

            SetProperty(ref selectedControlInstance, value);
        }
    }

    public List<string> MidiOutputDeviceList { get; }
    private int selectedMidiOutputIndex;
    public int SelectedMidiOutputIndex
    {
        get
        {
            return selectedMidiOutputIndex;
        }
        set
        {
            selectedMidiOutputIndex = value;
            DisposeMidiOutput(); // Reset as the constructor takes this property
            IsMidiDeviceEnabled = false;
            OnPropertyChanged();
        }
    }

    private event EventHandler<bool>? midiDeviceEnabledChanged;
    event EventHandler<bool> IMidiDeviceService.MidiDeviceEnabledChanged
    {
        add => midiDeviceEnabledChanged += value;
        remove => midiDeviceEnabledChanged -= value;
    }

    private bool isMidiDeviceEnabled;
    public bool IsMidiDeviceEnabled
    {
        get => isMidiDeviceEnabled;
        private set
        {
            if (SetProperty(ref isMidiDeviceEnabled, value))
            {
                midiDeviceEnabledChanged?.Invoke(this, value);
            }
        }
    }

    private bool isInitializing;
    public bool IsInitializing
    {
        get => isInitializing;
        private set => SetProperty(ref isInitializing, value);
    }

    private RolandSysExClient rolandSysExClient = new RolandSysExClient(StartAddress.MaxAddressByteCount);

    RolandSysExClient IMidiDeviceService.SysExClient => rolandSysExClient;

    public MidiViewModel(INavigator navigator, MidiDocument midiDocument)
    {
        this.navigator = navigator;
        this.midiDocument = midiDocument;
        DeviceName = midiDocument.DeviceName;

        MidiOutputDeviceList = RolandSysExClient.MidiOutputs;

        OnNavigatedFrom = new AsyncRelayCommand(async () =>
        {
            await DisposeMidiOutputAsync();
            Dispose();
        });

        ToggleMidiEnableCommand = new AsyncRelayCommand(async () =>
        {
            try
            {
                if (IsMidiDeviceEnabled)
                {
                    var success = await EnableMidi();

                    if (!success)
                    {
                        IsMidiDeviceEnabled = false;
                    }
                }
                else
                {
                    DisposeMidiOutput();
                    IsMidiDeviceEnabled = false;
                }
            }
            catch (Exception ex)
            {
                DisposeMidiOutput();
                IsMidiDeviceEnabled = false;

                await navigator.ShowMessageDialogAsync(this,
                    title: "Error",
                    content: $"An unexpected error occurred: {ex.Message}");
            }
        });

    }

    public void Initialize()
    {
        if (AvailableControls.Count > 0)
        {
            SelectedControl = AvailableControls[0];
        }
    }

    private async Task<bool> EnableMidi()
    {
        if (SelectedMidiOutputIndex == -1)
        {
            if (MidiOutputDeviceList == null || MidiOutputDeviceList.Count == 0)
            {
                await navigator.ShowMessageDialogAsync(this, title: "No MIDI output device available. (Going back to main view and reopening MIDI view refreshes the MIDI device list)");
                return false;
            }

            await navigator.ShowMessageDialogAsync(this, title: "No MIDI output device selected.");
            return false;
        }

        try
        {
            await rolandSysExClient.InitAsync(SelectedMidiOutputIndex);
            return true;
        }
        catch (Exception ex)
        {
            await DisposeMidiOutputAsync();
            _ = navigator.ShowMessageDialogAsync(this, title: "Initializing MIDI connection failed.", content: $"Initializing MIDI connection to device {MidiOutputDeviceList[SelectedMidiOutputIndex]} failed.\n{ex}");
            return false;
        }
    }

    async Task IMidiDeviceService.DisableMidiDeviceAsync()
    {
        await DisposeMidiOutputAsync();
        IsMidiDeviceEnabled = false;
    }

    private void DisposeMidiOutput()
    {
        if (rolandSysExClient != null)
        {
            rolandSysExClient.Dispose();
            rolandSysExClient = new RolandSysExClient(StartAddress.MaxAddressByteCount);
            OnPropertyChanged(nameof(IMidiDeviceService.SysExClient));
        }
    }

    private async Task DisposeMidiOutputAsync()
    {
        if (rolandSysExClient != null)
        {
            await rolandSysExClient.DisposeAsync();
            rolandSysExClient = new RolandSysExClient(StartAddress.MaxAddressByteCount);
            OnPropertyChanged(nameof(IMidiDeviceService.SysExClient));
        }
    }

    public void Dispose()
    {
        // Dispose the current control if it exists
        if (selectedControlInstance is IDisposable disposableControl)
        {
            disposableControl.Dispose();
        }

        DisposeMidiOutput();
    }

}
