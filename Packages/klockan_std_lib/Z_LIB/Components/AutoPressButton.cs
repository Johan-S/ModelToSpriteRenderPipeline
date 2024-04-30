using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AutoPressButton : MonoBehaviour {

   public bool should_press;
   public float delay;
   public float cur_time;
   public int frame_delay;
   public int cur_frame;

   // Update is called once per frame
   void Update() {
      if (should_press && (cur_time >= delay || cur_frame >= frame_delay)) {
         should_press = false;
         GetComponent<Button>().onClick.Invoke();
         enabled = false;
      }
      cur_frame++;
      cur_time += Time.deltaTime;
   }
}
