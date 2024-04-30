using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowContext : MonoBehaviour {

   public Texture2D cursor;
   public virtual bool cursor_action => false;

   public bool AmTopContext() {
   
      if (WindowManager.instance) {
         return WindowManager.instance.IsTop(this);
      }
      return false;
   }

   public virtual void FrontCallback() {
      InputExt.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
      if (transform is RectTransform) {
         AnnotatedUI.ReVisit(transform);
      }
   }
   public virtual void ContextUpdate(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }
   public virtual void Hover(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public virtual void LeftClick(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public virtual void RightClick(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
   }

   public virtual void Escape(ref bool fallthrough, ref bool drop_me, WindowManager manager) {
      fallthrough = true;
   }
}
