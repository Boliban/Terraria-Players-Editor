using System.Diagnostics;
using System.Text;

namespace Terraria_Players_Editor.Services.Memory;

/// <summary>
/// Wraps an opened target process: raw read/write primitives, pointer-chain
/// resolution and the main thread's stack base (used to anchor the Player
/// pointer chain: threadstack0 - 0x3D8).
/// All target addresses are 32-bit (Terraria is an x86 process).
/// </summary>
public sealed class MemoryProcess : IDisposable
{
    private readonly IntPtr _handle;

    public Process Process { get; }
    public int ProcessId => Process.Id;
    public uint MainThreadId { get; }
    public uint MainThreadStackBase { get; private set; }
    public bool IsConnected { get; private set; }
    public string? LastError { get; private set; }

    private MemoryProcess(Process process, IntPtr handle, uint mainThreadId)
    {
        Process = process;
        _handle = handle;
        MainThreadId = mainThreadId;
    }

    public static MemoryProcess? TryOpen(Process process) => TryOpen(process, out _);

    public static MemoryProcess? TryOpen(Process process, out string? error)
    {
        error = null;
        try
        {
            Win32Api.EnableDebugPrivilege();

            uint access = Win32Api.PROCESS_VM_READ | Win32Api.PROCESS_VM_WRITE |
                          Win32Api.PROCESS_VM_OPERATION | Win32Api.PROCESS_QUERY_INFORMATION;
            var handle = Win32Api.OpenProcess(access, false, (uint)process.Id);
            if (handle == IntPtr.Zero)
            {
                error = "OpenProcess failed (permission denied; try running as administrator)";
                return null;
            }

            // Reject 64-bit targets: all offsets in this editor are 4-byte pointers.
            if (!Win32Api.IsWow64Process(handle, out bool isWow64) || !isWow64)
            {
                error = "target process is not 32-bit (WOW64)";
                Win32Api.CloseHandle(handle);
                return null;
            }

            var mp = new MemoryProcess(process, handle, FindMainThreadId(process));
            if (mp.MainThreadId == 0)
            {
                error = "could not determine the main thread";
                Win32Api.CloseHandle(handle);
                return null;
            }
            // Mark connected first: ReadThreadStackBase goes through ReadBytes,
            // which refuses to work while IsConnected is false.
            mp.IsConnected = true;
            mp.MainThreadStackBase = mp.ReadThreadStackBase();
            if (mp.MainThreadStackBase == 0)
            {
                error = $"could not read the main thread stack base (tid={mp.MainThreadId})";
                Win32Api.CloseHandle(handle);
                return null;
            }
            return mp;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Determine the game's main thread: prefer the thread that owns the main
    /// window (the game loop runs on it), fall back to the first managed thread.
    /// </summary>
    private static uint FindMainThreadId(Process process)
    {
        try
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                // GetWindowThreadProcessId returns the thread id that owns the window.
                uint tid = Win32Api.GetWindowThreadProcessId(process.MainWindowHandle, out uint pid);
                if (tid != 0 && pid == (uint)process.Id)
                    return tid;
            }
        }
        catch
        {
            // fall through
        }

        // Fallback: the earliest-created thread — for a normal process that is
        // the main thread (Process.Threads enumeration order is not guaranteed).
        try
        {
            ProcessThread? earliest = null;
            foreach (ProcessThread t in process.Threads)
            {
                try
                {
                    if (earliest == null || t.StartTime < earliest.StartTime)
                        earliest = t;
                }
                catch
                {
                    // thread may have exited; skip
                }
            }
            if (earliest != null)
                return (uint)earliest.Id;
        }
        catch
        {
            // fall through
        }

        // Last resort: first thread.
        try
        {
            foreach (ProcessThread t in process.Threads)
                return (uint)t.Id;
        }
        catch
        {
            return 0;
        }
        return 0;
    }

    /// <summary>
    /// Read the main thread's stack base. For a WOW64 (32-bit) target the TEB
    /// returned by NtQueryInformationThread is the 64-bit TEB; its StackBase at
    /// +0x08 is an 8-byte value that lives below 4GB for the 32-bit stack, so
    /// its low 32 bits are the x86 stack base (CE's "threadstack0").
    /// </summary>
    private uint ReadThreadStackBase()
    {
        var threadHandle = Win32Api.OpenThread(Win32Api.THREAD_QUERY_INFORMATION, false, MainThreadId);
        if (threadHandle == IntPtr.Zero)
            return 0;
        try
        {
            if (Win32Api.NtQueryInformationThread(threadHandle, Win32Api.ThreadBasicInformation,
                    out var tbi, System.Runtime.InteropServices.Marshal.SizeOf<Win32Api.THREAD_BASIC_INFORMATION>(),
                    out _) != 0)
                return 0;
            if (tbi.TebBaseAddress == 0)
                return 0;
            uint teb = (uint)tbi.TebBaseAddress;
            if (!ReadUInt64(teb + 0x08, out ulong stackBase))
                return 0;
            return (uint)(stackBase & 0xFFFFFFFF);
        }
        finally
        {
            Win32Api.CloseHandle(threadHandle);
        }
    }

    #region Raw read / write

