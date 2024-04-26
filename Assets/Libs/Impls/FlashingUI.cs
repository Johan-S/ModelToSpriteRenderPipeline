using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
public class FlashingUI : MonoBehaviour {


   public Color color;

   public Color color2;
   public Image image;
   public float blink_duration;

   [System.NonSerialized]
   public int blink_times = 2;


   static Image BlinkPanelPrefab;

   public static Image GetBlinkPanelPrefab() {
      if (!BlinkPanelPrefab) {
         BlinkPanelPrefab = Resources.Load<Image>("BlinkPanel");
      }

      return BlinkPanelPrefab;
   }

   // Start is called before the first frame update
   IEnumerator Start() {

      image = Instantiate(GetBlinkPanelPrefab(), transform);
      image.name = "ignore_";
      image.gameObject.ForceComponent<DestroyAfter>().duration = 10;

      if (color.a == 0) {
         color = new Color(1, 1, 1, 0.25f);
         color2 = new Color(0.5f, 0.5f, 0.5f, 0.25f);
      }
      if (blink_duration == 0) {
         blink_duration = 0.2f;
      }
      var c1 = (Color)color;

      image.color = c1;

      if (blink_times < 1) blink_times = 1;
      if (blink_times > 10) blink_times = 10;

      if (blink_duration > 1) blink_duration = 1;

      int n = blink_times;
      var dur = blink_duration;

      for (int i = 0; i < n; ++i) {
         image.color = c1;
         image.gameObject.SetActive(true);
         yield return new WaitForSeconds(blink_duration);

         if (color2 is Color c2) {
            image.color = c2;
         } else {
            image.gameObject.SetActive(false);
         }


         if (i == n - 1) break;

         yield return new WaitForSeconds(blink_duration);
      }


      Destroy(image.gameObject);
      Destroy(this);
   }

   private void OnDisable() {
      if (image) Destroy(image.gameObject);
      Destroy(this);
   }

   private void OnDestroy() {
      if (image) Destroy(image.gameObject);
   }
}
