using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Verse;

namespace BuildProductive.Injection
{
    public class HookInjector
    {
        public struct PatchInfo
        {
            public Type SourceType;
            public Type TargetType;

            public MethodInfo SourceMethod;
            public MethodInfo TargetMethod;

            public int TargetSize;

            public IntPtr SourcePtr;
            public IntPtr TargetPtr;
        }

        private static readonly string MessagePrefix = "HookInjector: ";

        private List<PatchInfo> _patches = new List<PatchInfo>();

        private IntPtr _memPtr;
        private long _offset;

        private bool _isInitialized;

        public HookInjector()
        {
            _memPtr = Platform.AllocRWE();

            if (_memPtr == IntPtr.Zero)
            {
                Error("No memory allocated, injector disabled.");
                return;
            }

            // Patching has to be done after other mods (e.g. CCL) do it in order to handle rerouting
            LongEventHandler.QueueLongEvent(PatchAll, "HookInjector_PatchAll", false, null);
        }

        public void Inject(Type sourceType, string sourceName, Type targetType, string targetName = "")
        {
            if (targetName.Length < 1)
            {
                targetName = sourceType.Name + "_" + sourceName;
            }

            var pi = new PatchInfo();

            pi.SourceType = sourceType;
            pi.TargetType = targetType;

            pi.SourceMethod = sourceType.GetMethod(sourceName);
            if (pi.SourceMethod == null) pi.SourceMethod = sourceType.GetMethod(sourceName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.SourceMethod == null) pi.SourceMethod = sourceType.GetMethod(sourceName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (pi.SourceMethod == null)
            {
                Error("Source method {0}.{1} not found", sourceType.Name, sourceName);
                return;
            }

            pi.TargetMethod = targetType.GetMethod(targetName);
            if (pi.TargetMethod == null) pi.TargetMethod = targetType.GetMethod(targetName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.TargetMethod == null) pi.TargetMethod = targetType.GetMethod(targetName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (pi.TargetMethod == null)
            {
                Error("Target method {0}.{1} not found", targetType.Name, targetName);
                return;
            }

            pi.SourcePtr = pi.SourceMethod.MethodHandle.GetFunctionPointer();
            pi.TargetPtr = pi.TargetMethod.MethodHandle.GetFunctionPointer();

            pi.TargetSize = Platform.GetJitMethodSize(pi.TargetPtr);

            _patches.Add(pi);
            if (_isInitialized) Patch(pi);
        }

        void PatchAll()
        {
            foreach(var pi in _patches) Patch(pi);
            _isInitialized = true;
        }

        private bool Patch(PatchInfo pi)
        {
            var hookPtr = new IntPtr(_memPtr.ToInt64() + _offset);

            Message("Patching via hook @ {0:X}:", hookPtr.ToInt64());
            Log.Message(String.Format("    Source: {0}.{1} @ {2:X}", pi.SourceType.Name, pi.SourceMethod.Name, pi.SourcePtr.ToInt64()));
            Log.Message(String.Format("    Target: {0}.{1} @ {2:X}", pi.TargetType.Name, pi.TargetMethod.Name, pi.TargetPtr.ToInt64()));

            var s = new AsmHelper(hookPtr);

            // Main proc
            s.WriteJmp(pi.TargetPtr);
            var mainPtr = s.ToIntPtr();

            var src = new AsmHelper(pi.SourcePtr);

            // Check if already patched
            var isAlreadyPatched = false;
            var jmpLoc = src.PeekJmp();
            if (jmpLoc != 0)
            {
                Warning("Method already patched, rerouting.");
                pi.SourcePtr = new IntPtr(jmpLoc);
                isAlreadyPatched = true;
            }

            // Jump to detour if called from outside of detour
            var startAddress = pi.TargetPtr.ToInt64();
            var endAddress = startAddress + pi.TargetSize;

            s.WriteMovImmRax(startAddress);
            s.WriteCmpRaxRsp();
            s.WriteJl8(hookPtr);

            s.WriteMovImmRax(endAddress);
            s.WriteCmpRaxRsp();
            s.WriteJg8(hookPtr);

            if (isAlreadyPatched)
            {
                src.WriteJmp(mainPtr);
                s.WriteJmp(pi.SourcePtr);
            }
            else
            {
                // Copy source proc stack alloc instructions
                var stackAlloc = src.PeekStackAlloc();

                if (stackAlloc.Length < 5)
                {
                    Warning("Stack alloc too small to be patched, attempting full copy.");

                    var size = (Platform.GetJitMethodSize(pi.SourcePtr));
                    var bytes = new byte[size];
                    Marshal.Copy(pi.SourcePtr, bytes, 0, size);
                    s.Write(bytes);

                    // Write jump to main proc in source proc
                    src.WriteJmp(mainPtr);
                }
                else
                {
                    s.Write(stackAlloc);
                    s.WriteJmp(new IntPtr(pi.SourcePtr.ToInt64() + stackAlloc.Length));

                    // Write jump to main proc in source proc
                    if (stackAlloc.Length < 12) src.WriteJmpRel32(mainPtr);
                    else src.WriteJmp(mainPtr);

                    var srcOffset = (int)(src.ToInt64() - pi.SourcePtr.ToInt64());
                    src.WriteNop(stackAlloc.Length - srcOffset);
                }
            }

            s.WriteLong(0);

            _offset = s.ToInt64() - _memPtr.ToInt64();

            Message("Successfully patched.");
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
