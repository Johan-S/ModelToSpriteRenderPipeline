using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevisitSelf : MonoBehaviour {

   // Update is called once per frame
   void Update() {
      AnnotatedUI.ReVisit(transform);
   }
}
