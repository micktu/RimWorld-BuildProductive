﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Verse;

namespace BuildProductive
{
    public static class Platform
    {
        private static uint _pageSize;

        public static uint PageSize
        {
            get { return _pageSize; }
        }

        public static IntPtr AllocRWE()
        {
            IntPtr ptr = IntPtr.Zero;

            if (IntPtr.Size == 8)
            {
                long addr;
                _pageSize = (uint)Platform.getpagesize();

                Platform.posix_memalign(out addr, _pageSize, _pageSize);
                var result = Platform.mprotect(addr, _pageSize, 0x7);

                if (result != 0)
                {
                    Log.Error(string.Format("mprotect() failed at {0:X16} (error {1}))", addr, Marshal.GetLastWin32Error()));
                }

                ptr = new IntPtr(addr);
                Log.Message(string.Format("Allocated {0} bytes at 0x{1:X}.", _pageSize, addr));
            }
            else
            {
                _pageSize = 0x1000; // FIXME Maybe
                ptr = Platform.VirtualAllocEx(Process.GetCurrentProcess().Handle, IntPtr.Zero, _pageSize, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            }

            return ptr;
        }

        [DllImport("libc")]
        public static extern int getpagesize();

        [DllImport("libc")]
        public static extern IntPtr posix_memalign(out long memptr, uint alignment, uint size);

        [DllImport("libc", SetLastError = true)]
        public static extern int mprotect(long addr, uint len, int prot);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

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
    }
}

