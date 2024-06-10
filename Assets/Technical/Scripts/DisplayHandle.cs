using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DisplayHandle : MonoBehaviour {


   List<Sprite> sprites = new();

   public Image[] extra_displays;


   public Image diplay;
   public Image diplay_small;

   public Sprite sprite;

   void Start() {
      extra_displays = GetComponentsInChildren<Image>().Where(x => x.name == "image_sprite").ToArray();
   }

   public void DisplayTex(Texture2D t) {
      
      sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.height / 2, 0,
         SpriteMeshType.FullRect);
      
      sprites.Add(sprite);

      diplay.sprite = sprite;
      diplay_small.sprite = sprite;

      foreach (var d in extra_displays) {
         d.sprite = sprite;
      }

   }
}
