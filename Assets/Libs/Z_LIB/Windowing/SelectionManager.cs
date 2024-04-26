using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour {

   public TypeSwitch type_switch;

   public bool instantiate_subs;

   public List<object> selected = new List<object>();

   public List<GameObject> spawned_objects = new List<GameObject>();
   public List<SelectionHooks> selection_hooks = new List<SelectionHooks>();

   public bool dirty;

   public void Select(object o) {
      Select(o, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
   }
   private void Awake() {
      Console.print("Is SelectionManager used?", name);
   }

   public void Select(object o, bool additive) {
      if (selected.Count > 0 && o.GetType() != selected[0].GetType()) additive = false;
      if (!additive) ClearSelection();
      else dirty = true;

      GameObject go = null;
      if (o is GameObject) {
         go = (GameObject)o;
      }
      if (o is MonoBehaviour) {
         go = ((MonoBehaviour)o).gameObject;
      }
      if (go) {
         var sa = go.GetComponent<SelectionHooks>();
         if (sa != null) {
            o = sa.GetData();
            sa.SetSelected(true);
            selection_hooks.Add(sa);
         }
      }

      var prefab = type_switch.SwitchOn(o);
      if (instantiate_subs) {
         if (selected.Contains(o)) {
            return;
         }
         if (o is SelectionHooks) {
            var sa = (SelectionHooks)o;
            sa.SetSelected(true);
            selection_hooks.Add(sa);
         }
         if (prefab) {
            var ob = Instantiate(prefab, transform.root);
            AnnotatedUI.Visit(ob.transform, o, "Selection Manager");
            spawned_objects.Add(ob);
         }
      } else {
         if (o is SelectionHooks) {
            var sa = (SelectionHooks)o;
            sa.SetSelected(true);
            selection_hooks.Add(sa);
         }
         if (prefab) {
            prefab.SetActive(true);
            AnnotatedUI.Visit(prefab.transform, o, "Selection Manager");
            spawned_objects.Add(prefab);
         }
      }
      selected.Add(o);
   }

   public void RefreshGenerated() {
      foreach (var o in spawned_objects) {
         AnnotatedUI.ReVisit(o.transform);
      }
   }

   public void ClearSelection() {
      dirty = false;
      selected.Clear();

      foreach (var h in selection_hooks) {
         if (h != null) h.SetSelected(false);
      }

      if (instantiate_subs) {
         foreach (var s in spawned_objects) {
            if (s) Destroy(s);
         }
      } else {
         foreach (var s in spawned_objects.ToList()) {
            if (s) s.SetActive(false);
         }
      }

      spawned_objects.Clear();
   }

   private void Update() {
      if (dirty) {
         dirty = false;
         // RefreshGenerated();
      }
   }

   public bool AnySelected() {
      return selected.Count > 0;
   }

   void OnDisable() {
      ClearSelection();
   }
}
