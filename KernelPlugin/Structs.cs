using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KernelPlugin
{
    class Structs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct KERNEL_READ_REQUEST
        {
            public int ProcessId;
            public IntPtr Address;
            public IntPtr Buffer;
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERNEL_WRITE_REQUEST
        {
            public int ProcessId;
            public IntPtr Address;
            public IntPtr Buffer;
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KERNEL_PROCESS_PEB_INFO
        {
            public int ProcessId;
            //[MarshalAs(UnmanagedType.LPWStr, SizeConst = 256)]
            //public string FullName;
            public IntPtr PebAddress;
            public bool IsWow64;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LIST_ENTRY
        {
            public IntPtr Flink; //_LIST_ENTRY *Flink;
            public IntPtr Blink; //_LIST_ENTRY *Blink;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PEB_LDR_DATA
        {
            public uint Length;
            public bool Initialized;
            public IntPtr SsHandle;
            public LIST_ENTRY InLoadOrderModuleList;
            public LIST_ENTRY InMemoryOrderModuleList;
            public LIST_ENTRY InInitializationOrderModuleList;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct LDR_DATA_TABLE_ENTRY
        {
            public LIST_ENTRY InLoadOrderLinks;
            public LIST_ENTRY InMemoryOrderLinks;
            public LIST_ENTRY InInitializationOrderLinks;
            public IntPtr DllBase;
            public IntPtr EntryPoint;
            public uint SizeOfImage;
            public UNICODE_STRING FullDllName;
            public UNICODE_STRING BaseDllName;
            public uint Flags;
            public ushort LoadCount;
            public ushort TlsIndex;
            public LIST_ENTRY HashLinks;
            public uint TimeDateStamp;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PEB
        {
            public byte InheritedAddressSpace;
            public byte ReadImageFileExecOptions;
            public byte BeingDebugged;
            public byte BitField;
            public IntPtr Mutant;
            public IntPtr ImageBaseAddress;
            public IntPtr Ldr; //PPEB_LDR_DATA Ldr;
            public IntPtr ProcessParameters;
            public IntPtr SubSystemData;
            public IntPtr ProcessHeap;
            public IntPtr FastPebLock;
            public IntPtr AtlThunkSListPtr;
            public IntPtr IFEOKey;
            public IntPtr CrossProcessFlags;
            public IntPtr KernelCallbackTable;
            public uint SystemReserved;
            public uint AtlThunkSListPtr32;
            public IntPtr ApiSetMap;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LIST_ENTRY32
        {
            public uint Flink; //_LIST_ENTRY *Flink;
            public uint Blink; //_LIST_ENTRY *Blink;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PEB_LDR_DATA32
        {
            public uint Length;
            public bool Initialized;
            public uint SsHandle; //ULONG SsHandle;
            public LIST_ENTRY32 InLoadOrderModuleList;
            public LIST_ENTRY32 InMemoryOrderModuleList;
            public LIST_ENTRY32 InInitializationOrderModuleList;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LDR_DATA_TABLE_ENTRY32
        {
            public LIST_ENTRY32 InLoadOrderLinks;
            public LIST_ENTRY32 InMemoryOrderLinks;
            public LIST_ENTRY32 InInitializationOrderLinks;
            public uint DllBase;
            public uint EntryPoint;
            public uint SizeOfImage;
            public UNICODE_STRING32 FullDllName;
            public UNICODE_STRING32 BaseDllName;
            public uint Flags;
            public ushort LoadCount;
            public ushort TlsIndex;
            public LIST_ENTRY32 HashLinks;
            public uint TimeDateStamp;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PEB32
        {
            public byte InheritedAddressSpace;
            public byte ReadImageFileExecOptions;
            public byte BeingDebugged;
            public byte BitField;
            public uint Mutant;
            public uint ImageBaseAddress;
            public uint Ldr; //PPEB_LDR_DATA32
            public uint ProcessParameters;
            public uint SubSystemData;
            public uint ProcessHeap;
            public uint FastPebLock;
            public uint AtlThunkSListPtr;
            public uint IFEOKey;
            public uint CrossProcessFlags;
            public uint UserSharedInfoPtr;
            public uint SystemReserved;
            public uint AtlThunkSListPtr32;
            public uint ApiSetMap;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING32
        {
            public ushort Length;
            public ushort MaximumLength;
            public uint Buffer;
        }
    }
}
