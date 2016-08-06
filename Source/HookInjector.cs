using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace BuildProductive
{
    public class HookInjector : MonoBehaviour
    {
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

        private static readonly string MessagePrefix = "HookInjector: ";

        private List<PatchInfo> _patches = new List<PatchInfo>();

        private IntPtr _memPtr;
        private uint _memSize;
        private int _offset;

        public HookInjector()
        {
            _memPtr = Platform.AllocRWE();

            if (_memPtr == IntPtr.Zero)
            {
                Error("No memory allocated, injector disabled.");
            }

            _memSize = Platform.PageSize;

            var p1 = typeof(Command).GetMethod("ProcessInput").MethodHandle.GetFunctionPointer().ToInt64();
            var p2 = typeof(Command).GetMethod("get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer().ToInt64();
            var p3 = typeof(GizmoGridDrawer).GetMethod("DrawGizmoGrid", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();
            var p4 = typeof(PostLoadInitter).GetMethod("DoAllPostLoadInits", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();
            var p5 = Bootstrapper.InspectGizmoGrid.GetMethod("DrawInspectGizmoGridFor", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();

            Message("ProcessInput: {0:X}, IconDrawColor: {1:X}, DrawGizmoGrid: {2:X}, DoAllPostLoadInits: {3:X}, DrawInspectGizmoGridFor: {4:X}", p1, p2, p3, p4, p5);

        }

        public void AddPatch(Type sourceType, string sourceName, Type targetType, string targetName)
        {
            var pi = new PatchInfo();

            pi.SourceMethod = sourceType.GetMethod(sourceName);
            if (pi.SourceMethod == null) pi.SourceMethod = sourceType.GetMethod(sourceName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.SourceMethod == null)
            {
                Error("Source method not found");
                return;
            }

            pi.TargetMethod = targetType.GetMethod(targetName);
            if (pi.TargetMethod == null) pi.TargetMethod = targetType.GetMethod(targetName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.TargetMethod == null)
            {
                Error("Target method not found");
                return;
            }

            var sourcePtr = pi.SourceMethod.MethodHandle.GetFunctionPointer();
            var targetPtr = pi.TargetMethod.MethodHandle.GetFunctionPointer();

            pi.Is64 = IntPtr.Size == 8;

            if (pi.Is64)
            {
                pi.SourceAddress64 = sourcePtr.ToInt64();
                pi.TargetAddress64 = targetPtr.ToInt64();
                pi.HookAddress64 = _memPtr.ToInt64() + _offset;
            }
            else
            {
                pi.SourceAddress32 = sourcePtr.ToInt32();
                pi.TargetAddress32 = targetPtr.ToInt32();
                pi.HookAddress32 = _memPtr.ToInt32() + _offset;

            }

            _patches.Add(pi);
            Message("MethodCallPatcher: Patching {0}.{1} with {2}.{3}.", sourceType.Name, pi.SourceMethod.Name, targetType.Name, pi.TargetMethod.Name);

            //InjectHook32(pi);
            InjectHook64(pi);
        }

        private unsafe long FindEndAddress(long procAddress)
        {
            var p = (byte*)procAddress;

            uint headerHi = 0xEC8B4855; // pushq %rbp; movq %rsp, %rbp
            var isHeaderStatic = false;

            byte subq8 = 0;
            uint subq32 = 0;
            var isSubq32 = false;

            long retPos = 0;
            var zeroCount = 0;
            var retSearchLimit = 60;

            var dw = (uint*)p;

            if (*dw == headerHi)
            {
                Error("Instance headers are untested and disabled, please report.");
                return 0;

                p += 4;
            }
            // subq $imm8, %rsp
            else if (p[0] == 0x48 && p[1] == 0x83 && p[2] == 0xEC)
            {
                Message("Found subq imm8");
                isHeaderStatic = true;

                subq8 = p[3];

                p += 4;
            }
            // subq $imm32, %rsp
            else if (p[0] == 0x48 && p[1] == 0x81 && p[2] == 0xEC)
            {
                Message("Found subq imm32");
                isHeaderStatic = true;

                dw = (uint*)(p + 3);
                subq32 = *dw;
                isSubq32 = true;

                p += 7;
            }
            else
            {
                Error("Unsupported method header.");
                return 0;
            }

            try
            {
                while (true)
                {
                    if (isHeaderStatic)
                    {
                        if (isSubq32)
                        {
                            dw = (uint*)(p + 3);
                            if (p[0] == 0x48 && p[1] == 0x81 && p[2] == 0xC4 && *dw == subq32 && p[7] == 0xC3)
                            {
                                Message("Found addq imm32.");
                                retPos = (long)(p + 7);
                                break;
                            }
                        }
                        else
                        {
                            if (p[0] == 0x48 && p[1] == 0x83 && p[2] == 0xC4 && p[3] == subq8 && p[4] == 0xC3)
                            {
                                Message("Found addq imm8.");
                                retPos = (long)(p + 4);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (p[0] == 0xC9 && p[1] == 0xC3)
                        {
                            Message("Return found.");
                            retPos = (long)p + 1;
                            p += 2;
                            continue;
                        }

                        if (retPos > 0)
                        {
                            if ((long)(p - retPos) > retSearchLimit)
                            {
                                Error("Hit search limit.");
                                retPos = 0;
                                break;
                            }

                            if (p[0] == 0)
                            {
                                zeroCount++;
                                if (zeroCount > 7)
                                {
                                    Message("Zeroes found.");
                                    break;
                                }
                            }
                            else
                            {
                                zeroCount = 0;

                                dw = (uint*)p;
                                if (*dw == headerHi)
                                {
                                    Message("Next proc header found.");
                                    break;
                                }
                            }
                        }
                    }

                    p++;
                }
            }
            catch (NullReferenceException)
            {
                Message("Unallocated memory reached.");
            }

            if (retPos > 0)
            {
                var end = retPos + 1;
                Message("Method length: " + (end - procAddress));
                return end;
            }
            else
            {
                Error("Method length not found.");
            }

            return 0;
        }

        private unsafe byte[] CopyStackAlloc(long procAddress)
        {
            var p = (byte*)procAddress;

            var i = 0;
            // look for subq rsp
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

        private unsafe bool WriteHookJump(long procAddress, long hookAddress, int stackAllocSize = 0)
        {
            var p = (byte*)procAddress;

            int targetAddress = 0;

            try
            {
                targetAddress = Convert.ToInt32(hookAddress - procAddress - 5);
            }
            catch (OverflowException)
            {
                Error("The methods are too far apart to be encoded in rel32.");
                return false;
            }

            *p = 0xE9;
            var dw = (int*)(p + 1);
            *dw = targetAddress;

            if (stackAllocSize > 0)
            {
                for (var i = 5; i < stackAllocSize; i++)
                {
                    p[i] = 0x90;
                }
            }

            return true;
        }

        private unsafe bool InjectHook64(PatchInfo pi)
        {
            Message("Source: {0:X}, Target: {1:X}, Hook: {2:X}", pi.SourceAddress64, pi.TargetAddress64, pi.HookAddress64);

            var endAddress = FindEndAddress(pi.TargetAddress64);
            if (endAddress == 0)return false;

            var stackAlloc = CopyStackAlloc(pi.SourceAddress64);

            if (stackAlloc.Length < 5)
            {
                Error("Stack alloc too small to be patched.");
                return false;
            }

            var result = WriteHookJump(pi.SourceAddress64, pi.HookAddress64, stackAlloc.Length);

            if (!result) return false;

            using (var w = new BinaryWriter(new UnmanagedMemoryStream(
                (byte*)pi.HookAddress64, _memSize - _offset, _memSize - _offset, FileAccess.Write)))
            {
                w.Write(new byte[] { 0x48, 0xB8 }); // movabsq $, %rax
                w.Write(pi.TargetAddress64);
                w.Write(new byte[] { 0x48, 0x39, 0x04, 0x24 }); // cmpq %rax, (%rsp)
                w.Write(new byte[] { 0x7C, 0x12 }); // jl hook

                w.Write(new byte[] { 0x48, 0xB8 }); // movabsq $, %rax
                w.Write(endAddress);
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

            Message("Successfull patched.");

            return true;
        }

        private void Message(string message)
        {
            Log.Message(MessagePrefix + message);
        }

        private void Message(string message, params object[] args)
        {
            Log.Message(String.Format(MessagePrefix + message, args));
        }

        private void Warning(string message)
        {
            Log.Warning(MessagePrefix + message);
        }

        private void Warning(string message, params object[] args)
        {
            Log.Warning(String.Format(MessagePrefix + message, args));
        }

        private void Error(string message)
        {
            Log.Error(MessagePrefix + message);
        }

        private void Error(string message, params object[] args)
        {
            Log.Error(String.Format(MessagePrefix + message, args));
        }
    }
}
