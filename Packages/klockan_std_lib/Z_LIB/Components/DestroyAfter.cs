using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfter : MonoBehaviour {

   public float duration;

   public bool one_frame;

   // Start is called before the first frame update
   void Start() {

   }

   // Update is called once per frame
   void Update() {
      duration -= Time.deltaTime;
      if (duration < 0) {
         Destroy(gameObject);
      }
   }
}
