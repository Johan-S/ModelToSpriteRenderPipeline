using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GeneratedElements : MonoBehaviour {
   public GeneratedElements parent;
   public string top_name;

   public string original_visit_reason;


   public object original_val;

   public Dictionary<string, object> ui_vals = new Dictionary<string, object>();

   List<System.Action> on_dirty = new List<System.Action>();

   public void OnDirty(System.Action a) {
      on_dirty.Add(a);
   }

   public List<Transform> generated = new List<Transform>();

   public void Add(Transform t) {
      generated.Add(t);
   }

   public bool dirty;

   public void Clear() {
      foreach (var o in generated) {
         o.gameObject.name = "skip_";
         Destroy(o.gameObject);
      }

      generated.Clear();
   }

   private void Start() {
      if (dirty) {
         // Debug.Log("Dirty Update");
         dirty = false;
         foreach (var a in on_dirty) a();
         AnnotatedUI.EagerRevisit(transform, original_val, original_visit_reason, this);
      }
   }

   public void Update() {
      if (dirty) {
         // Debug.Log("Dirty Update");
         dirty = false;
         foreach (var a in on_dirty) a();
         AnnotatedUI.EagerRevisit(transform, original_val, original_visit_reason, this);
      }
   }
}