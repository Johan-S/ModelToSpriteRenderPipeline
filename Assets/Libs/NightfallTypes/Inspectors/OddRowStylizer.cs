using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

// [RequireComponent(typeof(Image))]
public class OddRowStylizer : MonoBehaviour {

   public int group_size = 1;
   public int group_start = 1;


   public float tint_factor_percent = 3;
   
   public Image img;
   Transform tr;
   Transform itr;
   
   void Start() {
      tr = GetComponent<Transform>();
      if (!img) {
         img = Instantiate(FlashingUI.GetBlinkPanelPrefab(), tr);
         img.name = "skip_blink";
      }
      itr = img.transform;
      itr.SetAsFirstSibling();
   }

   void OnDestroy() {
      if (img) Destroy(img.gameObject);
   }

   void LateUpdate() {
      bool odd_calc = (tr.GetSiblingIndex() + group_start + 100 * group_size) / group_size % 2 == 1;

      var c = odd_calc ? Color.black : Color.white;
      c.a = tint_factor_percent * 0.01f;
      img.color = c;
      itr.SetAsFirstSibling();
   }
}
