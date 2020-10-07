using ReClassNET.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static KernelPlugin.Imports;
using static KernelPlugin.Macro;
using static KernelPlugin.Structs;

namespace KernelPlugin
{
    class KernelDriver
    {
        private IntPtr Handle;
        private string RegistryPath;

        public static readonly uint IO_READ_REQUEST = CTL_CODE(/*FILE_DEVICE_UNKNOWN*/0x22, 0xb52801, /*METHOD_BUFFERED*/0, /*FILE_SPECIAL_ACCESS*/0);
        public static readonly uint IO_WRITE_REQUEST = CTL_CODE(/*FILE_DEVICE_UNKNOWN*/0x22, 0xb52802, /*METHOD_BUFFERED*/0, /*FILE_SPECIAL_ACCESS*/0);
        public static readonly uint IO_PROCESS_PEB_INFO = CTL_CODE(/*FILE_DEVICE_UNKNOWN*/0x22, 0xb52803, /*METHOD_BUFFERED*/0, /*FILE_SPECIAL_ACCESS*/0);

        public static readonly string SERVICE_NAME = "KernelClass";
        public static readonly string FILE_NAME = "KernelDriver.sys";

        public KernelDriver(string RegistryPath)
        {
            this.RegistryPath = RegistryPath;
        }

        public bool Load()
        {
            StopAndDelete();
            InstallAndStart();

            if (ServiceInstaller.GetServiceStatus(SERVICE_NAME) != ServiceState.Running)
            {
                OutputDebugString("Driver is installed but not running");
                return false;
            }

            Handle = CreateFile(RegistryPath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileOptions.None, IntPtr.Zero);
            OutputDebugString("Driver handle: " + Handle);
            return IsLoaded();
        }

        public bool IsLoaded()
        {
            return Handle != new IntPtr(-1);    //INVALID_HANDLE_VALUE
        }

        public void InstallAndStart()
        {
            string FileName = System.Environment.CurrentDirectory + "\\Plugins\\" + FILE_NAME;
            OutputDebugString("Loading driver: " + FileName);
            ServiceInstaller.InstallAndStart(SERVICE_NAME, SERVICE_NAME, FileName, ServiceInstaller.SERVICE_KERNEL_DRIVER);
            OutputDebugString("Driver installed");
        }

        public void StopAndDelete()
        {
            if (!ServiceInstaller.ServiceIsInstalled(SERVICE_NAME))
            {
                OutputDebugString("Driver is not installed, skipping");
                return;
            }

            var State = ServiceInstaller.GetServiceStatus(SERVICE_NAME);
            if (State != ServiceState.StopPending)
            {
                ServiceInstaller.StopService(SERVICE_NAME);
                OutputDebugString("Driver has been stopped");
            }

            ServiceInstaller.Uninstall(SERVICE_NAME);
            OutputDebugString("Driver has been uninstalled");
        }

        public T ReadMemory<T>(int ProcessId, IntPtr Address)
        {
            int Size = Marshal.SizeOf(typeof(T));
            byte[] Buffer;

            if (!ReadMemory(ProcessId, Address, out Buffer, Size, out _))
            {
                return default(T);
            }

            var handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            var result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return result;
        }

        public string ReadUnicodeString(int ProcessId, IntPtr Address, int Length, bool Pointer = false)
        {
            byte[] Buffer;

            if (Pointer)
            {
                Address = ReadMemory<IntPtr>(ProcessId, Address);
            }

            if (Length <= 0)
            {
                return "";
            }

            if (!ReadMemory(ProcessId, Address, out Buffer, Length, out _))
            {
                return "";
            }

            return Encoding.Unicode.GetString(Buffer, 0, Length);
        }

        public bool ReadMemory(int ProcessId, IntPtr ReadAddress, out byte[] lpBuffer, int Size, out int NumberOfBytesRead)
        {
            if (!IsLoaded())
            {
                NumberOfBytesRead = 0;
                lpBuffer = new byte[NumberOfBytesRead];
                return false;
            }

            KERNEL_READ_REQUEST Request = new KERNEL_READ_REQUEST();
            Request.ProcessId = ProcessId;
            Request.Address = ReadAddress;
            Request.Size = Size;
            Request.Buffer = Marshal.AllocHGlobal(Size);

            int BufferSize = Marshal.SizeOf(Request);
            IntPtr Buffer = Marshal.AllocHGlobal(BufferSize);
            Marshal.StructureToPtr(Request, Buffer, false);

            bool Result = DeviceIoControl(Handle, IO_READ_REQUEST, Buffer, BufferSize, Buffer, BufferSize, out _, IntPtr.Zero);
            if (Result)
            {
                NumberOfBytesRead = Size;
                lpBuffer = new byte[Size];
                Marshal.Copy(Request.Buffer, lpBuffer, 0, Size);
            }
            else
            {
                NumberOfBytesRead = 0;
                lpBuffer = new byte[0];
            }

            Marshal.FreeHGlobal(Buffer);
            Marshal.FreeHGlobal(Request.Buffer);
            return Result;
        }

