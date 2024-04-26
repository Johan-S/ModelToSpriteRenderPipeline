using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceBar : MonoBehaviour {

   public float val {
      set {
         float x = value;
         if (x > 1) x = 1;
         if (x < 0) x = 0;
         if (x == 1) {
            front.gameObject.SetActive(false);
            back.gameObject.SetActive(false);
            return;
         } else {
            front.gameObject.SetActive(true);
            back.gameObject.SetActive(true);

            float xr = 1 - x;
            var ls = back.localScale;
            var lp = back.localPosition;

            front.localPosition = lp + Vector3.left * xr * 0.5f * ls.x;
            ls.x *= x;
            front.localScale = ls;
         }
      }
   }

   public Transform front;
   public Transform back;

   // Start is called before the first frame update
   void Start() {

   }

   // Update is called once per frame
   void Update() {

   }
}
