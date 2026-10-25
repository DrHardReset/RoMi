using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using RoMi.Models.Converters;
using Timer = System.Timers.Timer;

namespace RoMi.Presentation.Controls;

// class shall be partial for trimming and AOT compatibility
public partial class MidiIoTreeControlViewModel : INotifyPropertyChanged, IDisposable
{
    private bool disposed = false;

    private readonly INavigator navigator;
    private readonly IMidiDeviceService midiDeviceService;
    private readonly MidiTableNavigator midiTableNavigator;
    public List<TreeItem> TreeItems { get; } = [];

    private readonly Timer sysexTimer;

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private readonly MidiDocument midiDocument;
    public string DeviceName { get; set; }

    // Planner settings. Gaps (e.g. reserved addresses) up to MaxGapBytes are read over and discarded.
    private const int MaxGapBytes = 16;
    private const int MaxRangeBytes = 128;
    private const byte DeviceId = 0x7F; // Broadcast, could be made configurable in the future

    private IReadOnlyList<TreeItem> selectedTreeItems = [];
    private int pollRunning;

    public bool IsMidiDeviceEnabled => midiDeviceService.IsMidiDeviceEnabled;

    private bool isAutoRefreshEnabled = true;
    public bool IsAutoRefreshEnabled
    {
        get => isAutoRefreshEnabled;
        set
        {
            if (isAutoRefreshEnabled != value)
            {
                isAutoRefreshEnabled = value;
                OnPropertyChanged();
                UpdateTimerState();
            }
        }
    }

    public IAsyncRelayCommand RefreshCommand { get; }

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

        RefreshCommand = new AsyncRelayCommand(PollSelectionAsync, () => midiDeviceService.IsMidiDeviceEnabled);

        // Subscribe last, so that the handler never sees uninitialized members
        midiDeviceService.MidiDeviceEnabledChanged += OnMidiDeviceEnabledChanged;

        UpdateTimerState();
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

    private void OnMidiDeviceEnabledChanged(object? sender, bool isEnabled)
    {
        UpdateTimerState();
        RefreshCommand.NotifyCanExecuteChanged();

        // The event may be raised from a non UI thread
        if (App.MainWindow?.DispatcherQueue is { } dispatcherQueue)
        {
            dispatcherQueue.TryEnqueue(() => OnPropertyChanged(nameof(IsMidiDeviceEnabled)));
        }
        else
        {
            OnPropertyChanged(nameof(IsMidiDeviceEnabled));
        }
    }

    public void SetSelectedTreeItems(IEnumerable<TreeItem> items)
    {
        selectedTreeItems = [.. items];
    }

    private void UpdateTimerState()
    {
        if (!disposed && isAutoRefreshEnabled && midiDeviceService.IsMidiDeviceEnabled)
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
        await PollSelectionAsync();
    }

    private static bool TryGetAbsoluteAddress(TreeItem leafItem, MidiTableLeafEntry leaf, out byte[] address)
    {
        address = [];
        List<MidiTableBranchEntry> branches = [];

        for (TreeItem? parent = leafItem.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.MidiTableEntry is not MidiTableBranchEntry branch)
            {
                return false;
            }

            branches.Insert(0, branch);
        }

        // root + branch1 (+ branch2) are supported by AccumulateStartAddreses
        if (branches.Count is < 2 or > 3)
        {
            return false;
        }

        address = MidiDocument.AccumulateStartAddreses(branches[0], branches[1], branches.Count == 3 ? branches[2] : null, leaf);
        return true;
    }

    private static string FormatLeafValue(MidiTableLeafEntry leaf, byte[] bytes)
    {
        int value = MidiDocument.CalculateValueFromBytes(leaf.ValueDataByteBitMasks, bytes);
        List<int> values = leaf.MidiValueList.GetValues();
        int valueIndex = values.FindIndex(x => x == value);

        if (valueIndex < 0)
        {
            throw new Exception($"The received value {value} could not be found in the value list which contains {values.Count} entries.");
        }

        return $"{leaf.MidiValueList.GetDescriptions()[valueIndex]} [{BitConverter.ToString(bytes)}]";
    }

    private async Task PollSelectionAsync()
    {
        if (disposed)
        {
            sysexTimer.Stop();
            return;
        }

        if (!midiDeviceService.IsMidiDeviceEnabled)
        {
            sysexTimer.Stop();
            return;
        }

        var rolandSysExClient = midiDeviceService.SysExClient;

        if (rolandSysExClient == null || !rolandSysExClient.IsReady())
        {
            sysexTimer.Stop();
            await navigator.ShowMessageDialogAsync(this, title: "Not Ready", content: "MIDI device is not connected or ready.");
            return;
        }

        // Collect all selected leaves (branches are ignored for now)
        List<SysExReadItem<TreeItem>> reads = [];

        foreach (TreeItem item in selectedTreeItems)
        {
            if (item.Type == TreeItemType.Leaf
                && item.MidiTableEntry is MidiTableLeafEntry leaf
                && TryGetAbsoluteAddress(item, leaf, out byte[] address))
            {
                reads.Add(new SysExReadItem<TreeItem>(item, address.ToIntegerRepresentation(StartAddress.MaxAddressByteCount), leaf.ValueDataByteBitMasks.Count));
            }
        }

        if (reads.Count == 0)
        {
            return;
        }

        // Skip this run if the previous one is still active
        if (Interlocked.Exchange(ref pollRunning, 1) == 1)
        {
            return;
        }

        try
        {
            foreach (var range in SysExRangePlanner.Plan(reads, MaxGapBytes, MaxRangeBytes))
            {
                byte[] startAddress = range.StartAddress.From7bitIntegerRepresentation(StartAddress.MaxAddressByteCount);
                byte[] rq1 = midiDocument.CalculateRq1(DeviceId, startAddress, range.TotalSize);
                byte[] dt1 = await rolandSysExClient.SendRq1AndWaitForResponseAsync(rq1);

                if (dt1.Length != range.TotalSize)
                {
                    throw new NotSupportedException($"The received DT1 message has {dt1.Length} bytes instead of the requested {range.TotalSize} bytes.");
                }

                List<(TreeItem Item, string Text)> results = [];

                foreach (var member in range.Members)
                {
                    var leaf = (MidiTableLeafEntry)member.Tag.MidiTableEntry;

                    try
                    {
                        byte[] bytes = dt1.AsSpan(member.Offset, member.Size).ToArray();
                        results.Add((member.Tag, FormatLeafValue(leaf, bytes)));
                    }
                    catch (Exception ex)
                    {
                        // a single faulty value shall not stop the whole polling
                        results.Add((member.Tag, $"Error: {ex.Message}"));
                    }
                }

                await (App.MainWindow?.DispatcherQueue?.EnqueueAsync(() =>
                {
                    foreach (var (item, text) in results)
                    {
                        item.ReceivedValue = text;
                    }
                }) ?? Task.CompletedTask);
            }
        }
        catch (Exception ex)
        {
            sysexTimer.Stop();

            await (App.MainWindow?.DispatcherQueue?.EnqueueAsync(async () =>
            {
                if (!disposed)
                {
                    await navigator.ShowMessageDialogAsync(this, title: "Sending MIDI Message Failed", content: $"Error: {ex.Message}");
                    await midiDeviceService.DisableMidiDeviceAsync();
                }
            }) ?? Task.CompletedTask);
        }
        finally
        {
            Interlocked.Exchange(ref pollRunning, 0);
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
