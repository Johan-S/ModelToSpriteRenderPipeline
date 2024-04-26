using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TagButtonRevisit : MonoBehaviour {


   bool dirty;

   public void MarkDirty() {
      dirty = true;
   }


   List<MonoBehaviour> components = new List<MonoBehaviour>();

   // Start is called before the first frame update
   void Awake() {
      var b = GetComponent<Button>();
      if (b) {
         var eb = b.GetComponent<EagerButton>();
         if (eb) {
            eb.onClick.AddListener(MarkDirty);
         } else {
            b.onClick.AddListener(MarkDirty);
         }

         var nav = new Navigation();
         nav.mode = Navigation.Mode.None;
         b.navigation = nav;
      }
      {
         var sl = GetComponent<Slider>();
         if (sl) {
            sl.onValueChanged.AddListener(x => MarkDirty());
         }
      }
      {
         var sl = GetComponent<Toggle>();
         if (sl) {
            sl.onValueChanged.AddListener(x => MarkDirty());
         }
      }
      {
         var sl = GetComponent<InputField>();
         if (sl) {
            sl.onValueChanged.AddListener(x => MarkDirty());
            sl.onEndEdit.AddListener(x => MarkDirty());
         }
      }
      {
         var sl = GetComponent<TMP_InputField>();
         if (sl) {
            sl.onValueChanged.AddListener(x => MarkDirty());
            sl.onEndEdit.AddListener(x => MarkDirty());
         }
      }
   }

   bool last_sticky;

   // Update is called once per frame
   void Update() {
      if (dirty) {
         dirty = false;
         AnnotatedUI.PropagateDirty(transform);
      }
   }
}
