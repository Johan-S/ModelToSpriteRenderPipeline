using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class AlwaysExecTarget : MonoBehaviour {
   public UnityEvent onEditorUpdate;

   void Awake() {
      if (Application.isPlaying) Destroy(this);
   }



   double throttle = -1;
   void RunMe() {

      if (Application.isPlaying) return;

      var rt = EditorApplication.timeSinceStartup;

      if (rt > throttle + 0.01f) {
         throttle = rt;
         onEditorUpdate.Invoke();
      }

   }

   void OnEnable() {
      EditorApplication.update += RunMe;
      
   }

   void OnDisable() {

      EditorApplication.update -= RunMe;

   }
}