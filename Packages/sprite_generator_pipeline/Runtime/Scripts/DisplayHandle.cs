using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DisplayHandle : MonoBehaviour {


   public Image[] extra_displays;


   public RawImage diplay;
   public RawImage diplay_small;

   void Start() {
      extra_displays = GetComponentsInChildren<Image>().Where(x => x.name == "image_sprite").ToArray();
   }

   public void DisplayTex(Texture t, Texture raw_t) {
      

      diplay.texture = raw_t;
      diplay_small.texture = t;

      foreach (var d in extra_displays) {
         // d.sprite = sprite;
      }

   }
}
