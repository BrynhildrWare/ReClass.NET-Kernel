using Microsoft.Win32.SafeHandles;
using ReClassNET.Core;
using ReClassNET.Debugger;
using ReClassNET.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static KernelPlugin.Imports;
using static KernelPlugin.Structs;

namespace KernelPlugin
{
    public class KernelPluginExt : Plugin, ICoreProcessFunctions
    {
        private IPluginHost Host;
        private KernelDriver Driver;

        public override bool Initialize(IPluginHost Host)
        {
            Contract.Requires(Host != null);

            this.Host = Host ?? throw new ArgumentNullException(nameof(Host));

            Driver = new KernelDriver("\\\\.\\KernelClass");

            if (!Driver.Load())
            {
                //Driver.StopAndDelete();
                throw new ApplicationException("Driver cannot be loaded successfully");
            }

            Host.Process.CoreFunctions.RegisterFunctions("Kernel Driver by MagicDroidX", this);

            OutputDebugString("Kernel plugin loaded.");

            return true;
        }

        public override void Terminate()
        {
            Host = null;

            OutputDebugString("Kernel plugin unloaded.");
        }

        public bool AttachDebuggerToProcess(IntPtr id)
        {
            // Do nothing
            return false;
        }

        public bool AwaitDebugEvent(ref DebugEvent evt, int timeoutInMilliseconds)
        {
            // Do nothing
            return false;
        }

        public void CloseRemoteProcess(IntPtr process)
        {
            // BEFORE: Close the handle to the remote process.
            // NOW: We don't have a HANDLE so it is just a fake function
        }

        public void ControlRemoteProcess(IntPtr process, ControlRemoteProcessAction action)
        {
            // Do nothing
        }

        public void DetachDebuggerFromProcess(IntPtr id)
        {
            // Do nothing
        }

        public void EnumerateProcesses(EnumerateProcessCallback callbackProcess)
        {
            Process[] processes = Process.GetProcesses();
            for (int i = 0; i < processes.Length; i++)
            {
                var Process = processes[i];

                if (Process.Id == 0 || Process.Id == 4) continue;

                var Data = new EnumerateProcessData();
                Data.Id = new IntPtr(Process.Id);

                try
                {
                    Data.Name = Process.MainModule.ModuleName;
                    Data.Path = Process.MainModule.FileName;
                }
                catch (Exception)
                {
                    bool Fallback = Driver.GetProcessInfo(Process.Id, ref Data);
                    if (!Fallback)
                    {
                        Data.Name = Process.ProcessName;
                    }
                }
                callbackProcess.Invoke(ref Data);
            }
        }

        public void EnumerateRemoteSectionsAndModules(IntPtr process, EnumerateRemoteSectionCallback callbackSection, EnumerateRemoteModuleCallback callbackModule)
        {
            OutputDebugString("EnumerateRemoteSectionsAndModules");
            Driver.GetProcessModules(process.ToInt32(), ref callbackModule);
        }

        public void HandleDebugEvent(ref DebugEvent evt)
        {
            // Do nothing
        }

        public bool IsProcessValid(IntPtr pid/*process*/)
        {
            // BEFORE: Check if the handle is valid.
            // NOW: If is not null it is enough, we are using the PID now instead of a HANDLE
            return pid != IntPtr.Zero;
        }

        public IntPtr OpenRemoteProcess(IntPtr pid, ProcessAccess desiredAccess)
        {
            // BEFORE: Open the remote process with the desired access rights and return the handle to use with the other functions.
            // NOW: We are just returning the ID to the process instead. Now each methods takes care of resolving this ID (PID) to the respective process.
            // We need to do this to stop using privileged HANDLEs to the process
            return pid;
        }

        public bool ReadRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            bool Result = Driver.ReadMemory(process.ToInt32(), address, out byte[] Buffer, size, out _);
            if (Result) Buffer.CopyTo(buffer, offset);
            return Result;
        }

        public bool SetHardwareBreakpoint(IntPtr id, IntPtr address, HardwareBreakpointRegister register, HardwareBreakpointTrigger trigger, HardwareBreakpointSize size, bool set)
        {
            // Do nothing
            return false;
        }

        public bool WriteRemoteMemory(IntPtr process, IntPtr address, ref byte[] buffer, int offset, int size)
        {
            byte[] lpBuffer = new byte[size];
            Buffer.BlockCopy(buffer, offset, lpBuffer, 0, size);
            return Driver.WriteMemory(process.ToInt32(), address, lpBuffer, size, out _);
        }
    }
}
