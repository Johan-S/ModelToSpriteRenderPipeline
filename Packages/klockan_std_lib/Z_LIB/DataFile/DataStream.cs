using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

public class DataStream {

   public DataStream(Stream stream) {

      MemoryStream new_stream = new MemoryStream();
      long pos_in = stream.Position;
      stream.CopyTo(new_stream);
      stream.Position = pos_in;
      res = new List<byte>(new_stream.ToArray());

   }

   public DataStream() {

   }

   public bool ByteEquals(DataStream o) {
      return res.ElementEquals(o.res);
   }

   public DataStream(byte[] btes, int data_size = -1) {
      if (data_size == -1) {
         res = new List<byte>(btes);
      }  else {
         res = new List<byte>();
         for (int i = 0; i < data_size; ++i) res.Add(btes[i]);
      }
   }
   public DataStream GetReadStream() {
      var ds = new DataStream();
      ds.res = res;
      return ds;
   }

   public byte[] GetBytes() {
      return res.ToArray();
   }

   public void FinishSubWriter(DataStream sub_stream) {

   }

   List<byte> res = new List<byte>();


   public int ByteCount() {
      return res.Count;
   }

   int cursor;

   // integer
   public void Write(int a) {
      byte b1 = (byte)(a >> 0);
      byte b2 = (byte)(a >> 8);
      byte b3 = (byte)(a >> 16);
      byte b4 = (byte)(a >> 24);

      WriteByte(b1);
      WriteByte(b2);
      WriteByte(b3);
      WriteByte(b4);
   }

   public bool MaybeSkip(int a) {
      int i = cursor;
      if (Read(out int val)) {
         if (val == a) return true;
      }
      cursor = i;
      return false;
   }


   public bool Read(out int a) {
      byte b1 = ReadByte();
      byte b2 = ReadByte();
      byte b3 = ReadByte();
      byte b4 = ReadByte();

      a = b1 + (b2 << 8) + (b3 << 16) + (b4 << 24);

      return true;
   }


   // long
   public void Write(long a) {
      byte b1 = (byte)(a >> 0);
      byte b2 = (byte)(a >> 8);
      byte b3 = (byte)(a >> 16);
      byte b4 = (byte)(a >> 24);
      byte b5 = (byte)(a >> 32);
      byte b6 = (byte)(a >> 40);
      byte b7 = (byte)(a >> 48);
      byte b8 = (byte)(a >> 56);

      WriteByte(b1);
      WriteByte(b2);
      WriteByte(b3);
      WriteByte(b4);

      WriteByte(b5);
      WriteByte(b6);
      WriteByte(b7);
      WriteByte(b8);

   }

   public bool Read(out long a) {
      long b1 = ReadByte();
      long b2 = ReadByte();
      long b3 = ReadByte();
      long b4 = ReadByte();
      long b5 = ReadByte();
      long b6 = ReadByte();
      long b7 = ReadByte();
      long b8 = ReadByte();

      a = b1 + (b2 << 8) + (b3 << 16) + (b4 << 24)
         + (b5 << 32) + (b6 << 40) + (b7 << 48) + (b8 << 56);
      return true;
   }

   // byte
   public void Write(byte a) {
      res.Add(a);
   }
   public void WriteByte(int a) {
      res.Add((byte)a);
   }
   public void WriteByte(byte a) {
      res.Add(a);
   }
   public byte ReadByte() {
      return res[cursor++];
   }
   public bool Read(out byte a) {
      a = ReadByte();
      return true;
   }


