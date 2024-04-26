using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static AnnotatedUI;

public class TooltipSpawner : MonoBehaviour, AnnotatedUI.ValueHolder {

   public interface TooltipRef {
      object ref_tooltip {
         get;
      }
   }

   public bool override_tooltip;
   public object hard_set_object;

   public System.Action<Transform> spawned_tooltip_callback;

   public object set_object;

   public object GetUsedTooltip() {
      if (get() is AnnotatedUI.TooltipValueWrapper wr) {
         return wr.wrapped_tooltip ?? wr;
      }
      return get();
   }

   public Rect corner;

   public static bool HasTooltip(object o) {
      if (o == null) return false;
      return TooltipSwitch.instance ? TooltipSwitch.instance.GetTooltipFor(o) : false;
   }

   static string ObjectChain(Transform t) {
      string res = t.name;
      while (t.parent) {
         t = t.parent;
         res = t.name + "." + res;
      }
      return res;
   }
   public Rect? fixed_screen_pos;

   public static RectTransform InitTooltipObject(RectTransform tt, object o, RectTransform tr, Transform object_to_track, Rect extent, System.Action<Transform> spawned_tooltip_callback = null, Rect? fixed_screen_pos = null, System.Func<GameObject, GameObject> tooltip_init = null) {
      var alto = tooltip_init?.Invoke(tt.gameObject);
      if (alto) {
         tt = alto.GetComponent<RectTransform>();
      }

      var tracker = tt.gameObject.ForceComponent<TrackCanvasObject>();
      tracker.fixed_screen_pos = fixed_screen_pos;
      tracker.extent = extent;
      tracker.track = object_to_track;
      tt.gameObject.SetActive(true);
      tt.gameObject.ForceComponent<HoverBlocker>();
      var cg = tt.gameObject.ForceComponent<CanvasGroup>();
      cg.blocksRaycasts = MouseCursorTracker.sticky_tooltip;
      AnnotatedUI.Visit(tt, o, "InitTooltipObject");
      AnnotatedUI.EagerRevisit_IfDirty(tt);
      spawned_tooltip_callback?.Invoke(tt);

      return tt;
   }

   public GameObject SpawnTooltipFor(object o, RectTransform tr, Transform object_to_track, Rect extent, System.Func<GameObject, GameObject> tooltip_init) {
      if (o is TooltipValueWrapper) o = ((TooltipValueWrapper)o).wrapped_tooltip ?? o;
      RectTransform tt;
      if (o is AnnotatedUI.TooltipGen tg) {
         tt = tg.GenTooltip(tr);
      } else {
         var prefab = TooltipSwitch.instance.GetTooltipFor(o);
         if (prefab == null) {
            throw new System.Exception($"Tooltip doesn't exist for type: {o.GetType().Name}. Source object: {ObjectChain(object_to_track)}");
         }
         tt = Instantiate(prefab, tr);
      }
      tt = InitTooltipObject(tt, o, tr, object_to_track, extent, spawned_tooltip_callback, fixed_screen_pos: fixed_screen_pos, tooltip_init);
      return tt.gameObject;
   }

   RectTransform canvas = null;

   public void SpawnTooltip() {
      if (!canvas) canvas = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
      SpawnTooltipFor(get(), canvas, GetComponent<RectTransform>(), corner, tooltip_init: null);
   }

   public object get() {
      return override_tooltip ? hard_set_object : set_object;
   }

   public bool has_set_tooltip;

   public void set(object o) {
      set_object = o;
      has_set_tooltip = o != null;
   }
}
