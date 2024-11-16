using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DisplayHandle : MonoBehaviour {
   public RawImage[] displays;

   public RawImage[] displays_small;

   public void DisplayTex(Texture t, Texture raw_t) {
      foreach (var d in displays) {
         d.texture = raw_t;
      }

      foreach (var d in displays_small) {
         d.texture = t;
      }
   }
}