

using System.Dynamic;
using System.Reflection;
using UnityEngine;
// ReSharper disable All


using System;

using System.Collections.Generic;


public class StaticReference : DynamicObject {

   public static dynamic Get<T>() => new StaticReference(typeof(T));

   public StaticReference(Type type) {
      this.type = type;

      method_dict = type.GetMethods().Where(x => x.IsStatic).ToKeyDict(x => x.Name);
   }

   private Dictionary<string, List<MethodInfo>> method_dict;

   private Type type;


   public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
      result = null;

      if (!method_dict.TryGetValue(binder.Name, out var methods)) {
         return false;
      }

      var argt = args.Map(x => x?.GetType()).ToArray();
        
      
      foreach (MethodInfo m in methods) {

         if (m.HasFuncSignature(binder.ReturnType, argt)) {
            var res = m.Invoke(null, args);
            result = res;
            return true;
         }
         
         
      }
      return false;
      
   }
}