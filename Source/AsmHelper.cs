using System;
using System.Runtime.InteropServices;

namespace BuildProductive
{
    public struct AsmHelper
    {
        private long _value;

        public bool Is64
        {
            get { return IntPtr.Size == 8; }
        }

        static AsmHelper()
        {
            var methods = typeof(AsmHelper).GetMethods();

            foreach (var m in methods)
            {
                m.MethodHandle.GetFunctionPointer();
            }
        }

        public AsmHelper(IntPtr ptr) : this(ptr.ToInt64()) { }

        public AsmHelper(long address)
        {
           
            _value = address;
        }

        public unsafe byte[] PeekStackAlloc()
        {
            var p = (byte*)_value;

            var i = 0;
            // look for sub esp, $ or subq $, %rsp
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
            Marshal.Copy(new IntPtr(_value), bytes, 0, i + 1);

            return bytes;
        }

        public unsafe long PeekJmp()
        {
            var p = (byte*)_value;

            if (p[0] == 0xE9)
            {
                var dp = (int*)(_value + 1);
                return (*dp + _value + 5);
            }
            else if (p[0] == 0x48 && p[1] == 0xB8 && p[10] == 0xFF && p[11] == 0xE0)
            {
                var lp = (long*)(_value + 2);
                return *lp;
            }

            return 0;
        }

        public void WriteMovImmRax(long value)
        {
            WriteMovImmRax(new IntPtr(value));
        }

        public void WriteMovImmRax(IntPtr ptr)
        {
            WriteRexW();
            WriteByte(0xB8);
            WriteIntPtr(ptr);
        }

        public void WriteJmp(IntPtr ptr)
        {
            if (Is64)
            {
                WriteMovImmRax(ptr.ToInt64());
                Write(new byte[] { 0xFF, 0xE0 }); // jmpq *%rax
            }
            else WriteJmpRel32(ptr);
        }

        public void WriteCallRel32(long address)
        {
            var offset = Convert.ToInt32(address - _value - 5);
            WriteByte(0xE8); // CALL $
            WriteInt(offset);
        }

        public void WriteJmpRel32(IntPtr ptr)
        {
            var offset = Convert.ToInt32(ptr.ToInt64() - _value - 5);
            WriteByte(0xE9); // JMP $
            WriteInt(offset);
        }

        public void WriteCmpRaxRsp()
        {
            WriteRexW();
            Write(new byte[] { 0x39, 0x04, 0x24 });
        }

        public void WriteJmp8(IntPtr ptr)
        {
            var offset = Convert.ToSByte(ptr.ToInt64() - _value - 2);
            WriteByte(0xEB);
            WriteByte((byte)offset);
        }


        public void WriteJl8(IntPtr ptr)
        {
            var offset = Convert.ToSByte(ptr.ToInt64() - _value - 2);
            WriteByte(0x7C);
            WriteByte((byte)offset);
        }

        public void WriteJg8(IntPtr ptr)
        {
            var offset = Convert.ToSByte(ptr.ToInt64() - _value - 2);
            WriteByte(0x7F);
            WriteByte((byte)offset);
        }

        public void WriteRexW()
        {
            if (Is64) WriteByte(0x48);
        }

        public void WriteNop(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                WriteByte(0x90);
            }
        }

        public unsafe void Write(byte[] bytes)
        {
            byte* p = (byte*)_value;

            foreach(var b in bytes)
            {
                *p = b;
                p++;
            }

            _value = (long)p;
        }

        public unsafe void WriteByte(byte n)
        {
            byte* p = (byte*)_value;
            *p = n;
            _value += 1;
        }

        public void WriteIntPtr(IntPtr value)
        {
            if (Is64) WriteLong(value.ToInt64());
            else WriteInt(value.ToInt32());
        }

        public unsafe void WriteInt(int n)
        {
            int* p = (int*)_value;
            *p = n;
            _value += 4;
        }

        public unsafe void WriteLong(long n)
        {
            long* p = (long*)_value;
            *p = n;
            _value += 8;
        }

        public long ToInt64()
        {
            return _value;
        }

        public IntPtr ToIntPtr()
        {
            return new IntPtr(_value);
        }

        public static explicit operator long(AsmHelper p)
        {
            return p.ToInt64();
        }

        public static explicit operator IntPtr(AsmHelper p)
        {
            return p.ToIntPtr();
        }
    }
}
