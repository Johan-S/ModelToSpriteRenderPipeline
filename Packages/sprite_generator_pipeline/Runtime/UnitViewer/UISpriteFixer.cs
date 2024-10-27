using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.RectTransform.Axis;

public class UISpriteFixer : MonoBehaviour {
   public float rescale = 1;

   Sprite old_sprite;


   Image image;

   // Start is called before the first frame update
   void Start() {
      image = GetComponent<Image>();
   }

   // Update is called once per frame
   void LateUpdate() {
      if (image.sprite != old_sprite) {
         old_sprite = image.sprite;

         var rt = image.rectTransform;

         var cp = rt.anchoredPosition;

         rt.pivot = old_sprite.pivot / old_sprite.rect.size;
         rt.SetSizeWithCurrentAnchors(Horizontal, old_sprite.rect.width * 64 / old_sprite.pixelsPerUnit );
         rt.SetSizeWithCurrentAnchors(Vertical, old_sprite.rect.height * 64 / old_sprite.pixelsPerUnit );
         rt.anchoredPosition = cp;
      }
   }
}