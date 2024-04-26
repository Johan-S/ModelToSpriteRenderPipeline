using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;
using System.Globalization;
using System.Collections;

using System.Reflection;
using LogBuilder_Overrides;
// ReSharper disable All
using Type = System.Type;
public class DebugAttribute : System.Attribute {
}


public static class Overloads {

   public static IEnumerable<object> ToGeneric(this IEnumerable ie) {
      foreach (var o in ie) {
         yield return o;
      }
   }

   public static string EscapeCode(this string s) {
      return "\"" + EscapeStandard(s) + "\"";
   }

   public static string EscapeStandard(string s) {
      var sb = new System.Text.StringBuilder();

      foreach (var ch in s) {

         if (ch == '\n') {
            sb.Append("\\n");
            continue;
         }
         if (ch == '\r') {
            sb.Append("\\r");
            continue;
         }
         if (ch == '\0') {
            sb.Append("\\0");
            continue;
         }
         if (ch == '\t') {
            sb.Append("\\t");
            continue;
         }
         if (ch == '\\') {
            sb.Append("\\\\");
            continue;
         }
         if (ch == '\"') {
            sb.Append("\\\"");
            continue;
         }
         sb.Append(ch);
      }
      return sb.ToString();
   }

   public static T[] AppendArray<T>(this T[] orig, params T[] arg)
   {
      T[] objArray = new T[orig.Length + arg.Length];
      orig.CopyTo((System.Array) objArray, 0);
      arg.CopyTo((System.Array) objArray, orig.Length);
      return objArray;
   }
   public static T[] PrependArray<T>(this T[] orig, params T[] arg)
   {
      T[] objArray = new T[orig.Length + arg.Length];
      arg.CopyTo((System.Array) objArray, 0);
      orig.CopyTo((System.Array) objArray, arg.Length);
      return objArray;
   }

   public static int IndexOf<T>(this T[] a, T needle) {
      for (int i = 0; i < a.Length; ++i) if (a[i].Equals(needle)) return i;
      return -1;
   }

   public static System.ArraySegment<T> ArraySegment<T>(this T[] arr, int i, int n = 99999999) {
      if (i >= arr.Length) return new System.ArraySegment<T>();
      if (i + n > arr.Length) n = arr.Length - i;

      return new System.ArraySegment<T>(arr, i, n);
   }

   public static bool Contains<T>(this T[] a, T needle) {
      for (int i = 0; i < a.Length; ++i) if (a[i].Equals(needle)) return true;
      return false;
   }


   public static void AddRange<T>(this IList<T> l, IEnumerable<T> o) {
      foreach (var e in o) l.Add(e);
   }


   public static bool IsOdd(this int i) {
      if (i < 0) {
         i = -i;
      }
      return (i & 1) == 1;

   }

   public static string AsABC(this int i) {
      string res = "";
      while (i >= 0) {
         int ab = i % 26;
         res = (char)('A' + ab) + res;
         i = (i / 26) - 1;
      }
      return res;
   }


   public static string WithRows(this string msg, params object[] ie) {
      return msg + "\n" + ie.Map(x => x.ToString()).Join("\n");
   }

   public static bool HasFuncSignature<T>(this MethodInfo m) {
      return HasFuncSignature(m, typeof(T));
   }

   public static bool HasFuncSignature(this MethodInfo m, Type rt, params Type[] ts) {

      if (!CanCastToAction(m, ts)) return false;
      
      var res = m.ReturnType;
      if (!res.Is(rt)) return false;
      return true;
   }

   public static bool CanCastToAction(this MethodInfo m, params Type[] ts) {
      var param = m.GetParameters();
      if (param.Length != ts.Length) return false;

      for (int i = 0; i < ts.Length; i++) {
         if (!ts[i].Is(param[i].ParameterType)) {
            return false;
         }
      }

      return true;
   }


   public static bool CanCreateLambda<T>(this MethodInfo m, object o = null) {
      if (!m.ReturnType.Is<T>()) return false;
      if (m.GetParameters().Length != 0) return false;
      if (o == null && !m.IsStatic) return false;
      return true;
   }
   public static bool CanCreateLambda<P1, R>(this MethodInfo m, object o = null) {
      if (!m.ReturnType.Is<R>()) return false;
      if (m.GetParameters().Length != 1) return false;
      if (!typeof(P1).Is(m.GetParameters()[0].ParameterType)) return false;
      if (o == null && !m.IsStatic) return false;
      return true;
   }
   public static System.Func<T> CreateLambda<T>(this MethodInfo m, object o = null) {
      Debug.Assert(m.ReturnType.Is<T>());
      Debug.Assert(m.GetParameters().Length == 0);
      Debug.Assert(o != null || m.IsStatic);
      return () => (T)m.Invoke(o, empty_call);
   }
   public static System.Func<P1, R> CreateLambda<P1, R>(this MethodInfo m, object o = null) {
      Debug.Assert(m.ReturnType.Is<R>());
      Debug.Assert(m.GetParameters().Length == 1);
      Debug.Assert(typeof(P1).Is(m.GetParameters()[0].ParameterType));
      Debug.Assert(o != null || m.IsStatic);
      return (p1) => (R)m.Invoke(o, new object[] { p1 });
   }




   public static V GetDefaultC<K, V>(this Dictionary<K, V> dict, K key) where V : new() {

      if (dict.TryGetValue(key, out V val)) return val;
      V nv = new V();
      dict[key] = nv;
      return nv;
   }

   public static bool NotContains<T>(this IEnumerable<T> ie, T t) {
      return !ie.Contains(t);
   }


   public static int Increment<T>(this Dictionary<T, int> di, T t, int n = 1) {

      if (di.TryGetValue(t, out var res)) {
         di[t] = res + n;
         return res + n;
      } else {
         di[t] = n;
         return n;
      }
   }

   public static IEnumerable<int2> Times(this int n, int m, int step = 1) {

      for (int i = 0; i < n; ++i) {
         for (int j = 0; j < m; ++j) {
            yield return (i * step, j * step);
         }
      }
   }

   public static IEnumerable<int2> Times(this int n, int m, (int, int) step) {

      for (int i = 0; i < n; ++i) {
         for (int j = 0; j < m; ++j) {
            yield return (i * step.Item1, j * step.Item2);
         }
      }
   }
   public static int SetBits(this int n, params int[] extra_bits) {
      int res = n;
      foreach (var b in extra_bits) res |= 1 << b;
      return res;
   }
   public static IEnumerable<int> Times(this int n) {

      for (int i = 0; i < n; ++i) {
         yield return i;
      }
   }

   public static T[] Copy<T>(this T[] val) {
      T[] res = new T[val.Length];
      val.CopyTo(res, 0);
      return res;
   }

   public static void AddRange<T>(this HashSet<T> s, IEnumerable<T> vals) {
      foreach (var a in vals) s.Add(a);
   }

   public static FieldInfo[] GetDeclaredFields(this System.Type t) {
      return t.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
   }

   public static float AdditiveColorAdd(float a, float b) {
      a = Mathf.Clamp01(a);
      b = Mathf.Clamp01(b);

      var res = 1 - (1 - a) * (1 - b);

      return res;
   }

   public static Color AdditiveColor(this Color a, Color b) {
      Color res = new Color();

      if (a.a <= 0) return b;
      if (b.a <= 0) return a;

      float res_a = AdditiveColorAdd(a.a, b.a);

      float af = a.a / res_a;
      float bf = b.a / res_a;

      res.r = AdditiveColorAdd(a.r * af, b.r * bf);
      res.g = AdditiveColorAdd(a.g * af, b.g * bf);
      res.b = AdditiveColorAdd(a.b * af, b.b * bf);
      res.a = res_a;

      return res;
   }

