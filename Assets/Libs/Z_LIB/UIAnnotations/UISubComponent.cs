using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Reflection;


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class UITypeAttribute : Attribute {

   public UITypeAttribute() {
   }
}

[System.Serializable]
public abstract class UISubComponent : MonoBehaviour {

   private class TypeDispatcher {

      public TypeDispatcher(System.Type t) {

         foreach (MethodInfo m in t.GetMethods()) {
            var attr = m.GetCustomAttribute<UITypeAttribute>();
            if (attr != null) {
               var par = m.GetParameters();
               if (par.Length != 1) {
                  throw new Exception($"Bad method {m}");
               }

               MethodInfo reflection_method = m;

               System.Action<UISubComponent, object> invoker = (uisc, obj) => {
                  reflection_method.Invoke(uisc, new object[] { obj });
               };
               dispatch_functions.Add((par[0].ParameterType, invoker));

            } 
         }

         if (dispatch_functions.Count == 0) {
            throw new Exception($"Type {t} doesn't have any dispatch functions!");
         }
         
      }

      public List<(System.Type, System.Action<UISubComponent, object>)> dispatch_functions = new List<(Type, Action<UISubComponent, object>)>();

      public void Dispatch(UISubComponent c, object o) {
         var t = o.GetType();
         foreach (var ft in dispatch_functions) {
            if (t.Is(ft.Item1)) {
               ft.Item2(c, o);
               return;
            }
         }
         c.ThrowTypeErrorFor(o, dispatch_functions.Map(x => x.Item1.Name).ToArray());
      }

      public bool CanDispatch(System.Type t) {
         foreach (var ft in dispatch_functions) {
            if (t.Is(ft.Item1)) {
               return true;
            }
         }
         return false;
      }
   }

   private static Dictionary<System.Type, TypeDispatcher> type_dispatchers = new Dictionary<Type, TypeDispatcher>();


   private TypeDispatcher GetDispatcher() {
      if (!type_dispatchers.TryGetValue(GetType(), out var dispatcher)) {
         dispatcher = new TypeDispatcher(GetType());
         type_dispatchers[GetType()] = dispatcher;
      }
      return dispatcher;
   }

   private TypeDispatcher dispatcher => GetDispatcher();

   public void DefaultDispatch(object o) {
      dispatcher.Dispatch(this, o);
   }

   public bool CanDispatch(System.Type t) {
      return dispatcher.CanDispatch(t);
   }


   public void ThrowTypeErrorFor(object o, params string[] valid_types) {
      if (valid_types.Length == 1) {
         throw new System.Exception($"Expected a '{valid_types[0]}', got '{o.GetType().Name}'");
      } else {
         string tn = ", ".Join(valid_types.Map(x => $"'{x}'"));
         throw new System.Exception($"Expected one of {tn}, got '{o.GetType().Name}'");
      }
   }




   public void DisableAllSubBut(Transform t) {
      
      foreach (var ch in transform.ChildList()) {
         ch.gameObject.SetActive(t == ch);
      }
   }

   public virtual void SetValue(object o) {
      DefaultDispatch(o);
   }
}
