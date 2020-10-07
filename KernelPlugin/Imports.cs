using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KernelPlugin
{
    class Imports
    {
        [DllImport("kernel32.dll")]
        public static extern void OutputDebugString(string lpOutputString);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(string filename, FileAccess access, FileShare sharing, IntPtr SecurityAttributes, FileMode mode, FileOptions options, IntPtr template);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DeviceIoControl(
                                                IntPtr hDevice,
                                                uint dwIoControlCode,
                                                IntPtr InBuffer,
                                                int nInBufferSize,
                                                IntPtr OutBuffer,
                                                int nOutBufferSize,
                                                out int pBytesReturned,
                                                IntPtr lpOverlapped);
    }
}
