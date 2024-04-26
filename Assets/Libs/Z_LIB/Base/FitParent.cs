using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class FitParent : MonoBehaviour {

   RectTransform rt;


   void Fit() {
      rt.anchorMin = new Vector2();
      rt.anchorMax = new Vector2(1, 1);

      rt.offsetMin = new Vector2();
      rt.offsetMax = new Vector2();
   }

   // Start is called before the first frame update
   void Start() {
      rt = GetComponent<RectTransform>();
      var le = gameObject.ForceComponent<LayoutElement>();
      le.ignoreLayout = true;
      Fit();
   }

   // Update is called once per frame
   void Update() {
      Fit();
   }
}
