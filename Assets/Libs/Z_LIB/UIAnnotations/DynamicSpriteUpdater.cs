using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AnnotatedUI;

public class DynamicSpriteUpdater : MonoBehaviour {
   public Image image;



   public void SetVars(Image image, ColoredSprite sprite_in) {
      this.image = image;

      this.sprite_in = sprite_in;

      UpdateImage();
   }
   public void UpdateImageColors() {
      var img = image;
      var cs = sprite_in;

      original_color = original_color ?? img.color;


      var color = cs.GetColor();
      img.sprite = cs.GetSprite();
      if (!cs.UseAlpha()) color.a = (float)original_color?.a;
      img.color = color;
   }

   public RectTransform layoutTransform;


   UIBoxLayoutVars original_layout;

   public void CleanupSprite() {
      if (image && original_color is Color a) {
         image.color = a;
      }
      sprite_in = null;
      if (original_layout != null) {
         original_layout.ApplyTo(image.rectTransform);
      }
   }

   public SpriteSizeSetter sprite_size_setter;
   public SpriteSizeSetter sprite_size_setter_parent;
   private void Start() {
   }

   private void Awake() {
      
   }

   public RectTransform rectTransform;

   [System.NonSerialized]
   public bool no_sprite_size_getter;

   void FetchSpriteSizeSetter() {
      if (!sprite_size_setter) {
         var tr = transform;
         var p = tr.parent;
         sprite_size_setter = tr.GetComponent<SpriteSizeSetter>();
         if (sprite_size_setter) return;
         sprite_size_setter = p.GetComponent<SpriteSizeSetter>();
         if (sprite_size_setter) {
            return;
         }
         no_sprite_size_getter = true;
      }

   }

   void SpriteSizeMaybe() {
      if (no_sprite_size_getter) return;
      if (sprite_size_setter == null) {
         FetchSpriteSizeSetter();
         if (sprite_size_setter == null) {
            return;
         }
      }

      if (sprite_in.MaybeGetSize() is Vector2 sprite_size) {
         sprite_size_setter.SetSize(sprite_size);
      } else {
         sprite_size_setter.ClearSize();
      }
   }

   public void UpdateImage() {
      var img = image;

      if (original_layout == null) {
         original_layout = new UIBoxLayoutVars(img.rectTransform);
      } else {
         original_layout.ApplyTo(img.rectTransform);
      }
      var cs = sprite_in;

      UpdateImageColors();

      if (cs.GetOffset() is Vector2 offset) {
         img.rectTransform.anchoredPosition = offset;
         img.preserveAspect = true;
      }


      SpriteSizeMaybe();

      var box_layout = cs.BoxLayout();
      if (box_layout != null) {
         if (!layoutTransform) layoutTransform = img.rectTransform;
         box_layout.ApplyTo(layoutTransform);
      }

   }

   Color? original_color;

   public ColoredSprite sprite_in;
   // Update is called once per frame
   void Update() {
      if (sprite_in != null && sprite_in.Dynamic()) UpdateImageColors();
   }
}