   public static bool IsNullEmpty(this string s) => s == null || s.Length == 0;

   public static string Join(this string sep, IEnumerable<string> val) {
      return string.Join(sep, val);
   }

   public static T[] GetComponentsInParent_Safe<T>(this Transform tr) where T : MonoBehaviour {
      List<T> r = new();
      while (tr) {
         var res = tr.GetComponent<T>();
         if (res) r.Add(res);
         tr = tr.parent;
      }

      return r.ToArray();
   }
   public static T GetComponentInParent_Safe<T>(this Transform tr) where T : MonoBehaviour {
      while (tr) {
         var res = tr.GetComponent<T>();
         if (res) return res;
         tr = tr.parent;
      }
      return null;
   }

   public static bool ElementEquals<T>(this IList<T> a, IList<T> b) {
      if (a.Count != b.Count) return false;
      int n = a.Count;
      for (int i = 0; i < n; ++i) {
         if (!object.Equals(a[i], b[i])) return false;
      }
      return true;
   }
   public static List<T> ExclusiveSetDiff<T>(this IList<T> a, IList<T> b) {

      List<T> res = new List<T>();

      HashSet<T> a_items = new HashSet<T>(a);
      HashSet<T> b_items = new HashSet<T>(b);

      res.AddRange(a.Where(x => !b_items.Contains(x)));
      res.AddRange(b.Where(x => !a_items.Contains(x)));

      return res;
   }
   public static List<T> SetUnion<T>(this IList<T> a, IList<T> b) {

      List<T> res = new List<T>();
      HashSet<T> a_items = new HashSet<T>(a);

      res.AddRange(a);
      res.AddRange(b.Where(x => !a_items.Contains(x)));

      return res;
   }
   public static bool ElementSetEquals<T>(this IList<T> a, IList<T> b) {
      if (a.Count != b.Count) return false;
      var aset = a.ToCountSet();
      var bset = b.ToCountSet();
      if (aset.Count != bset.Count) return false;
      foreach (var key in aset.Keys) {
         if (!bset.TryGetValue(key, out int bval)) {
            return false;
         }
         if (aset[key] != bval) return false;
      }
      return true;
   }

   public static string GreyedColor(this string s) {
      return $"<color=#aaa>{s}</color>";
   }
   public static string ImportantColor(this string s) {
      return $"<color=#ff3>{s}</color>";
   }

   static string To2Hex(byte by) {
      int a = by;
      if (a < 0) a += 128;
      int b = a / 16;
      a = a % 16;
      return b.ToString("X") + a.ToString("X");
   }

   public static string AsRGB_String(this Color c) {
      Color32 ab = c;
      return To2Hex(ab.r) + To2Hex(ab.g) + To2Hex(ab.b);
   }

   public static string GoodBadColor_Spectrum(this string s, float goodness, float color_pow = 1) {

      if (color_pow != 1) {
         var t = 2 * goodness - 1;
         float sgn = Mathf.Sign(t);
         t *= sgn;
         t = Mathf.Pow(t, color_pow);
         t *= sgn;

         goodness = (t + 1) * 0.5f;
      }

      Color good = new Color32(128, 255, 96, 255);
      Color mid = new Color32(0, 0, 0, 255);
      Color bad = new Color32(255, 96, 32, 255);
      var cp = Color.Lerp(bad, mid, goodness);
      var c = Color.Lerp(cp, good, goodness);

      var res = $"<color=#{c.AsRGB_String()}>{s}</color>";
      return res;
   }

   public static string GoodColor(this string s) {
      return $"<color=#2f2>{s}</color>";
   }
   public static string GoodBadColor(this string s) {
      return $"<color=#bc2>{s}</color>";
   }

   public static string BadColor(this string s) {
      return $"<color=#f82>{s}</color>";
   }

   static int DecaDigit_Pos(float val) {
      if (val < 0.001) {
         val = 0.001f;
      }

      long xval = (long)(val * 1000000 + 0.5f);

      int digits = 0;
      while (xval < 1000000) {
         xval *= 10;
         digits--;
      }
      while (xval >= 10000000) {
         xval /= 10;
         digits++;
      }

      if (digits < 0) {
         digits -= (-digits + 2) / 3;
      }
      return digits;
   }


   public static string ToCharString(this char c, int n) {
      if (n <= 0) return "";
      char[] k = new char[n];
      for (int i = 0; i < n; ++i) k[i] = c;
      return new string(k);
   }
   public static IEnumerable<string> NormalizePad_Left(this IEnumerable<string> times, char c = ' ') {
      int max_n = times.Max(x => x.Length);
      foreach (var t in times) {
         yield return ToCharString(c, max_n - t.Length) + t;
      }
   }

   public static IEnumerable<string> NormalizePad_Right(this IEnumerable<string> times, char c = ' ') {
      int max_n = times.Max(x => x.Length);
      foreach (var t in times) {
         yield return t + ToCharString(c, max_n - t.Length);
      }
   }
   public static (string num, string unit) AsPaddedTimeString_Parts(this float secs, float largest) {

      int digits_above = DecaDigit_Pos(largest) - DecaDigit_Pos(secs);


      string pad = ' '.ToCharString(digits_above);


      if (secs < 0.009999f) {
         return (pad + (secs * 1000).ToString("0.00"), " ms");
      }
      if (secs < 0.09999f) {
         return (pad + (secs * 1000).ToString("0.0"), " ms");
      }

      if (secs < 1) {
         return (pad + (secs * 1000).ToString("0."), " ms");
      }

      if (secs < 10) {
         return (pad + secs.ToString("0.00"), "sec");
      }
      if (secs < 100) {
         return (pad + secs.ToString("0.0"), "sec");
      }

      return (pad + secs.ToString("0."), "sec");
   }
   public static string AsTimeString(this float secs) {

      if (secs < 0.009999f) {
         return (secs * 1000).ToString("0.00") + " ms";
      }
      if (secs < 0.09999f) {
         return (secs * 1000).ToString("0.0") + " ms";
      }

      if (secs < 1) {
         return (secs * 1000).ToString("0.") + "  ms";
      }

      if (secs < 10) {
         return secs.ToString("0.00") + "sec";
      }
      if (secs < 100) {
         return secs.ToString("0.0") + "sec";
      }

      return secs.ToString("0.") + " sec";
   }

   public class CountDict<T> {

      Dictionary<T, int> counter = new Dictionary<T, int>();


      int ops = 0;

      public int tot_count = 0;


      public int Count(T val) {
         return counter.Get(val, 0);
      }
      public bool Contains(T val) {
         return Count(val) > 0;
      }

      public void Add(T val) {
         counter.Increment(val);
         tot_count++;
      }
      public void AddRange(IEnumerable<T> val) {
         foreach (var x in val) {
            Add(x);
         }
      }
      public void Remove(T val) {
         int n = counter.Decrement(val);
         ops++;
         if (n < 0) {
            Debug.LogError($"Removed too many of {val}, cur count {n}");
         } else {
            tot_count--;
         }
         if (ops > (counter.Count + 10) * 10) {

            foreach (var kv in counter.ToList()) {
               if (kv.Value == 0) {
                  counter.Remove(kv.Key);
               }

            }
            ops = 0;
         }
      }
      public void RemoveRange(IEnumerable<T> val) {
         foreach (var x in val) {
            Remove(x);
         }
      }

   }

