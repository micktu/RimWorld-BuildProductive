using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Verse;

namespace BuildProductive
{
    public struct AsmStream
    {
        private long _value;

        public bool Is64
        {
            get { return IntPtr.Size == 8; }
        }

        static AsmStream()
        {
            var methods = typeof(AsmStream).GetMethods();

            foreach (var m in methods)
            {
                m.MethodHandle.GetFunctionPointer();
            }
        }

        public AsmStream(IntPtr ptr) : this(ptr.ToInt64()) { }

        public AsmStream(long address)
        {
           
            _value = address;
        }

        public void WriteMovXaxImm(long value)
        {
            WriteRexW();
            WriteByte(0xB8);
            WriteIntOrLong(value);
        }

        public void WriteJmp(long address)
        {
            if (Is64)
            {
                WriteMovXaxImm(address);
                Write(new byte[] { 0xFF, 0xE0 }); // jmpq *%rax
            }
            else WriteJmpRel32(address);
        }

        public void WriteCallRel32(long address)
        {
            var offset = Convert.ToInt32(address - _value - 5);
            WriteByte(0xE8); // CALL $
            WriteInt(offset);
        }

        public void WriteJmpRel32(long address)
        {
            var offset = Convert.ToInt32(address - _value - 5);
            WriteByte(0xE9); // JMP $
            WriteInt(offset);
        }

        public void WriteCmpXspXax()
        {
            WriteRexW();
            Write(new byte[] { 0x39, 0x04, 0x24 });
        }

        public void WriteJmp8(long address)
        {
            var offset = Convert.ToSByte(address - _value - 2);
            WriteByte(0xEB);
            WriteByte((byte)offset);
        }


        public void WriteJl8(long address)
        {
            var offset = Convert.ToSByte(address - _value - 2);
            WriteByte(0x7C);
            WriteByte((byte)offset);
        }

        public void WriteJg8(long address)
        {
            var offset = Convert.ToSByte(address - _value - 2);
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

        public void WriteHexString(string hexString)
        {
            hexString = Regex.Replace(hexString, @"\s+", "");
            Write(StringToByteArrayFastest(hexString));
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

        public void WriteIntOrLong(long value)
        {
            if (Is64) WriteLong(value);
            else WriteInt((int)value);
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

        public static AsmStream operator +(AsmStream p, long offset)
        {
            return new AsmStream((long)p + offset);
        }

        public static explicit operator long(AsmStream p)
        {
            return p.ToInt64();
        }

        public long ToInt64()
        {
            return _value;
        }

        // http://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
