using System.Runtime.InteropServices;

namespace Terraria_Players_Editor.Services.Memory;

/// <summary>
/// Win32 / ntdll platform invoke declarations used by the memory editing features.
/// All target addresses are 32-bit values (Terraria is an x86 process).
/// </summary>
internal static class Win32Api
{
    // Process access rights
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_VM_WRITE = 0x0020;
    public const uint PROCESS_VM_OPERATION = 0x0008;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    // Thread access rights
    public const uint THREAD_QUERY_INFORMATION = 0x0040;
    public const uint THREAD_GET_CONTEXT = 0x0008;

    // Memory allocation / protection
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_RELEASE = 0x8000;
    public const uint PAGE_EXECUTE_READWRITE = 0x40;
    public const uint PAGE_READWRITE = 0x04;

    // ntdll information classes
    public const int ThreadBasicInformation = 0;

    public const int GWLP_WNDPROC = -4;

    /// <summary>
    /// THREAD_BASIC_INFORMATION as filled by ntdll for a 64-bit caller (this
    /// editor runs as x64 even though the target game is x86/WOW64).
    /// Layout (48 bytes): ExitStatus(+0), padding(+4), TebBaseAddress(+8, 8),
    /// ClientId(+16, 16: two 8-byte handles), AffinityMask(+32), Priority(+40),
    /// BasePriority(+44).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct THREAD_BASIC_INFORMATION
    {
        public uint ExitStatus;      // +0x00
        public uint Reserved;        // +0x04 padding
        public ulong TebBaseAddress; // +0x08 (32-bit TEB address for WOW64 targets)
        public ulong UniqueProcess;  // +0x10
        public ulong UniqueThread;   // +0x18
        public ulong AffinityMask;   // +0x20
        public int Priority;         // +0x28
        public int BasePriority;     // +0x2C
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, int dwLength);

    /// <summary>
    /// MEMORY_BASIC_INFORMATION as returned by VirtualQueryEx for a 64-bit
    /// caller (48 bytes). This editor is x64, and the structure layout follows
    /// the caller's bitness even when querying a 32-bit (WOW64) target; the
    /// addresses are 64-bit values whose low 32 bits are the target addresses.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;      // +0x00
        public ulong AllocationBase;   // +0x08
        public uint AllocationProtect; // +0x10
        public uint Alignment1;        // +0x14
        public ulong RegionSize;       // +0x18
        public uint State;             // +0x20
        public uint Protect;           // +0x24
        public uint Type;              // +0x28
        public uint Alignment2;        // +0x2C
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQueryInformationThread(IntPtr threadHandle, int infoClass, out THREAD_BASIC_INFORMATION info, int infoLength, out int returnLength);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool LookupPrivilegeValue(string? systemName, string name, out long lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    public const uint TOKEN_QUERY = 0x0008;
    public const uint SE_PRIVILEGE_ENABLED = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public long Luid;
        public uint Attributes;
    }

    /// <summary>Try to enable SeDebugPrivilege for the current process (best effort).</summary>
    public static void EnableDebugPrivilege()
    {
        try
        {
            var current = System.Diagnostics.Process.GetCurrentProcess();
            if (!OpenProcessToken(current.Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var token))
                return;
            try
            {
                if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out var luid))
                    return;
                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };
                AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                CloseHandle(token);
            }
        }
        catch
        {
            // Best effort only.
        }
    }
}
