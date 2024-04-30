using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ExplosionFade : MonoBehaviour {

   void UpdateVars(float x) {
      if (x >= 1) {
         Destroy(gameObject);return;
      }

      float sig = Mathf.SmoothStep(0, 1, 1 - x);

      rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.height * sig);

      cg.alpha = Mathf.Min(1 - x, 1);
   }


   private void OnDisable() {
      Destroy(gameObject);
   }
   private void OnDestroy() {
   }

   float time;

   CanvasGroup cg;

   RectTransform rt;

   Rect rect;

   // Start is called before the first frame update
   void Start() {
      cg = gameObject.ForceComponent<CanvasGroup>();

      var csf = GetComponent<ContentSizeFitter>();
      if (csf) csf.enabled = false;
      rt = GetComponent<RectTransform>();
      rect = rt.rect;
      UpdateVars(0);
   }

   // Update is called once per frame
   void Update() {
      time += Time.deltaTime;
      Debug.Log($"Time stuff: {time}");
      UpdateVars(time);
   }
}
