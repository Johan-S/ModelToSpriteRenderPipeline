// Decompiled with JetBrains decompiler
// Type: Console
// Assembly: Unity Utility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D25FCDD2-9E0B-41FC-9B46-0D75E06C6427
// Assembly location: C:\Users\johst\repos\WoM 2020\Assets\Z_DLL\Unity Utility.dll

using System;
 
 using LogBuilder_Overrides;
 using System;
 using System.Collections;
 using System.Collections.Generic;
 using System.Reflection;
 using UnityEngine;
using System;
using System.Collections.Generic;
// Decompiled with JetBrains decompiler
// Type: LogBuilder
// Assembly: Unity Utility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D25FCDD2-9E0B-41FC-9B46-0D75E06C6427
// Assembly location: C:\Users\johst\repos\WoM 2020\Assets\Z_DLL\Unity Utility.dll

using System.Collections.Generic;

public class LogBuilder
{
  private List<object> rows = new List<object>();

  public LogBuilder()
  {
  }

  public LogBuilder(string msg) => this.rows.Add((object) msg);

  public LogBuilder WithString(string format, params object[] args)
  {
    this.rows.Add((object) new LogBuilder.LazyFormatString()
    {
      format = format,
      args = args
    });
    return this;
  }

  public LogBuilder With(object o)
  {
    this.rows.Add(o);
    return this;
  }

  public LogBuilder WithKeyVals(object o)
  {
    this.rows.Add(o);
    return this;
  }

  public LogBuilder WithRows(IEnumerable<object> o)
  {
    this.rows.AddRange(o);
    return this;
  }

  public static implicit operator LogBuilder(string s) => new LogBuilder(s);

  public override string ToString()
  {
    List<string> values = new List<string>();
    foreach (object row in this.rows)
    {
      string str = DebugEx.StandardTopConversion(row);
      values.Add(str);
    }
    return string.Join("\n", (IEnumerable<string>) values);
  }

  public struct LazyFormatString
  {
    public string format;
    public object[] args;
    private string cache;

    public override string ToString() => string.Format(this.format, this.args);
  }
}

public static class Console
{
   public static void WriteLine(params object[] items) {
      DebugEx.Log(items);
   }
   public static Func<string> context_gen;

   public static void PrintContext(params object[] items)
   {
      if (Console.context_gen != null)
         items = items.Prepend<object>((object) Console.context_gen());
      DebugEx.Log(items);
   }

   public static void print(params object[] items) => DebugEx.Log(items);

   public static void Print(params object[] items) => DebugEx.Log(items);

   public static void Log(params object[] items) => DebugEx.Log(items);

   public static void LogFormat(string msg, params object[] items) => DebugEx.Log((object) string.Format(msg, items));

   public static void LogWarning(params object[] items) => DebugEx.LogWarning(items);

