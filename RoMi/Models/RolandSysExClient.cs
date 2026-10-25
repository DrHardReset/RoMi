namespace RoMi.Models;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commons.Music.Midi;

// class shall be partial for trimming and AOT compatibility
public partial class RolandSysExClient(int maxAddressByteCount) : IAsyncDisposable, IDisposable
{
    private bool disposed = false;
    private readonly int maxAddressByteCount = maxAddressByteCount;
#pragma warning disable CS0618 // Type or element is obsolete
    private static readonly IMidiAccess midi = MidiAccessManager.Default;
#pragma warning restore CS0618 // Type or element is obsolete
    private string deviceName = "undefined";
    private IMidiInput? input;
    private IMidiOutput? output;

    private const byte rolandId = 0x41;
    private readonly object sendLock = new();

    // Key = start address (7-bit integer representation)
    private readonly ConcurrentDictionary<int, PendingRangeRequest> pendingRequests = new();
    
    public static List<string> MidiOutputs { get => midi.Outputs.Select(x => x.Name + " " + x.Id).ToList(); }
    public static List<string> MidiInputs { get => midi.Inputs.Select(x => x.Name + " " + x.Id).ToList(); }

    public async Task InitAsync(int outputDeviceIndex)
    {
        deviceName = midi.Outputs.ToArray()[outputDeviceIndex].Name;
        var outPort = midi.Outputs.FirstOrDefault(x => x.Name.StartsWith(deviceName));
        var inPort = midi.Inputs.FirstOrDefault(x => x.Name.StartsWith(deviceName));

        if (outPort == null)
        {
            throw new InvalidOperationException($"No suitable MIDI-output-port for '{deviceName}' available.");
        }

        if (inPort == null)
        {
            throw new InvalidOperationException($"No suitable MIDI-input-port for '{deviceName}' available.");
        }

        output = await midi.OpenOutputAsync(outPort.Id);
        input = await midi.OpenInputAsync(inPort.Id);
        input.MessageReceived += OnMessageReceived;

        await Task.Delay(500); // Initialization of MIDI devices is buggy and sometimes fails with first call
    }

    public bool IsReady()
    {
        if (output == null || input == null)
        { 
            return false;
        }

        return true;
    }

    public void Send(byte[] sysExBytes)
    {
        if (output == null)
        {
            throw new NotSupportedException("MIDI output is not initialized.");
        }

        lock (sendLock)
        {
            output.Send(sysExBytes, 0, sysExBytes.Length, 0);
        }
    }

    private sealed class PendingRangeRequest(int startAddress, int size)
    {
        private readonly byte[] buffer = new byte[size];
        private readonly bool[] filled = new bool[size];
        private int filledCount;

        public int StartAddress { get; } = startAddress;
        public int Size { get; } = size;
        public TaskCompletionSource<byte[]> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void AddData(int address, byte[] data)
        {
            lock (buffer)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    int index = address - StartAddress + i;

                    if (!filled[index])
                    {
                        filled[index] = true;
                        filledCount++;
                    }

                    buffer[index] = data[i];
                }

                if (filledCount == Size)
                {
                    Completion.TrySetResult((byte[])buffer.Clone());
                }
            }
        }
    }

    private static int Decode7Bit(byte[] data, int offset, int count)
    {
        int value = 0;

        for (int i = 0; i < count; i++)
        {
            value = (value << 7) | (data[offset + i] & 0x7F);
        }

        return value;
    }

    public async Task<byte[]> SendRq1AndWaitForResponseAsync(byte[] sysExBytes, int timeoutMs = 2000)
    {
        // address starts at position 8, followed by the size (same length)
        int startAddress = Decode7Bit(sysExBytes, 8, maxAddressByteCount);
        int size = Decode7Bit(sysExBytes, 8 + maxAddressByteCount, maxAddressByteCount);

        if (size <= 0)
        {
            throw new ArgumentException("The RQ1 message must request at least one byte.", nameof(sysExBytes));
        }

        if (input == null)
        {
            throw new NotSupportedException("MIDI input is not initialized.");
        }

        // Remove any stale request for this address
        if (pendingRequests.TryRemove(startAddress, out var stale))
        {
            stale.Completion.TrySetCanceled();
        }

        var request = new PendingRangeRequest(startAddress, size);

        if (!pendingRequests.TryAdd(startAddress, request))
        {
            throw new InvalidOperationException($"There is already a pending request for address 0x{startAddress:X}.");
        }

        try
        {
            Send(sysExBytes);

            using var cts = new CancellationTokenSource(timeoutMs);
            using (cts.Token.Register(() => request.Completion.TrySetCanceled(), useSynchronizationContext: false))
            {
                try
                {
                    return await request.Completion.Task;
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException($"Request for address 0x{startAddress:X} ({size} bytes) timed out after {timeoutMs} ms.");
                }
            }
        }
        finally
        {
            // only removes the entry if it still is this request
            pendingRequests.TryRemove(new KeyValuePair<int, PendingRangeRequest>(startAddress, request));
        }
    }

    private void OnMessageReceived(object? sender, MidiReceivedEventArgs e)
    {
        var data = e.Data;

        // Ignore non expected messages
        if (data.Length < 14 || data[0] != 0xF0 || data[^1] != 0xF7 || data[7] != 0x12)
        {
            return;
        }

        // Verify checksum (covers address + data)
        int sum = 0;

        for (int i = 8; i < data.Length - 2; i++)
        {
            sum += data[i];
        }

        if (((sum + data[^2]) & 0x7F) != 0)
        {
            return;
        }

        int address = Decode7Bit(data, 8, maxAddressByteCount);
        int dataStart = 8 + maxAddressByteCount;
        int dataEnd = data.Length - 2; // before checksum
        byte[] valueData = data[dataStart..dataEnd];

        if (valueData.Length == 0)
        {
            return;
        }

        foreach (var request in pendingRequests.Values)
        {
            if (address >= request.StartAddress && address + valueData.Length <= request.StartAddress + request.Size)
            {
                request.AddData(address, valueData);
            }
        }
    }

    #region ressource management

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();

        // Suppress finalizer
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (disposed)
            return;

        if (input != null)
        {
            input.MessageReceived -= OnMessageReceived;
            await input.CloseAsync();
            input = null;
        }

        if (output != null)
        {
            await output.CloseAsync();
            output = null;
        }

        disposed = true;
    }

    // Fallback-Sync Dispose, in case of using IDisposable.
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            DisposeSyncCore();
            GC.SuppressFinalize(this);
        }
    }

    private void DisposeSyncCore()
    {
        if (input != null)
        {
            input.MessageReceived -= OnMessageReceived;
            try { input.CloseAsync().GetAwaiter().GetResult(); } catch { }
            input = null;
        }

        if (output != null)
        {
            try { output.CloseAsync().GetAwaiter().GetResult(); } catch { }
            output = null;
        }
    }

    ~RolandSysExClient()
    {
        Dispose();
    }

    #endregion ressource management
}
