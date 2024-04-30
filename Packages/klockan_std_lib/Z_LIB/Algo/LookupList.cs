using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class CustomListBase<T> : IList<T> {

   [UnityEngine.SerializeField]
   private List<T> val;
   public CustomListBase() {
      val = new();
      
   }
   public CustomListBase(IEnumerable<T> o) {
      val = new(o);
   }
   
   public virtual T this[int index] { get => val[index]; set => val[index] = value; }
   
   
   public virtual T this[System.Index index] { get => val[index]; set => val[index] = value; }

   public CustomListBase<T> this[System.Range range] {
      get {

         var (start, length) = range.GetOffsetAndLength(val.Count);
         return new CustomListBase<T> {
            val = val.GetRange(start, length)
         };
      }
      set {
         var cv = value.val;
         if (cv == this.val) {
            cv = cv.ToList();
         }
         var (start, length) = range.GetOffsetAndLength(val.Count);
         val.RemoveRange(start, length);
         val.InsertRange(start, cv);
      }
   }

   public int Count => val.Count;

   public bool IsReadOnly => false;

   public void Add(T item) {
      val.Add(item);
   }

   public void Clear() {
      val.Clear();
   }

   public bool Contains(T item) {
      return val.Contains(item);
   }

   public void CopyTo(T[] array, int arrayIndex) {
      val.CopyTo(array, arrayIndex);
   }

   public IEnumerator<T> GetEnumerator() {
      return val.GetEnumerator();
   }

   public int IndexOf(T item) {
      return val.IndexOf(item);
   }

   public void Insert(int index, T item) {
      val.Insert(index, item);
   }

   public bool Remove(T item) {
      return val.Remove(item);
   }

   public void RemoveAt(int index) {
      val.RemoveAt(index);
   }

   IEnumerator IEnumerable.GetEnumerator() {
      return val.GetEnumerator();
   }

   public override string ToString() {
      return val.Join(",");
   }
}

public struct ListImplicit<T> : IList<T> {



   static readonly IList<T> dummy = Array.Empty<T>();

   private List<T> val;
   
   private IList<T> val_s => val ?? dummy;
   
   public void RemoveRange(int index, int count) {
      val.RemoveRange(index, count);
   }

   public ListImplicit(List<T> o) {

      val = o;
   }
   public ListImplicit(T o) {
      val = new List<T> { o };
   }

   public T this[int index] { get => val[index]; set => val[index] = value; }
   
   
   public T this[System.Index index] { get => val[index]; set => val[index] = value; }

   public ListImplicit<T> this[System.Range range] {
      get {

         var (start, length) = range.GetOffsetAndLength(val.Count);
         return val.GetRange(start, length);
      }
      set {
         var cv = value.val;
         if (cv == this.val) {
            cv = cv.ToList();
         }
         var (start, length) = range.GetOffsetAndLength(val.Count);
         val.RemoveRange(start, length);
         val.InsertRange(start, cv);
      }
   }

   public int Count => val_s.Count;

   public bool IsReadOnly => false;

   public void Add(T item) {

      if (val == null) val = new(){item};
      else val.Add(item);
   }

   public void Clear() {
      val.Clear();
   }

   public bool Contains(T item) {
      return val_s.Contains(item);
   }

   public void CopyTo(T[] array, int arrayIndex) {
      val.CopyTo(array, arrayIndex);
   }

   public IEnumerator<T> GetEnumerator() {
      return val_s.GetEnumerator();
   }

   public int IndexOf(T item) {
      return val_s.IndexOf(item);
   }

   public void Insert(int index, T item) {
      val.Insert(index, item);
   }

   public bool Remove(T item) {
      return val.Remove(item);
   }

   public void RemoveAt(int index) {
      val.RemoveAt(index);
   }

   IEnumerator IEnumerable.GetEnumerator() {
      return val_s.GetEnumerator();
   }
   public static implicit operator List<T>(ListImplicit<T> l) => l.val;

   public static implicit operator ListImplicit<T>(List<T> l) => new ListImplicit<T> ( l);

