using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[ExecuteAlways]
public class ScrollRectEx : MonoBehaviour, ILayoutElement {
   [Header("Layout Element")]

   public bool custom_layout;

   public float max_height = -1;
   public float min_width = -1;
   public float max_width = -1;
   public bool control_content_width;

   [Header("Move Viewport")]
   public bool move_viewport_if_bar_visible;

   public Vector2 vertical_move_dist;

   public Vector2 current_perturb;

   public ScrollRect scroll_rect;

   public float minWidth {
      get {
         if (min_width == -1) return -1;
         var bh = scroll_rect.content.rect.width;
         if (min_content_width != -1) {
            bh = min_content_width;
         }
         if (bh < min_width) bh = min_width;
         return bh;
      }
   }

   public float flexibleWidth => -1;

   public float minHeight => -1;

   public float preferredHeight {
      get {
         if (!custom_layout) return -1;
         var bh = scroll_rect.content.rect.height;
         if (max_height == -1) return -1;
         if (bh > max_height) bh = max_height;
         return bh;
      }
   }
   public float preferredWidth {
      get {
         if (max_width == -1) return -1;
         if (!custom_layout) return -1;
         float mw = minWidth;
         var bh = scroll_rect.content.rect.width;
         if (preferred_content_width != -1) {
            bh = preferred_content_width;
         }
         if (bh > max_width) bh = max_width;
         if (bh < mw) {
            bh = mw;
         }
         return bh;
      }
   }

   public float flexibleHeight => -1;

   public int layoutPriority => -1;

   float time;

   bool is_first_frame => time == Time.time;

   void Awake() {
      time = Time.time;
   }

   // Start is called before the first frame update
   void Start() {
      if (!scroll_rect) scroll_rect = GetComponent<ScrollRect>();
      if (!rect_transform) rect_transform = GetComponent<RectTransform>();


   }

   // Update is called once per frame
   void LateUpdate() {
      UpdateVars();

      if (move_viewport_if_bar_visible) {
         var viewport = scroll_rect.viewport;

         var wanted_perturb = new Vector2();

         if (scroll_rect.verticalScrollbar) {
            var o = scroll_rect.verticalScrollbar.gameObject;
            if (o.activeSelf) {
               if (move_viewport_if_bar_visible) {
                  wanted_perturb += vertical_move_dist;
               }
            }
         }

         if (wanted_perturb != current_perturb) {
            viewport.anchoredPosition += wanted_perturb - current_perturb;
            current_perturb = wanted_perturb;
         }
      }
   }

   void Update() {
   }

   public void CalculateLayoutInputHorizontal() {
      if (control_content_width) {
         UpdateContentStuff();
      }
   }

   public void CalculateLayoutInputVertical() {
      if (!custom_layout) return;
   }

   RectTransform rect_transform;
   float last_body_height;

   void RebuildVars() {
      MaybeUpdateSize();
      MaybeUpdateContentSize();

   }

   void UpdateVars() {
      MaybeUpdateSize();
      MaybeUpdateContentSize();
   }

   float preferred_content_width;
   float min_content_width;

   bool content_stuff_init = false;

   void UpdateContentStuff() {
      content_stuff_init = true;
      float preferred_content_width = -1;
      float min_content_width = -1;
      var content = scroll_rect.content;
      var el = content.GetComponents<ILayoutElement>().ToList().Sorted(x => -x.layoutPriority);

      if (el.Count == 0) {
      } else {
         int stop_after = 0;

         foreach (var e in el) {
            if (e.layoutPriority < stop_after) break;
            e.CalculateLayoutInputHorizontal();
            if (e.preferredWidth != -1) {
               preferred_content_width = Mathf.Max(preferred_content_width, e.preferredWidth);
               stop_after = e.layoutPriority;
            }
         }
         stop_after = 0;
         foreach (var e in el) {
            if (e.layoutPriority < stop_after) break;
            if (e.minWidth != -1) {
               min_content_width = Mathf.Max(min_content_width, e.minWidth);
               stop_after = e.layoutPriority;
            }
         }
      }

      this.preferred_content_width = preferred_content_width;
      this.min_content_width = min_content_width;
   }

   float WantedWidth() {
      float wanted_width = rect_transform.rect.width;
      if (!content_stuff_init) {
         UpdateContentStuff();
      }

      float wmin = minWidth;
      float wmax = preferredWidth;

      if (wanted_width < wmin) wanted_width = wmin;
      if (wanted_width > wmax && wmax != -1) wanted_width = wmax;
      DebugEx.Log("wmin", wmin, "wmax", wmax, "res", wanted_width);
      return wanted_width;
   }

   void MaybeUpdateContentSize() {
      if (!control_content_width) return;
      {
         var content = scroll_rect.content;
         var cw = content.rect.width;
         float wanted_width = rect_transform.rect.width;
         if (wanted_width != cw) {
            DebugEx.Log("Content Width", cw, wanted_width, preferredWidth);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wanted_width);
         }
      }
   }
   void MaybeUpdateSize() {
      if (!custom_layout) return;
      if (last_body_height == scroll_rect.content.rect.height) return;
      last_body_height = scroll_rect.content.rect.height;
      if (max_height >= 0) {
         var h = rect_transform.rect.height;
         var ph = preferredHeight;
         if (h != ph) {
            rect_transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ph);
         }
      }
      if (max_width >= 0 || min_width >= 0) {
         var h = rect_transform.rect.width;
         var ph = WantedWidth();
         if (h != ph) {
            DebugEx.Log("Outside Width", h, ph);
            rect_transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ph);
         }
      }

   }

}
