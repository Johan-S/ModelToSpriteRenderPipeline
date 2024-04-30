using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextEnhance : MonoBehaviour {

   public Text text;

   public void A_SetText(float f) {
      text.text = $"{f:0.#}";
   }
   public void A_SetText(int f) {
      text.text = $"{f:0.#}";
   }

   // Start is called before the first frame update
   void Awake() {
      text = GetComponent<Text>();
      Debug.Assert(text);
   }
}
