using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[DefaultExecutionOrder(-800)]
public class PT_ModularArmor : MonoBehaviour {
   public GameObject pt_modular_target;


   static string GetKey(string name) {
      var ns = name.Split("_");

      if (char.IsDigit(name.Last())) {
         return ns[^2];
      }
      

      var a = ns[^1];
      if (a == "nv") return ns[^2];
      return a;
   }

   static string[] cats = {

      "body",
      "boots",
      "gauntlets",
      // "helmet",
      "legs",
      "head",
      "hair",
   };

   public void SetParts(Transform tr) {

      int n = tr.childCount;


      List<GameObject> subs = new();
      
      for (int i = 1; i < n; i++) {
         subs.Add(tr.GetChild(i).gameObject);
      }

      var gr = subs.GroupBy(x => GetKey(x.name)).ToDictionary(x => x.Key, x => x.ToList());

      foreach (var (cat, o) in gr) {

         bool inc = cats.Contains(cat);

         for (int i = 0; i < o.Count; i++) {
            o[i].SetActive(i == 0 && inc);
         }
         
      }


   }

   void Awake() {
      SetParts(pt_modular_target.transform);
   }
}
