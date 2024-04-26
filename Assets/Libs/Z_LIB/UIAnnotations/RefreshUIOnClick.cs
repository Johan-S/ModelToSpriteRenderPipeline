using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefreshUIOnClick : MonoBehaviour {

   bool dirty;
   // Start is called before the first frame update
   void Start() {
      gameObject.OnClick(() => dirty = true);
   }

   // Update is called once per frame
   void Update() {
      if (dirty) {
         AnnotatedUI.ReVisit(transform);
         dirty = false;
      }
   }
}