   public static int Increment<T>(this Dictionary<T, int> a, T val) {
      if (!a.ContainsKey(val)) return a[val] = 1;
      else return a[val] += 1;
   }
   public static int Decrement<T>(this Dictionary<T, int> a, T val) {
      if (!a.ContainsKey(val)) return a[val] = -1;
      else return a[val] -= 1;
   }

   public static bool LessThan(this Vector2Int a, Vector2Int b) {
      if (a.x < b.x) return true;
      if (b.x < a.x) return false;
      return a.y < b.y;
   }

   public static bool Exists<T>(this IEnumerable<T> ie, System.Predicate<T> p) {
      foreach (var t in ie) if (p(t)) return true;
      return false;
   }

   public static void ConsumeAll(this IEnumerator e) {
      while (e.MoveNext()) {
         var ee = e.Current;
         if (ee is IEnumerator ie) {
            ie.ConsumeAll();
         }
      }
   }

   public static bool Any<T>(this IEnumerable<T> ie, System.Predicate<T> p) {
      foreach (var x in ie) if (p(x)) return true;
      return false;
   }

   public static List<T> Reversed<T>(this List<T> e) {
      List<T> res = new List<T>(e);
      res.Reverse();
      return res;
   }

   public static int gcd(this int i, int o) {
      if (i == 0) return o;
      int r = o / i;
      if (i * r == o) return i;
      o -= r * i;
      return o.gcd(i);
   }
   public static int scm(this int i, int o) {
      if (i == 0) return o;
      int g = i.gcd(o);
      return i / g * o;
   }

   public static int UpperDiv_TwoSides(this int i, int o) {
      int t = o < 0 ? -1 : 1;
      o = t * o;
      if (i < 0) {
         return (i - o + 1) / o * t;
      } else {
         return (i + o - 1) / o * t;
      }
   }

   public static string ToBigString_Scientific(this long i) {
      long ai = i;
      if (i < 0) ai = -i;

      if (ai >= 10000000000) {
         var ij = i / 1000000;
         return $"{ij}G";
      }
      if (ai >= 1000000000) {
         var ij = i / 100000000;
         return $"{ij.ToDecaString()}G";
      }
      if (ai >= 10000000) {
         var ij = i / 1000000;
         return $"{ij}M";
      }
      if (ai >= 1000000) {
         var ij = i / 100000;
         return $"{ij.ToDecaString()}M";
      }
      if (ai >= 10000) {
         var ij = i / 1000;
         return $"{ij}K";
      }
      if (ai >= 1000) {
         var ij = i / 100;
         return $"{ij.ToDecaString()}K";
      }
      return i.ToString();
   }
   public static string ToBigString(this int i) {
      int ai = Mathf.Abs(i);

      if (ai >= 1000000000) {
         var ij = i / 100000000;
         return $"{ij.ToDecaString()}B";
      }
      if (ai >= 10000000) {
         var ij = i / 1000000;
         return $"{ij}M";
      }
      if (ai >= 1000000) {
         var ij = i / 100000;
         return $"{ij.ToDecaString()}M";
      }
      if (ai >= 10000) {
         var ij = i / 1000;
         return $"{ij}K";
      }
      if (ai >= 1000) {
         var ij = i / 100;
         return $"{ij.ToDecaString()}K";
      }
      return i.ToString();
   }

   public static string ToFractionString(this int i, int denom) {
      bool negative = i < 0;
      if (negative) i = -i;

      int t = i / denom;
      int r = i % denom;
      if (negative) {
         t = -t;
      }
      if (r == 0) {
         return t.ToString();
      }
      if (t == 0) {
         if (negative) {
            r = -r;
         }
         return $"{r}/{denom}";
      }
      return $"{t} {r}/{denom}";
   }

   public static void SetRendering(this Camera cam, Rect r) {
      cam.transform.position = r.center;
      cam.nearClipPlane = -1000;
      cam.farClipPlane = +1000;
      cam.orthographic = true;
      cam.orthographicSize = r.height / 2;
      cam.aspect = r.width / r.height;
   }

   public static System.Func<object[], object> GetGeneric(this System.Type base_type, string name, params System.Type[] generic_args) {
      var m = base_type.GetMethod(name);
      var generic = m.MakeGenericMethod(generic_args);
      return args => generic.Invoke(null, args);
   }

   public static object ConstructEmpty(this System.Type base_type) {
      if (base_type.IsValueType) {
         var c = base_type.GetConstructor(new System.Type[] { });
         if (c == null) {
            var cc = base_type.GetConstructors();
            Debug.LogError($"No empty constructror for {base_type.Name}! There are {cc.Length} constructors!");
         }
         return c.Invoke(empty_call);
      } else {
         var c = base_type.GetConstructor(new System.Type[] { });
         if (c == null) {
            var cc = base_type.GetConstructors();
            Debug.LogError($"No empty constructror for {base_type.Name}! There are {cc.Length} constructors!");
         }
         return c.Invoke(empty_call);
      }
   }


   public static object[] empty_call = new object[] { };

   public static MethodInfo GetGenericMethod(this System.Type t, string name, params System.Type[] generic_args) {
      return t.GetMethod(name).MakeGenericMethod(generic_args);
   }

   public static object InvokeEmpty(this System.Reflection.MethodInfo base_type, object instance = null) {
      return base_type.Invoke(instance, empty_call);
   }
   public static object InvokeGenericEmpty(this System.Reflection.MethodInfo base_type, params System.Type[] generic_args) {
      return base_type.MakeGenericMethod(generic_args).Invoke(null, empty_call);
   }
   public static T InvokeGenericEmpty<T>(this System.Reflection.MethodInfo base_type, params System.Type[] generic_args) {
      return (T)base_type.MakeGenericMethod(generic_args).Invoke(null, empty_call);
   }
   public static string SubstringAfter(this string s, string sep) {
      int i = s.IndexOf(sep);
      if (i == -1) return s;
      i += sep.Length;
      return s.Substring(i);
   }
   public static string SubstringBefore(this string s, string sep) {
      int i = s.IndexOf(sep);
      if (i == -1) return s;
      return s.Substring(0, i);
   }

   public static bool InRange(this int i, int low, int high) {

      if (i < low) return false;
      if (i >= high) return false;
      return true;
   }

   public static List<int> range(this int e) {
      List<int> res = new List<int>();
      for (int i = 0; i < e; ++i) res.Add(i);
      return res;
   }


   public static bool StartsWith(this string haystack, string needle, out string remaining) {
      if (haystack.FastStartsWith(needle)) {
         remaining = haystack.Substring(needle.Length);
         return true;
      }
      remaining = null;
      return false;
   }

   public static List<int> range(this int s, int e) {
      List<int> res = new List<int>();
      for (int i = s; i < e; ++i) res.Add(i);
      return res;
   }

   public static string Until(this string s, char first) {
      int b = s.IndexOf(first);
      Debug.Assert(b >= 0, $"Bad string {s}, should contain ['{first}']");
      return s.Substring(0, b);
   }

   public static string Between(this string s, char first, char last) {
      int b = s.IndexOf(first) + 1;
      int e = s.IndexOf(last);
      Debug.Assert(b <= e, $"Bad string {s}, should contain ['{first}', '{last}']");
      return s.Substring(b, e - b);
   }


