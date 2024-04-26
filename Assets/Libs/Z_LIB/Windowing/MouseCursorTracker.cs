using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCursorTracker : MonoBehaviour {
   static GameObject impl;


   static float sticky_tooltip_max_dist_extra = 24;
   public static GameObject hoverTooltip {
      get {
         if (impl) return impl;
         return null;
      }
      set {
         last_sticky_dist = null;
         while (impl) {
            if (sticky_tooltip) {
               var r = impl.GetComponent<RectTransform>();
               if (r.ContainsMouse()) {
                  sticky_to_destroy.Add(impl);
                  impl = null;
               } else {
                  Destroy(impl);
                  if (sticky_to_destroy.Count > 0) {
                     impl = sticky_to_destroy.Pop();
                  } else {
                     impl = null;
                  }
               }
            } else {
               Destroy(impl);
               impl = null;
            }
         }
         tooltip_mouse_pos = InputExt.mousePosition;
         mouse_pos = tooltip_mouse_pos;
         impl = value;
         RefreshMouse();
      }
   }

   public static void PopStickyTooltip() {
      Destroy(impl);
      if (sticky_to_destroy.Count > 0) {
         impl = sticky_to_destroy.Pop();
      } else {
         impl = null;
      }
      last_sticky_dist = null;
   }

   public static void ClearStickyTooltips() {
      foreach (var a in sticky_to_destroy) Destroy(a);
      sticky_to_destroy.Clear();
   }

   static List<GameObject> sticky_to_destroy = new List<GameObject>();

   public static void RefreshTooltipMouse() {
      tooltip_mouse_pos = InputExt.mousePosition;
   }

   public static Vector2 mouse_pos;
   public static Vector2 tooltip_mouse_pos;
   public float mouse_still_time {
      get {
         RefreshMouse();
        return Time.time - last_mouse_move;
      }
   }
   public static float last_mouse_move;

   // Start is called before the first frame update
   void Start() {
      RefreshMouse();
   }

   public static bool sticky_tooltip {
      get {
         return InputExt.alt;
      }
   }

   static float? last_sticky_dist;

   public static void RefreshMouse() {
      Vector2 new_mouse_pos = InputExt.mousePosition;
      if (new_mouse_pos != mouse_pos) {
         last_mouse_move = Time.time;
         if (!sticky_tooltip) {
            if (Vector2.Distance(new_mouse_pos, tooltip_mouse_pos) > 10) {
               Destroy(hoverTooltip);
            }
         }
         if (hoverTooltip) {
            var r = hoverTooltip.GetComponent<RectTransform>().ToScreenSpace();
            var rp = r.Clamp(new_mouse_pos);
            float dist_to_sticky = Vector2.Distance(new_mouse_pos, rp);
            if (last_sticky_dist is float lsd) {
               if (dist_to_sticky > lsd + sticky_tooltip_max_dist_extra) {
                  PopStickyTooltip();
               } else {
                  last_sticky_dist = Mathf.Min(dist_to_sticky, lsd);
               }
            } else {
               last_sticky_dist = dist_to_sticky;
            }
         }
      }
      if (hoverTooltip) {
         var a = hoverTooltip.GetComponent<CanvasGroup>();
         if (a) a.blocksRaycasts = sticky_tooltip;
      }
      mouse_pos = new_mouse_pos;
   }

   // Update is called once per frame
   void Update() {
      RefreshMouse();
      if (!sticky_tooltip) {
         ClearStickyTooltips();
      }
   }
}
