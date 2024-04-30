using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectCategory : MonoBehaviour, AnnotatedUI.ValueHolder {

   public string category;
   static Dictionary<string, SelectCategory> selected = new Dictionary<string, SelectCategory>();
   static Dictionary<string, System.Func<object, bool>> select_callback = new Dictionary<string, System.Func<object, bool>>();
   public Color base_color;
   public Color selected_color = new Color(0.5f, 1, 0.5f);

   System.Func<object, bool> select_callback_local;

   public static object GetSelected(string category) {
      var o = selected.Get(category, null);
      if (o) return o.get();
      return null;
   }

   public static void CallbackForSelected(string category, System.Func<object, bool> f) {
      select_callback[category] = f;
   }

   void Ok() {
   }

   void Error() {
   
   }

   void Select() {
      var prev = selected.Get(category, null);
      if (prev) prev.Unselect();
      if (select_callback_local != null) {
         if (select_callback_local(set_object)) {
            Ok();
         } else {
            Error();
            return;
         }
      } else {
         Ok();
      }
      selected[category] = this;
      GetComponent<Image>().color = selected_color;
   
   }

   void Unselect() {
      GetComponent<Image>().color = base_color;
   }

   // Start is called before the first frame update
   void Awake() {
      base_color = GetComponent<Image>().color;
      gameObject.OnClick(Select);
   }

   void Start() {
      select_callback_local = select_callback.Get(category, null);
   }

   void Update() {
      if (select_callback_local != null) {
         GetComponent<Button>().interactable = select_callback_local(set_object);
      }
   }

   public object set_object;

   public object get() {
      return set_object;
   }

   public void set(object o) {
      set_object = o;
   }
}
