using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ButtonPressTrigger : MonoBehaviour {


   Color original;
   float fade;
   Color pressed;
   Color hovered;

   [System.NonSerialized]
   float last_color_change;

   [System.NonSerialized]
   float last_pressed = - 1;
   [System.NonSerialized]
   float last_hovered = -1;

   [System.NonSerialized]
   bool color_init;

   void InitColors(Selectable b) {
      if (color_init) return;
      color_init = true;
      var c = b.colors;
      original = c.normalColor;
      pressed = c.pressedColor;
      hovered = c.highlightedColor;

      last_color_change = Time.time;
   }
   public void HoverAnimation() {
      last_hovered = Time.time;
   }
   public void PressAnimation() {
      last_pressed = Time.time;
   }


   // Start is called before the first frame update
   void Start() {
      var b = GetComponent<Selectable>();
      InitColors(b);
   }

   // Update is called once per frame
   void LateUpdate() {

      float dur = 0.2f;


      float time_since_pressed = Time.time - last_pressed;

      float press_x = dur - (time_since_pressed);
      press_x = Mathf.Clamp01(press_x / dur);

      float hovered_x = dur - (Time.time - last_hovered);
      hovered_x = Mathf.Clamp01(hovered_x / dur);

      var b = GetComponent<Selectable>();
      InitColors(b);

      var c = b.colors;

      if (press_x > 0) {
         c.normalColor = pressed;
         b.colors = c;
      } else if (last_hovered == Time.time) {
         c.normalColor = hovered;
         b.colors = c;
      } else {
         c.normalColor = original;
         b.colors = c;
      }



   }
}
