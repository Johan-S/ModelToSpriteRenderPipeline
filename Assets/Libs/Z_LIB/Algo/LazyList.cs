using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class LazyListSource<T> : IList<T> {


   List<T> val = new List<T>();

   int version;

   public void RemoveRange(int index, int count) {
      val.RemoveRange(index, count);
      version++;
   }

   public T this[int index] {
      get => val[index];
      set {
         var oval = val[index];
         if (!oval.Equals(value)) {
            version++;
         }
         val[index] = value;
      }
   }

   public int Count => val.Count;

   public bool IsReadOnly => false;

   public void Add(T item) {
      val.Add(item);
      version++;
   }

   public void Clear() {
      if (val.Count > 0) {
         version++;
      }
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
      version++;
      val.Insert(index, item);
   }

   public bool Remove(T item) {
      if (val.Remove(item)) {
         version++;
         return true;
      }
      return false;
   }

   public void RemoveAt(int index) {
      val.RemoveAt(index);
      version++;
   }
   IEnumerator IEnumerable.GetEnumerator() {
      return val.GetEnumerator();
   }
}
