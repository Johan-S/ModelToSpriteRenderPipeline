using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorTheme : MonoBehaviour {


   public static ColorTheme theme;

   public Color bright = Color.white;
   public Color dark = Color.black;

   private void Awake() {
      theme = this;
   }

   public void Select() {
      theme = this;
   }
}
