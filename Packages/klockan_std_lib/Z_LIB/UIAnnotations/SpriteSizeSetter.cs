using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteSizeSetter : MonoBehaviour {
   public Vector2 original_size;
   public void SetSize(Vector2 size) {
      RectTransform r = GetComponent<RectTransform>();
      LayoutElement el = GetComponent<LayoutElement>();
      if (el) {
         if (original_size.x == 0) {
            original_size = new Vector2(el.minWidth, el.minHeight);
         }
         el.minWidth = size.x;
         el.minHeight = size.y;

      } else {
         if (original_size.x == 0) {
            original_size = r.rect.size;
         }
      }
      if (r) {
         r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
         r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
      }
   }
   public void ClearSize() {
      if (original_size.x != 0) {
         SetSize(original_size);
      }
   }
}
