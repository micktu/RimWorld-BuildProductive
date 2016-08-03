using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BuildProductive
{
    class MethodCallPatcher : Thing
    {
        public struct PatchInfo
        {
            public MethodInfo SourceMethod;
            public MethodInfo TargetMethod;
            public MethodInfo ReplacementMethod;

            public int SourceAddress;
            public int TargetAddress;
            public int ReplacementAddress;
        }

        private List<PatchInfo> _patches = new List<PatchInfo>();

        public override void Tick()
        {
            for (var i = 0; i < _patches.Count; i++)
            {
                if (TryPatch(_patches[i]))
                {
                    _patches.RemoveAt(i);
                    i--;
                }
            }
        }

        public void AddPatch(Type sourceType, string sourceName, Type targetType, string targetName, Type replacementType, string replacementName)
        {
            var pi = new PatchInfo();

            pi.SourceMethod = sourceType.GetMethod(sourceName);
            pi.TargetMethod = targetType.GetMethod(targetName);
            pi.ReplacementMethod = replacementType.GetMethod(replacementName, BindingFlags.Static | BindingFlags.NonPublic);
            //pi.ReplacementMethod = replacementType.GetMethod(replacementName);

            pi.SourceAddress = pi.SourceMethod.MethodHandle.GetFunctionPointer().ToInt32();
            pi.TargetAddress = pi.TargetMethod.MethodHandle.GetFunctionPointer().ToInt32();
            pi.ReplacementAddress = pi.ReplacementMethod.MethodHandle.GetFunctionPointer().ToInt32();

            _patches.Add(pi);
            Log.Message("MethodCallPatcher: Want to patch " + pi.TargetMethod.Name + " in " + pi.SourceMethod.Name + ".");
        }

        unsafe private bool TryPatch(PatchInfo pi)
        {
            var isPatched = false;

            var ptr = (byte*)pi.SourceAddress;
            int* leavePtr = (int*)0;

            do
            {
                if (*ptr == 0xe8)
                {
                    var offsetPtr = (int*)(ptr + 1);
                    var address = (int)ptr + 5 + *offsetPtr;

                    if (address == pi.TargetAddress)
                    {
                        Log.Message("MethodCallPatcher: Patched call to " + pi.TargetMethod.Name + " in " + pi.SourceMethod.Name + ".");
                        *offsetPtr = pi.ReplacementAddress - (int)ptr - 5;
                        isPatched = true;
                    }
                }

                ptr++;
                leavePtr = (int*)ptr;
            } while (*leavePtr != 0xc3c9);

            return isPatched;
        }
    }
}
