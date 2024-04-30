using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ApplyColorTheme : MonoBehaviour {


   public bool dark;

   public Image img;

   // Start is called before the first frame update
   void Start() {
      img = GetComponent<Image>();
      if (ColorTheme.theme) {
         var c = dark ? ColorTheme.theme.dark : ColorTheme.theme.bright;
         var oc = img.color;
         c.a = oc.a;
         img.color = c;
      }
   }

   // Update is called once per frame
   void Update() {
      if (ColorTheme.theme) {
         var c = dark ? ColorTheme.theme.dark : ColorTheme.theme.bright;
         var oc = img.color;
         c.a = oc.a;
         img.color = c;
      }
   }
}
