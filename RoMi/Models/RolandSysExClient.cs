namespace RoMi.Models;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commons.Music.Midi;

// class shall be partial for trimming and AOT compatibility
internal partial class RolandSysExClient(int maxAddressByteCount, int outputDeviceIndex) : IAsyncDisposable, IDisposable
{
    private bool disposed = false;
    private readonly int maxAddressByteCount = maxAddressByteCount;
    private static readonly IMidiAccess midi = MidiAccessManager.Default;
    private readonly string deviceName = midi.Outputs.ToArray()[outputDeviceIndex].Name;
    private IMidiInput? input;
    private IMidiOutput? output;

    private const byte rolandId = 0x41;
    private readonly object sendLock = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> pendingRequests = new();

    public static List<string> MidiOutputs
    {
        get
        {
            return midi.Outputs.Select(x => x.Name + " " + x.Id).ToList();
        }
    }

    public static List<string> MidiInputs
    {
        get
        {
            return midi.Inputs.Select(x => x.Name + " " + x.Id).ToList();
        }
    }

    public async Task InitAsync()
    {
        // TODO: Make MIDI output and input individually selectable
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

    public async Task<byte[]> SendRq1AndWaitForResponseAsync(byte[] sysExBytes, int timeoutMs = 1000)
    {
        // extract address (maxAddressByteCount bytes from position 8)
        byte[] address = sysExBytes.Skip(8).Take(maxAddressByteCount).ToArray();
        string key = BitConverter.ToString(address);
        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!pendingRequests.TryAdd(key, tcs))
        {
            throw new InvalidOperationException($"There is already a pending Request for address {key}.");
        }

        if (input == null)
        {
            throw new NotSupportedException("MIDI input is not initialized.");
        }

        Send(sysExBytes);

        using var cts = new CancellationTokenSource(timeoutMs);
        using (cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false))
        {
            try
            {
                return await tcs.Task;
            }
            finally
            {
                pendingRequests.TryRemove(key, out _);
            }
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

        // extract address (maxAddressByteCount bytes from position 8)
        byte[] address = data.Skip(8).Take(maxAddressByteCount).ToArray();
        string key = BitConverter.ToString(address);

        // extract data which starts after address and ends before checksum:
        int dataStart = 12;
        int dataEnd = data.Length - 2; // before checksum
        byte[] valueData = data[dataStart..dataEnd];

        if (pendingRequests.TryGetValue(key, out var tcs))
        {
            tcs.TrySetResult(valueData);
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
