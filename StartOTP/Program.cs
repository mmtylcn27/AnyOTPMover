using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;


namespace StartOTP
{
    class Program
    {
        private const string bAppName = "AnyOTP.exe";
        private const string bDllName = "HookWMIC.dll";

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
        ProcessAccessFlags processAccess,
        bool bInheritHandle,
        int processId);
        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.Id);
        }

        // VirtualAllocEx signture https://www.pinvoke.net/default.aspx/kernel32.virtualallocex
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

        // VirtualFreeEx signture  https://www.pinvoke.net/default.aspx/kernel32.virtualfreeex
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
        int dwSize, AllocationType dwFreeType);

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

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        // WriteProcessMemory signture https://www.pinvoke.net/default.aspx/kernel32/WriteProcessMemory.html
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [MarshalAs(UnmanagedType.AsAny)] object lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesWritten);

        // GetProcAddress signture https://www.pinvoke.net/default.aspx/kernel32.getprocaddress
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        // GetModuleHandle signture http://pinvoke.net/default.aspx/kernel32.GetModuleHandle
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // CreateRemoteThread signture https://www.pinvoke.net/default.aspx/kernel32.createremotethread
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(
        IntPtr hProcess,
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        // CloseHandle signture https://www.pinvoke.net/default.aspx/kernel32.closehandle
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);


        static void Main(string[] args)
        {
            var bCurrentDir = Environment.CurrentDirectory;
            var bOtpPath = Path.Combine(bCurrentDir, bAppName);
            var bDllPath = Path.Combine(bCurrentDir, bDllName);

            if (File.Exists(bOtpPath))
            {
                if (File.Exists(bDllPath))
                {
                    var bProcess = new Process();
                    bProcess.StartInfo.FileName = bOtpPath;

                    if (bProcess.Start())
                    {
                        var bPID = bProcess.Id;

                        IntPtr ProcHandle = OpenProcess(
                            ProcessAccessFlags.All,
                            false,
                            bPID);

                        if (ProcHandle != null)
                        {
                            IntPtr DllSpace = VirtualAllocEx(
                                ProcHandle,
                                IntPtr.Zero,
                                (IntPtr) bDllPath.Length,
                                AllocationType.Reserve | AllocationType.Commit,
                                MemoryProtection.ExecuteReadWrite);

                            if (DllSpace != null)
                            {
                                byte[] bytes = Encoding.ASCII.GetBytes(bDllPath);

                                bool DllWrite = WriteProcessMemory(
                                    ProcHandle,
                                    DllSpace,
                                    bytes,
                                    bytes.Length,
                                    out var bytesread
                                );

                                if (DllWrite)
                                {
                                    IntPtr Kernel32Handle = GetModuleHandle("Kernel32.dll");
                                    IntPtr LoadLibraryAAddress = GetProcAddress(Kernel32Handle, "LoadLibraryA");

                                    IntPtr RemoteThreadHandle = CreateRemoteThread(
                                        ProcHandle,
                                        IntPtr.Zero,
                                        0,
                                        LoadLibraryAAddress,
                                        DllSpace,
                                        0,
                                        IntPtr.Zero
                                    );

                                    if (RemoteThreadHandle != null)
                                    {
                                        WaitForSingleObject(RemoteThreadHandle, 0xFFFFFFFF);
                                        VirtualFreeEx(ProcHandle, DllSpace, 0, AllocationType.Release);
                                        CloseHandle(RemoteThreadHandle);
                                        CloseHandle(ProcHandle);
                                    }
                                    else
                                    {
                                        Console.WriteLine(
                                            $"CreateRemoteThread Failed! Error: {Marshal.GetLastWin32Error()}");
                                        VirtualFreeEx(ProcHandle, DllSpace, 0, AllocationType.Release);
                                        CloseHandle(ProcHandle);
                                        Console.ReadLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(
                                        $"WriteProcessMemory Failed! Error: {Marshal.GetLastWin32Error()}");
                                    VirtualFreeEx(ProcHandle, DllSpace, 0, AllocationType.Release);
                                    CloseHandle(ProcHandle);
                                    Console.ReadLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine($"VirtualAllocEx Failed! Error: {Marshal.GetLastWin32Error()}");
                                CloseHandle(ProcHandle);
                                Console.ReadLine();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"OpenProcess Failed! Error: {Marshal.GetLastWin32Error()}");
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        Console.Write("Hata-1");
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine($"{bDllPath} Bulunamadı!");
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine($"{bOtpPath} Bulunamadı!");
                Console.ReadLine();
            }
        }
    }
}
