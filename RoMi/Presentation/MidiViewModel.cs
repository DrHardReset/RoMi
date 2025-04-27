using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.WinUI;
using RoMi.Models.Converters;

namespace RoMi.Presentation;

// class shall be partial for trimming and AOT compatibility
public partial class MidiViewModel : INotifyPropertyChanged
{
    private readonly INavigator navigator;
    private readonly MidiTableNavigator midiTableNavigator;

    private RolandSysExClient? rolandSysExClient;
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
            OnPropertyChanged();
        }
    }

    public ICommand DoSendSysexDt1ToDevice { get; }
    public ICommand DoSendSysexRq1ToDevice { get; }
    public ICommand OnNavigatedFrom { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly MidiDocument midiDocument;
    public string DeviceName { get; set; }

    public List<DeviceIdDisplayItem> DeviceIdList { get; } = [.. MidiDocument.DeviceIds.Select(i => new DeviceIdDisplayItem(i))];

    private byte selectedDeviceId;
    public byte SelectedDeviceId
    {
        get
        {
            return selectedDeviceId;
        }
        set
        {
            selectedDeviceId = value;
            DisposeMidiOutput(); // Reset as the constructor takes this property
            OnPropertyChanged();

            if (RootTable == null)
            {
                return;
            }

            TriggerSysExCalculation();
        }
    }

    private MidiTable rootTable;
    public MidiTable RootTable
    {
        get
        {
            return rootTable;
        }
        set
        {
            rootTable = value;
            OnPropertyChanged();
            RootTableSelectedIndex = 0;
        }
    }

    private int rootTableSelectedIndex = 0;
    public int RootTableSelectedIndex
    {
        get
        {
            return rootTableSelectedIndex;
        }
        set
        {
            rootTableSelectedIndex = NormalizeSelectedIndex(value);
            OnPropertyChanged();

            var childTable = midiTableNavigator.GetChildTable(RootTable, rootTableSelectedIndex);

            if (midiTableNavigator.IsLeafTable(childTable))
            {
                /*
                 * Insert dummy table which disables Branch ComboBox in View if selected root table's child reference is a leaf table.
                 * Example: AX-Edge's "Setup" entry of Root table directly references the Leaf table [Setup]
                 */
                BranchTable1 = new MidiTable { new MidiTableBranchEntry() };
                BranchTable2 = BranchTable1;
                LeafTable = childTable;
            }
            else
            {
                BranchTable1 = childTable;
            }
        }
    }

    private MidiTable branchTable1;
    public MidiTable BranchTable1
    {
        get
        {
            return branchTable1;
        }
        set
        {
            int currentSelectedBranchTableIndex = BranchTableSelectedIndex1;

            branchTable1 = value;
            OnPropertyChanged();

            // Try to assign previous BranchTableSelectedIndex to new table. This may be useful when switching from table "Program Tone 01" to "Program Tone 02" as it keeps selected entries of sub tables.
            BranchTableSelectedIndex1 = branchTable1.Count >= currentSelectedBranchTableIndex + 1 ? currentSelectedBranchTableIndex : 0;
        }
    }

    private int branchTableSelectedIndex1 = 0;
    public int BranchTableSelectedIndex1
    {
        get => branchTableSelectedIndex1;
        set
        {
            branchTableSelectedIndex1 = NormalizeSelectedIndex(value);
            OnPropertyChanged();

            var childTable = midiTableNavigator.GetChildTable(BranchTable1, branchTableSelectedIndex1);

            if (midiTableNavigator.IsLeafTable(childTable))
            {
                /*
                * Insert dummy table which disables second Branch ComboBox in View if (like in most cases) there is only one branch table between root and leaf table.
                */
                BranchTable2 = new MidiTable { new MidiTableBranchEntry() };
                LeafTable = childTable;
            }
            else
            {
                BranchTable2 = childTable;
            }
        }
    }

    private MidiTable branchTable2;
    public MidiTable BranchTable2
    {
        get
        {
            return branchTable2;
        }
        set
        {
            int currentSelectedBranchTableIndex = BranchTableSelectedIndex2;

            branchTable2 = value;
            OnPropertyChanged();

            // Try to assign previous BranchTableSelectedIndex to new table. This may be useful when switching from table "Program Tone 01" to "Program Tone 02" as it keeps selected entries of sub tables.
            BranchTableSelectedIndex2 = branchTable2.Count >= currentSelectedBranchTableIndex + 1 ? currentSelectedBranchTableIndex : 0;
        }
    }

    private int branchTableSelectedIndex2 = 0;
    public int BranchTableSelectedIndex2
    {
        get => branchTableSelectedIndex2;
        set
        {
            branchTableSelectedIndex2 = NormalizeSelectedIndex(value);
            OnPropertyChanged();

            var childTable = midiTableNavigator.GetChildTable(BranchTable2, branchTableSelectedIndex2);
            LeafTable = childTable;
        }
    }

    private MidiTable leafTable;
    public MidiTable LeafTable
    {
        get
        {
            return leafTable;
        }
        set
        {
            int currentSelectedLeafTableIndex = LeafTableSelectedIndex;

            leafTable = value;
            OnPropertyChanged();

            // Try to assign previous LeafTableSelectedIndex to new table. This may be useful when switching from table "Program Tone 01" to "Program Tone 02" as it keeps selected entries of sub tables.
            LeafTableSelectedIndex = leafTable.Count >= currentSelectedLeafTableIndex + 1 ? currentSelectedLeafTableIndex : 0;
        }
    }

    private int leafTableSelectedIndex = 0;
    public int LeafTableSelectedIndex
    {
        get
        {
            return leafTableSelectedIndex;
        }
        set
        {
            leafTableSelectedIndex = NormalizeSelectedIndex(value);
            OnPropertyChanged();

            if (LeafTable[leafTableSelectedIndex] is not MidiTableLeafEntry selectedLeafEntry)
            {
                return;
            }

            /*
             * Android has massive performance issues when loading a ComboBox with hundrets of entries.
             * if not android: Show ComboBox fully loaded
             * if android: show paged ListView
             */
#if HAS_UNO
            int itemsPerPage = 100;
#else
            int itemsPerPage = selectedLeafEntry.MidiValueList.Count;
#endif

            PagedListSource source = new(selectedLeafEntry.MidiValueList.GetDescriptions().Select(x => (object)x).ToList());
            Values = new IncrementalLoadingCollection<PagedListSource, object>(source, itemsPerPage);
        }
    }

    private IncrementalLoadingCollection<PagedListSource, object> values;
    public IncrementalLoadingCollection<PagedListSource, object> Values
    {
        get
        {
            return values;
        }
        set
        {
            // Try to keep the currently selected value index if it exists in new value set.
            int currentSelectedValueIndex = ValuesSelectedIndex;

            values = value;
            values.OnEndLoading = () => {
                ValuesSelectedIndex = Values.Count >= currentSelectedValueIndex + 1 ? currentSelectedValueIndex : 0;
            };

            _ = values.LoadMoreItemsAsync(1); // Loads first page of items
            OnPropertyChanged();
        }
    }

    private int valuesSelectedIndex = 0;
    public int ValuesSelectedIndex
    {
        get
        {
            return valuesSelectedIndex;
        }
        set
        {
            valuesSelectedIndex = value == -1 ? 0 : value;
            OnPropertyChanged();
            TriggerSysExCalculation();
        }
    }

    private byte[] sysExMessageDt1 = [];
    public byte[] SysExMessageDt1
    {
        get
        {
            return sysExMessageDt1;
        }
        set
        {
            sysExMessageDt1 = value;
            OnPropertyChanged();
        }
    }

    private byte[] sysExMessageRq1 = [];
    public byte[] SysExMessageRq1
    {
        get
        {
            return sysExMessageRq1;
        }
        set
        {
            sysExMessageRq1 = value;
            OnPropertyChanged();
        }
    }

    public MidiViewModel(INavigator navigator, MidiDocument midiDocument)
    {
        this.navigator = navigator;
        this.midiDocument = midiDocument;
        // TODO: store selected device ID for next app start
        SelectedDeviceId = MidiDocument.DeviceIds[0];
        DeviceName = midiDocument.DeviceName;
        midiTableNavigator = new MidiTableNavigator(midiDocument.MidiTables);
        RootTable = midiTableNavigator.GetRootTable();

        MidiOutputDeviceList = RolandSysExClient.MidiOutputs;
        DoSendSysexDt1ToDevice = new AsyncRelayCommand(async () => await SendSysexToDevice(true));
        DoSendSysexRq1ToDevice = new AsyncRelayCommand(async () => await SendSysexToDevice(false));
        OnNavigatedFrom = new AsyncRelayCommand(async () => await DisposeMidiOutputAsync());

        if (rootTable is null || branchTable1 is null || branchTable2 is null || leafTable is null || values is null)
        {
            throw new Exception("Error while initiating comboboxes.");
        }
    }

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// We want to have a selected index of 0 if the selected index is -1. This is needed for the ComboBox binding.
    /// </summary>
    /// <param name="value">The new value that shall be assigned to 'selected index' variable and will be normalized.</param>
    /// <returns>The new normalized value.</returns>
    private int NormalizeSelectedIndex(int value)
    {
        return value == -1 ? 0 : value;
    }

    private void TriggerSysExCalculation()
    {
        if (RootTable[RootTableSelectedIndex] is not MidiTableBranchEntry rootEntry ||
            BranchTable1[BranchTableSelectedIndex1] is not MidiTableBranchEntry branchEntry1 ||
            BranchTable2[BranchTableSelectedIndex2] is not MidiTableBranchEntry branchEntry2 ||
            LeafTable[LeafTableSelectedIndex] is not MidiTableLeafEntry leafEntry ||
            leafEntry.MidiValueList.Count == 0)
        {
            SysExMessageDt1 = [];
            SysExMessageRq1 = [];
            return;
        }

        byte[] sysExStartAddress = MidiDocument.AccumulateStartAddreses(rootEntry, branchEntry1, branchEntry2, leafEntry);
        int value = leafEntry.MidiValueList[ValuesSelectedIndex].Value;
        SysExMessageDt1 = midiDocument.CalculateDt1(SelectedDeviceId, sysExStartAddress, leafEntry.ValueDataByteBitMasks, value);
        SysExMessageRq1 = midiDocument.CalculateRq1(SelectedDeviceId, sysExStartAddress, leafEntry.ValueDataByteBitMasks.Count);
    }

    public async Task SendSysexToDevice(bool isDt1)
    {
        /*
         * The Uno project suggests to use "Windows.Devices.Midi". But this library does not read the proper MIDI device names for my gear.
         * The "managed-midi" library does a better job for device names.
         */
        if (SelectedMidiOutputIndex == -1)
        {
            if (MidiOutputDeviceList == null || MidiOutputDeviceList.Count == 0)
            {
                _ = navigator.ShowMessageDialogAsync(this, title: "No MIDI output device available. (Going back to main view and reopening MIDI view refreshes the MIDI device list)");
                return;
            }

            _ = navigator.ShowMessageDialogAsync(this, title: "No MIDI output device selected.");
            return;
        }

        try
        {
            if (rolandSysExClient == null || !rolandSysExClient.IsReady())
            {
                rolandSysExClient = new RolandSysExClient(StartAddress.MaxAddressByteCount, SelectedMidiOutputIndex);
                await rolandSysExClient.InitAsync();
            }

            if (isDt1) // DT1
            {
                rolandSysExClient.Send(SysExMessageDt1);
            }
            else // RQ1
            {
                if (SysExMessageRq1.Length == 0)
                {
                    return;
                }

                MidiTableLeafEntry leafEntry = (MidiTableLeafEntry)LeafTable[LeafTableSelectedIndex];
                byte[] dt1 = await rolandSysExClient.SendRq1AndWaitForResponseAsync(SysExMessageRq1);

                if (dt1.Length > leafEntry.ValueDataByteBitMasks.Count)
                {
                    throw new NotSupportedException($"The received DT1 message value has a size of {dt1.Length} bytes instead of the maximum supported {leafEntry.ValueDataByteBitMasks.Count} bytes.");
                }

                int dt1Value = MidiDocument.CalculateValueFromBytes(leafEntry.ValueDataByteBitMasks, dt1);

                List<int> values = leafEntry.MidiValueList.GetValues();
                int valueIndex = values.FindIndex(x => x == dt1Value);

                if (valueIndex < 0)
                {
                    throw new Exception($"The received DT1 message value index {valueIndex} could not be found in the value list which contains {values.Count} entries.");
                }

                string hexString = BitConverter.ToString(dt1);
                string dt1ValueDescription = leafEntry.MidiValueList.GetDescriptions()[valueIndex];

                await new Dt1ContentDialog(hexString, dt1ValueDescription).ShowAsync();
            }
        }
        catch(Exception ex)
        {
            await DisposeMidiOutputAsync();
            _ = navigator.ShowMessageDialogAsync(this, title: "Sending MIDI message failed.", content: $"Sending MIDI message {SysExMessageDt1.ByteArrayToHexString()} to device {MidiOutputDeviceList[SelectedMidiOutputIndex]} failed.\n{ex}");
        }
    }

    private void DisposeMidiOutput()
    {
        if (rolandSysExClient != null)
        {
            rolandSysExClient.Dispose();
            rolandSysExClient = null;
        }
    }

    private async Task DisposeMidiOutputAsync()
    {
        if (rolandSysExClient != null)
        {
            await rolandSysExClient.DisposeAsync();
            rolandSysExClient = null;
        }
    }
}
