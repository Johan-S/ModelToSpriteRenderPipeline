using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class ChangeTrackerList<T> : IList<T> {

   List<T> val = new List<T>();

   GameStateSource source = new GameStateSource();

   public void RemoveRange(int index, int count) {
      val.RemoveRange(index, count);
      source.MarkDirty();
   }

   public T this[int index] {
      get => val[index];
      set {
         source.MarkDirty();
         val[index] = value;
      }
   }

   public int Count => val.Count;

   public bool IsReadOnly => false;

   public void Add(T item) {
      val.Add(item);
      source.MarkDirty();
   }

   public void Clear() {
      if (val.Count > 0) {
         source.MarkDirty();
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
      source.MarkDirty();
      val.Insert(index, item);
   }

   public bool Remove(T item) {
      if (val.Remove(item)) {
         source.MarkDirty();
         return true;
      }
      return false;
   }

   public void RemoveAt(int index) {
      val.RemoveAt(index);
      source.MarkDirty();
   }
   IEnumerator IEnumerable.GetEnumerator() {
      return val.GetEnumerator();
   }
}