using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HotkeyManager : MonoBehaviour {

   public class Hotkey : NullIsFalse {

      public KeyCode unity_key;
      public bool control;
      public bool alt;
      public bool shift;

      public static implicit operator Hotkey(KeyCode k) => new Hotkey { unity_key = k};

      public bool KeyDownExact() {
         if (InputExt.GetKeyDown(unity_key)) {
            if (control != InputExt.control) return false;
            if (alt != InputExt.alt) return false;
            if (shift != InputExt.shift) return false;
            return true;
         }
         return false;
      }


      public bool KeyDown() {
         if (InputExt.GetKeyDown(unity_key)) {
            if (control && !InputExt.control) return false;
            if (alt && !InputExt.alt) return false;
            if (shift && !InputExt.shift) return false;
            return true;
         }
         return false;
      }

      public override string ToString() {

         string res = unity_key.ToString();


         if (control) {
            res = "ctrl + " + res;
         }
         if (shift) {
            res = "shift + " + res;
         }
         if (alt) {
            res = "alt + " + res;
         }
         return res;
      }
   }
   public class HotkeyFunction : NullIsFalse, Named {
      public string name {
         get; set;
      }

      public Hotkey default_key = KeyCode.None;
      public Hotkey current_key => user_set_key ?? default_key;
      public Hotkey user_set_key;

      public System.Action action;
   }


   public class HotkeyView : NullIsFalse, Named {

      public string name {
         get; set;
      }

      public System.Func<bool> enabled_if = () => true;
      public System.Func<bool> visible_if = () => true;


      public List<HotkeyFunction> keys = new List<HotkeyFunction>();

      public void AddKey(HotkeyFunction func) {
         keys.Add(func);
      }
      public void AddKey(string name, Hotkey key, System.Action action) {
         keys.Add(new HotkeyFunction { name = name, default_key = key, action = action});
      }
   }

   public SelectClass<HotkeyView> hotkey_view_select {
      get {
         if (hotkey_view_select_cache == null) {
            hotkey_view_select_cache = SelectUtils.MakeSelectClass(hotkey_views);
            hotkey_view_select_cache.filter = x => x.visible_if();
            Debug.Log(hotkey_views.Count);
         }

         return hotkey_view_select_cache;
      }
   }
   SelectClass<HotkeyView> hotkey_view_select_cache;

   public List<HotkeyView> hotkey_views = new List<HotkeyView>();



   public IEnumerable<HotkeyFunction> hotkey_functions {
      get {
         if (hotkey_view_select.selected_comp == null) yield break;
         foreach (var i in hotkey_view_select.selected.keys) {
            yield return i;
         }
      }
   }


   public static HotkeyView CreateAndregisterHotkeyView(string name, System.Func<bool> enabled_if, System.Func<bool> visible_if = null) {
      var view = new HotkeyView { name = name, enabled_if = enabled_if };
      if (visible_if != null) view.visible_if = visible_if;
      instance.hotkey_views.Add(view);
      return view;
   }


   public IEnumerable<HotkeyView> ActiveViews() {
      foreach (var v in instance.hotkey_views) {
         if (v.enabled_if()) yield return v;
      }
   }

   public static HotkeyManager instance;

   private void Awake() {
      if (instance) {
         Debug.LogError($"Multiple {GetType().Name}");
      }
      instance = this;
   }

   // Start is called before the first frame update
   void Start() {

   }

   // Update is called once per frame
   void Update() {

   }
}
