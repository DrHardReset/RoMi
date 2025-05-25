using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using Timer = System.Timers.Timer;
using CommunityToolkit.WinUI;
using System.Timers;

namespace RoMi.Presentation.Controls;

// class shall be partial for trimming and AOT compatibility
public partial class MidiIoTreeControlViewModel : INotifyPropertyChanged, IDisposable
{
    private bool disposed = false;

    private readonly INavigator navigator;
    private readonly IMidiDeviceService midiDeviceService;
    private readonly MidiTableNavigator midiTableNavigator;
    public List<TreeItem> TreeItems { get; } = [];

    // Property to store the currently selected tree item
    private TreeItem? selectedTreeItem;
    public TreeItem? SelectedTreeItem
    {
        get => selectedTreeItem;
        set
        {
            selectedTreeItem = value;
            OnPropertyChanged();
        }
    }

    private readonly Timer sysexTimer;

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private readonly MidiDocument midiDocument;
    public string DeviceName { get; set; }

    public MidiIoTreeControlViewModel(INavigator navigator, MidiDocument midiDocument, IMidiDeviceService midiDeviceService)
    {
        this.navigator = navigator;
        this.midiDocument = midiDocument;
        this.midiDeviceService = midiDeviceService;

        midiDeviceService.MidiDeviceEnabledChanged += OnMidiDeviceEnabledChanged;

        DeviceName = midiDocument.DeviceName;
        midiTableNavigator = new MidiTableNavigator(midiDocument.MidiTables);
        BuildTree();

        sysexTimer = new Timer(TimeSpan.FromMilliseconds(500));
        sysexTimer.Elapsed += OnSysexTimerElapsed;
        sysexTimer.AutoReset = true;

        if (midiDeviceService.IsMidiDeviceEnabled)
        {
            sysexTimer.Start();
        }
    }

    private void BuildTree()
    {
        var rootTable = midiTableNavigator.GetRootTable();

        foreach (var item in rootTable)
        {
            if (item is MidiTableBranchEntry branchEntry)
            {
                // Create branch item with lazy loading factory
                var treeItem = new TreeItem(
                    branchEntry,
                    TreeItemType.Branch,
                    () => [.. LoadChildren(branchEntry)]
                );

                TreeItems.Add(treeItem);
            }
        }
    }

    private ObservableCollection<TreeItem> LoadChildren(MidiTableBranchEntry parent)
    {
        var children = new ObservableCollection<TreeItem>();
        var matchingTableIndex = midiDocument.MidiTables.GetTableIndexByName(parent.LeafName);

        if (matchingTableIndex < 0)
        {
            return children;
        }

        var nextTable = midiDocument.MidiTables[matchingTableIndex];

        foreach (var item in nextTable)
        {
            if (item is MidiTableBranchEntry nextBranchEntry)
            {
                // Create branch item with lazy loading factory
                var treeItem = new TreeItem(
                    item,
                    TreeItemType.Branch,
                    () => [.. LoadChildren(nextBranchEntry)]
                );

                children.Add(treeItem);
            }
            else
            {
                // Create leaf items without children
                var treeItem = new TreeItem(
                    item,
                    TreeItemType.Leaf
                );

                children.Add(treeItem);
            }
        }

        return children;
    }

    // Method to collect all parent entries up to the selected node
    private (MidiTableBranchEntry? root, MidiTableBranchEntry? branch1, MidiTableBranchEntry? branch2, MidiTableLeafEntry? leaf) GetSelectedNodeHierarchy()
    {
        // Default null values
        MidiTableBranchEntry? rootEntry = null;
        MidiTableBranchEntry? branchEntry1 = null;
        MidiTableBranchEntry? branchEntry2 = null;
        MidiTableLeafEntry? leafEntry = null;

        // If no selection or selected item is not a leaf, we can't process
        if (selectedTreeItem == null || selectedTreeItem.Type != TreeItemType.Leaf)
        {
            return (root: null, branch1: null, branch2: null, leaf: null);
        }

        // The selected item is a leaf entry
        if (selectedTreeItem.MidiTableEntry is MidiTableLeafEntry leaf)
        {
            leafEntry = leaf;
        }
        else
        {
            return (root: null, branch1: null, branch2: null, leaf: null);
        }

        // Find the parent tree nodes by traversing the tree
        var pathEntries = MidiIoTreeControlViewModel.FindPathToNode(TreeItems, selectedTreeItem);

        // Extract branch entries from path
        int index = 0;

        foreach (var entry in pathEntries)
        {
            if (entry.MidiTableEntry is MidiTableBranchEntry branchEntry)
            {
                if (index == 0)
                {
                    rootEntry = branchEntry;
                }
                else if (index == 1)
                {
                    branchEntry1 = branchEntry;
                }
                else if (index == 2)
                {
                    branchEntry2 = branchEntry;
                }
            }

            index++;
        }

        return (root: rootEntry, branch1: branchEntry1, branch2: branchEntry2, leaf: leafEntry);
    }