   // bool
   public void WriteBoolArray(bool[] a) {

      int sz = a.Length;

      Write(sz);


      int n = sz / 8;
      int en = sz - n * 8;

      for (int i = 0; i < n; ++i) {

         int s = i * 8;
         int cb = 0;

         for (int x = 0; x < 8; ++x) {
            if (a[s + x]) {
               cb = cb ^ (1 << x);
            }
         }

         byte b = (byte)cb;

         Write(b);
      }

      if (en > 0) {
         int s = n * 8;
         int cb = 0;
         for (int x = 0; x < en; ++x) {
            if (a[s + x]) {
               int bit = 1 << x;
               cb = cb ^ bit;
            }
         }

         byte b = (byte)cb;

         Write(b);
      }
   }
   public bool ReadBoolArray(out bool[] a) {

      int sz;

      if (!Read(out sz)) {
         a = null;
         return false;
      }

      a = new bool[sz];

      int n = sz / 8;
      int en = sz - n * 8;

      for (int i = 0; i < n; ++i) {

         int s = i * 8;
         int cb = ReadByte();

         for (int x = 0; x < 8; ++x) {
            int bit = 1 << x;
            if ((cb & bit) != 0) {
               a[s + x] = true;
            }
         }

         byte b = (byte)cb;

         Write(b);
      }
      if (en > 0) {

         int s = n * 8;
         int cb = ReadByte();
         for (int x = 0; x < en; ++x) {
            int bit = 1 << x;
            if ((cb & bit) != 0) {
               a[s + x] = true;
            }
         }
      }
      return true;
   }


   // bool
   public void Write(bool a) {
      WriteByte(a ? 1 : 0);
   }
   public bool Read(out bool a) {
      var b = ReadByte();
      if (b == 1) {
         a = true;
         return true;
      }
      if (b == 0) {
         a = false;
         return true;
      }
      throw new Exception($"Bad bool type {b}!");
   }
   // string

   public void Write(string a) {
      Encoding unicode = Encoding.Unicode;

      var bytes = unicode.GetBytes(a);

      int n = bytes.Length;
      Write(n);
      foreach (var b in bytes) {
         WriteByte(b);
      }
   }


   public bool Read(out string a) {
      if (!Read(out int n)) {
         a = null;
         return false;
      }

      if (n < 0) {
         a = null;
         return false;
      }

      byte[] bytes = new byte[n];
      for (int i = 0; i < n; ++i) {
         bytes[i] = ReadByte();
      }
      a = Encoding.Unicode.GetString(bytes);

      return true;
   }

   public List<string> cached_strings = new List<string>();

   // contains 1 indexed position in cached strings list.
   public Dictionary<string, int> cached_string_keys = new Dictionary<string, int>();

   public void WriteMaybeCachedString(string a) {
      Encoding unicode = Encoding.Unicode;

      if (cached_string_keys.TryGetValue(a, out int int_key)) {
         int type_n = -int_key;
         Write(type_n);
      } else {

         int i = cached_strings.Count;
         cached_strings.Add(a);
         int_key = i + 1;
         cached_string_keys[a] = int_key;

         var bytes = unicode.GetBytes(a);

         int n = bytes.Length;
         Write(n);
         foreach (var b in bytes) {
            WriteByte(b);
         }
      }

   }
   public bool ReadMaybeCachedString(out string a) {
      int type_n;
      if (!ReadTypedString(out a, out type_n)) return false;
      if (type_n >= 0) {
         cached_strings.Add(a);
         var int_key = cached_strings.Count;
         cached_string_keys[a] = int_key;
         return true;
      } else {
         var int_key = -type_n;
         int i = int_key - 1;
         if (cached_strings.Count <= i) {
            throw new Exception("Serialization error: Missing cached string!");
            // return false;
         }
         a = cached_strings[i];
         return true;
      }
   }

   public bool ReadTypedString(out string a, out int type_n) {
      type_n = 0;
      if (!Read(out int n)) {
         a = null;
         return false;
      }
      if (n < 0) {
         a = null;
         type_n = n;
         return true;
      }

      byte[] bytes = new byte[n];
      for (int i = 0; i < n; ++i) {
         bytes[i] = ReadByte();
      }
      a = Encoding.Unicode.GetString(bytes);

      return true;
   }


}
