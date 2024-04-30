using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DoubleDict<T, U> : IDictionary<T, U> where T : class where U : class {

   public DoubleDict(Dictionary<T, U> dict, Dictionary<U, T> o_dict) {
      this.dict = dict;
      this.o_dict = o_dict;
   }

   Dictionary<T, U> dict;
   Dictionary<U, T> o_dict;

   public U this[T key] {
      get => dict[key];
      set {
         var t = key;
         var u = value;
         var left = dict;
         var right = o_dict;
         if (t != null) {
            if (left.TryGetValue(t, out var pu)) {
               right[pu] = null;
            }
            left[t] = u;
         }
         if (u != null) {
            if (right.TryGetValue(u, out var pt)) {
               left[pt] = null;
            }
            right[u] = t;
         }
      }
   }

   public ICollection<T> Keys => dict.Keys;

   public ICollection<U> Values => dict.Values;

   public int Count => dict.Count;

   public bool IsReadOnly => false;

   public void Add(T key, U value) {
      this[key] = value;
   }

   public void Add(KeyValuePair<T, U> item) {
      this[item.Key] = item.Value;
   }

   public void Clear() {
      dict.Clear();
      foreach (var k in o_dict.Keys.ToList()) {
         o_dict[k] = null;
      }
   }

   public bool Contains(KeyValuePair<T, U> item) {
      return dict.Contains(item);
   }

   public bool ContainsKey(T key) {
      return dict.ContainsKey(key);
   }

   public void CopyTo(KeyValuePair<T, U>[] array, int arrayIndex) {
      throw new NotImplementedException();
   }

   public IEnumerator<KeyValuePair<T, U>> GetEnumerator() {
      return dict.GetEnumerator();
   }

   public bool Remove(T key) {
      if (dict.TryGetValue(key, out var val)) {
         if (val != null) {
            o_dict[val] = null;
         }
         return dict.Remove(key);
      }
      return false;
   }

   public bool Remove(KeyValuePair<T, U> item) {
      var key = item.Key;
      if (dict.TryGetValue(key, out var val)) {
         if (val != item.Value) return false;
         if (val != null) {
            o_dict[val] = null;
         }
         return dict.Remove(key);
      }
      return false;
   }

   public bool TryGetValue(T key, out U value) {
      return dict.TryGetValue(key, out value);
   }

   IEnumerator IEnumerable.GetEnumerator() {
      return dict.GetEnumerator();
   }
}

public class OneToOneSet<T, U> where T : class where U : class {

   public class Element {
      public T left;
      public U right;
   }

   Dictionary<T, U> left = new Dictionary<T, U>();
   Dictionary<U, T> right = new Dictionary<U, T>();

   public DoubleDict<T, U> Left => new DoubleDict<T, U>(left, right);
   public DoubleDict<U, T> Right => new DoubleDict<U, T>(right, left);

   public void AddLeft(T t) {

      left[t] = null;
   }

   public void AddRight(U u) {
      right[u] = null;
   }

   public bool RemoveLeft(T t) {
      if (t != null) {
         if (left.TryGetValue(t, out var pu)) {
            right[pu] = null;
         }
         return left.Remove(t);
      }
      return false;
   }

   public bool RemoveRight(U u) {
      if (u != null) {
         if (right.TryGetValue(u, out var pt)) {
            left[pt] = null;
         }
         return right.Remove(u);
      }
      return false;
   }

   public U GetLeft(T t) {
      return left[t];
   }

   public T GetRight(U u) {
      return right[u];
   }

   public void Connect(T t, U u) {
      if (t != null) {
         if (left.TryGetValue(t, out var pu)) {
            right[pu] = null;
         }
         left[t] = u;
      }
      if (u != null) {
         if (right.TryGetValue(u, out var pt)) {
            left[pt] = null;
         }
         right[u] = t;
      }
   }


   public IEnumerable<Element> LeftJoin() {
      foreach (var l in left) yield return new Element { left = l.Key, right = l.Value };
   }

   public IEnumerable<Element> RightJoin() {
      foreach (var l in right) yield return new Element { left = l.Value, right = l.Key };
   }

   public IEnumerable<Element> FullJoin() {
      foreach (var l in left) yield return new Element { left = l.Key, right = l.Value };
      foreach (var l in right) if (l.Value == null) yield return new Element { left = l.Value, right = l.Key };
   }
}