   public static string ToSentenceCapitalization(this string a) {
      TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

      var sb = new System.Text.StringBuilder();

      bool first_letter = true;

      foreach (var c in a) {

         if (c == '.') {
            first_letter = true;
            sb.Append(c);
            continue;
         }

         if (first_letter) {
            if (char.IsDigit(c)) {
               first_letter = false;
               sb.Append(c);
               continue;
            }
            if (char.IsLetter(c)) {
               first_letter = false;
               sb.Append(myTI.ToUpper(c));
               continue;
            }
         }
         if (char.IsLetter(c)) {
            sb.Append(myTI.ToLower(c));
         } else {
            sb.Append(c);
         }
      }

      return sb.ToString();
   }
   public static string ToSnakeCase(this string a) {
      TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

      a = a.Replace(' ', '_');

      var sb = new System.Text.StringBuilder();

      int last_seen_us = 0;

      foreach (var c in a) {
         bool add_us = c == '_';
         if (c == '\'') continue;

         if (char.IsUpper(c)) {
            add_us = true;
         }

         if (add_us) {
            if (last_seen_us > 0) {
               sb.Append('_');
            }
            last_seen_us = 0;
         }

         if (c != '_') {
            sb.Append(myTI.ToLower(c));
            last_seen_us++;

         }
      }

      return sb.ToString();
   }


   public static string ToTitleCase(this string a) {
      TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

      a = a.Replace('_', ' ');

      var sb = new System.Text.StringBuilder();

      foreach (var c in a) {
         if (char.IsUpper(c)) {
            if (sb.Length > 0 && sb[sb.Length - 1] != ' ') {
               sb.Append(' ');
            }
         }
         sb.Append(c);
      }

      return myTI.ToTitleCase(sb.ToString());
   }
   public static IEnumerable<(T, U)> Zip<T, U>(this IEnumerator<T> ai, IEnumerator<U> bi) {

      while (ai.MoveNext() && bi.MoveNext()) {
         yield return ((T)ai.Current, (U)bi.Current);
      }
   }
   public static IEnumerable<List<T>> SplitIntoMaxSize<T>(this IEnumerable<T> a, int max_sz) {
      List<T> cur = new List<T>();
      foreach (var x in a) {
         cur.Add(x);
         if (cur.Count >= max_sz) {
            yield return cur;
            cur = new List<T>();
         }
      }
      if (cur.Count > 0) yield return cur;
   }
   public static string Join<T>(this IEnumerable<T> a, string sep) {
      if (a == null) return null;
      return string.Join(sep, a);
   }

   private static bool IsInstanceOfGenericType(System.Type type, Type genericType) {
      while (type != null) {
         if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
            return true;

         type = type.BaseType;
      }
      return false;
   }


   public static string GetCodeTypeName(this System.Type system) {
      if (system == typeof(void)) return "void";
      string append_val = "";
      string prepend_val = "";
      if (system.IsArray) {
         append_val = "[]";
         system = system.GetElementType();
      }

      if (system.IsNullableType()) {
         append_val = "?" + append_val;
         system = system.GetGenericArguments()[0];
      }

      if (system.IsByRef) {
         prepend_val = "ref  " + prepend_val;
         system = system.GetElementType();
      }

      var clean_name = system.Name.Split('`')[0];

      if (system.IsGenericType) {
         var ts = system.GetGenericArguments();
         
         var tns = ts.Join(", ", GetCodeTypeName);
         
         return prepend_val + short_type_names.Get(system, clean_name) + $"<{tns}>" + append_val;
         
      }

      return prepend_val + short_type_names.Get(system, clean_name) + append_val;
   }

   public static bool IsNullableType(this System.Type t) {
      if (!t.IsGenericType) return false;
      return nullable_base == t.GetGenericTypeDefinition();
   }

   static System.Type nullable_base = GetNullableType(GetNullableValue<int>()).GetGenericTypeDefinition();
   public static Dictionary<Type, string> short_type_names = new Dictionary<Type, string> {
      { typeof(int), "int" },
      { typeof(string), "string" },
      { typeof(float), "float" },
      { typeof(bool), "bool" },
      { typeof(char), "char" },
      { typeof(byte), "byte" },
      { typeof(byte*), "byte*" },
      { typeof(System.Exception), "System.Exception" },
      { typeof(object), "object" },
   };

   static T? GetNullableValue<T>() where T : struct {
      return new T { };
   }
   static System.Type GetNullableType<T>(T? o) where T : struct {
      o = new T();
      return typeof(T?).GetGenericTypeDefinition();
   }

   public static string Join<T>(this IEnumerable<T> a, string sep, System.Func<T, string> conv) {
      if (a == null) return null;
      return string.Join(sep, a.Map(conv));
   }
   public static IEnumerable<(T, T)> UniquePairs<T>(this IList<T> a) {
      for (int i = 0; i < a.Count; ++i) {
         for (int j = 0; j < i; ++j) {
            yield return (a[i], a[j]);
         }
      }
   }
   public static IEnumerable<(T, T)> UniquePairs<T>(this IEnumerable<T> a, IEnumerable<T> b) {

      HashSet<(T, T)> taken = new HashSet<(T, T)>();

      foreach (var kv in a.Cross(b)) {
         if (taken.Contains(kv)) continue;
         var (i, j) = kv;
         if (i.Equals(j)) continue;
         taken.Add((i, j));
         taken.Add((j, i));
         yield return (i, j);
      }
   }

   public static IEnumerable<(T, U)> Cross<T, U>(this IEnumerable<T> a, IEnumerable<U> b) {

      foreach (var i in a) foreach (var j in b) yield return (i, j);

   }
   public static IEnumerable<(int, T)> ZipIndex<T>(this IEnumerable<T> a) {

      int i = 0;
      foreach (var t in a) yield return (i++, t);
   }
   public static IEnumerable<(T, U)> Zip<T, U>(this IEnumerable<T> a, IEnumerator<U> bi) {
      return Zip(a.GetEnumerator(), bi);
   }
   public static IEnumerable<(T, U)> Zip<T, U>(this IEnumerator<T> ai, IEnumerable<U> b) {
      return Zip(ai, b.GetEnumerator());
   }
   public static bool Empty<T>(this List<T> a) {
      return a == null || a.Count == 0;
   }
   public static bool Empty<T>(this IEnumerable<T> a) {
      return a == null || !a.GetEnumerator().MoveNext();
   }
   public static bool NotEmpty<T>(this List<T> a) {
      return !a.Empty();
   }
   public static bool NotEmpty<T>(this IEnumerable<T> a) {
      return !a.Empty();
   }
   public static bool TrueForAll<T>(this IEnumerable<T> a, System.Func<T, bool> f) {
      foreach (var t in a) if (!f(t)) return false;
      return true;
   }

   public static IEnumerable<(T, U)> Zip<T, U>(this IEnumerable<T> a, IEnumerable<U> b) {
      return Zip(a.GetEnumerator(), b.GetEnumerator());
   }
   /*
   public static IEnumerable<(T, U, V)> Zip<T, U, V>(this List<T> a, List<U> b, List<V> c) {

      var ai = a.GetEnumerator();
      var bi = b.GetEnumerator();
      var ci = c.GetEnumerator();

      while (ai.MoveNext() && bi.MoveNext() && ci.MoveNext()) {
         yield return (ai.Current, bi.Current, ci.Current);
      }
   }
   */
   public static Color ScaleAlpha(this Color c, float a) {
      c.a *= a;
      return c;
   }
   public static Color KeepAlpha(this Color c, Color a) {
      a.a = c.a;
      return a;
   }

