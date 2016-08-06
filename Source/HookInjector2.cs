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

        [DllImport("libc")]
        static extern int getpagesize();

        [DllImport("libc")]
        static extern IntPtr posix_memalign(out long memptr, uint alignment, uint size);

        [DllImport("libc", SetLastError = true)]
        static extern int mprotect(long addr, uint len, int prot);


        private List<PatchInfo> _patches = new List<PatchInfo>();

        private IntPtr _buffer;

        uint _pageSize;

        private int _offset;

        public HookInjector2()
        {
            if (IntPtr.Size == 8)
            {
                long addr;
                _pageSize = (uint)getpagesize();

                posix_memalign(out addr, _pageSize, _pageSize);
                var result = mprotect(addr, _pageSize, 0x7);

                if (result != 0)
                {
                    Log.Message(string.Format("mprotect() failed at {0:X16} (error {1}))", addr, Marshal.GetLastWin32Error()));
                }

                _buffer = new IntPtr(addr);
                Log.Message(string.Format("HookInjector: Allocated {0} bytes at {1:X}.", _pageSize, addr));
            }
            else
            {
                _buffer = VirtualAllocEx(Process.GetCurrentProcess().Handle, IntPtr.Zero, _pageSize, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            }
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

            pi.Is64 = IntPtr.Size == 8;

            var p1 = typeof(Command).GetMethod("ProcessInput").MethodHandle.GetFunctionPointer().ToInt64();
            var p2 = typeof(Command).GetMethod("get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer().ToInt64();
            var p3 = typeof(GizmoGridDrawer).GetMethod("DrawGizmoGrid", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();
            var p4 = typeof(PostLoadInitter).GetMethod("DoAllPostLoadInits", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();
            var p5 = Bootstrapper.InspectGizmoGrid.GetMethod("DrawInspectGizmoGridFor", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();

            if (pi.Is64)
            {
                pi.SourceAddress64 = sourcePtr.ToInt64();
                pi.TargetAddress64 = targetPtr.ToInt64();
                pi.HookAddress64 = _buffer.ToInt64() + _offset;

                Log.Message(String.Format("ProcessInput: {0:X}, IconDrawColor: {1:X}, DrawGizmoGrid: {2:X}, DoAllPostLoadInits: {3:X}, DrawInspectGizmoGridFor: {4:X}", p1, p2, p3, p4, p5));
            }
            else
            {
                pi.SourceAddress32 = sourcePtr.ToInt32();
                pi.TargetAddress32 = targetPtr.ToInt32();
                pi.HookAddress32 = _buffer.ToInt32() + _offset;

                Log.Message(String.Format("ProcessInput: {0:X}, IconDrawColor: {1:X}, DrawGizmoGrid: {2:X}, DoAllPostLoadInits: {3:X}, DrawInspectGizmoGridFor: {4:X}", p1, p2, p3, p4, p5));
            }

            _patches.Add(pi);
            Log.Message("MethodCallPatcher: Want to patch " + pi.TargetMethod.Name + " in " + pi.SourceMethod.Name + ".");

            //InjectHook32(pi);
            InjectHook64(pi);
        }

        private unsafe long FindEndAddress(long procAddress)
        {
            var p = (byte*)procAddress;

            try
            {
                while (true)
                {
                    var dw = (int*)p;

                    // LEAVE, RETN, 00, 00
                    if (*dw == 0xc3c9)
                    {
                        p += 2;
                        break;
                    }
                    if (p[0] == 0x48 && p[1] == 0x83 && p[2] == 0xC4 && p[4] == 0xC3)
                    {
                        p += 5;
                        break;
                    }

                    // POP, LEAVE, RETN
                    /*if (p[0] > 0x57 && p[0] < 0x60 &&
                        p[1] == 0xC9 && (p[2] == 0xC3 || p[2] == 0xC2))
                    {
                        p += 3;
                        break;
                    }
        */
                    p++;
                }
            }
            catch (NullReferenceException)
            {
                Log.Warning("null pointer");
            }

            Log.Message("method length: " + ((long)p - procAddress));

            return (long)p;
        }

        private unsafe byte[] CopyStackAlloc(long procAddress)
        {
            var p = (byte*)procAddress;

            var i = 0;
            // look for sub esp or subq rsp
            while (true)
            {
                if (p[i] == 0x83 && p[i + 1] == 0xEC)
                {
                    i += 2;
                    break;
                }
                else if (p[i] == 0x81 && p[i + 1] == 0xEC)
                {
                    i += 5;
                    break;
                }

                i++;
            }

            var bytes = new byte[i + 1];
            Marshal.Copy(new IntPtr(procAddress), bytes, 0, i + 1);

            return bytes;
        }

        private unsafe void WriteHookJump(long procAddress, long hookAddress, int stackAllocSize = 0)
        {
            var p = (byte*)procAddress;

            *p = 0xE9;
            var dw = (int*)(p + 1);
            *dw = Convert.ToInt32(hookAddress - procAddress - 5);

            if (stackAllocSize > 0)
            {
                for (var i = 5; i < stackAllocSize; i++)
                {
                    p[i] = 0x90;
                }
            }
        }

        private unsafe bool InjectHook64(PatchInfo pi)
        {
            Log.Message(string.Format("Source: {0:X}, Target: {1:X}, Hook: {2:X}",
                                      pi.SourceAddress64, pi.TargetAddress64, pi.HookAddress64));

            var stackAlloc = CopyStackAlloc(pi.SourceAddress64);
            WriteHookJump(pi.SourceAddress64, pi.HookAddress64, stackAlloc.Length);

            using (var w = new BinaryWriter(new UnmanagedMemoryStream(
                (byte*)pi.HookAddress64, _pageSize - _offset, _pageSize - _offset, FileAccess.Write)))
            {
                w.Write(new byte[] { 0x48, 0xB8 }); // movabsq $, %rax
                w.Write(pi.TargetAddress64);
                w.Write(new byte[] { 0x48, 0x39, 0x04, 0x24 }); // cmpq %rax, (%rsp)
                w.Write(new byte[] { 0x7C, 0x12 }); // jl hook

                w.Write(new byte[] { 0x48, 0xB8 }); // movabsq $, %rax
                w.Write(FindEndAddress(pi.TargetAddress64));
                w.Write(new byte[] { 0x48, 0x39, 0x04, 0x24 }); // cmpq %rax, (%rsp)
                w.Write(new byte[] { 0x7F, 0x02 }); // jg hook
                w.Write(new byte[] { 0xEB, 0x0C }); // jmp stackAlloc

                // hook:
                w.Write(new byte[] { 0x48, 0xB8 }); // movabsq $, %rax
                w.Write(pi.TargetAddress64);
                w.Write(new byte[] { 0xFF, 0xE0 }); // jmpq *%rax

                // stackAlloc:
                w.Write(stackAlloc);
                w.Write(new byte[] { 0x48, 0xB8 }); // movabsq $, %rax
                w.Write(pi.SourceAddress64 + stackAlloc.Length);
                w.Write(new byte[] { 0xFF, 0xE0 }); // jmpq *%rax

                w.Write(0);
                w.Write(0);

                _offset += (int)w.BaseStream.Position;
            }

            return true;
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
