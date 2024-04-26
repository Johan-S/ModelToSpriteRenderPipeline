using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Subsystem : MonoBehaviour {

   public static void Set<T>(T t) where T : Component {


      var old = instance.GetType_NoCreate(typeof(T));

      if (old) {
         Destroy(old.gameObject);
      }
      var dict = instance.dict;
      

      dict[typeof(T)] = t;

      t.transform.parent = instance.transform;

   }

   public static T Get<T>() where T : Component {

      

      var ct = instance.GetType(typeof(T));


      return (T)ct;
   }

   static Subsystem _instance;


   static Subsystem instance {
      get {
         if (!_instance) {
            _instance = new GameObject("Subsystem").AddComponent<Subsystem>();
            DontDestroyOnLoad(_instance.gameObject);
         }

         return _instance;
      }
   }


   Dictionary<Type, Component> dict = new ();


   Component GetType_NoCreate(Type t) {

      if (!dict.TryGetValue(t, out var o)) {

         o = GetComponentInChildren(t);

         dict[t] = o;

      }

      return o;
   }

   Component GetType(Type t) {

      var o = GetType_NoCreate(t);

      if (!o) dict[t] = o = new GameObject(t.Name, t).GetComponent(t);

      return o;
   }
   
}