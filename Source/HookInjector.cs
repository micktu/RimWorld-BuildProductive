using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BuildProductive
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

            // The reason is doing it after CCL does it
            LongEventHandler.QueueLongEvent(PatchAll, "HookInjector_PatchAll", false, null);
            
            /*
            var p1 = typeof(Command).GetMethod("ProcessInput").MethodHandle.GetFunctionPointer().ToInt64();
            var p2 = typeof(Command).GetMethod("get_IconDrawColor", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer().ToInt64();
            var p3 = typeof(GizmoGridDrawer).GetMethod("DrawGizmoGrid", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();
            var p4 = typeof(PostLoadInitter).GetMethod("DoAllPostLoadInits", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();
            var p5 = Bootstrapper.InspectGizmoGrid.GetMethod("DrawInspectGizmoGridFor", BindingFlags.Static | BindingFlags.Public).MethodHandle.GetFunctionPointer().ToInt64();

            Message("ProcessInput: {0:X}, IconDrawColor: {1:X}, DrawGizmoGrid: {2:X}, DoAllPostLoadInits: {3:X}, DrawInspectGizmoGridFor: {4:X}", p1, p2, p3, p4, p5);
            */
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
            if (pi.SourceMethod == null)
            {
                Error("Source method {0}.{1} not found", sourceType.Name, sourceName);
                return;
            }

            pi.TargetMethod = targetType.GetMethod(targetName);
            if (pi.TargetMethod == null) pi.TargetMethod = targetType.GetMethod(targetName, BindingFlags.Static | BindingFlags.NonPublic);
            if (pi.TargetMethod == null)
            {
                Error("Target method {0}.{1} not found", targetType.Name, targetName);
                return;
            }

            pi.SourcePtr = pi.SourceMethod.MethodHandle.GetFunctionPointer();
            pi.TargetPtr = pi.TargetMethod.MethodHandle.GetFunctionPointer();

            pi.TargetSize = Platform.GetJitMethodSize(pi.TargetPtr);

            if (_isInitialized) Patch(pi);
            else _patches.Add(pi);
        }

        void PatchAll()
        {
            foreach(var pi in _patches) Patch(pi);
            _patches.Clear();
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

            // Copy source proc stack alloc instructions
            var src = new AsmHelper(pi.SourcePtr);

            var isAlreadyPatched = false;
            var jmpLoc = src.PeekJmp();
            if (jmpLoc != 0)
            {
                Warning("Method already patched, rerouting.");
                pi.SourcePtr = new IntPtr(jmpLoc);
                isAlreadyPatched = true;
            }

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
                var stackAlloc = src.PeekStackAlloc();

                if (stackAlloc.Length < 5)
                {
                    Error("Stack alloc too small to be patched, aborting.");
                    return false;
                }
                s.Write(stackAlloc);
                s.WriteJmp(new IntPtr(pi.SourcePtr.ToInt64() + stackAlloc.Length));

                // Write jump to main proc in source proc
                src.WriteJmpRel32(mainPtr);
                var srcOffset = (int)(src.ToInt64() - pi.SourcePtr.ToInt64());
                src.WriteNop(stackAlloc.Length - srcOffset);
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
