using System.ComponentModel;
using System.Runtime.CompilerServices;
using Commons.Music.Midi;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI;
using RoMi.Business.Converters;

namespace RoMi.Presentation;

public class MidiViewModel : INotifyPropertyChanged
{
    private readonly INavigator navigator;

    private IMidiOutput? midiOutput = null;
    public List<IMidiPortDetails> MidiPortDetails { get; }
    private IMidiPortDetails? selectedMidiPortDetails;
    public IMidiPortDetails? SelectedMidiPortDetails
    {
        get
        {
            return selectedMidiPortDetails;
        }
        set
        {
            selectedMidiPortDetails = value;
            OnPropertyChanged();
            _ = DisposeMidiOutput();
        }
    }

    public ICommand DoSendSysexToDevice { get; }
    public ICommand OnNavigatedFrom { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly MidiDocument midiDocument;

    public List<int> DeviceId { get; } = MidiDocument.DeviceIds;

    public string DeviceName { get; set; }

    private int selectedDeviceId = 0;
    public int SelectedDeviceId
    {
        get
        {
            return selectedDeviceId;
        }
        set
        {
            selectedDeviceId = value;
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
            rootTableSelectedIndex = value == -1 ? 0 : value;
            OnPropertyChanged();

            string childBranchName = ((MidiTableBranchEntry)RootTable[rootTableSelectedIndex]).LeafName;
            int targetTableIndex = midiDocument.MidiTables.GetTableIndexByName(childBranchName);

            MidiTable childTable = midiDocument.MidiTables[targetTableIndex];

            if (childTable.MidiTableType == MidiTableType.LeafTable)
            {
                /*
                 * Insert dummy table which disables Branch ComboBox in View if selected root table's child reference is a leaf table.
                 * Example: AX-Edge's "Setup" entry of Root table directly references the Leaf table [Setup]
                 */
                BranchTable1 = new()
                {
                    new MidiTableBranchEntry()
                };
                BranchTable2 = BranchTable1;
                LeafTable = childTable;
                return;
            }

            BranchTable1 = midiDocument.MidiTables[targetTableIndex];
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
        get
        {
            return branchTableSelectedIndex1;
        }
        set
        {
            branchTableSelectedIndex1 = value == -1 ? 0 : value;
            OnPropertyChanged();

            string childBranchName = ((MidiTableBranchEntry)BranchTable1[branchTableSelectedIndex1]).LeafName;
            int targetTableIndex = midiDocument.MidiTables.GetTableIndexByName(childBranchName);
            MidiTable childTable = midiDocument.MidiTables[targetTableIndex];

            if (childTable.MidiTableType is MidiTableType.BranchTable)
            {
                BranchTable2 = midiDocument.MidiTables[targetTableIndex];
            }
            else
            {
                /*
                 * Insert dummy table which disables second Branch ComboBox in View if (like in most cases) there is only one branch table between root and leaf table.
                 */
                BranchTable2 = new()
                {
                    new MidiTableBranchEntry()
                };

                LeafTable = midiDocument.MidiTables[targetTableIndex];
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
        get
        {
            return branchTableSelectedIndex2;
        }
        set
        {
            branchTableSelectedIndex2 = value == -1 ? 0 : value;
            OnPropertyChanged();

            string childBranchName = ((MidiTableBranchEntry)BranchTable2[branchTableSelectedIndex2]).LeafName;
            int targetTableIndex = midiDocument.MidiTables.GetTableIndexByName(childBranchName);
            LeafTable = midiDocument.MidiTables[targetTableIndex];
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

            // Not sure if it really makes sense here, but as we try to assign previous SelectedIndex for Branch Table we do it here, too.
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
            leafTableSelectedIndex = value == -1 ? 0 : value;
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
            int itemsPerPage = selectedLeafEntry.ValueDescriptions.Count;
#endif

            PagedListSource source = new(selectedLeafEntry.ValueDescriptions.Select(x => (object)x).ToList());
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

    private byte[] sysExMessage = [];
    public byte[] SysExMessage
    {
        get
        {
            return sysExMessage;
        }
        set
        {
            sysExMessage = value;
            OnPropertyChanged();
        }
    }

    public MidiViewModel(INavigator navigator, MidiDocument midiDocument)
    {
        this.navigator = navigator;
        this.midiDocument = midiDocument;
        // TODO: store selected device ID for next app start
        SelectedDeviceId = 17;
        DeviceName = midiDocument.DeviceName;
        RootTable = midiDocument.MidiTables[0];

        MidiPortDetails = MidiAccessManager.Default.Outputs.ToList();
        SelectedMidiPortDetails = MidiPortDetails.FirstOrDefault();
        DoSendSysexToDevice = new AsyncRelayCommand(SendSysexToDevice);
        OnNavigatedFrom = new AsyncRelayCommand(DisposeMidiOutput);

        if (rootTable is null || branchTable1 is null || branchTable2 is null || leafTable is null || values is null)
        {
            throw new Exception("Error while initiating comboboxes.");
        }
    }

    public void OnPropertyChanged([CallerMemberName] string name = "") =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void TriggerSysExCalculation()
    {
        byte selectedDeviceId = (byte)(SelectedDeviceId - 1); // the documentation states that 0x10 == 17 -> subtract 1 of the integer value

        if (RootTable[RootTableSelectedIndex] is not MidiTableBranchEntry rootEntry ||
            BranchTable1[BranchTableSelectedIndex1] is not MidiTableBranchEntry branchEntry1 ||
            BranchTable2[BranchTableSelectedIndex2] is not MidiTableBranchEntry branchEntry2 ||
            LeafTable[LeafTableSelectedIndex] is not MidiTableLeafEntry leafEntry ||
            leafEntry.Values.Count == 0)
        {
            SysExMessage = [];
            return;
        }

        int value = leafEntry.Values[ValuesSelectedIndex];
        SysExMessage = MidiDocument.CalculateSysex(midiDocument.ModelIdBytes, selectedDeviceId, rootEntry, branchEntry1, branchEntry2, leafEntry, value);
    }

    public async Task SendSysexToDevice()
    {
        /*
         * The Uno project suggests to use "Windows.Devices.Midi". But this library does not read the proper MIDI device names for my gear.
         * The "managed-midi" library does a better job for device names.
         */
        if (SelectedMidiPortDetails == null)
        {
            if (MidiPortDetails == null || MidiPortDetails.Count == 0)
            {
                _ = navigator.ShowMessageDialogAsync(this, title: "No MIDI output device available. (Going back to main view and reopening MIDI view refreshes the MIDI device list)");
                return;
            }

            _ = navigator.ShowMessageDialogAsync(this, title: "No MIDI output device selected.");
            return;
        }

        try
        {
            await Task.Run(async () =>
            {
                if (midiOutput?.Connection != MidiPortConnectionState.Open)
                {
                    var accessManager = MidiAccessManager.Default;
                    midiOutput = await accessManager.OpenOutputAsync(SelectedMidiPortDetails.Id);
                }

                midiOutput.Send(SysExMessage, 0, SysExMessage.Length, 0);
            });
        }
        catch(Exception ex)
        {
            _ = navigator.ShowMessageDialogAsync(this, title: "Sending MIDI message failed.", content: $"Sending MIDI message {SysExMessage.ByteArrayToHexString()} to device {SelectedMidiPortDetails.Name} failed.\n{ex}");
        }
    }

    private async Task DisposeMidiOutput()
    {
        if (midiOutput == null)
        {
            return;
        }

        await midiOutput.CloseAsync();
    }
}
