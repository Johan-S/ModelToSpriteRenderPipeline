using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeMenuContext : WindowContext {


   public RectTransform canvas;

   public GameObject escape_menu_prefab;


   public override void LeftClick(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public override void RightClick(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public override void Escape(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
      Instantiate(escape_menu_prefab, canvas);
   }
   public override void Hover(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

}
