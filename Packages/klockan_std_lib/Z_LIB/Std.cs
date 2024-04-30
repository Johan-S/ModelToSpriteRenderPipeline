﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static System.Linq.Enumerable;
using static System.Reflection.BindingFlags;
using Random = UnityEngine.Random;


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ButtonAttribute : Attribute {
}


[Serializable]
public class UnityEventString : UnityEvent<string> {
}


[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class PrefAttribute : Attribute {
}


public static class Std {

   public static Vector2 Average(this IEnumerable<Vector2> p) {
      var res = new Vector2();

      int n = 0;

      foreach (var x in p) {
         res = (res + x);
         n++;
      }

      if (n == 0) return default;
      return res / n;
   }
   

   // Clamped noise to -1 to 1
   public static float PNoise(float2 pos, float2 repete) {
      float a = Unity.Mathematics.noise.pnoise(new Vector2(pos.x, pos.y), new Vector2(repete.x, repete.y));
      return Mathf.Clamp(a, -1, 1);
   }

   public static List<T[]> Split<T>(this T[] arr, Predicate<T> pr) {
      List<T[]> res = new();
      int last = 0;
      int i = 0;
      for (; i < arr.Length; i++) {
         if (pr(arr[i])) {
            res.Add(arr[last..i]);
            last = i + 1;
         }
      }
      res.Add(arr[last..i]);
      return res;
   }
   
   public static Span<T> AsSpan_WIP<T>(this List<T> l) {
      T[] itemss = SpanCache<T>.get(l);
      return itemss.AsSpan(0, l.Count);
   }

   public static bool IsWhitespace(this string s) {
      foreach (var c in s) {
         if (!char.IsWhiteSpace(c)) return false;
      }
      return true;
   }
   
   public static bool IsNullEmptyWhitespace(this string s) => s == null || s.Length == 0 || s.IsWhitespace();

   static class SpanCache<T> {
      public static FieldInfo span_field = typeof(List<T>).GetField("_items", NonPublic | Instance | GetField);
      public static T[] get(List<T> l) => (T[])span_field.GetValue(l);
   }


   public static Vector2Int plus(this Vector2Int a, int x, int y) {
      return new(a.x + x, a.y + y);
   }

   public static IDisposable CallAfter(Action a) {
      return new CallDisposed(a);
   }

   class CallDisposed : IDisposable {
      Action a;

      public CallDisposed(Action a) {
         this.a = a;
      }

      public void Dispose() {
         a?.Invoke();
      }
   }

   static int load_id = 1;
   public static int LoadId => load_id;


   public static void CopyShallowDuckTyped(object from_t, object to_u) {
      var t = from_t.GetType();
      var u = to_u.GetType();

      var fa = t.GetFields(GetField | Public | Instance).ToDictionary(x => (x.Name, x.FieldType), x => x);
      var fb = u.GetFields(SetField | Public | Instance).ToDictionary(x => (x.Name, x.FieldType), x => x);

      foreach (var kv in fb) {
         if (fa.TryGetValue(kv.Key, out var ft)) {
            kv.Value.SetValue(to_u, ft.GetValue(from_t));
         }
      }
   }

   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void OnLoad() {
      load_id++;
   }

   public static (T, T) Unpack2<T>(this T[] t) {
      Debug.Assert(t.Length == 2);
      return (t[0], t[1]);
   }

   public static (T, T, T) Unpack3<T>(this T[] t) {
      Debug.Assert(t.Length == 3);
      return (t[0], t[1], t[2]);
   }

   public static Vector2Int RotateRight(this Vector2Int p) {
      if (p.x == 0) {
         return new(p.y, p.y);
      }

      if (p.y == 0) {
         return new(p.x, -p.x);
      }

      return new((p.x + p.y) / 2, -(p.x - p.y) / 2);
   }
   
   public static Vector2 Rotate(this Vector2 v, float degrees) {
      degrees *= Mathf.Deg2Rad;
      return new Vector2(
         v.x * Mathf.Cos(degrees) - v.y * Mathf.Sin(degrees),
         v.x * Mathf.Sin(degrees) + v.y * Mathf.Cos(degrees)
      );
   }

   public static Vector2Int RotateLeft(this Vector2Int p) {
      if (p.x == 0) {
         return new(-p.y, p.y);
      }

      if (p.y == 0) {
         return new(p.x, p.x);
      }

      return new((p.x - p.y) / 2, (p.x + p.y) / 2);
   }

   public static int DistanceSquare(this Vector2Int p, Vector2Int o) {
      return (p - o).sqrMagnitude;
   }

   public static float DistanceTo(this Vector2Int p, Vector2Int o) {
      return (p - o).magnitude;
   }

   public static int DistanceMax(this Vector2Int p, Vector2Int o) {
      return Mathf.Max(Mathf.Abs(p.x - o.x), Mathf.Abs(p.y - o.y));
   }

   public static int DistanceTaxi(this Vector2Int p, Vector2Int o) {
      return Mathf.Abs(p.x - o.x) + Mathf.Abs(p.y - o.y);
   }

   public static int Approx2Distance(this Vector2Int p, Vector2Int o) {
      var a = (p - o).abs();

      return a.x + a.y + Mathf.Max(a.x, a.y);
   }

   public static Vector2Int abs(this Vector2Int p) => new(Mathf.Abs(p.x), Mathf.Abs(p.y));

   public static Vector2Int Floor(this Vector2 p) {
      return Vector2Int.FloorToInt(p);
   }

   public static float DiffNearest(this float f, params float[] pts) {

      if (pts.IsNullOrEmpty()) return 0;

      return f - pts.WhereSmallest(x => Mathf.Abs(x - f));
   }

   public static float Sign(this float f) => Mathf.Sign(f);
   public static float Abs(this float f) => Mathf.Abs(f);

   public static Vector2Int Round(this Vector2 p) {
      return Vector2Int.RoundToInt(p);
   }

   public static T Find<T>(this T[] arr, Predicate<T> p) {
      return Array.Find(arr, p);
   }

   public static Vector2 Float(this Vector2Int p) => p;

   public static Vector2 ProjectTo(this Vector2 a, Vector2 axis) =>  a.Dot(axis) / axis.sqrMagnitude * axis;

   public static float Dot(this Vector2 a, Vector2 b) => Vector2.Dot(a, b);
   public static int Dot(this Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y;


   public static Vector2 average(this IEnumerable<Vector2Int> ul) {
      int c = 0;
      Vector2Int r = default;
      foreach (var p in ul) {
         c++;
         r += p;
      }

      if (c > 0) return r.Float() / c;
      return r;
   }

   public static IEnumerable<(int i, T value)> enumerate<T>(this IEnumerable<T> xs) {
      int i = 0;
      foreach (var x in xs) yield return (i++, x);
   }

   public static IEnumerable<int2> times(this (int i, int j) n) {
      for (int i = 0; i < n.i; i++) {
         for (int j = 0; j < n.j; j++) {
            yield return (i, j);
         }
      }
   }
   public static IEnumerable<T> times<T>(this (int i, int j) n, Func<int2, T> f) {
      for (int i = 0; i < n.i; i++) {
         for (int j = 0; j < n.j; j++) {
            yield return f((i, j));
         }
      }
   }

   public static IEnumerable<(int x, int y)> times(this Vector2Int n) {
      for (int i = 0; i < n.x; i++) {
         for (int j = 0; j < n.y; j++) {
            yield return (i, j);
         }
      }
   }

   public static Vector2Int Min(this Vector2Int n, Vector2Int p) {
      return new(Mathf.Min(n.x, p.x), Mathf.Min(n.y, p.y));
   }

   public static T GetRand<T>(this IList<T> l) {
      return l[Random.Range(0, l.Count)];
   }

   public static int[] times(this int n) {
      int[] r = new int[n];
      for (int i = 0; i < n; i++) r[i] = i;
      return r;
   }

   public static string ToBaseSTring(this int a, int base_i) => System.Convert.ToString(a, base_i);

   public static int PopCount(this int n) {

      int i = 0;
      int res = 0;
      while (n > 0) {
         if ((n & 1) != 0) res++;
         n >>= 1;
         i++;
      }

      return res;
   }
   public static int[] bits_set(this int n) {


      static IEnumerable<int> bits(int n) {
         int i = 0;
         while (n > 0) {
            if ((n & 1) != 0) yield return i;
            n >>= 1;
            i++;
         }
      }

      return bits(n).ToArray();
   }

   public static T[] times<T>(this int n, Func<int, T> f) => n.times().map(f);

   public static string join<T>(this string sep, IEnumerable<T> e, Func<T, string> to_str = null) {
      if (to_str != null) return string.Join(sep, e.Select(to_str));
      return string.Join(sep, e);
   }

   public static string join<T>(this IEnumerable<T> ie, string sep, Func<T, string> to_str = null) {
      return sep.join(ie, to_str);
   }

   public static T[] SubArray<T>(this T[] arr, int s, int e) {
      int n = e - s;
      var res = new T[n];
      for (int i = s; i < e; i++) {
         res[i - s] = arr[i];
      }

      return res;
   }

   public static Vector2 SafeNormalized(this Vector2 v) {
      if (v == Vector2.zero) return new(1, 0);
      return v.normalized;
   }

   public static FieldInfo[] InstanceFields(this object o, bool non_public = false) {
      var f = Public | GetField | Instance;
      if (non_public) f |= NonPublic;
      return o.GetType().GetFields(f);
   }

   public static int AUDIO_SAMPLE_RATE;

#if UNITY_EDITOR
   [InitializeOnLoadMethod()]
#endif
   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
   static void SetSampleRate() {
      AUDIO_SAMPLE_RATE = AudioSettings.outputSampleRate;

      AUDIO_SAMPLE_SIN_D = Mathf.PI * 2 / AUDIO_SAMPLE_RATE;
   }

   public static float AUDIO_SAMPLE_SIN_D;

   public static string[] SplitE(this string s, string sep, int count) =>
      s.Trim().Split(sep, count, StringSplitOptions.RemoveEmptyEntries).Trim();

   public static string[] SplitE(this string s, string sep) =>
      s.Trim().Split(sep, StringSplitOptions.RemoveEmptyEntries).Trim();

   public static string[] Splits(this string s, string sep) =>
      s.Split(sep, StringSplitOptions.RemoveEmptyEntries).Trim();

   public static E ToEnum<E>(this string s) where E : struct, Enum {
      return Enum.Parse<E>(s);
   }

   public static bool ParseTo<E>(this string s, ref E i) where E : struct, Enum {
      if (Enum.TryParse<E>(s, out var r)) {
         i = r;
         return true;
      }

      return false;
   }

   public static bool ParseToInt(this string s, ref int i) {
      if (int.TryParse(s, out var r)) {
         i = r;
         return true;
      }

      return false;
   }

   public static void Do<K, V>(this Dictionary<K, V> d, K k, Action<V> a) {
      if (d.TryGetValue(k, out var r)) a(r);
   }

   public static string[] Trim(this IEnumerable<string> sl) => sl.Select(x => x.Trim()).ToArray();

   public static string[][] Splits(this string s, string sep, string sep2) {
      if (s.Length == 0) return Array.Empty<string[]>();
      return s.Split(sep).Select(x => x.Split(sep2).Trim()).ToArray();
   }

   public static (string k, string v)[] SplitPairs(this string s, string sep, string sep2) {
      if (s.Length == 0) return Array.Empty<(string, string)>();
      return s.Split(sep).Select(x => x.Split(sep2).Trim()).Select(x => (x[0], x[1])).ToArray();
   }


   public static void FillSineSamples(this float[] data, int channels, float hz, float amplitude, ref float phase) {
      float p = phase;

      float d = hz * AUDIO_SAMPLE_SIN_D;

      int n = data.Length / channels;

      for (int i = 0; i < n; i++) {
         data[i] = Mathf.Sin(p) * amplitude;
         p += d;
      }

      if (channels > 1) {
         for (int i = data.Length - 1; i >= 0; i--) {
            data[i] = data[i / channels] * amplitude;
         }
      }


      phase = p.NormalizeRadians();
   }

   public static float[] GetSampleData(this AudioClip clip) {
      float[] d = new float[clip.samples];
      clip.GetData(d, 0);

      return d;
   }

   public static void FillSineSamples(this float[] data, int channels, float hz, float amplitude) {
      float a = 0;
      FillSineSamples(data, channels, hz, amplitude, ref a);
   }


   public static AudioClip MakeToneClip_Streamed(float hz, float seconds = 1) {
      float i = 0;

      void PcmCall(float[] xs) {
         FillSineSamples(xs, 1, hz, 0.1f, ref i);
      }

      var clip = AudioClip.Create($"Time {hz}", Mathf.RoundToInt(AudioSettings.outputSampleRate * seconds), 1,
         AudioSettings.outputSampleRate, true, PcmCall);
      return clip;
   }

   public static void PlayClip(this AudioClip clip) {
      if (!Application.isPlaying) return;
      var audio = Resources.Load<AudioSource>("AudioSource");

      audio = GameObject.Instantiate(audio);

      audio.clip = clip;

      audio.Play();

      GameObject.Destroy(audio, clip.length + 0.1f);
   }

   public static AudioClip MakeToneClip(float hz, float seconds = 1, float amplitude = 0.08f) {
      var clip = AudioClip.Create($"tone_{hz}", Mathf.RoundToInt(AudioSettings.outputSampleRate * seconds), 1,
         AudioSettings.outputSampleRate, false);
      float[] data = new float[clip.samples];
      FillSineSamples(data, clip.channels, hz, amplitude);
      clip.SetData(data, 0);
      return clip;
   }

   public static void SmoothEdges(float[] data, int n, int r) {
      for (int i = 0; i < n; i++) {
         float t = 1f * i / n;
         data[i] *= t;
      }

      for (int i = 0; i < r; i++) {
         float t = 1f * i / r;
         data[data.Length - i - 1] *= t;
      }
   }

   public static T[] Shuffled<T>(this IEnumerable<T> col) {
      var l = col.ToArray();
      for (int i = 0; i + 1 < l.Length; i++) {
         int c = Random.Range(i, l.Length);

         (l[i], l[c]) = (l[c], l[i]);
      }

      return l;
   }

   public static string[] EnsureUniqueSymbols(this string[] names) {
      HashSet<string> c = new();

      names = names.ToArray();

      for (int i = 0; i < names.Length; i++) {
         int ec = 1;
         string cand = names[i];

         while (!c.Add(cand)) {
            cand = names[i] + $"{++ec}";
         }

         names[i] = cand;
      }

      return names;
   }

   public static string[] SplitLines(this string text, bool skip_empty = false) {
      var t = text.Replace("\r\n", "\n").Split("\n");
      if (skip_empty) t = t.Where(x => x.Trim().Length > 0).ToArray();
      return t;
   }

   public static string[] GetLines(this TextAsset text) {
      return text.text.SplitLines();
   }

   public static T GetComp<T>(this GameObject o) {
      return (o.GetComponent<T>());
   }

   public static (T a, U b) GetComp<T, U>(this GameObject o) {
      return (o.GetComponent<T>(), o.GetComponent<U>());
   }

   public static (T a, U b, V c) GetComp<T, U, V>(this GameObject o) {
      return (o.GetComponent<T>(), o.GetComponent<U>(), o.GetComponent<V>());
   }

   public static T GetComp<T>(this Component o) {
      return (o.GetComponent<T>());
   }

   public static (T a, U b) GetComp<T, U>(this Component o) {
      return (o.GetComponent<T>(), o.GetComponent<U>());
   }

   public static (T a, U b, V c) GetComp<T, U, V>(this Component o) {
      return (o.GetComponent<T>(), o.GetComponent<U>(), o.GetComponent<V>());
   }

   public static T[] GetChildren<T>(this Transform tr) {
      int n = tr.childCount;
      List<T> res = new();

      for (int i = 0; i < n; i++) {
         var t = tr.GetChild(i).GetComponent<T>();
         if (t != null) res.Add(t);
      }

      return res.ToArray();
   }

   public static Transform[] GetChildren(this Transform tr) {
      int n = tr.childCount;
      var res = new Transform[n];

      for (int i = 0; i < n; i++) {
         res[i] = tr.GetChild(i);
      }

      return res;
   }

   public static void SmoothEdges(float[] data, int n) => SmoothEdges(data, n, n);

   public static AudioClip MakeMultiToneClip(float[] hz, float seconds = 1, float falloff = -1) {
      if (falloff == -1f) falloff = seconds / 2;
      var clip = AudioClip.Create($"tone_" + "_".join(hz), Mathf.RoundToInt(AudioSettings.outputSampleRate * seconds),
         1,
         AudioSettings.outputSampleRate, false);

      float[] d = new float[clip.samples];
      float[] data = new float[clip.samples];

      foreach (var x in hz) {
         FillSineSamples(data, clip.channels, x, 0.1f);

         for (int i = 0; i < d.Length; i++) {
            d[i] += data[i] / hz.Length;
         }
      }

      SmoothEdges(d, 50, 48 * 400);
      clip.SetData(d, 0);
      return clip;
   }

   public static float NormalizeRadians(this float rad) {
      var r = rad.ModFloat(Mathf.PI * 2);
      if (r < 0) r += Mathf.PI * 2;
      return r;
   }

   public static double ModFloat(this double d, double mod) {
      mod = Math.Abs(mod);
      if (d < 0) return -ModFloat(-d, mod);
      double r = Math.Floor(d / mod);
      return d - r * mod;
   }

   public static float ModFloat(this float d, float mod) {
      mod = Mathf.Abs(mod);
      if (d < 0) return -ModFloat(-d, mod);
      float r = Mathf.Floor(d / mod);
      return d - r * mod;
   }

   public static float Dist2D(this Transform a, GameObject b) {
      return Dist2D(a, b.transform);
   }

   public static float Dist2D(this Transform a, Transform b) {
      return Vector2.Distance(a.position, b.position);
   }

   public static T WhereSmallest<T, U>(this IEnumerable<T> a, Func<T, U> o) where U : IComparable<U> {
      T res = default;
      U best = default;

      bool ok = false;

      foreach (var x in a) {
         var c = o(x);
         if (!ok || c.CompareTo(best) <= 0) {
            res = x;
            best = c;
         }

         ok = true;
      }

      return res;
   }

   public static T WhereGreatest<T, U>(this IEnumerable<T> a, Func<T, U> o) where U : IComparable<U> {
      T res = default;
      U best = default;

      bool ok = false;

      foreach (var x in a) {
         var c = o(x);
         if (!ok || c.CompareTo(best) >= 0) {
            res = x;
            best = c;
         }

         ok = true;
      }

      return res;
   }

   public static T[] filter<T>(this T[] arr, Predicate<T> f) {
      return arr.Where(t => f(t)).ToArray();
   }

   public static T[] filtered<T>(this T[] arr, Predicate<T> f) {
      return arr.Where(t => f(t)).ToArray();
   }

   public static T[] append<T>(this T[] arr, T val) {
      var r = new T[arr.Length + 1];
      r[arr.Length] = val;
      Array.Copy(arr, 0, r, 0, arr.Length);
      return r;
   }

   public static T[] prepend<T>(this T[] arr, T val) {
      var r = new T[arr.Length + 1];
      r[0] = val;
      Array.Copy(arr, 0, r, 1, arr.Length);
      return r;
   }

   public static U[] map<T, U>(this List<T> arr, Func<T, U> f) {
      U[] res = new U[arr.Count];

      for (int i = 0; i < res.Length; i++) {
         res[i] = f(arr[i]);
      }

      return res;
   }

   public static U[] map<T, U>(this T[] arr, Func<T, U> f) {
      U[] res = new U[arr.Length];

      for (int i = 0; i < res.Length; i++) {
         res[i] = f(arr[i]);
      }

      return res;
   }

   public static U[] cast<U, T>(this T[] arr) where T : U {
      return arr.map<T, U>(x => (U)x);
   }

   public static IEnumerable<T> ForEach<T>(this IEnumerable<T> arr, Action<T> f) {
      foreach (var a in arr) f(a);
      return arr;
   }

   public static bool IsNullOrEmpty<T>(this ICollection<T> s) {
      return s == null || s.Count == 0;
   }

   public static bool IsNullOrEmpty(this string s) {
      return string.IsNullOrEmpty(s);
   }

   public static bool IsNotZero(this string s) {
      s = s?.Trim();
      if (s.IsNullOrEmpty()) return false;
      if (s == "0") return false;
      return true;
   }

   public static bool IsNonEmpty(this string s) {
      return !string.IsNullOrEmpty(s);
   }

   public static bool IsNonEmpty<T>(this ICollection<T> s) {
      return s != null && s.Count > 0;
   }

   public static void LoadPrefFields(object o, bool log = false) {
      var t = o.GetType();

      var fs = t.GetFields(Instance | GetField | Public | NonPublic);

      foreach (var f in fs) {
         var attr = f.GetCustomAttribute<PrefAttribute>();
         if (attr != null) {
            var name = $"{t.Name}.{f.Name}";
            if (log) Debug.Log($"Loading pref: {name}");
            if (PlayerPrefs.HasKey(name)) {
               var val = PlayerPrefs.GetString(name);
               f.SetValue(o, val);
            }
         }
      }
   }

   public static void StorePrefFields(object o, bool log = false) {
      var t = o.GetType();

      var fs = t.GetFields(Instance | GetField | Public | NonPublic);

      foreach (var f in fs) {
         var attr = f.GetCustomAttribute<PrefAttribute>();
         if (attr != null) {
            var name = $"{t.Name}.{f.Name}";
            if (log) Debug.Log($"Storing pref: {name}");
            var val = (string)f.GetValue(o);
            if (val == null) {
               if (PlayerPrefs.HasKey(name)) PlayerPrefs.DeleteKey(name);
            } else {
               PlayerPrefs.SetString(name, val);
            }
         }
      }
   }

   public static IEnumerable<KeyVal<V>> GetFields<V>(object o) {
      var t = o.GetType();

      foreach (var f in t.GetFields_Fast()) {
         if (f.IsStatic || f.FieldType != typeof(V)) continue;
         var name = f.Name;
         var val = f.GetValue_Slow(o);
         yield return new KeyVal<V>(name, (V)val);
      }
   }

   public static List<KeyVal> GetFields(object o) {
      var t = o.GetType();

      var fs = t.GetFields_Fast();

      int n = fs.Count(x => !x.IsStatic);

      var res = new List<KeyVal>(fs.Length);

      int fi = 0;

      for (int i = 0; i < fs.Length; i++) {
         var f = fs[i];
         if (f.IsStatic) continue;

         var name = f.Name;
         var val = f.GetValue_Slow(o);
         res.Add(new KeyVal(name, val));
      }

      return res;
   }

   public static bool IsNotEmpty(object o) {
      if (o == null) return false;
      if (o is string s) return s != "";
      if (o is ICollection il) return il.Count > 0;
      if (o is System.Array arr) return arr.Length > 0;
      return true;
   }

   public static IEnumerable<KeyVal> GetSetFields(object o) {
      var t = o.GetType();

      foreach (var f in t.GetFields_Fast()) {
         if (f.IsStatic) continue;
         var name = f.Name;
         var val = f.GetValue_Slow(o);
         if (val == null) continue;
         yield return new KeyVal(name, val);
      }
   }

   public static List<Vector2Int> ToList(this RectInt.PositionEnumerator ip) {
      static IEnumerable<Vector2Int> ToList(RectInt.PositionEnumerator ip) {

         foreach (var a in ip) {
            yield return a;
         }
      }
      return ToList(ip).ToList();
   }
}