    /// <summary>
    /// Enumerate committed, readable memory regions in the 32-bit target address
    /// space. Regions are capped at 32 MB per iteration so scanners can process
    /// them in bounded chunks.
    /// </summary>
    public IEnumerable<(uint BaseAddress, int Size)> EnumerateReadableRegions(uint start = 0x00400000, uint end = 0x20000000)
    {
        long cursor = start;
        while (cursor < end)
        {
            if (Win32Api.VirtualQueryEx(_handle, (IntPtr)cursor, out var m,
                    System.Runtime.InteropServices.Marshal.SizeOf<Win32Api.MEMORY_BASIC_INFORMATION64>()) == 0)
                break;
            long size = (long)m.RegionSize;
            if (size <= 0)
                break;
            if (m.State == 0x1000 /* MEM_COMMIT */ && IsReadableProtect(m.Protect))
            {
                uint baseAddr = (uint)m.BaseAddress;
                long capped = Math.Min(size, 0x2000000L);
                if (baseAddr + capped > start && baseAddr < end)
                {
                    long clippedStart = Math.Max(0, (long)start - baseAddr);
                    long clippedEnd = Math.Min(capped, (long)end - baseAddr);
                    if (clippedEnd > clippedStart)
                        yield return (baseAddr + (uint)clippedStart, (int)(clippedEnd - clippedStart));
                }
            }
            cursor = (long)m.BaseAddress + size;
            if (cursor <= 0) break;
        }
    }

    private static bool IsReadableProtect(uint protect)
    {
        uint p = protect & 0xFF;
        return p is 0x02 or 0x04 or 0x10 or 0x20 or 0x40 or 0x80;
    }

    public bool ReadBytes(uint address, byte[] buffer, int offset, int count)
    {
        if (!IsConnected) return false;
        if (count <= 0 || offset < 0 || offset + count > buffer.Length) return false;
        var chunk = new byte[count];
        if (!Win32Api.ReadProcessMemory(_handle, (IntPtr)address, chunk, count, out int read) || read != count)
            return false;
        Array.Copy(chunk, 0, buffer, offset, count);
        return true;
    }

    public bool ReadBytes(uint address, Span<byte> buffer)
    {
        if (!IsConnected || buffer.Length == 0) return false;
        var arr = buffer.ToArray();
        if (!Win32Api.ReadProcessMemory(_handle, (IntPtr)address, arr, arr.Length, out int read) || read != arr.Length)
            return false;
        arr.CopyTo(buffer);
        return true;
    }

    public bool ReadUInt32(uint address, out uint value)
    {
        value = 0;
        var buf = new byte[4];
        if (!ReadBytes(address, buf, 0, 4)) return false;
        value = BitConverter.ToUInt32(buf, 0);
        return true;
    }

    public bool ReadUInt64(uint address, out ulong value)
    {
        value = 0;
        var buf = new byte[8];
        if (!ReadBytes(address, buf, 0, 8)) return false;
        value = BitConverter.ToUInt64(buf, 0);
        return true;
    }

    public bool ReadInt32(uint address, out int value)
    {
        value = 0;
        var buf = new byte[4];
        if (!ReadBytes(address, buf, 0, 4)) return false;
        value = BitConverter.ToInt32(buf, 0);
        return true;
    }

    public bool ReadByte(uint address, out byte value)
    {
        value = 0;
        var buf = new byte[1];
        if (!ReadBytes(address, buf, 0, 1)) return false;
        value = buf[0];
        return true;
    }

    public uint ReadUInt32(uint address) => ReadUInt32(address, out var v) ? v : 0;

    public int ReadInt32(uint address) => ReadInt32(address, out var v) ? v : 0;

    public bool WriteBytes(uint address, ReadOnlySpan<byte> data)
    {
        if (!IsConnected || data.Length == 0) return false;
        var arr = data.ToArray();
        return Win32Api.WriteProcessMemory(_handle, (IntPtr)address, arr, arr.Length, out _);
    }

    public bool WriteUInt32(uint address, uint value) => WriteBytes(address, BitConverter.GetBytes(value));

    public bool WriteInt32(uint address, int value) => WriteBytes(address, BitConverter.GetBytes(value));

    public bool WriteByte(uint address, byte value) => WriteBytes(address, new[] { value });

    /// <summary>Read a .NET System.String referenced by the 4-byte pointer at <paramref name="refAddress"/>.</summary>
    public string? ReadDotNetString(uint refAddress)
    {
        if (!ReadUInt32(refAddress, out uint strObj) || strObj == 0)
            return null;
        // x86 .NET string: [0]=MethodTable, [4]=length, [8]=UTF-16 data.
        if (!ReadInt32(strObj + 0x04, out int len))
            return null;
        if (len < 0 || len > 1024)
            return null;
        var buf = new byte[len * 2];
        if (!ReadBytes(strObj + 0x08, buf, 0, buf.Length))
            return null;
        return Encoding.Unicode.GetString(buf);
    }

    /// <summary>
    /// Resolve a pointer chain in CE semantics: for each offset, read the
    /// pointer at the current address, then add the offset. Optionally perform
    /// one final dereference (some chains end with a stored pointer).
    /// </summary>
    public bool ResolveChain(uint baseAddress, IReadOnlyList<uint> offsets, bool finalDeref, out uint result)
    {
        result = 0;
        uint addr = baseAddress;
        foreach (var off in offsets)
        {
            if (!ReadUInt32(addr, out uint ptr))
                return false;
            addr = ptr + off;
        }
        if (finalDeref)
        {
            if (!ReadUInt32(addr, out uint ptr))
                return false;
            addr = ptr;
        }
        result = addr;
        return true;
    }

    #endregion

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            Win32Api.CloseHandle(_handle);
            IsConnected = false;
        }
    }
}
