using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Verse;

namespace BuildProductive
{
    internal class MethodSizeHelper
    {
        private IntPtr _mem;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MethodSize
        {
            public int Address;
            public int Size;
        }

        public MethodSizeHelper()
        {
            _mem = Platform.AllocRWE();

            Platform.MemoryProtection prot;

            var addr = 0x100ED164; // mono_destroy_compile

            var result = Platform.VirtualProtect(new IntPtr(0x100ED164), 5, Platform.MemoryProtection.ExecuteReadWrite, out prot);

            var s = new AsmStream(_mem);
            s.WriteByte(0x60); // pushal
            s.WriteMovXaxImm(_mem.ToInt64());
            s.WriteHexString(String.Concat(
                "8B 5C 24 24", // mov ebx, dword ptr ss:[esp + 24]
                "8B 48 40", // mov ecx, dword ptr ds:[eax + 40]
                "8B 93 44 01 00 00", // mov edx, dword ptr ds:[ebx + 144]
                "89 54 C8 44", // mov dword ptr ds:[eax + ecx * 8 + 44], edx
                "8B 93 4C 01 00 00", // mov edx, dword ptr ds:[ebx + 14C]
                "89 54 C8 48", // mov dword ptr ds:[eax + ecx * 8 + 48], edx
                "FE 40 40", // inc byte ptr ds:[eax + 40]
                "61" // popal
            ));
            s.WriteJmpRel32(0x100B07FA); // jmp mono.mono_empty_compile

            s = new AsmStream(0x100ED164);
            s.WriteCallRel32(_mem.ToInt64());
        }

        public unsafe int GetMethodSize(long address)
        {
            int* p = (int*)(_mem.ToInt64() + 0x44);

            for (int i = 0; i < 256; i++)
            {
                var code = *p;
                p++;
                var size = *p;
                p++;

                Log.Message(String.Format("Address: {0:X}, size: {1}", code, size));

                if (code == address) return size;
            }

            return 0;
        }
    }
}