   public static void LogError(params object[] items) => DebugEx.LogError(items);
}// Decompiled with JetBrains decompiler
 // Type: DebugEx
 // Assembly: Unity Utility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
 // MVID: D25FCDD2-9E0B-41FC-9B46-0D75E06C6427
 // Assembly location: C:\Users\johst\repos\WoM 2020\Assets\Z_DLL\Unity Utility.dll
 
 public static class DebugEx
 {
   private static Dictionary<Type, Func<object, string>> conv_dict = new Dictionary<Type, Func<object, string>>();
 
   public static string ToString(object o) => DebugEx.StandardConversion(o);
 
   public static string AsString(object o) => DebugEx.StandardConversion(o);
 
   public static string ToRowString(this IList<string> s) => "[" + s.Join(", ") + "]";
 
   public static global::LogBuilder LogBuilder(this string s, params object[] args) => new global::LogBuilder().WithString(s, args);
 
   public static void Log(params object[] items) => Debug.Log((object) ((IEnumerable<object>) items).Map<object, string>(new Func<object, string>(DebugEx.StandardTopConversion)).Join(" "));
 
   public static void LogWarning(params object[] items) => Debug.LogWarning((object) ((IEnumerable<object>) items).Map<object, string>(new Func<object, string>(DebugEx.StandardTopConversion)).Join(" "));
 
   public static void LogError(params object[] items) => Debug.LogError((object) ((IEnumerable<object>) items).Map<object, string>(new Func<object, string>(DebugEx.StandardTopConversion)).Join(" "));
 
   private static bool IsInstanceOfGenericType(Type type, Type genericType)
   {
     for (; type != (Type) null; type = type.BaseType)
     {
       if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
         return true;
     }
     return false;
   }
 
   static DebugEx() => DebugEx.conv_dict[typeof (string)] = (Func<object, string>) (o => string.Format("\"{0}\"", o));
 
   private static Func<object, string> GetConverstionFunction(Type t)
   {
     if (t == typeof (object))
       return new Func<object, string>(DebugEx.StandardConversion);
     Func<object, string> converstionFunction = (Func<object, string>) (o => o.ToString());
     if (DebugEx.conv_dict.TryGetValue(t, out converstionFunction))
       return converstionFunction;
     Func<object, string> func = (Func<object, string>) (o => o.ToString());
     if (t.Is<DebugEx.CustomToString>())
       func = (Func<object, string>) (o => ((DebugEx.CustomToString) o).CustomToString());
     else if (DebugEx.IsInstanceOfGenericType(t, typeof (IList<>)) || DebugEx.IsInstanceOfGenericType(t, typeof (List<>)) || t.Is<IList>())
       func = (Func<object, string>) (o =>
       {
         if (!(o is IEnumerable enumerable2))
           return (string) null;
         List<string> val = new List<string>();
         foreach (object o1 in enumerable2)
           val.Add(DebugEx.StandardConversion(o1));
         return "[" + ", ".Join((IEnumerable<string>) val) + "]";
       });
     else if (DebugEx.IsInstanceOfGenericType(t, typeof (HashSet<>)))
     {
       Type genericTypeArgument = t.GenericTypeArguments[0];
       Func<object, string> st_conv = DebugEx.GetConverstionFunction(genericTypeArgument);
       func = !genericTypeArgument.Is<IComparable>() ? (Func<object, string>) (o =>
       {
         if (!(o is IEnumerable enumerable4))
           return (string) null;
         List<string> val = new List<string>();
         foreach (object o2 in enumerable4)
           val.Add(DebugEx.StandardConversion(o2));
         val.Sort();
         return "{" + ", ".Join((IEnumerable<string>) val) + "}";
       }) : (Func<object, string>) (o =>
       {
         if (!(o is IEnumerable enumerable6))
           return (string) null;
         List<object> objectList = new List<object>();
         foreach (object obj in enumerable6)
           objectList.Add(obj);
         objectList.Sort();
         List<string> val = new List<string>();
         foreach (object obj in objectList)
           val.Add(st_conv(obj));
         return "{" + ", ".Join((IEnumerable<string>) val) + "}";
       });
     }
     else if (DebugEx.IsInstanceOfGenericType(t, typeof (Dictionary<,>)))
     {
       Type genericTypeArgument1 = t.GenericTypeArguments[0];
       Type genericTypeArgument2 = t.GenericTypeArguments[1];
       Func<object, string> key_conv = DebugEx.GetConverstionFunction(genericTypeArgument1);
       Func<object, string> val_conv = DebugEx.GetConverstionFunction(genericTypeArgument2);
       Dictionary<int, int> dictionary = new Dictionary<int, int>();
       PropertyInfo get_keys_prop = t.GetProperty("Keys");
       MethodInfo get_val_method = t.GetMethod("TryGetValue");
       Func<object, IEnumerable> get_keys = (Func<object, IEnumerable>) (dict_o => (IEnumerable) get_keys_prop.GetValue(dict_o));
       Func<object, object, object> get_val = (Func<object, object, object>) ((dict_o, key) =>
       {
         object[] parameters = new object[2]{ key, null };
         return !(bool) get_val_method.Invoke(dict_o, parameters) ? (object) null : parameters[1];
       });
       func = !genericTypeArgument1.Is<IComparable>() ? (Func<object, string>) (o =>
       {
         if (o == null)
           return (string) null;
         List<object> objectList = new List<object>();
         foreach (object obj in get_keys(o))
           objectList.Add(obj);
         objectList.Sort();
         List<string> val = new List<string>();
         foreach (object obj1 in objectList)
         {
           object obj2 = get_val(o, obj1);
           val.Add(key_conv(obj1) + ": " + val_conv(obj2));
         }
         return "{" + ", ".Join((IEnumerable<string>) val) + "}";
       }) : (Func<object, string>) (o =>
       {
         if (o == null)
           return (string) null;
         List<object> objectList = new List<object>();
         foreach (object obj in get_keys(o))
           objectList.Add(obj);
         objectList.Sort();
         List<string> val = new List<string>();
         foreach (object obj3 in objectList)
         {
           object obj4 = get_val(o, obj3);
           val.Add(key_conv(obj3) + ": " + val_conv(obj4));
         }
         return "{" + ", ".Join((IEnumerable<string>) val) + "}";
       });
     }
     else if (t.Is<IEnumerable>())
       func = (Func<object, string>) (o =>
       {
         if (!(o is IEnumerable enumerable8))
           return (string) null;
         List<string> val = new List<string>();
         foreach (object o3 in enumerable8)
           val.Add(DebugEx.StandardConversion(o3));
         return "(" + ", ".Join((IEnumerable<string>) val) + ")";
       });
     DebugEx.conv_dict[t] = func;
     return DebugEx.conv_dict[t];
   }
 
   public static string StandardTopConversion(object o)
   {
     if (o is string str)
       return str;
     return o == null ? (string) null : DebugEx.GetConverstionFunction(o.GetType())(o);
   }
 
   public static string StandardConversion(object o) => o == null ? (string) null : DebugEx.GetConverstionFunction(o.GetType())(o);
 
   public class Logger
   {
     public Func<string> pre_message;
     public bool log_to_console;
     private List<string> string_logs = new List<string>();
 
     public List<string> GetLogs() => this.string_logs;
 
     public virtual void Log(params object[] items)
     {
       string str1 = ((IEnumerable<object>) items).Map<object, string>(new Func<object, string>(DebugEx.StandardTopConversion)).Join(" ");
       Func<string> preMessage = this.pre_message;
       string str2 = preMessage != null ? preMessage() : (string) null;
       if (str2 != null)
         str1 = str2 + str1;
       this.string_logs.Add(str1);
       if (!this.log_to_console)
         return;
       this.Log((object) str1);
     }
 
     public static implicit operator bool(DebugEx.Logger a) => a != null;
   }
   public interface CustomToString
   {
     string CustomToString();
   }
 }
