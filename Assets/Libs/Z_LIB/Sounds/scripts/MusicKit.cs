using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicKit : MonoBehaviour {

   public float duration;

   bool fading_in = true;
   float fade_in_time = 0;

   float fade_time;
   float fade_duration = 0.5f;
   bool fading;

   float base_volume;

   public void SetFadeOut() {
      fade_time = 1;
      fading = true;
   }


   // Start is called before the first frame update
   void Awake() {
      var a = GetComponent<AudioSource>();
      base_volume = a.volume;
   }

   // Update is called once per frame
   void Update() {
      if (fading_in) {
         fade_in_time += Time.deltaTime / fade_duration;
         if (fade_in_time > 1) {
            fade_in_time = 1;
            fading_in = false;
         }
         var a = GetComponent<AudioSource>();
         a.volume = base_volume * fade_in_time;
      }

      if (fading) {
         fade_time -= Time.deltaTime / fade_duration;

         if (fade_time < 0) {
            Destroy(gameObject);
         } else {
            var a = GetComponent<AudioSource>();
            a.volume = base_volume * fade_time;
         }
      }
   }
}
