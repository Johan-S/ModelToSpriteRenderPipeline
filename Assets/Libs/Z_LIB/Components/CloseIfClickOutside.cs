using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseIfClickOutside : MonoBehaviour {

   public float time_enabled = -1;

   private void OnEnable() {
      time_enabled = Time.time;
   }

   // Update is called once per frame
   void Update() {
      if (time_enabled == Time.time) return;
      if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) {
         if (!GetComponent<RectTransform>().ContainsMouse()) {
            gameObject.SetActive(false);
         }
      }
   }
}
