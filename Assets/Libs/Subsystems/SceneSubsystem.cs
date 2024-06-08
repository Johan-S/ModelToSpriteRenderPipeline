using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


[DefaultExecutionOrder(-2000)]
[ExecuteAlways]
public class SceneSubsystem : MonoBehaviour {
   static Dictionary<Scene, SceneSubsystem> _subsystems = new();

   public void _Deactivate(GameObject o) {
      o.SetActive(false);
   }

#if UNITY_EDITOR
   public static bool dirty;

   void OnValidate() {
      dirty = true;
   }
#endif
   static void MakeDirty() {
#if UNITY_EDITOR
      dirty = true;
#endif
   }


   public UnityEvent beforeAwake;
   public UnityEvent beforeStart;
   public UnityEvent onUpdate;

   public UnityEvent onLateUpdate;

   void Awake() {
      if (Application.isEditor) return;
      var s = gameObject.scene;

      if (_subsystems.TryGetValue(s, out var v)) {
         if (v) {
            Debug.Log($"duplicate scene subsystem! {v} and {this}");
            enabled = false;
            return;
         }
      }

      beforeAwake?.Invoke();
   }

   void OnEnable() {
      MakeDirty();
   }

   void OnDisable() {
      MakeDirty();
   }

   // Start is called before the first frame update
   void Start() {
      if (Application.isEditor) return;
      beforeStart?.Invoke();
   }

   // Update is called once per frame
   void Update() {
      if (Application.isEditor) return;
      onUpdate?.Invoke();
   }

   void LateUpdate() {
      if (Application.isEditor) return;
      onLateUpdate?.Invoke();
   }
}