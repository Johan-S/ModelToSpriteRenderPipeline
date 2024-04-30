using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirtyUpdater : MonoBehaviour {

   public interface DirtyTracker {
      int isDirty();
      void raiseDirty();
   }


   public void Track(DirtyTracker d) {
      o = d;
      cur = d.isDirty();
   }

   int cur;
   DirtyTracker o;


   // Update is called once per frame
   void Update() {
      if (o.isDirty() != cur) {
         AnnotatedUI.Visit(transform, o, "DirtyUpdater");
         cur = o.isDirty();
      }
   }

   private void LateUpdate() {
      
   }
}
