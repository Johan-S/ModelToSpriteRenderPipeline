using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DispatchButton : MonoBehaviour, AnnotatedUI.ValueHolder {

   [System.Serializable]
   public class OnClick : UnityEvent<object> {
   }

   public OnClick onClick;


   public void DispatchClick() {
      onClick.Invoke(set_object);
      // AnnotatedUI.ReVisit(transform);
   }

   // Start is called before the first frame update
   void Start() {
      GetComponent<Button>().onClick.AddListener(DispatchClick);
   }

   public object set_object;

   public object get() {
      return set_object;
   }

   public void set(object o) {
      set_object = o;
   }
}
