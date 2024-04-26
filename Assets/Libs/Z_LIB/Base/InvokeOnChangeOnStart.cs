using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class InvokeOnChangeOnStart : MonoBehaviour {
   // Start is called before the first frame update
   void Start() {
      if (GetComponent<Slider>()) {
         var s = GetComponent<Slider>();
         s.onValueChanged.Invoke(s.value);
      }
      if (GetComponent<InputField>()) {
         var s = GetComponent<InputField>();
         s.onValueChanged.Invoke(s.text);
      }
   }

   // Update is called once per frame
   void Update() {

   }
}
