using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveOverTime : MonoBehaviour {


   public Vector3 movement;

   // Update is called once per frame
   void Update() {
      transform.localPosition = transform.localPosition + movement * Time.deltaTime;
   }
}
