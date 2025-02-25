using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace StartOTP
{
    class Program
    {
        private const string bAppName = "AnyOTP.exe";
        private const string bDllName = "HookWMIC.dll";

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        const uint CREATE_SUSPENDED = 0x00000004;

        // VirtualFreeEx signture  https://www.pinvoke.net/default.aspx/kernel32.virtualfreeex
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);

        // VirtualAllocEx signture https://www.pinvoke.net/default.aspx/kernel32.virtualallocex
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        // WriteProcessMemory signture https://www.pinvoke.net/default.aspx/kernel32/WriteProcessMemory.html
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

        // GetProcAddress signture https://www.pinvoke.net/default.aspx/kernel32.getprocaddress
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        // GetModuleHandle signture http://pinvoke.net/default.aspx/kernel32.GetModuleHandle
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // CreateRemoteThread signture https://www.pinvoke.net/default.aspx/kernel32.createremotethread
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern bool ResumeThread(IntPtr hThread);

        static void Main(string[] args)
        {
            var bCurrentDir = Environment.CurrentDirectory;
            var bOtpPath = Path.Combine(bCurrentDir, bAppName);
            var bDllPath = Path.Combine(bCurrentDir, bDllName);

            if (!File.Exists(bOtpPath))
            {
                Console.WriteLine($"{bOtpPath} Bulunamadı!");
                Console.ReadLine();
                return;
            }

            if (!File.Exists(bDllPath))
            {
                Console.WriteLine($"{bDllPath} Bulunamadı!");
                Console.ReadLine();
                return;
            }

            var startupInfo = new STARTUPINFO();
            startupInfo.cb = (uint)Marshal.SizeOf(startupInfo);

            if (!CreateProcess(
                    null, 
                    bOtpPath,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    CREATE_SUSPENDED,
                    IntPtr.Zero,
                    null,
                    ref startupInfo, 
                    out var processInfo))
            {
                Console.WriteLine($"CreateProcess Failed! Error: {Marshal.GetLastWin32Error()}");
                Console.ReadLine();
                return;
            }

            var dllNameAddress = VirtualAllocEx(
                processInfo.hProcess,
                IntPtr.Zero,
                (IntPtr) bDllPath.Length,
                AllocationType.Reserve | AllocationType.Commit,
                MemoryProtection.ExecuteReadWrite);

            if (dllNameAddress == IntPtr.Zero)
            {
                Console.WriteLine($"VirtualAllocEx Failed! Error: {Marshal.GetLastWin32Error()}");
                Console.ReadLine();
                return;
            }

            try
            {
                var dllNameBytes = Encoding.ASCII.GetBytes(bDllPath);

                if (!WriteProcessMemory(
                        processInfo.hProcess,
                        dllNameAddress,
                        dllNameBytes,
                        dllNameBytes.Length,
                        out _
                    ))
                {
                    Console.WriteLine($"WriteProcessMemory Failed! Error: {Marshal.GetLastWin32Error()}");
                    Console.ReadLine();
                    return;
                }

                var kernel32Address = GetModuleHandle("kernel32.dll");

                if (kernel32Address == IntPtr.Zero)
                {
                    Console.WriteLine($"GetModuleHandle Failed! Error: {Marshal.GetLastWin32Error()}");
                    Console.ReadLine();
                    return;
                }

                var loadLibraryAddress = GetProcAddress(kernel32Address, "LoadLibraryA");

                if (loadLibraryAddress == IntPtr.Zero)
                {
                    Console.WriteLine($"GetProcAddress Failed! Error: {Marshal.GetLastWin32Error()}");
                    Console.ReadLine();
                    return;
                }

                var hThread = CreateRemoteThread(
                    processInfo.hProcess,
                    IntPtr.Zero,
                    0,
                    loadLibraryAddress,
                    dllNameAddress,
                    0,
                    IntPtr.Zero
                );

                if (hThread == IntPtr.Zero)
                {
                    Console.WriteLine($"CreateRemoteThread Failed! Error: {Marshal.GetLastWin32Error()}");
                    Console.ReadLine();
                    return;
                }

                WaitForSingleObject(hThread, 0xFFFFFFFF);
                ResumeThread(processInfo.hThread);
            }
            finally
            {
                VirtualFreeEx(processInfo.hProcess, dllNameAddress, 0, AllocationType.Release);
            }
        }
    }
}
