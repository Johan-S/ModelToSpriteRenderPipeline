using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Technical.Editor {
   public class EditorDropdownTools {
      
      [MenuItem("Nightfall/Run Test")]
      private static void ShowWindow() {

      }

      static void MakeTaperingCircle() {
         
         int pix = 256;

         float mid = (pix - 1) / 2;


         float e = mid;

         float s = mid * 0.5f;

         WriteCirclePng("CircleTest_Shaded.png", pix, r => Mathf.InverseLerp(e, s, r));

      }


      static void WriteCirclePng(string file_name, int pixels, Func<float, float> radius_alpha) {
         
         Debug.Log($"WriteCirclePng {pixels} -> {file_name}");

         float mid = (pixels - 1) / 2;
         var tex = new Texture2D(256, 256);
         


         foreach (var pi in (256, 256).times()) {
            var (x, y) = pi;
            Vector2 d = new Vector2(x - mid, y - mid);


            var r = d.magnitude;

            var a = radius_alpha(r);

            tex.SetPixel(x, y, new Color(1, 1, 1, a));




         }
         tex.Apply();

         var png = tex.EncodeToPNG();
         
         File.WriteAllBytes("Assets/CircleTest_Shaded.png", png);
      }
      
   }
}