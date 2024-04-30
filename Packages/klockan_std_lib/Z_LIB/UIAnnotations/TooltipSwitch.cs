using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TooltipSwitch : MonoBehaviour {

   public RectTransform text_tooltip_panel_prefab;
   public RectTransform text_tooltip_panel_prefab_short;

   public RectTransform standard_tooltip_prefab;
   public RectTransform standard_tooltip_prefab_short;

   public RectTransform this_string_tooltip;


   static HashSet<System.Type> tooltip_type_log = new();

   public virtual RectTransform GetTooltipFor(object o) {

      if (o is string) return this_string_tooltip;

      if (o is TooltipSpawner.TooltipRef tool_ref) o = tool_ref.ref_tooltip;

      if (o is StandardTooltip) return standard_tooltip_prefab;
      if (o is KeyVal) return this_string_tooltip;
      if (o is TextTooltip || o is ParagraphTooltip) return text_tooltip_panel_prefab;

      if (o == null) throw new System.NullReferenceException($"Trying to get tooltip for null object!");

      if (tooltip_type_log.Add(o.GetType())) {
         Debug.LogError($"Type {o.GetType().FullName} doesn't have a tooltip. Full object: {o}");
      }

      return this_string_tooltip;
   }
   public virtual RectTransform GetShortTooltipFor(object o) {
      if (o is string) return this_string_tooltip;
      if (o is TooltipSpawner.TooltipRef tool_ref) o = tool_ref.ref_tooltip;
      if (o is TextTooltip || o is ParagraphTooltip) return text_tooltip_panel_prefab_short;
      if (o is StandardTooltip) return standard_tooltip_prefab_short;
      return GetTooltipFor(o);
   }
   public static TooltipSwitch instance;


   [RuntimeInitializeOnLoadMethod]
   static void InitOnLoad() {
      
      if (!instance) {
         var pref = Resources.Load<TooltipSwitch>("Tooltips/TooltipSwitch");
         if (pref) {
            var po = Instantiate(pref);
         }
      }
   }

   private void Awake() {
      instance = this;
      DontDestroyOnLoad(gameObject);
   }
}