        public bool WriteMemory<T>(int ProcessId, IntPtr Address, T Value)
        {
            int Size = Marshal.SizeOf(typeof(T));
            byte[] Buffer = new byte[Size];
            IntPtr Pointer = Marshal.AllocHGlobal(Size);
            Marshal.StructureToPtr(Value, Pointer, false);
            Marshal.Copy(Pointer, Buffer, 0, Size);
            Marshal.FreeHGlobal(Pointer);

            return WriteMemory(ProcessId, Address, Buffer, Size, out _);
        }

        public bool WriteMemory(int ProcessId, IntPtr ReadAddress, byte[] lpBuffer, int Size, out int NumberOfBytesWritten)
        {
            if (!IsLoaded())
            {
                NumberOfBytesWritten = 0;
                return false;
            }

            KERNEL_WRITE_REQUEST Request = new KERNEL_WRITE_REQUEST();
            Request.ProcessId = ProcessId;
            Request.Address = ReadAddress;
            Request.Size = Size;
            Request.Buffer = Marshal.AllocHGlobal(Size);
            Marshal.Copy(lpBuffer, 0, Request.Buffer, Size);

            int BufferSize = Marshal.SizeOf(Request);
            IntPtr Buffer = Marshal.AllocHGlobal(BufferSize);
            Marshal.StructureToPtr(Request, Buffer, false);

            bool Result = DeviceIoControl(Handle, IO_WRITE_REQUEST, Buffer, BufferSize, Buffer, BufferSize, out _, IntPtr.Zero);
            if (Result)
            {
                NumberOfBytesWritten = Size;
            }
            else
            {
                NumberOfBytesWritten = 0;
            }

            Marshal.FreeHGlobal(Buffer);
            Marshal.FreeHGlobal(Request.Buffer);
            return Result;
        }

        public bool GetProcessModules(int ProcessId, ref EnumerateRemoteModuleCallback Callback)
        {
            if (!IsLoaded())
            {
                return false;
            }

            KERNEL_PROCESS_PEB_INFO Request = new KERNEL_PROCESS_PEB_INFO();
            Request.ProcessId = ProcessId;

            int BufferSize = Marshal.SizeOf(Request);
            IntPtr Buffer = Marshal.AllocHGlobal(BufferSize);
            Marshal.StructureToPtr(Request, Buffer, false);

            bool Result = DeviceIoControl(Handle, IO_PROCESS_PEB_INFO, Buffer, BufferSize, Buffer, BufferSize, out _, IntPtr.Zero);
            KERNEL_PROCESS_PEB_INFO Response = (KERNEL_PROCESS_PEB_INFO)Marshal.PtrToStructure(Buffer, typeof(KERNEL_PROCESS_PEB_INFO));

            if (Result)
            {
                IntPtr PebAddress = Response.PebAddress;

                if (Response.IsWow64)
                {
                    OutputDebugString("Getting 32bit process PEB " + PebAddress.ToString("X"));

                    var Peb32 = ReadMemory<PEB32>(ProcessId, PebAddress);

                    if (Peb32.Ldr == 0)
                    {
                        OutputDebugString("Process LDR is not initialised");
                        return false;
                    }

                    var Ldr = ReadMemory<PEB_LDR_DATA32>(ProcessId, new IntPtr(Peb32.Ldr));

                    IntPtr Head = new IntPtr(Ldr.InLoadOrderModuleList.Flink);
                    IntPtr Next = Head;

                    do
                    {
                        var Entry = ReadMemory<LDR_DATA_TABLE_ENTRY32>(ProcessId, Next);
                        Next = new IntPtr(Entry.InLoadOrderLinks.Flink);

                        if (Entry.SizeOfImage == 0 || Entry.DllBase == 0) { continue; }

                        var FullDllName = ReadUnicodeString(ProcessId, new IntPtr(Entry.FullDllName.Buffer), Entry.FullDllName.Length);
                        var BaseDllName = ReadUnicodeString(ProcessId, new IntPtr(Entry.BaseDllName.Buffer), Entry.BaseDllName.Length);

                        var Data = new EnumerateRemoteModuleData();
                        Data.BaseAddress = new IntPtr(Entry.DllBase);
                        Data.Path = FullDllName;
                        Data.Size = new IntPtr(Entry.SizeOfImage);

                        OutputDebugString(
                            BaseDllName +
                            ", Address: " + Entry.DllBase.ToString("X") +
                            ", Size: " + Entry.SizeOfImage.ToString("X"));

                        Callback.Invoke(ref Data);
                    } while (Next != Head);
                }
                else
                {
                    OutputDebugString("Getting 64bit process PEB " + PebAddress.ToString("X"));

                    var Peb = ReadMemory<PEB>(ProcessId, PebAddress);

                    if (Peb.Ldr == IntPtr.Zero)
                    {
                        OutputDebugString("Process LDR is not initialised");
                        return false;
                    }

                    OutputDebugString("Process LDR at: " + Peb.Ldr.ToString("X"));

                    var Ldr = ReadMemory<PEB_LDR_DATA>(ProcessId, Peb.Ldr);

                    IntPtr Head = Ldr.InLoadOrderModuleList.Flink;
                    IntPtr Next = Head;

                    do
                    {
                        var Entry = ReadMemory<LDR_DATA_TABLE_ENTRY>(ProcessId, Next);
                        Next = Entry.InLoadOrderLinks.Flink;

                        if (Entry.SizeOfImage == 0 || Entry.DllBase == IntPtr.Zero) { continue; }

                        var FullDllName = ReadUnicodeString(ProcessId, Entry.FullDllName.Buffer, Entry.FullDllName.Length);
                        var BaseDllName = ReadUnicodeString(ProcessId, Entry.BaseDllName.Buffer, Entry.BaseDllName.Length);

                        var Data = new EnumerateRemoteModuleData();
                        Data.BaseAddress = Entry.DllBase;
                        Data.Path = FullDllName;
                        Data.Size = new IntPtr(Entry.SizeOfImage);

                        OutputDebugString(
                            BaseDllName +
                            ", Address: " + Entry.DllBase.ToString("X") +
                            ", Size: " + Entry.SizeOfImage.ToString("X"));

                        Callback.Invoke(ref Data);

                    } while (Next != Head);
                }
            }

            Marshal.FreeHGlobal(Buffer);
            return Result;
        }

