using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static AnnotatedUI;

public class ValueHolderBase : MonoBehaviour, AnnotatedUI.ValueHolder {
   public object set_object;

   public bool save_in_context;
   
   public object get() {
      return set_object;
   }

   public void set(object o) {
      set_object = o;
   }
}
