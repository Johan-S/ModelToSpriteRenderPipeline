using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneSubsystemWindow : EditorWindow {
   [MenuItem("Nightfall/Scene Subsystem")]
   private static void ShowWindow() {
      var window = GetWindow<SceneSubsystemWindow>();
      window.titleContent = new GUIContent("Scene Subsystem");
      window.Show();
   }

   static bool is_open;

   static bool in_window;


   [CustomEditor(typeof(SceneSubsystem))]
   public class OpeneSceneSubsystemWindow : Editor {
      public override void OnInspectorGUI() {
         if (!in_window) {
            using (new EditorGUI.DisabledScope(is_open)) {
               if (GUILayout.Button("Open Subsystem Window")) {
                  ShowWindow();
               }
            }
         }

         base.OnInspectorGUI();
      }
   }


   public SceneSubsystem subsystem;
   public Editor editor;

   void OnEnable() {
      is_open = true;
      Refresh();
   }

   void OnDisable() {
      is_open = false;
   }

   void Update() {
      if (SceneSubsystem.dirty) {
         if (Refresh()) {
            this.Repaint();
         }
      }
   }

   bool Refresh() {
      SceneSubsystem.dirty = false;

      if (editor is not null) {
         if (!editor.serializedObject.targetObject) {
            editor = null;
         }
      }

      if (subsystem && subsystem.isActiveAndEnabled && subsystem.gameObject.scene.IsValid() &&
          subsystem.gameObject.scene == EditorSceneManager.GetActiveScene()) {
         // Debug.Log("No refresh");
         if (!editor) {
            editor = Editor.CreateEditor(subsystem);
            // Debug.Log("But recreate editor!");
         }

         return false;
      }

      subsystem = FindObjectOfType<SceneSubsystem>();
      if (subsystem && subsystem.gameObject.scene != EditorSceneManager.GetActiveScene()) {
         subsystem = null;
      }

      if (subsystem) editor = Editor.CreateEditor(subsystem);
      else {
         editor = null;
      }

      // Debug.Log("Refresh");
      return true;
   }

   private void OnGUI() {
      in_window = true;
      try {
         if (editor is not null) {
            if (!editor.serializedObject.targetObject) {
               Debug.Log("Destroyed!");
               // Refresh();
            }
         }

         if (SceneSubsystem.dirty) {
            Refresh();
         }

         if (editor) editor.OnInspectorGUI();
      }
      finally {
         in_window = false;
      }
   }
}