   public static T AsEnum<T>(this string s) where T : System.Enum {
      return EnumUtil.Parse<T>(s);
   }

   public static float Square(this float f) {
      return f * f;
   }
   public static string ToDecaString(this long f) {
      bool ltz = f < 0;
      if (ltz) f = -f;
      long d = f % 10;
      long r = f / 10;
      if (ltz) r = -r;
      if (d == 0) return r.ToString();
      return $"{r}.{d}";
   }
   public static string ToDecaString(this int f) {
      bool ltz = f < 0;
      if (ltz) f = -f;
      int d = f % 10;
      int r = f / 10;
      if (ltz) r = -r;
      if (d == 0) return r.ToString();
      return $"{r}.{d}";
   }
   public static string DecaString(this int f) {
      bool ltz = f < 0;
      if (ltz) f = -f;
      int d = f % 10;
      int r = f / 10;
      if (ltz) r = -r;
      if (d == 0) return r.ToString();
      return $"{r}.{d}";
   }
   public static int percent(this int f, int o = 1) {
      return f * o / 100;
   }
   public static string SignedString(this int f) {
      if (f > 0) return $"+{f}";
      return f.ToString();
   }
   public static IEnumerable<int> EnumerateToZero(this int f) {
      while (f > 0) {
         yield return f;
         f--;
      }
   }

   public static bool Is(this System.Type a, System.Type b) {
      return a == b || a.IsSubclassOf(b) || b.IsAssignableFrom(a);
   }
   public static bool Is<T>(this System.Type a) {
      var b = typeof(T);
      return a == b || a.IsSubclassOf(b) || b.IsAssignableFrom(a);
   }
   public static T GetLooped<T>(this IList<T> l, int i) {
      i %= l.Count;
      if (i < 0) i += l.Count;
      return l[i];
   }
   public static T GetDefault<T>(this IList<T> l, int i, T d) {
      if (l == null) return d;
      if (i < 0) return d;
      if (i >= l.Count) return d;
      return l[i];
   }

   public static T GetClamped<T>(this IList<T> l, int i) {
      if (i < 0) return l[0];
      if (i >= l.Count) return l[l.Count - 1];
      return l[i];
   }

   public static bool IsBitSet(this int i, int bit) {
      int k = i >> bit;
      return (k & 1) == 1;
   }

   public static int SetBit(this int i, int bit) {
      return i | (1 << bit);
   }
   public static bool IsBitSet(this long i, int bit) {
      long k = i >> bit;
      return (k & 1) == 1;
   }

   public static long SetBit(this long i, int bit, bool to_val) {
      if (to_val) return i | (1u << bit);
      return i & ~(1u << bit);
   }

   public static T Back<T>(this T[] val) {
      if (val.Length == 0) return default;
      return val[val.Length - 1];
   }

   public static string ToLabel(this string s) => s.AdjustRight(20);

   public static string ToLabel(this string s, string marker) => s.AdjustRight(20 - marker.Length) + marker;

   public static string AdjustLeft(this string s, int width, char c = ' ') {
      int e = width - s.Length;
      if (e > 0) s = s + new string(c, e);

      return s;
   }
   public static string AdjustRight(this string s, int width, char c = ' ') {
      int e = width - s.Length;
      if (e > 0) s = new string(c, e) + s;

      return s;
   }

   public static string Join<T>(this string s, IEnumerable<T> o, System.Func<T, string> to_string = null) {
      if (to_string != null) return string.Join(s, o.Map(x => to_string(x)));
      return string.Join(s, o.Map(x => x.ToString()));
   }

   public static T[] ToArray<T>(this ICollection<T> c) {
      return c.ToList().ToArray();
   }
   public static T[] ToArray<T>(this IList<T> c) {
      T[] arr = new T[c.Count];
      c.CopyTo(arr, 0);
      return arr;
   }

   public static Dictionary<string, List<T>> Group<T>(this IEnumerable<T> vals, System.Func<T, string> key) {
      Dictionary<string, List<T>> res = new Dictionary<string, List<T>>();

      foreach (var v in vals) {
         var k = key(v);
         List<T> r;
         if (res.TryGetValue(k, out r)) {
            r.Add(v);
         } else {
            res[k] = new List<T> { v };
         }
      }
      return res;
   }

   public static Rect WorldRect(this Camera camera) {
      float h = camera.orthographicSize;
      float w = h * camera.aspect;
      var a = new Vector3(w, h, 0);
      var res = new Rect(camera.transform.position - a, a * 2);
      return res;
   }

   public static T Front<T>(this IEnumerable<T> e) {
      var en = e.GetEnumerator();
      if (en.MoveNext()) return en.Current;
      return default;
   }
   public static T First<T>(this IEnumerable<T> e) {
      var en = e.GetEnumerator();
      if (en.MoveNext()) return en.Current;
      return default;
   }
   public static T Find<T>(this IEnumerable<T> e, System.Func<T, bool> f) {
      foreach (var t in e) if (f(t)) return t;
      return default;
   }

   public static T WhereSmallest<T, U>(this List<T> e, System.Func<T, U> f) where U : System.IComparable {
      U best = f(e[0]);
      T res = e[0];
      for (int i = 1; i < e.Count; ++i) {
         U cand = f(e[i]);
         if (cand.CompareTo(best) < 0) {
            res = e[i];
            best = cand;
         }
      }
      return res;
   }
   public static List<T> Rotated<T>(this IList<T> list, int steps) {

      List<T> res = new List<T>(list);

      for (int i = 0; i < list.Count; ++i) {
         res[(i + steps) % list.Count] = list[i];
      }
      return res;
   }

