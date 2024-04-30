using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class AlwaysExecTarget : MonoBehaviour {
   public bool alwaysEcecute;
   public bool notPlaymode;
   public UnityEvent onEditorUpdate;


   void Awake() {
      if (Application.isPlaying && notPlaymode) Destroy(this);
   }

   void Update() {
      if (!alwaysEcecute) return;
      if (Application.isPlaying && notPlaymode) return;
      RunMe();
   }


   double throttle = -1;

   void RunMe() {
      if (!alwaysEcecute) return;

      if (Application.isPlaying) return;

      var rt = EditorApplication.timeSinceStartup;

      if (rt > throttle + 0.02f) {
         throttle = rt;
         onEditorUpdate.Invoke();
      }
   }

#if UNITY_EDITOR
   void OnEnable() {
      EditorApplication.update += RunMe;
   }

   void OnDisable() {
      EditorApplication.update -= RunMe;
   }
#endif
}