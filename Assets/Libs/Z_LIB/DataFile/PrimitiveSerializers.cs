using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using UnityEngine;
using static MySerializer;

public class ArrayConverter<Struct, Element> {


   public ArrayConverter(FieldInfo[] fields) {
      this.fields = fields;
   }

   public FieldInfo[] fields;

   public Element[] Serialize(Struct s) {
      int n = fields.Length;
      Element[] res = new Element[n];
      for (int i = 0; i < n; ++i) {
         var f = fields[i];
         if (f == null) continue;
         res[i] = (Element)f.GetValue(s);
      }
      return res;
   }
   public Struct Deserialize(Element[] e) {
      Debug.Assert(e.Length == fields.Length, typeof(Struct));
      Struct res = (Struct)typeof(Struct).ConstructEmpty();

      Set(e, ref res);

      return res;
   }
   public void Set(Element[] e, ref Struct res) {
      Debug.Assert(e.Length == fields.Length, typeof(Struct));
      int n = fields.Length;
      for (int i = 0; i < n; ++i) {
         var f = fields[i];
         if (f == null) continue;
         f.SetValue(res, e[i]);
      }
   }
}

public static class PrimitiveSerializers {

   public static void RegisterPrimitiveSerializers() {

      MySerializer.RegisterPrimitiveSerializer(new IntSerializer(),1);
      MySerializer.RegisterPrimitiveSerializer(new MaybeIntSerializer(), 2);
      MySerializer.RegisterPrimitiveSerializer(new ByteSerializer(), 3);
      MySerializer.RegisterPrimitiveSerializer(new BoolSerializer(), 4);
      MySerializer.RegisterPrimitiveSerializer(new Vector2IntSerializer(), 6);
      MySerializer.RegisterPrimitiveSerializer(new LongSerializer(), 7);
      MySerializer.RegisterPrimitiveSerializer(new TypeSerializer(), 8);
      MySerializer.RegisterPrimitiveSerializer(new MaybeLongSerializer(), 9);
      // MySerializer.RegisterPrimitiveSerializer(new BoolArraySerializer(), 10);

      // MySerializer.RegisterPrimitiveSerializer(new StringSerializer(), 5);
      MySerializer.RegisterPrimitiveSerializer(new CachedStringSerializer(), 5);
   }

   public class EnumSerializer<T> : PrimitiveSerializer where T : Enum {
      public Type type => typeof(T);

      public int type_id {
         get;set;
      }

      System.Func<int, T> des;
      System.Func<T, int> ser;

      public EnumSerializer(System.Func<int, T> des, System.Func<T, int> ser) {
         this.ser = ser;
         this.des = des;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out int ival)) {
            res = des(ival);
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is T ival) {
            int i = ser(ival);
            builder.Write(i);
         }
      }

   }


   public class IntSerializer : PrimitiveSerializer {
      public Type type => typeof(int);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out int ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is int ival) {
            builder.Write(ival);
         }
      }
   }
   public class ByteSerializer : PrimitiveSerializer {
      public Type type => typeof(byte);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out byte ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is byte ival) {
            builder.Write(ival);
         }
      }
   }
   public class BoolSerializer : PrimitiveSerializer {
      public Type type => typeof(bool);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out bool ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is bool ival) {
            builder.Write(ival);
         }
      }
   }
   public class StringSerializer : PrimitiveSerializer {
      public Type type => typeof(string);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out string ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is string ival) {
            builder.Write(ival);
         }
      }
   }
   public class CachedStringSerializer : PrimitiveSerializer {
      public Type type => typeof(string);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.ReadMaybeCachedString(out string ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is string ival) {
            builder.WriteMaybeCachedString(ival);
         }
      }
   }
   public class Vector2IntSerializer : PrimitiveSerializer {
      public Type type => typeof(Vector2Int);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (!raw.Read(out int x)) {
            res = default;
            return false;
         }
         if (!raw.Read(out int y)) {
            res = default;
            return false;
         }
         res = new Vector2Int(x, y);
         return true;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is Vector2Int v2i) {
            builder.Write(v2i.x);
            builder.Write(v2i.y);
         }
      }
   }
   public class MaybeIntSerializer : PrimitiveSerializer {
      public Type type => typeof(int?);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out int ival)) {
            int? res_val = ival;
            res = res_val;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is int ival) {
            builder.Write(ival);
         }
      }
   }
   public class MaybeLongSerializer : PrimitiveSerializer {
      public Type type => typeof(long?);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out long ival)) {
            long? res_val = ival;
            res = res_val;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is long ival) {
            builder.Write(ival);
         }
      }
   }
   public class LongSerializer : PrimitiveSerializer {
      public Type type => typeof(long);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out long ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is long ival) {
            builder.Write(ival);
         }
      }
   }
   public class TypeSerializer : PrimitiveSerializer {
      public Type type => typeof(Type);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.Read(out int ival)) {
            res = MySerializer.GetTypeForId(ival);
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is Type t) {

            int ival = MySerializer.GetTypeIdFor(t);


            builder.Write(ival);
         }
      }
   }
   public class BoolArraySerializer : PrimitiveSerializer {
      public Type type => typeof(bool[]);


      public int type_id {
         get; set;
      }

      public bool Deserialize(DataStream raw, out object res) {
         if (raw.ReadBoolArray(out bool[] ival)) {
            res = ival;
            return true;
         }
         res = null;
         return false;
      }

      public void Serialize(object val, DataStream builder) {
         if (val is bool[] ival) {
            builder.WriteBoolArray(ival);
         }
      }
   }
}
