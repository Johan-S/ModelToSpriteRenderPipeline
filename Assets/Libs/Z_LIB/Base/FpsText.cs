using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FpsText : MonoBehaviour {

   Text text;


   [System.NonSerialized]
   float fps = 60;

   float ticker = 0;

   // Start is called before the first frame update
   void Start() {
      text = GetComponent<Text>();
   }

   // Update is called once per frame
   void Update() {
      float dt = Time.unscaledDeltaTime;

      fps = Mathf.Lerp(fps,1 / dt, Mathf.Min(Time.deltaTime * 4, 1));

      ticker += dt;
      if (ticker >= 0.2f) {
         text.text = $"FPS: {fps:0.#}";
         ticker = 0;
      }
   }
}
