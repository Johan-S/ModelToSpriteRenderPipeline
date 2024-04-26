using System;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class AlwaysExecTarget : MonoBehaviour {
   public UnityEvent onEditorUpdate;

   void Awake() {
      if (Application.isPlaying) Destroy(this);
   }


   void Update() {
      if (!Application.isPlaying) onEditorUpdate.Invoke();
   }
}