        public bool GetProcessInfo(int ProcessId, ref EnumerateProcessData Data)
        {
            if (!IsLoaded())
            {
                return false;
            }

            KERNEL_PROCESS_PEB_INFO Request = new KERNEL_PROCESS_PEB_INFO();
            Request.ProcessId = ProcessId;

            int BufferSize = Marshal.SizeOf(Request);
            IntPtr Buffer = Marshal.AllocHGlobal(BufferSize);
            Marshal.StructureToPtr(Request, Buffer, false);

            bool Result = DeviceIoControl(Handle, IO_PROCESS_PEB_INFO, Buffer, BufferSize, Buffer, BufferSize, out _, IntPtr.Zero);
            KERNEL_PROCESS_PEB_INFO Response = (KERNEL_PROCESS_PEB_INFO)Marshal.PtrToStructure(Buffer, typeof(KERNEL_PROCESS_PEB_INFO));

            if (Result)
            {
                IntPtr PebAddress = Response.PebAddress;
                OutputDebugString("Process PEB is empty.");
                if (PebAddress == IntPtr.Zero) return false;

                if (Response.IsWow64)
                {
                    OutputDebugString("Getting 32bit process PEB " + PebAddress.ToString("X"));

                    var Peb32 = ReadMemory<PEB32>(ProcessId, PebAddress);

                    if (Peb32.Ldr == 0)
                    {
                        OutputDebugString("Process LDR is not initialised");
                        return false;
                    }

                    var Ldr = ReadMemory<PEB_LDR_DATA32>(ProcessId, new IntPtr(Peb32.Ldr));

                    IntPtr Head = new IntPtr(Ldr.InLoadOrderModuleList.Flink);
                    var Entry = ReadMemory<LDR_DATA_TABLE_ENTRY32>(ProcessId, Head);

                    if (Entry.SizeOfImage == 0 || Entry.DllBase == 0) { return false; }

                    var FullDllName = ReadUnicodeString(ProcessId, new IntPtr(Entry.FullDllName.Buffer), Entry.FullDllName.Length);
                    var BaseDllName = ReadUnicodeString(ProcessId, new IntPtr(Entry.BaseDllName.Buffer), Entry.BaseDllName.Length);

                    Data.Name = BaseDllName;
                    Data.Path = FullDllName;
                }
                else
                {
                    OutputDebugString("Getting 64bit process PEB " + PebAddress.ToString("X"));

                    var Peb = ReadMemory<PEB>(ProcessId, PebAddress);

                    if (Peb.Ldr == IntPtr.Zero)
                    {
                        OutputDebugString("Process LDR is not initialised");
                        return false;
                    }

                    OutputDebugString("Process LDR at: " + Peb.Ldr.ToString("X"));

                    var Ldr = ReadMemory<PEB_LDR_DATA>(ProcessId, Peb.Ldr);

                    IntPtr Head = Ldr.InLoadOrderModuleList.Flink;
                    var Entry = ReadMemory<LDR_DATA_TABLE_ENTRY>(ProcessId, Head);


                    if (Entry.SizeOfImage == 0 || Entry.DllBase == IntPtr.Zero) { return false; }

                    var FullDllName = ReadUnicodeString(ProcessId, Entry.FullDllName.Buffer, Entry.FullDllName.Length);
                    var BaseDllName = ReadUnicodeString(ProcessId, Entry.BaseDllName.Buffer, Entry.BaseDllName.Length);

                    Data.Name = BaseDllName;
                    Data.Path = FullDllName;
                }
            }

            Marshal.FreeHGlobal(Buffer);
            return Result;
        }
    }
}