    // Recursively find the path from root to the selected node
    private static List<TreeItem> FindPathToNode(List<TreeItem> items, TreeItem target)
    {
        foreach (TreeItem item in items)
        {
            if (item == target)
            {
                return [item];
            }

            if (item.AreChildrenLoaded)
            {
                List<TreeItem> path = MidiIoTreeControlViewModel.FindPathToNode(item.Children, target);

                if (path.Count > 0)
                {
                    path.Insert(0, item);
                    return path;
                }
            }
        }
        return [];
    }

    private void OnMidiDeviceEnabledChanged(object? sender, bool isEnabled)
    {
        if (isEnabled)
        {
            sysexTimer.Start();
        }
        else
        {
            sysexTimer.Stop();
        }
    }

    private async void OnSysexTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!midiDeviceService.IsMidiDeviceEnabled)
        {
            sysexTimer.Stop();
            return;
        }

        if (disposed)
        {
            sysexTimer.Stop();
            return;
        }

        var rolandSysExClient = midiDeviceService.SysExClient;

        // Verify we have all needed components
        if (rolandSysExClient == null || !rolandSysExClient.IsReady())
        {
            sysexTimer.Stop();
            await navigator.ShowMessageDialogAsync(this, title: "Not Ready", content: "MIDI device is not connected or ready.");
            return;
        }

        if (selectedTreeItem == null || selectedTreeItem.Type != TreeItemType.Leaf)
        {
            // TODO: Edit if multiple selection is supported
            return;
        }

        try
        {
            // Get the selected node hierarchy
            var (rootEntry, branchEntry1, branchEntry2, leafEntry) = GetSelectedNodeHierarchy();

            // If we don't have a root, branch1 or leaf entry, we can't proceed
            if (leafEntry == null || branchEntry1 == null || rootEntry == null)
            {
                await navigator.ShowMessageDialogAsync(this, "Selection Required", "Please select a leaf node in the tree.");
                return;
            }

            // Use broadcast device ID, could be made configurable in the future
            byte selectedDeviceId = 0x7F;

            // Calculate the complete address by combining all branches
            byte[] sysExStartAddress = MidiDocument.AccumulateStartAddreses(rootEntry, branchEntry1, branchEntry2, leafEntry);

            // Create the RQ1 message
            byte[] sysExMessageRq1 = midiDocument.CalculateRq1(selectedDeviceId, sysExStartAddress, leafEntry.ValueDataByteBitMasks.Count);

            // Send the RQ1 message and get the response
            byte[] dt1 = await rolandSysExClient.SendRq1AndWaitForResponseAsync(sysExMessageRq1);

            // Validate the response
            if (dt1.Length > leafEntry.ValueDataByteBitMasks.Count)
            {
                throw new NotSupportedException($"The received DT1 message value has a size of {dt1.Length} bytes instead of the maximum supported {leafEntry.ValueDataByteBitMasks.Count} bytes.");
            }

            // Calculate the value from the received bytes
            int dt1Value = MidiDocument.CalculateValueFromBytes(leafEntry.ValueDataByteBitMasks, dt1);

            // Find the corresponding description for this value
            List<int> values = leafEntry.MidiValueList.GetValues();
            int valueIndex = values.FindIndex(x => x == dt1Value);

            if (valueIndex < 0)
            {
                throw new Exception($"The received DT1 message value {dt1Value} could not be found in the value list which contains {values.Count} entries.");
            }

            // Format the hex data for display
            string hexString = BitConverter.ToString(dt1);
            string dt1ValueDescription = leafEntry.MidiValueList.GetDescriptions()[valueIndex];

            // Display the result on the UI thread
            await (App.MainWindow?.DispatcherQueue?.EnqueueAsync(() =>
            {
                selectedTreeItem.ReceivedValue = $"{dt1ValueDescription} [{hexString}]";
            }) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            sysexTimer.Stop();

            // process UI updates on UI thread
            await (App.MainWindow?.DispatcherQueue?.EnqueueAsync(async () =>
            {
                if (!disposed)
                {
                    await navigator.ShowMessageDialogAsync(this, title: "Sending MIDI Message Failed", content: $"Error: {ex.Message}");
                    await midiDeviceService.DisableMidiDeviceAsync();
                }
            }) ?? Task.CompletedTask);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            midiDeviceService.MidiDeviceEnabledChanged -= OnMidiDeviceEnabledChanged;

            if (sysexTimer != null)
            {
                sysexTimer.Stop();
                sysexTimer.Elapsed -= OnSysexTimerElapsed;
                sysexTimer.Dispose();
            }
        }

        disposed = true;
    }

    ~MidiIoTreeControlViewModel()
    {
        Dispose(false);
    }
}
