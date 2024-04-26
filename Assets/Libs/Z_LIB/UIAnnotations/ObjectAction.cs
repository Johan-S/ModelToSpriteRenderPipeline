using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectAction<T> {

   public T target;
   public string type;
   public string name;

   public int cost;
   public int progress;

   public string description;

   public object obj;

   public ObjectAction(T target, string type, string name, int cost=0) {
      this.target = target;
      this.type = type;
      this.name = name;
      this.cost = cost;
   }

   public static implicit operator bool(ObjectAction<T> t) {
      return t != null && t.type != null && t.type.Length > 0;
   }

   public override string ToString() {
      if (cost != 0) {
         return name == null ? $"{type} ({progress}/{cost})" : $"{type} {name} ({progress}/{cost})";
      }
      return name == null ? type : $"{type} {name}";
   }
}
