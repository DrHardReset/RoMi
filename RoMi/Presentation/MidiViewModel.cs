using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using Commons.Music.Midi;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI;
using RoMi.Business.Converters;

namespace RoMi.Presentation
{
    public class MidiViewModel : INotifyPropertyChanged
    {
        private readonly INavigator navigator;

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
            }
        }
        public ICommand DoSendSysexToDevice { get; }

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
                     * Insert dummy table which disables Branch Picker in View if selected root table's child reference is a leaf table.
                     * For example the "Setup" entry of Root table directly references the Leaf table [Setup]
                     */
                    MidiTableBranchEntry midiTableDummyEntry = new MidiTableBranchEntry("000000", string.Empty)
                    {
                        Description = " ", // Does not work with empty string
                        LeafName = childTable.Name
                    };

                    MidiTable midiTableDummy = new MidiTable(string.Empty, string.Empty, null);
                    midiTableDummy.Add(midiTableDummyEntry);
                    BranchTable = midiTableDummy;
                    LeafTable = childTable;
                    return;
                }

                BranchTable = midiDocument.MidiTables[targetTableIndex];
            }
        }

        private MidiTable branchTable;
        public MidiTable BranchTable
        {
            get
            {
                return branchTable;
            }
            set
            {
                int currentSelectedBranchTableIndex = BranchTableSelectedIndex;

                branchTable = value;
                OnPropertyChanged();

                // Try to assign previous BranchTableSelectedIndex to new table. This may be useful when switching from table "Program Tone 01" to "Program Tone 02" as it keeps selected entries of sub tables.
                BranchTableSelectedIndex = branchTable.Count >= currentSelectedBranchTableIndex + 1 ? currentSelectedBranchTableIndex : 0;
            }
        }

        private int branchTableSelectedIndex = 0;
        public int BranchTableSelectedIndex
        {
            get
            {
                return branchTableSelectedIndex;
            }
            set
            {
                branchTableSelectedIndex = value == -1 ? 0 : value;
                OnPropertyChanged();

                string childBranchName = ((MidiTableBranchEntry)BranchTable[branchTableSelectedIndex]).LeafName;
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

                // Not sure if it really makes sense her, but as we try to assign previous SelectedIndex for Branch Table we do it here, too.
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

                MidiTableLeafEntry? selectedLeafEntry = LeafTable[leafTableSelectedIndex] as MidiTableLeafEntry;

                if (selectedLeafEntry == null)
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

                PagedListSource source = new PagedListSource(selectedLeafEntry.ValueDescriptions.Select(x => (object)x).ToList());
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
                valuesSelectedIndex = value;
                OnPropertyChanged();

                if (valuesSelectedIndex == -1)
                {
                    SysExMessage = Array.Empty<byte>();
                    return;
                }

                TriggerSysExCalculation();
            }
        }

        private byte[] sysExMessage = Array.Empty<byte>();
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

            if (rootTable is null || branchTable is null || leafTable is null)
            {
                throw new Exception("Error while initiating comboboxes.");
            }
        }

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void TriggerSysExCalculation()
        {
            byte selectedDeviceId = (byte)(SelectedDeviceId - 1); // the documentation states that 0x10 == 17 -> subtract 1 of the integer value
            MidiTableBranchEntry? rootEntry = RootTable[RootTableSelectedIndex] as MidiTableBranchEntry;
            MidiTableBranchEntry? branchEntry = BranchTable[BranchTableSelectedIndex] as MidiTableBranchEntry;
            MidiTableLeafEntry? leafEntry = LeafTable[LeafTableSelectedIndex] as MidiTableLeafEntry;

            if (rootEntry == null || branchEntry == null || leafEntry == null || leafEntry.Values.Count == 0)
            {
                SysExMessage = new byte[0];
                return;
            }

            int value = leafEntry.Values[ValuesSelectedIndex];
            SysExMessage = MidiDocument.CalculateSysex(midiDocument.ModelIdBytes, selectedDeviceId, rootEntry, branchEntry, leafEntry, value);
        }

        public async Task SendSysexToDevice()
        {
            if (SelectedMidiPortDetails == null)
            {
                if (MidiPortDetails == null || MidiPortDetails.Count == 0)
                {
                    _ = navigator.ShowMessageDialogAsync(this, title: "No MIDI output device available.");
                    return;
                }

                _ = navigator.ShowMessageDialogAsync(this, title: "No MIDI output device selected.");
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    var accessManager = MidiAccessManager.Default;
                    var output = accessManager.OpenOutputAsync(SelectedMidiPortDetails.Id).Result;
                    // Workaround: After first call of OpenOutputAsync the call of output.Send always fails. Closing and reopening the output helps.
                    await output.CloseAsync();
                    output = accessManager.OpenOutputAsync(SelectedMidiPortDetails.Id).Result;

                    try
                    {
                        output.Send(SysExMessage, 0, SysExMessage.Length, 0);
                    }
                    finally
                    {
                        await output.CloseAsync();
                    }
                });
            }
            catch(Exception ex)
            {
                _ = navigator.ShowMessageDialogAsync(this, title: "Sending MIDI message failed.", content: $"Sending MIDI message {SysExMessage.ByteArrayToHexString()} to device {SelectedMidiPortDetails.Name} failed.\n{ex}");
            }
        }
    }
}
