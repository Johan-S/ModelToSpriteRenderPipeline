using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class HighlightUI : MonoBehaviour {
   public Color color;

   public Color color_2;

   public Color GetColor() {
      if (color.a != 0) {
         var c1 = color;
         if (color_2.a != 0) {
            var c2 = color_2;
            var a = c1.a + c2.a;
            if (a > 1) a = 1;
            var res = Color.Lerp(c1, c2, 0.5f);
            res.a = a;
            return res;
         }
         return c1;
      } else {
         if (color_2.a != 0) {
            return color_2;
         }
         return new Color();
      }
   }

   public Image image;

   public bool dont_destroy;
   public bool destroy_next_update;

   public bool spawn_back;
   void Start() {

      image = Instantiate(FlashingUI.GetBlinkPanelPrefab(), transform);
      image.name = "ignore_";
      image.color = GetColor();
      if (spawn_back) image.rectTransform.SetSiblingIndex(0);
      if (!dont_destroy) {
         image.gameObject.ForceComponent<DestroyAfter>().duration = 10;
      }
      outside_mouse = color_2 != null;
   }
   bool outside_mouse;
   private void Update() {
      if (dont_destroy) {
         return;
      }
      if (destroy_next_update || !image.rectTransform.ContainsMouse() && color_2 == null) {
         Destroy(this);
         Destroy(image.gameObject);
         return;
      }
      if (image) {
         image.gameObject.ForceComponent<DestroyAfter>().duration = 1;
      }
      image.color = GetColor();
      destroy_next_update = true;
      color = default;
      outside_mouse = color_2.a != 0;
      color_2 = default;
   }
   private void LateUpdate() {
      if (dont_destroy) {
         return;
      }
      if (!outside_mouse && image && image.gameObject && !image.rectTransform.ContainsMouse()) {
         Destroy(this);
         Destroy(image.gameObject);
         return;
      }
      outside_mouse = false;
   }
   private void OnDisable() {
      if (image) Destroy(image.gameObject);
      Destroy(this);
   }

   private void OnDestroy() {
      if (image) Destroy(image.gameObject);
   }
}