   public static implicit operator ListImplicit<T>(T l) => new ListImplicit<T>(l);
   public static implicit operator ListImplicit<T>(T[] l) => new ListImplicit<T>(l.ToList());
   
   
   public static implicit operator bool(ListImplicit<T> l) => l.val.IsNonEmpty();


   public override string ToString() {
      if (val == null) return "List[]";
      return $"List[{", ".join(val)}]";
   }
}


public class LookupList<T> : DebugEx.CustomToString, IEnumerable<T> where T : class, Named {

   public LookupList<T> Parse(string lines) {
      LookupList<T> res = new LookupList<T>();
      foreach (var r in lines.Split('\n')) {
         var s = r.Trim();
         if (s.Length > 0) {
            res.Add(this[s]);
         }
      }
      return res;
   }

   public LookupList() {
   }


   public IEnumerable<T> UnsafeIterator() {
      for (int i = 0; i < list.Count; ++i) {
         yield return list[i];
      }
   }

   public LookupList(IEnumerable<T> data) {
      foreach (var t in data) Add(t);
   }

   Dictionary<string, T> dict = new Dictionary<string, T>();
   public List<T> list = new List<T>();

   public void Clear() {

      list.Clear();
      dict.Clear();
   }

   public void AddFirst(T val) {
      if (dict.ContainsKey(val.name)) {
         dict.Remove(val.name);
         list.Remove(val);
      }
      dict[val.name] = val;
      list.Insert(0, val);
   }
   public void Add(T val) {
      if (dict.ContainsKey(val.name)) {
         throw new Exception($"Duplicate name {val.name} found!");
      }
      dict[val.name] = val;
      list.Add(val);
   }
   public bool AddOrIgnore(T val) {
      if (dict.TryGetValue(val.name, out T existing)) {
         return false;
      }
      dict[val.name] = val;
      list.Add(val);
      return true;
   }

   public void AddOrReplace(T val) {
      if (dict.TryGetValue(val.name, out T existing)) {
         Remove(existing);
      }
      dict[val.name] = val;
      list.Add(val);
   }

   public void AddRange(IEnumerable<T> vals) {
      foreach (var t in vals) Add(t);
   }

   public void AddRange_SkipDuplicates(IEnumerable<T> vals) {
      foreach (var t in vals) {
         if (dict.ContainsKey(t.name)) continue;
         Add(t);
      }
   }

   public T this[string name] {
      get {
         if (dict.TryGetValue(name, out T res)) return res;
         string message = $"Couldn't find {name}.";
         throw new Exception(message);
      }
   }

   public T this[int id] {
      get => list[id];
   }
   public bool Get(string name, out T def) {
      def = default;
      if (name == null) return false;
      return dict.TryGetValue(name, out def);
   }
   public T GetOrDefault(string name, T def) {
      if (name == null) return def;
      return dict.Get(name, def);
   }

   public void Remove(T val) {
      list.Filter(x => x.name != val.name);
      dict.Remove(val.name);
   }
   public void Filter(System.Predicate<T> f) {
      foreach (var l in list) {
         if (!f(l)) dict.Remove(l.name);
      }
      list.Filter(f);
   }

   public T Get(string name, T def = null) {
      if (name == null) return def;
      if (dict.TryGetValue(name, out T res)) return res;
      return def;
   }

   public T Get(int id, T def) {
      if (id < 0 || id >= list.Count) return def;
      return list[id];
   }

   public IEnumerator<T> GetEnumerator() {
      return list.GetEnumerator();
   }

   IEnumerator IEnumerable.GetEnumerator() {
      return list.GetEnumerator();
   }

   public bool Contains(string name) => name == null ? false : dict.ContainsKey(name);
   public bool Contains(T name) => name == null ? false : dict.ContainsKey(name.name);
   public bool NotContains(string name) => !Contains(name);
   public bool NotContains(T name) => !Contains(name);

   public int Count => list.Count;

   public static implicit operator bool(LookupList<T> l) {
      return l.list.Count > 0;
   }

   public List<T> Where(System.Predicate<T> p) => list.Where(p);

   public string CustomToString() {

      string tn = typeof(T).Name;

      var content = list.Map(x => x.name).Join(", ");

      return $"{tn}[{content}]";
   }
   public override string ToString() {
      return CustomToString();
   }
}