using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace BuildProductive
{
    public class HookInjector2 : MonoBehaviour
    {
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

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

        public struct PatchInfo
        {
            public MethodInfo SourceMethod;
            public MethodInfo TargetMethod;

            public bool Is64;

            public int SourceAddress32;
            public int TargetAddress32;
            public int HookAddress32;

            public long SourceAddress64;
            public long TargetAddress64;
            public long HookAddress64;
        }

        private List<PatchInfo> _patches = new List<PatchInfo>();

        private IntPtr _buffer;

        private int _offset;

        public HookInjector2()
        {
            _buffer = VirtualAllocEx(Process.GetCurrentProcess().Handle, IntPtr.Zero, 0x400, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
        }

        public void AddPatch(Type sourceType, string sourceName, Type targetType, string targetName)
        {
            var pi = new PatchInfo();

            pi.SourceMethod = sourceType.GetMethod(sourceName);
            if (pi.SourceMethod == null) pi.SourceMethod = sourceType.GetMethod(sourceName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.SourceMethod == null) Log.Warning("Source method not found");

            pi.TargetMethod = targetType.GetMethod(targetName);
            if (pi.TargetMethod == null) pi.TargetMethod = targetType.GetMethod(targetName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.TargetMethod == null) Log.Warning("Target method not found");

            var sourcePtr = pi.SourceMethod.MethodHandle.GetFunctionPointer();
            var targetPtr = pi.TargetMethod.MethodHandle.GetFunctionPointer();

            if (IntPtr.Size == 8) pi.Is64 = true;

            if (pi.Is64)
            {
                pi.SourceAddress64 = sourcePtr.ToInt64();
                pi.TargetAddress64 = targetPtr.ToInt64();
                pi.HookAddress64 = _buffer.ToInt64() + _offset;
            }
            else
            {
                pi.SourceAddress32 = sourcePtr.ToInt32();
                pi.TargetAddress32 = targetPtr.ToInt32();
                pi.HookAddress32 = _buffer.ToInt32() + _offset;
            }

            _patches.Add(pi);
            Log.Message("MethodCallPatcher: Want to patch " + pi.TargetMethod.Name + " in " + pi.SourceMethod.Name + ".");

            InjectHook32(pi);
        }

        private unsafe bool InjectHook32(PatchInfo pi)
        {
            var isPatched = false;

            var p = (byte*)pi.TargetAddress32;
            while(true)
            {
                var dw = (int*)p;
                // LEAVE, RETN, 00 00
                if (*dw == 0xc3c9) break;
                // POP, LEAVE, RETN
                if (p[0] > 0x57 && p[0] < 0x60 &&
                    p[1] == 0xC9 && (p[2] == 0xC3 || p[2] == 0xC2)) break;
                p++;
            }

            var endAddress = (int)p;
            Log.Message("method length: " + (endAddress - pi.TargetAddress32));

            using (var w = new BinaryWriter(new UnmanagedMemoryStream((byte*)pi.HookAddress32, 0x400, 0x400, FileAccess.Write)))
            {
                int* dwPtr;
                int stripSize;
                int addr;

                w.Seek(0, SeekOrigin.Begin);

                // Data
                w.Write(pi.TargetAddress32);
                w.Write(endAddress);
                w.Write(0);
                w.Write(0);

                // Override routing
                w.Write((byte)0x50);
                w.Write(new byte[] { 0x8B, 0x44, 0x24, 0x04 });
                w.Write(new byte[] { 0x39, 0x05 });
                w.Write(pi.HookAddress32 + 4);
                w.Write(new byte[] { 0x7C, 0x0A });
                w.Write(new byte[] { 0x39, 0x05 });
                w.Write(pi.HookAddress32 + 0);
                w.Write(new byte[] { 0x7F, 0x02 });
                w.Write(new byte[] { 0xEB, 0x06 });
                w.Write((byte)0x58);
                addr = pi.HookAddress32 + (int)w.BaseStream.Position + 5;
                w.Write((byte)0xE9);
                w.Write(pi.TargetAddress32 - addr);

                var src = (byte*)pi.SourceAddress32;

                // Beginning of source method
                w.Write((byte)0x58);
                var i = 0;
                while (true)
                {
                    w.Write(src[i]);

                    // SUB ESP imm8 opcode
                    if (src[i] == 0x83 && src[i + 1] == 0xEC)
                    {
                        i++;
                        w.Write(src[i]);
                        i++;
                        w.Write(src[i]);
                        break;
                    }
                    // SUB ESP imm32 opcode
                    else if (src[i] == 0x81 && src[i + 1] == 0xEC)
                    {
                        i++;
                        w.Write(src[i]);
                        i++;
                        w.Write(src[i]);
                        i++;
                        w.Write(src[i]);
                        i++;
                        w.Write(src[i]);
                        i++;
                        w.Write(src[i]);
                        break;
                    }

                    i++;
                }
                stripSize = i + 1;

                // Jump in source method
                addr = pi.HookAddress32 + 16;
                *src = 0xE9;
                dwPtr = (int*)(src + 1);
                *dwPtr = addr - pi.SourceAddress32 - 5;

                // NOP stripped instructions for debugger clarity
                for (i = 5; i < stripSize; i++)
                {
                    src[i] = 0x90;
                }

                // Jump to source method
                addr = pi.HookAddress32 + (int)w.BaseStream.Position + 5;
                w.Write((byte)0xE9);
                w.Write(pi.SourceAddress32 + stripSize - addr);

                _offset += (int)w.BaseStream.Position + 4;
            }

            isPatched = true;

            Log.Message(String.Format("buffer @ {0:X8}", _buffer.ToInt32()));
            
            return isPatched;
        }

    }
}