   public static void Shuffle<T>(this IList<T> list) {
      list.Shuffle(UnityEngine.Random.Range);
   }
   public static List<T> Shuffled<T>(this IList<T> list) {
      var res = list.ToList();
      res.Shuffle();
      return res;
   }
   public static List<T> Perturbed<T>(this IList<T> list) {
      var res = list.ToList();

      for (int i = 1; i < list.Count; i += 2) {

         if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f) {
            var a = list[i];
            list[i] = list[i - 1];
            list[i - 1] = a;
         }

      }
      for (int i = 2; i < list.Count; i += 2) {
         if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f) {
            var a = list[i];
            list[i] = list[i - 1];
            list[i - 1] = a;
         }
      }

      return res;
   }
   public static void Shuffle<T>(this IList<T> list, System.Func<int, int, int> rand_range) {
      int n = list.Count;
      while (n > 1) {
         n--;
         int k = rand_range(0, n + 1);
         T value = list[k];
         list[k] = list[n];
         list[n] = value;
      }
   }
   public static RectInt Clamp(this RectInt r, RectInt a) {
      var mi = r.Clamp(a.min);
      var ma = r.Clamp(a.max);
      return new RectInt(mi, ma - mi + Vector2Int.one);
   }

   public static Rect Include(this Rect r, Vector2 p) {
      if (!r.Contains(p)) {
         if (r.xMin > p.x) {
            r.xMin = p.x;
         } else if (r.xMax < p.x) {
            r.xMax = p.x;
         }

         if (r.yMin > p.y) {
            r.yMin = p.y;
         } else if (r.yMax < p.y) {
            r.yMax = p.y;
         }
      }
      return r;
   }
   public static Vector2 Clamp(this Rect r, Vector2 a) {
      var o = a;
      a.x = Mathf.Clamp(a.x, r.xMin, r.xMax - 1);
      a.y = Mathf.Clamp(a.y, r.yMin, r.yMax - 1);
      return a;
   }

   public static Vector2Int Clamp(this RectInt r, Vector2Int a) {
      var o = a;
      a.x = Mathf.Clamp(a.x, r.xMin, r.xMax - 1);
      a.y = Mathf.Clamp(a.y, r.yMin, r.yMax - 1);
      return a;
   }
   public static Vector2Int Rand(this RectInt rect) {
      return new Vector2Int(UnityEngine.Random.Range(rect.xMin, rect.xMax - 1), UnityEngine.Random.Range(rect.yMin, rect.yMax - 1));
   }
   public static Vector2Int Rand(this RectInt rect, System.Func<int, int, int> Range) {
      return new Vector2Int(Range(rect.xMin, rect.xMax - 1), Range(rect.yMin, rect.yMax - 1));
   }

   public static RectInt Pad(this RectInt r, int a) {
      return new RectInt(r.min - Vector2Int.one * a, r.size + Vector2Int.one * 2 * a);
   }
   public static Rect Pad(this Rect r, float a) {
      return new Rect(r.min - Vector2.one * a, r.size + Vector2.one * 2 * a);
   }
   public static Rect Pad(this Rect r, float w, float h) {
      Vector2 a = new Vector2(w, h);
      return new Rect(r.min - a, r.size + 2 * a);
   }
   public static Rect ResizeFraction(this Rect r, float a) {
      var sz = r.size;
      sz *= a;
      var d = r.size - sz;

      Rect nr = new Rect(r.position + d * 0.5f, sz);

      return nr;
   }
   public static void View(this Camera camera, Rect r) {
      float h = Mathf.Max(r.height, r.width / camera.aspect);
      camera.transform.position = r.center;
      camera.orthographicSize = h / 2;
   }

   public static IEnumerable<T> RandomStart<T>(this IList<T> a) {
      int s = UnityEngine.Random.Range(0, a.Count);
      for (int i = 0; i < a.Count; ++i) yield return a[(i + s) % a.Count];
   }

   public static T Random<T>(this IList<T> a) {
      return a[UnityEngine.Random.Range(0, a.Count)];
   }
   public static T Random<T>(this IList<T> a, System.Func<int, int, int> rand_range) {
      return a[rand_range(0, a.Count)];
   }
   public static int Sum(this IEnumerable<int> e) {
      int res = 0;
      foreach (var t in e) res += t;
      return res;
   }
   public static float Sum(this IEnumerable<float> e) {
      float res = 0;
      foreach (var t in e) res += t;
      return res;
   }
   public static int Sum<T>(this IEnumerable<T> e, System.Func<T, int> a) {
      int res = 0;
      foreach (var t in e) res += a(t);
      return res;
   }

   public static T Random<T>(this IList<T> a, T def) {
      return a.Count > 0 ? a[UnityEngine.Random.Range(0, a.Count)] : def;
   }
   public static T Random<T>(this IList<T> a, T def, System.Func<int, int, int> rand_range) {
      return a.Count > 0 ? a[rand_range(0, a.Count)] : def;
   }


   public static List<T> RandomN<T>(this List<T> a, int n) {
      a = new List<T>(a);
      a.Shuffle();
      if (a.Count > n) {
         a.RemoveRange(n, a.Count - n);
      }
      return a;
   }
   public static List<T> RandomN<T>(this List<T> a, int n, System.Func<int, int, int> rand_range) {
      a = new List<T>(a);
      a.Shuffle(rand_range);
      if (a.Count > n) {
         a.RemoveRange(n, a.Count - n);
      }
      return a;
   }
   public static void AddUnique<T>(this List<T> a, T element) {
      for (int i = 0; i < a.Count; ++i) {
         if (a[i].Equals(element)) return;
      }
      a.Add(element);
   }
   public static void AddUniqueRange<T>(this List<T> a, IEnumerable<T> element) {
      var pick = a.ToSet();
      foreach (var x in element) {
         if (pick.Contains(x)) continue;
         pick.Add(x);
         a.Add(x);
      }
   }
   public static void AddSorted<T>(this List<T> a, T element, System.Func<T, bool> insert_before) {
      int i = 0;
      for (; i < a.Count; ++i) {
         if (insert_before(a[i])) {
            a.Insert(i, element);
            return;
         }
      }
      a.Add(element);
   }
   public static List<T> Sorted<T>(this IList<T> a) {
      return a.OrderBy(x => x).ToList();
   }
   public static List<T> Sorted<T>(this IEnumerable<T> a) where T : System.IComparable {
      return a.OrderBy(x => x).ToList();
   }
   public static List<T> Sorted<T>(this IEnumerable<T> a, System.Func<T, System.IComparable> order_by) {
      return a.OrderBy(order_by).ToList();
   }
   public static List<T> SubList<T>(this IList<T> a, int i, int? ee = null) {
      int e = ee ?? a.Count;
      if (e > a.Count) e = a.Count;
      if (i < 0) i = a.Count + i;
      if (i < 0) i = 0;
      if (i > e) i = e;
      List<T> res = new List<T>();
      res.Capacity = e - i;
      for (; i < e; ++i) res.Add(a[i]);
      return res;
   }
   public static (List<T>, List<T>) SplitList<T>(this IList<T> a, int i) {
      if (a.Count == 0) return (new List<T>(), new List<T>());
      if (i < 0) i = a.Count + i;
      if (i >= a.Count) i = a.Count - 1;
      if (i < 0) i = 0;
      List<T> r1 = new List<T>();
      List<T> r2 = new List<T>();
      for (int j = 0; j < i; ++j) {
         r1.Add(a[j]);
      }
      for (int j = i; j < a.Count; ++j) {
         r2.Add(a[j]);
      }
      return (r1, r2);
   }

   public static List<T> SubList<T>(this IEnumerable<T> a, int i, int? ee = null) {
      return a.ToList().SubList(i, ee);
   }
   public static List<T> Sorted<T, U>(this IList<T> a, System.Func<T, U> order_by) where U : System.IComparable {
      return a.OrderBy(order_by).ToList();
   }

   public static List<T> Flatten<T>(this IEnumerable<T> a, params IEnumerable<T>[] b) {
      List<T> res = new List<T>();
      foreach (var x in a) res.Add(x);
      foreach (var l in b) foreach (var x in l) res.Add(x);
      return res;
   }
   public static List<T> Where<T>(this List<T> a, System.Predicate<T> p) {
      List<T> res = new List<T>();
      foreach (var x in a) if (p(x)) res.Add(x);
      return res;
   }

   public static int CountSpecific<T>(this IEnumerable<T> a, System.Predicate<T> p) {
      int res = 0;
      foreach (var x in a) if (p(x)) res++;
      return res;
   }

   public static void Filter<T>(this Queue<T> a, System.Predicate<T> p) {
      var old_actions = a.Where(x => p(x));
      a.Clear();
      foreach (var x in old_actions) a.Enqueue(x);
   }


   static object[] helper_array = new object[1024];

   public static T[] WhereRemoved<T>(this T[] a, T p) where T : class {

      int ix = a.IndexOf(p);

      T[] r;
      if (ix == -1) r = new T[a.Length];
      else r = new T[a.Length - 1];
      int o = 0;
      for (int i = 0; i < a.Length; ++i) {
         if (ix == i) continue;
         r[o++] = a[i];
      }
      return r;
   }
   public static T[] WhereArray<T>(this T[] a, System.Predicate<T> p) where T : class {


      int n = 0;
      int pn = a.Length;
      for (int i = 0; i < pn; ++i) {
         if (p(a[i])) helper_array[n++] = a[i];
      }

      T[] res = new T[n];

      for (int i = 0; i < n; ++i) {
         res[i] = (T)helper_array[i];
      }
      return res;
   }

   public static T[] WhereStructArray<T>(this T[] a, System.Predicate<T> p) where T : struct {


      int n = 0;
      int pn = a.Length;
      for (int i = 0; i < pn; ++i) {
         if (p(a[i])) n++;
      }
      T[] res = new T[n];

      int ni = 0;

      for (int i = 0; i < pn; ++i) {
         if (p(a[i])) res[ni++] = a[i];
      }
      return res;
   }
   public static IEnumerable<T> WhereExists<T>(this IEnumerable<T> a) where T: UnityEngine.Object {
      return a.Where(x => x);
   }
   public static List<T> Where<T>(this IEnumerable<T> a, System.Predicate<T> p) {
      List<T> res = new List<T>();
      foreach (var x in a) if (p(x)) res.Add(x);
      return res;
   }
   public static List<U> WhereType<U, T>(this IEnumerable<T> a) where U : T {
      List<U> res = new List<U>();
      foreach (var x in a) if (x is U) res.Add((U)x);
      return res;
   }
   public static List<U> WhereType<U>(this IEnumerable a) {
      List<U> res = new List<U>();
      foreach (var x in a) if (x is U) res.Add((U)x);
      return res;
   }

   public static ListImplicit<T> Filter<T>(this ListImplicit<T> a, System.Predicate<T> p) {
      int n = 0;
      for (int i = 0; i < a.Count; ++i) if (p(a[i])) a[n++] = a[i];
      a.RemoveRange(n, a.Count - n);
      return a;
   }
   public static ListImplicit<T> Filter<T>(this ListImplicit<T> a, System.Predicate<T> p, System.Action<T> on_remove) {
      int n = 0;
      for (int i = 0; i < a.Count; ++i) {
         if (p(a[i])) a[n++] = a[i];
         else on_remove(a[i]);
      }
      a.RemoveRange(n, a.Count - n);
      return a;
   }

   public static List<T> Filtered<T>(this List<T> a, System.Predicate<T> p) {
      List<T> res = new List<T>();
      foreach (var x in a) if (p(x)) res.Add(x);
      return res;
   }
   public static List<T> Filter<T>(this List<T> a, System.Predicate<T> p) {
      int n = 0;
      for (int i = 0; i < a.Count; ++i) if (p(a[i])) a[n++] = a[i];
      a.RemoveRange(n, a.Count - n);
      return a;
   }
   public static List<T> Filter<T>(this List<T> a, System.Predicate<T> p, System.Action<T> on_remove) {
      int n = 0;
      for (int i = 0; i < a.Count; ++i) {
         if (p(a[i])) a[n++] = a[i];
         else on_remove(a[i]);
      }
      a.RemoveRange(n, a.Count - n);
      return a;
   }
   public static HashSet<T> Filter<T>(this HashSet<T> a, System.Predicate<T> p) {
      a.RemoveWhere(x => !p(x));
      return a;
   }

   public static IEnumerable<Vector2Int> HexPosWithin(this Vector2Int center, int radius) {
      RectInt r = new RectInt(center.x - radius, center.y - radius, radius * 2 + 1, radius * 2 + 1);
      foreach (var p in r.allPositionsWithin) {
         if (center.HexDist(p) <= radius) yield return p;
      }
   }

   public static IList ToList(this IEnumerable a) {
      var res = new List<object>();
      foreach (var o in a) res.Add(o);
      return res;
   }
   public static List<T> ToList<T>(this IEnumerable<T> a) {
      List<T> res = new List<T>(a);
      return res;
   }
   public static HashSet<T> ToSet<T>(this IEnumerable<T> a) {
      HashSet<T> res = new HashSet<T>(a);
      return res;
   }
   public static Dictionary<K, T> ToKeyDictUnique<K, T>(this IEnumerable<T> a, System.Func<T, K> key_func) {

      Dictionary<K, T> res = new Dictionary<K, T>();
      foreach (var t in a) res[key_func(t)] = t;
      return res;
   }
   public static Dictionary<K, List<T>> ToKeyDict<K, T>(this IEnumerable<T> a, System.Func<T, K> key_func) {

      Dictionary<K, List<T>> res = new Dictionary<K, List<T>>();
      foreach (var t in a) res.GetDefaultC(key_func(t)).Add(t);
      return res;
   }
   public static Dictionary<T, int> ToCountSet<T>(this IEnumerable<T> a) {
      Dictionary<T, int> res = new Dictionary<T, int>();
      foreach (var t in a) res.Increment(t);
      return res;
   }
   public static List<T> Rotate<T>(this IEnumerable<T> aa, int steps) {
      List<T> a = new List<T>(aa);

      List<T> res = new List<T>();

      for (int i = 0; i < a.Count; ++i) {
         res.Add(a[(i + steps) % a.Count]);
      }

      return res;
   }
   public static List<T> ToList<T>(this IEnumerable<T> a, int limit) {
      List<T> res = new List<T>();

      foreach (var x in a) {
         if (limit-- <= 0) break;
         res.Add(x);
      }

      return res;
   }

   public static bool IsInRange<T>(this List<T> l, int i) {

      if (i < 0 || i >= l.Count) {
         return false;
      }
      return true;
   }

   public static T MaybeGet<T>(this List<T> l, int i) {

      if (i < 0 || i >= l.Count) {
         return default;
      }
      return l[i];
   }
   public static T GetSafe<T>(this List<T> l, int i) {

      if (i < 0 || i >= l.Count) {
         throw new System.Exception($"Index {i} out of range {l.Count}!");
      }
      return l[i];
   }

   public static T Min<T>(this IEnumerable<T> a) where T : System.IComparable {
      if (a.Empty()) return default;
      T res = a.First();
      foreach (var t in a) if (t.CompareTo(res) < 0) res = t;
      return res;
   }
   public static T Max<T>(this IEnumerable<T> a) where T : System.IComparable {
      if (a.Empty()) return default;
      T res = a.First();
      foreach (var t in a) if (t.CompareTo(res) > 0) res = t;
      return res;
   }
   public static U Min<T, U>(this IEnumerable<T> a, System.Func<T, U> f) where U : System.IComparable {
      if (a.Empty()) return default;
      var res = f(a.First());
      foreach (var t in a) {
         var u = f(t);
         if (u.CompareTo(res) < 0) res = u;
      }
      return res;
   }
   public static U Max<T, U>(this IEnumerable<T> a, System.Func<T, U> f) where U : System.IComparable {
      if (a.Empty()) return default;
      var res = f(a.First());
      foreach (var t in a) {
         var u = f(t);
         if (u.CompareTo(res) > 0) res = u;
      }
      return res;
   }
   public static U[] MapToArray<T, U>(this List<T> a, System.Func<T, U> p) {
      var res = new U[a.Count];
      int n = a.Count;
      for (int i = 0; i < n; ++i) res[i] = p(a[i]);
      return res;
   }
   public static List<U> Map<T, U>(this List<T> a, System.Func<T, U> p) {
      List<U> res = new List<U>();
      res.Capacity = a.Count;
      foreach (var x in a) res.Add(p(x));
      return res;
   }
   public static void Swap1<T>(this List<T> a, int at) {

      var x = a[at];
      var y = a[at - 1];
      a[at] = y;
      a[at - 1] = x;


   }
   public static List<T> Unique<T>(this IEnumerable<T> a) {

      HashSet<T> x = new HashSet<T>();
      List<T> res = new List<T>();

      foreach (var t in a) {
         if (x.Contains(t)) continue;
         x.Add(t);
         res.Add(t);
      }
      return res;
   }
   public static List<T> Unique<T, F>(this IEnumerable<T> a, System.Func<T, F> unique_map) {

      HashSet<F> x = new HashSet<F>();
      List<T> res = new List<T>();

      foreach (var t in a) {
         var f = unique_map(t);
         if (x.Contains(f)) continue;
         x.Add(f);
         res.Add(t);
      }
      return res;
   }

   public static U[] ArrayMap<T, U>(this T[] a, System.Func<T, U> p) {
      int n = a.Length;
      U[] res = new U[n];
      for (int i = 0; i < n; ++i) {
         res[i] = p(a[i]);
      }
      return res;
   }
   public static void Foreach<T>(this IEnumerable<T> a, System.Action<T> p) {
      foreach (var x in a) p(x);
   }
   public static U[] map<T, U>(this IEnumerable<T> a, System.Func<T, U> p) {
      return a.Map(p).ToArray();
   }
   public static List<U> Map<T, U>(this IEnumerable<T> a, System.Func<T, U> p) {
      List<U> res = new List<U>();
      foreach (var x in a) res.Add(p(x));
      return res;
   }
   public static List<U> Map<T, U>(this IEnumerator<T> a, System.Func<T, U> p) {
      List<U> res = new List<U>();
      while (a.MoveNext()) res.Add(p(a.Current));
      return res;
   }
   public static List<U> FlatMap<T, U>(this IEnumerable<T> a, System.Func<T, IEnumerable<U>> p) {
      List<U> res = new List<U>();
      foreach (var x in a) foreach (var y in p(x)) res.Add(y);
      return res;
   }
   public static List<U> MapFilter<T, U>(this IEnumerable<T> a, System.Func<T, U> p) where U : class {
      List<U> res = new List<U>();
      foreach (var x in a) {
         var r = p(x);
         if (r != null) res.Add((U)r);
      }
      return res;
   }
   public static void Filter<T, U>(this Dictionary<T, U> a, System.Predicate<KeyValuePair<T, U>> p) where U : class {
      foreach (var k in a.ToList()) {
         if (!p(k)) a.Remove(k.Key);
      }
   }

   public static void OnClick(this GameObject o, System.Action a) {
      var b = o.GetComponent<Button>();
      if (!b) b = o.AddComponent<Button>();
      b.onClick.AddListener(() => a());
   }

   public static string FormatHTML(this Color c) {
      int r = Mathf.RoundToInt(c.r * 255);
      int g = Mathf.RoundToInt(c.g * 255);
      int b = Mathf.RoundToInt(c.b * 255);
      return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
   }

   public static bool ItemEquals<T>(this List<T> a, List<T> b) {
      if (a.Count != b.Count) return false;
      for (int i = 0; i < a.Count; ++i) {
         var ai = a[i];
         var bi = b[i];
         if (!ai.Equals(bi)) {
            return false;
         }
      }
      return true;
   }

   public static List<Transform> ChildList(this Transform t) {
      int n = t.childCount;
      List<Transform> res = new List<Transform>();
      for (int i = 0; i < n; ++i) res.Add(t.GetChild(i));
      return res;
   }

   public static List<Transform> ReverseChildList(this Transform t) {
      int n = t.childCount;
      List<Transform> res = new List<Transform>();
      for (int i = n - 1; i >= 0; --i) res.Add(t.GetChild(i));
      return res;
   }

   public static RectTransform OnClick(this RectTransform t, System.Action a) {
      var b = t.GetComponent<Button>();
      if (b == null) b = t.gameObject.AddComponent<Button>();
      b.onClick.AddListener(() => a());
      return t;
   }

   public static V Get<K, V>(this Dictionary<K, V> d, K k, V def=default) {
      if (d == null) return def;
      if (d.TryGetValue(k, out V res)) {
         return res;
      }
      return def;
   }
   public static V Get<V>(this IList<V> d, int k, V alt=default) {
      if (d == null) return alt;
      if (k < 0 || k >= d.Count) return alt;
      return d[k];
   }

   public static Dictionary<K2, V> MapKeys<K, V, K2>(this Dictionary<K, V> d, System.Func<K, K2> f) {
      Dictionary<K2, V> res = new Dictionary<K2, V>();
      foreach (var it in d) res[f(it.Key)] = it.Value;
      return res;
   }

   public static Dictionary<K, V2> MapValues<K, V, V2>(this Dictionary<K, V> d, System.Func<V, V2> f) {
      Dictionary<K, V2> res = new Dictionary<K, V2>();
      foreach (var it in d) res[it.Key] = f(it.Value);
      return res;
   }

   public static T Back<T>(this IList<T> x) {
      T res = default;
      return x.Count > 0 ? x[x.Count - 1] : res;
   }

   public static T Back<T>(this IList<T> x, int i) {
      T res = default;
      return x.Count > 0 ? x[x.Count - 1 - i] : res;
   }

   public static T Pop<T>(this List<T> x) {
      var res = x.Back();
      x.RemoveAt(x.Count - 1);
      return res;
   }

   public static T PopFront<T>(this List<T> x) {
      var res = x[0];
      x.RemoveAt(0);
      return res;
   }

   public static void Push<T>(this List<T> x, T item) {
      x.Add(item);
   }

   public static Rect ToScreenSpace(this RectTransform transform) {
      var worldCorners = new Vector3[4];
      transform.GetWorldCorners(worldCorners);
      var result = new Rect(
                    worldCorners[0].x,
                    worldCorners[0].y,
                    worldCorners[2].x - worldCorners[0].x,
                    worldCorners[2].y - worldCorners[0].y);
      return result;
   }
   public static Rect ToScreenSpace_New(this RectTransform transform) {


      var rect = transform.rect;
      if (transform.parent) {
         var p = transform.parent.localToWorldMatrix;

         Vector2 min = p.MultiplyPoint(rect.min);
         Vector2 max = p.MultiplyPoint(rect.max);

         var sz = max - min;
         rect = new Rect(min, sz);
      }
      return rect;
   }

   public static Rect ToScreenSpaceBuggy(this RectTransform transform) {
      Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
      return new Rect((Vector2)transform.position - (size * 0.5f), size);
   }

   public static bool ContainsMouse(this RectTransform transform, int margin = 0) {
      Rect rect = transform.ToScreenSpace();
      Vector2 pointer_pos = InputExt.mousePosition;

      if (margin > 0) {
         rect = rect.Pad(-margin);
      }

      return rect.Contains(pointer_pos);
   }

   public static Vector2 NormalizedMousePos(this RectTransform transform) {
      Rect rect = transform.ToScreenSpace();
      Vector2 pointer_pos = InputExt.mousePosition;

      pointer_pos -= rect.position;

      pointer_pos /= rect.size;
      return pointer_pos;
   }

   public static T ForceComponent<T>(this GameObject o) where T : UnityEngine.Component {
      var res = o.GetComponent<T>();
      if (res != null) return res;
      return o.AddComponent<T>();
   }
   public static T ForceComponent<T>(this Component o) where T : Component {
      return o.gameObject.ForceComponent<T>();
   }
}
