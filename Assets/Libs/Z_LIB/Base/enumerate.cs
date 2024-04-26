using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class enumerate {

   public static IEnumerable<T> items<T>(params T[] args) {
      foreach (var t in args) yield return t;
   }

}
