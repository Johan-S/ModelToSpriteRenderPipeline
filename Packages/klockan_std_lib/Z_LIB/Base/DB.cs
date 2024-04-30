using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class DB {


   public static T FindGlobal<T>() where T: class {
      string name = typeof(T).Name;
      return GameObject.FindGameObjectWithTag(name)?.GetComponent<T>();
   }

}
