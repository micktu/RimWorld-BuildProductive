using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BuildProductive
{
    class MethodCallPatcher : MonoBehaviour
    {
        public struct PatchInfo
        {
            public MethodInfo SourceMethod;
            public MethodInfo TargetMethod;
            public MethodInfo ReplacementMethod;

			public bool Is64;

            public int SourceAddress32;
            public int TargetAddress32;
            public int ReplacementAddress32;

			public long SourceAddress64;
			public long TargetAddress64;
			public long ReplacementAddress64;
		}

		public static readonly float ChecksPerSecond = 10f;

		private float _counter;

        private List<PatchInfo> _patches = new List<PatchInfo>();

        void FixedUpdate()
        {
			var interval = 1f / ChecksPerSecond;

			if (_counter >= interval)
			{
				_counter -= interval;

				for (var i = 0; i < _patches.Count; i++)
				{
					var pi = _patches[i];
					if (pi.Is64 ? TryPatch64(pi) : TryPatch32(pi))
					{
						Log.Message("MethodCallPatcher: Patched call to " + pi.TargetMethod.Name + " in " + pi.SourceMethod.Name + ".");

						_patches.RemoveAt(i);
						i--;
					}
				}
			}

			_counter += Time.fixedDeltaTime;
        }

        public void AddPatch(Type sourceType, string sourceName, Type targetType, string targetName, Type replacementType, string replacementName)
        {
            var pi = new PatchInfo();

            pi.SourceMethod = sourceType.GetMethod(sourceName);
            pi.TargetMethod = targetType.GetMethod(targetName);

            pi.ReplacementMethod = replacementType.GetMethod(replacementName, BindingFlags.Static | BindingFlags.NonPublic);
			//pi.ReplacementMethod = replacementType.GetMethod(replacementName);

			var sourcePtr = pi.SourceMethod.MethodHandle.GetFunctionPointer();
			var targetPtr = pi.TargetMethod.MethodHandle.GetFunctionPointer();
			var replacementPtr = pi.ReplacementMethod.MethodHandle.GetFunctionPointer();

			if (IntPtr.Size == 8) pi.Is64 = true;

			if (pi.Is64)
			{
				pi.SourceAddress64 = sourcePtr.ToInt64();
				pi.TargetAddress64 = targetPtr.ToInt64();
				pi.ReplacementAddress64 = replacementPtr.ToInt64();
			}
			else
			{
				pi.SourceAddress32 = sourcePtr.ToInt32();
				pi.TargetAddress32 = targetPtr.ToInt32();
				pi.ReplacementAddress32 = replacementPtr.ToInt32();
			}

			_patches.Add(pi);
            Log.Message("MethodCallPatcher: Want to patch " + pi.TargetMethod.Name + " in " + pi.SourceMethod.Name + ".");

			if (pi.Is64)
			{
				Log.Message(string.Format("Platform is x64. Source address: {0:X16}, target address: {1:X16}, replacement address:D {2:X16}",
										  pi.SourceAddress64, pi.TargetAddress64, pi.ReplacementAddress64));
			}
			else
			{
				Log.Message(string.Format("Platform is x86. Source address: {0:X8}, target address: {1:X8}, replacement address:D {2:X8}",
							  pi.SourceAddress32, pi.TargetAddress32, pi.ReplacementAddress32));
			}

		}

        private unsafe bool TryPatch32(PatchInfo pi)
        {
            var isPatched = false;

			var ptr = (byte*)pi.SourceAddress32;

            int* leavePtr = (int*)0;

            do
            {
				// CALL rel32 opcode
				if (*ptr == 0xe8)
                {
					var offsetPtr = (int*)(ptr + 1);

					if ((int)ptr + 5 + *offsetPtr == pi.TargetAddress32)
                    {
						*offsetPtr = pi.ReplacementAddress32 - (int)ptr - 5;
						isPatched = true;
					}
                }

				ptr++;
				// check for LEAVE, RETN, zero padding sequencee
				leavePtr = (int*)ptr;
			} while (*leavePtr != 0xc3c9);

			return isPatched;
        }

		private unsafe bool TryPatch64(PatchInfo pi)
		{
			var isPatched = false;

			var ptr = (byte*)(pi.SourceAddress64);

			int* leavePtr = (int*)0;

			do
			{
				// MOVABSQ $, %r11 opcode
				if (ptr[0] == 0x49 && ptr[1] == 0xbb)
				{
					var offsetPtr = (long*)(ptr + 2);

					if (*offsetPtr == pi.TargetAddress64)
					{
						*offsetPtr = pi.ReplacementAddress64;
						isPatched = true;
					}
				}

				ptr++;
				// check for LEAVE, RETN, zero padding sequencee
				leavePtr = (int*)ptr;
			} while (*leavePtr != 0xc3c9);

			return isPatched;
		}
    }
}