// Decompiled with JetBrains decompiler
// Type: LogBuilder_Overrides.Overrides
// Assembly: Unity Utility, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D25FCDD2-9E0B-41FC-9B46-0D75E06C6427
// Assembly location: C:\Users\johst\repos\WoM 2020\Assets\Z_DLL\Unity Utility.dll


namespace LogBuilder_Overrides
{
  public static class Overrides
  {
    public static List<U> Map<T, U>(this IEnumerable<T> a, Func<T, U> p)
    {
      List<U> uList = new List<U>();
      foreach (T obj in a)
        uList.Add(p(obj));
      return uList;
    }

    public static T[] Prepend<T>(this T[] arr, params T[] items) {
       T[] res = new T[arr.Length + items.Length];

       int i = 0;
       foreach (T x in arr) {
          res[i++] = x;
       }
       foreach (T x in items) {
          res[i++] = x;
       }

       return res;
    }

    public static string Join(this string sep, IEnumerable<string> val) => string.Join(sep, val);

    public static string Join(this IEnumerable<string> a, string sep) => a == null ? (string) null : string.Join(sep, a);

    public static bool Is(this Type a, Type b) => a == b || a.IsSubclassOf(b) || b.IsAssignableFrom(a);

    public static bool Is<T>(this Type a)
    {
      Type c = typeof (T);
      return a == c || a.IsSubclassOf(c) || c.IsAssignableFrom(a);
    }
  }
}
