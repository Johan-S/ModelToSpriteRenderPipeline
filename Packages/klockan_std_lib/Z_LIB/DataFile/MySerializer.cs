using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using UnityEngine;


[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class DataAttribute : Attribute {

   public byte id;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DataTypeAttribute : Attribute {

   public System.Type array_type;

   public int outer_type_id;

   public bool register_type_only;
}
public static class MySerializer {

   class FieldSerializer {

      public byte field_id;

      public System.Type field_type;
      public DataAttribute field_attribute;

      public bool array;
      public bool list;
      public System.Type array_type;

      public bool generic;

      public System.Type parent;
      public System.Reflection.FieldInfo field;

      public IList ConstructCollection(int count) {
         if (array) {
            return (IList)array_type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { count });
         } else {
            IList res = (IList)array_type.ConstructEmpty();
            return res;
         }
      }

      public override string ToString() {
         return $"Field<{parent.Name}.{field.Name}, {field_type.FullName}>";
      }


   }

   static MySerializer() {
      PrimitiveSerializers.RegisterPrimitiveSerializers();
      RegisterTypeSerializer(typeof(SaveFileHeader), 200);
   }

   public struct SerializedFieldSerializer {
      public byte field_id;
      public int field_type;
      public bool array;
      public bool list;
   }

   public class SerializedTypeSerializer {

      public int type_id;
      public SerializedFieldSerializer[] field_types;

      public SerializedTypeSerializer() {
      }

      SerializedTypeSerializer(TypeSerializer o) {
         type_id = o.type_id;
         field_types = new SerializedFieldSerializer[o.fields.Length];
         for (int i = 0; i < field_types.Length; ++i) {
            var of = o.fields[i];
            field_types[i] = new SerializedFieldSerializer {
               field_id = of.field_id,
               field_type = GetTypeIdFor(of.field_type),
               array = of.array,
               list = of.list,
            };
         }
      }
   }

   class TypeSerializer : PrimitiveSerializer {

      public System.Type type {
         get; set;
      }
      public int type_id {

         get; set;
      }


      public FieldSerializer[] fields;

      public System.Func<object> constructor;

      public System.Func<object> MakeConstructor() {
         if (type.IsValueType) {
            return () => {
               return Activator.CreateInstance(type);
            };
         }

         var ci = type.GetConstructor(new Type[0]);
         if (ci == null) throw new Exception($"Type {type.Name} can't be serialized, lacks empty constructor!");
         object[] args = new object[0];

         return () => {
            return ci.Invoke(args);
         };
      }

      public static bool IsDefault(Type t, object o) {
         if (t.IsValueType) {
            if (o is int it) {
               return it == 0;
            }
            if (o is long lit) {
               return lit == 0;
            }
            if (o is bool itb) {
               return itb == false;
            }
            if (o is Enum enu) {
               return enu.GetHashCode() == 0;
            }
            return false;
         } else {
            return o == null;
         }
      }

      public void Serialize(object o, DataStream builder) {
         foreach (var f in fields) {
            var val = f.field.GetValue(o);
            if (val == null) {
            } else {
               if (f.array || f.list) {
                  var a = (IList)val;
                  builder.Write(f.field_id);
                  builder.Write(a.Count);
                  bool valtype = f.field_type.IsValueType;
                  foreach (var el in a) {
                     if (!valtype) {
                        if (el == null) {
                           builder.Write(0);
                           continue;
                        }
                        if (f.generic) {
                           var t = el.GetType();
                           MySerializer.Serialize(t, builder);
                        } else {
                           builder.Write(1);
                        }
                     }
                     MySerializer.Serialize(el, builder);
                  }
               } else {
                  if (IsDefault(f.field_type, val)) {
                     continue;
                  }
                  builder.Write(f.field_id);
                  if (f.generic) {
                     var t = o.GetType();
                     MySerializer.Serialize(t, builder);
                  }
                  MySerializer.Serialize(val, builder);
               }
            }
         }
         builder.WriteByte(0);
      }
      public bool Deserialize(DataStream data, out object res) {

         res = constructor();
         var o = res;
         byte field_id = 0;
         data.Read(out field_id);
         foreach (var f in fields) {
            if (field_id == 0) break;
            if (f.field_id > field_id) {
               data.Read(out field_id);
            }
            if (field_id == 0) break;
            if (f.field_id < field_id) continue;




            var field_type = f.field_type;

            if (f.array || f.list) {
               if (!data.Read(out int count)) {
                  throw new Exception($"Failed to parse list count {o.GetType().Name}.{f.field.Name}");
               }
               bool valtype = f.field_type.IsValueType;
               if (f.array) {
                  IList lrest = f.ConstructCollection(count);
                  for (int i = 0; i < count; ++i) {

                     if (!valtype) {
                        int field_type_id;
                        data.Read(out field_type_id);

                        if (field_type_id == 0) {
                           // null;
                           continue;
                        }

                        if (f.generic) {
                           field_type = GetTypeForId(field_type_id);
                        }

                     }
                     if (MySerializer.Deserialize(field_type, data, out var sub_res)) {
                        lrest[i] = sub_res;
                     } else {
                        throw new Exception($"Failed to parse {o.GetType().Name}.{f.field.Name}[{i}] ({field_type.Name})");
                     }
                  }
                  f.field.SetValue(o, lrest);
               } else {
                  IList lrest = f.ConstructCollection(count);
                  for (int i = 0; i < count; ++i) {
                     if (!valtype) {
                        int field_type_id;
                        data.Read(out field_type_id);

                        if (field_type_id == 0) {
                           // null;
                           continue;
                        }

                        if (f.generic) {
                           field_type = GetTypeForId(field_type_id);
                        }

                     }

                     if (MySerializer.Deserialize(field_type, data, out var sub_res)) {
                        lrest.Add(sub_res);
                     } else {
                        throw new Exception($"Failed to parse {o.GetType().Name}.{f.field.Name}[{i}] ({field_type.Name})");
                     }
                  }
                  f.field.SetValue(o, lrest);
               }

            } else {
               if (f.generic) {
                  if (!MySerializer.Deserialize<Type>(data, out field_type)) {
                     throw new Exception($"Failed to parse generic {o.GetType().Name}.{f.field.Name}");
                  }
               }

               if (MySerializer.Deserialize(field_type, data, out var sub_res)) {
                  f.field.SetValue(o, sub_res);
               } else {
                  throw new Exception($"Failed to parse {o.GetType().Name}.{f.field.Name} ({field_type.Name})");
               }
            }
         }
         if (field_id != 0) {
            data.Read(out field_id);
         }
         return true;
      }
   }

   static Dictionary<int, PrimitiveSerializer> serializers = new Dictionary<int, PrimitiveSerializer>();
   static Dictionary<System.Type, PrimitiveSerializer> serializers_by_type = new Dictionary<System.Type, PrimitiveSerializer>();


   public static void PrintTypeSerializers() {
      var ser = serializers.ToList().Sorted(x => x.Key);

      string msg = "\n".Join(ser.Map(x => $"{x.Key}: {x.Value.type.Name}"));
      UnityEngine.Debug.Log(msg);
   }

   public static void AssertTypeSerializerIntegrity() {
      HashSet<string> errors = new HashSet<string>();
      var ser = serializers.ToList().Sorted(x => x.Key);
      foreach (var s in ser) {
         if (s.Value is TypeSerializer ts) {
            foreach (var f in ts.fields) {
               if (!CanSerialize(f.field_type)) {
                  string err = $"Lack serializer for {f}";
                  errors.Add(err);
               }
            }
         }
      }
      if (errors.Count > 0) {
         var err = errors.ToList().Sorted();
         var err_msg = "\n".Join(err);
         UnityEngine.Debug.Log(err_msg);
         throw new Exception($"Serializer faulty, lacks: {err_msg}");
      }
   }

   public static ArrayConverter<Struct, Element> MakeArrayConverter<Struct, Element>() {
      var type = typeof(Struct);
      var as_array_type = typeof(Element);

      List<FieldInfo> fields = new List<FieldInfo>();

      int largest_id = 0;

      foreach (var fi in type.GetFields()) {
         var field_attribute = fi.GetCustomAttribute<DataAttribute>();
         if (field_attribute == null) continue;
         if (field_attribute.id == 0) {

            throw new Exception($"Serialization field id must not be zero, for field: {type.Name}.{fi.Name}");
         }

         var ft = fi.FieldType;
         Debug.Assert(as_array_type == ft, $"Bad array type {ft} in field  {type.Name}.{fi.Name}");

         fields.Add(fi);
         largest_id = Mathf.Max(largest_id, field_attribute.id);
      }
      FieldInfo[] field_arr = new FieldInfo[largest_id];
      foreach (var fi in type.GetFields()) {
         var field_attribute = fi.GetCustomAttribute<DataAttribute>();
         if (field_attribute == null) continue;
         field_arr[field_attribute.id - 1] = fi;
      }

      return new ArrayConverter<Struct, Element>(field_arr);
   }

   public static bool CanRegisterTypeSerializer(Type type, int type_id) {
      if (serializers_by_type.ContainsKey(type)) {
         throw new Exception($"Trying to register {type.Name} twice!");
      }

      if (type_id < 100) {
         throw new Exception($"Serialization id below 100 are reserved, got fauly for: {type.Name}");
      }

      if (serializers.TryGetValue(type_id, out var bad_res)) {

         return false;
      }
      return true;
   }

   public static void RegisterTypeSerializer(Type type, int type_id, System.Type as_array_type = null) {
      if (serializers_by_type.ContainsKey(type)) {
         throw new Exception($"Trying to register {type.Name} twice!");
      }

      if (type_id < 100) {
         throw new Exception($"Serialization id below 100 are reserved, got fauly for: {type.Name}");
      }

      if (serializers.TryGetValue(type_id, out var bad_res)) {

         int good_type_id = type_id;
         while (serializers.TryGetValue(good_type_id, out var nbad_res)) {
            good_type_id++;
         }

         throw new Exception($"Serialization type collision {type_id} between: {type.Name} and {bad_res.type.Name}, next free id: {good_type_id}");
      }

      List<FieldSerializer> field_infos = new List<FieldSerializer>();
      foreach (var fi in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
         var field_attribute = fi.GetCustomAttribute<DataAttribute>();
         if (field_attribute == null) continue;
         Debug.LogError($"Bad field {type.Name}.{fi.Name}");
      }
      foreach (var fi in type.GetFields()) {

         var field_attribute = fi.GetCustomAttribute<DataAttribute>();
         if (field_attribute == null) continue;
         if (field_attribute.id == 0) {

            throw new Exception($"Serialization field id must not be zero, for field: {type.Name}.{fi.Name}");
         }

         var ft = fi.FieldType;

         if (as_array_type != null) {
            Debug.Assert(as_array_type == ft, $"Bad array type {ft} in field  {type.Name}.{fi.Name}");
         }

         bool array = ft.IsArray;
         bool list = false;

         var element_type = ft;
         if (array) {
            string fullName = ft.FullName.Substring(0, ft.FullName.Length - 2);
            element_type = Type.GetType(string.Format("{0},{1}", fullName, ft.Assembly.GetName().Name));
         } else if (ft.Is(typeof(IList))) {
            list = true;
            var gta = ft.GenericTypeArguments;
            if (gta.Length != 1) {
               throw new Exception($"Bad type params: {ft.FullName}");
            }
            element_type = gta[0];
         }
         bool generic = false;
         if (element_type == typeof(object)) generic = true;

         field_infos.Add(new FieldSerializer {
            field = fi,
            field_id = field_attribute.id,
            field_type = element_type,
            parent = type,
            field_attribute = field_attribute,
            array = array,
            list = list,
            generic = generic,
            array_type = ft,
         });

      }
      field_infos = field_infos.Sorted(x => x.field_id);

      for (int i = 1; i < field_infos.Count; ++i) {
         var a = field_infos[i];
         var b = field_infos[i - 1];
         if (a.field_id == b.field_id) {
            throw new Exception($"Fields for type {type.Name} has same id: {a.field.Name} and {b.field.Name}");
         }
      }

      TypeSerializer res = new TypeSerializer();
      res.type = type;
      res.type_id = type_id;
      res.fields = field_infos.ToArray();

      {
         res.constructor = res.MakeConstructor();
      }

      RegisterPrimitiveSerializer(res, type_id);
   }

   static PrimitiveSerializer GetTypeSerializer(Type type) {
      PrimitiveSerializer res;
      if (serializers_by_type.TryGetValue(type, out res)) {
         return res;
      } else {
         if (type.Is<Type>()) {
            if (serializers_by_type.TryGetValue(typeof(Type), out res)) {
               return res;
            }
         }
      }
      throw new Exception($"Missing serializer for: {type.Name}");
   }
   static PrimitiveSerializer GetTypeSerializer(int type) {
      PrimitiveSerializer res;
      if (serializers.TryGetValue(type, out res)) {
         return res;
      }
      throw new Exception($"Missing serializer for type id: {type}");
   }


   public interface PrimitiveSerializer {
      System.Type type {
         get;
      }
      int type_id {
         get; set;
      }

      void Serialize(object val, DataStream builder);
      bool Deserialize(DataStream raw, out object res);
   }

   public static void RegisterPrimitiveSerializer(PrimitiveSerializer ser, int type_id) {
      var type = ser.type;
      ser.type_id = type_id;
      if (serializers_by_type.ContainsKey(type)) {
         throw new Exception($"Trying to register {type.Name} twice!");
      }

      if (serializers.TryGetValue(type_id, out var bad_res)) {
         throw new Exception($"Serialization type collision {type_id} between: {type.Name} and {bad_res.type.Name}");
      }
      if (type_id != ser.type_id) {
         throw new SystemException($"Need to set correct type id for {type.Name}!");
      }

      serializers[ser.type_id] = ser;
      serializers_by_type[ser.type] = ser;

      RegisterTypeId(ser.type, ser.type_id);
   }

   public static void RegisterTypeId(System.Type t, int id) {
      Debug.Assert(!id_to_type.ContainsKey(id));
      Debug.Assert(!type_to_id.ContainsKey(t));
      type_to_id[t] = id;
      id_to_type[id] = t;
   }

   public static void Serialize(object o, DataStream builder) {
      try {
         var ts = GetTypeSerializer(o.GetType());
         ts.Serialize(o, builder);
      } catch (Exception e) {
         throw new Exception($"Error serializing {o.GetType().Name}:\n{e}");
      }
   }

   public static bool CanSerialize(this object o) {
      if (o == null) {
         return false;
      }
      return CanSerialize(o.GetType());
   }

   public static void AssertCanSerialize(this object o) {
      if (o == null) {
         throw new Exception("Can't serialize null top objects!");
      }
      Debug.Assert(CanSerialize(o), $"Missing serialization for type {o.GetType()}");
   }

   public static T GenericDeserialize<T>(this DataStream ds) {
      if (Deserialize<T>(ds.GetReadStream(), out T res)) {
         return res;
      }
      throw new Exception("Failed to read data!");
   }
   public static bool SerializedEquals(this object a, object b) {
      {
         if (a is int i && b is int j) {
            return i == j;
         }
      }
      {
         if (a is long i && b is long j) {
            return i == j;
         }
      }

      if (a != null && b != null) {
         a.AssertCanSerialize();
         b.AssertCanSerialize();
         return a.GenericSerialize().ByteEquals(b.GenericSerialize());
      }
      return a == b;
   }

   public static DataStream GenericSerialize(this object o) {
      var builder = new DataStream();
      builder.Write(1337);
      builder.Write(42);

      var ts = GetTypeSerializer(o.GetType());

      builder.Write(ts.type_id);

      ts.Serialize(o, builder);
      return builder;
   }

   public static DataStream AsNewData(byte[] bytes, int data_size = -1) {
      DataStream res = new DataStream(bytes, data_size);
      int x;
      if (!res.Read(out x)) {
         return null;
      }
      if (x != 1337) return null;
      if (!res.Read(out x)) {
         return null;
      }
      if (x != 42) return null;
      return res;
   }

   public static DataStream AsNewData(DataStream res) {
      int x;
      if (!res.Read(out x)) {
         return null;
      }
      if (x != 1337) return null;
      if (!res.Read(out x)) {
         return null;
      }
      if (x != 42) return null;
      return res;
   }

   static Dictionary<System.Type, int> type_to_id = new Dictionary<Type, int>();
   static Dictionary<int, System.Type> id_to_type = new Dictionary<int, Type>();


   public static bool CanSerialize(this System.Type type) {
      if (type == typeof(object)) return true;
      if (serializers_by_type.ContainsKey(type)) return true;
      return false;
   }
   public static Type GetTypeForId(int type) {
      if (id_to_type.ContainsKey(type)) return id_to_type[type];
      throw new Exception($"TypeId {type} doesn't have serializer!");
   }

   public static int GetTypeIdFor(System.Type type) {
      if (type_to_id.ContainsKey(type)) return type_to_id[type];
      throw new Exception($"Type {type.Name} doesn't have serializer!");
   }
   public static bool Deserialize<T>(DataStream raw, out T res) {
      var type = typeof(T);
      var ts = GetTypeSerializer(type);
      bool ok = ts.Deserialize(raw, out var resr);
      if (ok) {
         res = (T)resr;
         return true;
      } else {
         res = default;
         return false;
      }
   }


   public static bool Deserialize(System.Type type, DataStream raw, out object res) {
      try {
         var ts = GetTypeSerializer(type);
         return ts.Deserialize(raw, out res);
      } catch (Exception e) {
         throw new Exception($"Error Deserializing {type.Name}:\n {e}");
      }
   }
   public static bool Deserialize(int type, DataStream raw, out object res) {
      var ts = GetTypeSerializer(type);
      return ts.Deserialize(raw, out res);
   }


   public static DataStream SerializeTop(object o) {
      var builder = new DataStream();
      builder.Write(default_serializer_format.Item1);
      builder.Write(default_serializer_format.Item2);

      if (o is SaveFileHeaderContainer cont) {
         var header = cont.file_header;

         if (header != null) {
            var header_serializer = GetTypeSerializer(200);

            builder.Write(200);
            header_serializer.Serialize(header, builder);
         }
      }

      var ts = GetTypeSerializer(o.GetType());

      builder.Write(ts.type_id);

      ts.Serialize(o, builder);
      return builder;
   }

   static (int, int) default_serializer_format = (1337, 42);



   public static SaveFileHeader ReadHeader(byte[] bytes) {
      DataStream res = new DataStream(bytes);
      int x;
      if (!res.Read(out x)) {
         return null;
      }
      if (!res.Read(out int x2)) {
         return null;
      }

      if (default_serializer_format != (x, x2)) {
         return null;
      }

      if (res.MaybeSkip(200)) {
         if (Deserialize<SaveFileHeader>(res, out var header)) {
            return header;
         }
         throw new Exception($"Bad Header!");
      } else {
         return null;
      }
   }
   public static bool DeserializeTop(DataStream ds, out object res) {
      SaveFileHeader header = null;
      if (ds.MaybeSkip(200)) {
         if (MySerializer.Deserialize<SaveFileHeader>(ds, out header)) {
         } else {
            throw new Exception($"Bad Header!");
         }
      }

      if (!ds.Read(out int type_id)) {
         throw new System.Exception($"Bad new Data!");
      }

      if (MySerializer.Deserialize(type_id, ds, out res)) {
         if (res is SaveFileHeaderContainer cont) {
            cont.file_header = header;
         }
         return true;
      } else {
         throw new System.Exception($"Bad new Data!");
      }
   }
}