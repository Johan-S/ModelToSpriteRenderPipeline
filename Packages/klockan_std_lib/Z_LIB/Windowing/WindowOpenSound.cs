using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowOpenSound : MonoBehaviour {

   private void OnEnable() {
      SoundManager.instance?.MenuOpened();
   }
   private void OnDisable() {
      SoundManager.window_closed_now = true;
   }
}
