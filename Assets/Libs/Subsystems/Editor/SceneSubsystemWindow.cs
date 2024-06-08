using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

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
   public bool had_editor;

   bool IsRemoved(Object o) {
      return !o;
   }

   bool RemoveDesstroyedEditor() {
      
      if (editor is not null) {
         
         
         if (IsRemoved(editor.serializedObject.targetObject)) {
            // Debug.Log("Destroyed!");
            editor = null;
            return true;
         }
      } else {
         if (had_editor) {
            // Debug.Log("Dropped editor!");
            had_editor = editor;
            return true;
         }
      }

      had_editor = editor;
      return false;
   }

   void OnEnable() {
      is_open = true;
      Refresh();
      this.autoRepaintOnSceneChange = true;
   }

   void OnDisable() {
      is_open = false;
   }

   void FastUpdate() {
      bool rd = RemoveDesstroyedEditor();
      if (SceneSubsystem.dirty || rd) {
         if (Refresh() || rd) {
            // Debug.Log("repaint!");
            this.Repaint();
         }
      }
   }

   void Update_slow() {
      bool rd = RemoveDesstroyedEditor();
      if (SceneSubsystem.dirty || rd) {
         if (Refresh() || rd) {
            this.Repaint();
         }
      }
   }

   bool Refresh() {
      SceneSubsystem.dirty = false;

      RemoveDesstroyedEditor();

      if (subsystem && subsystem.isActiveAndEnabled && subsystem.gameObject.scene.IsValid() &&
          subsystem.gameObject.scene == EditorSceneManager.GetActiveScene()) {
         // Debug.Log("No refresh");
         if (!editor) {
            editor = Editor.CreateEditor(subsystem);
            // Debug.Log("But recreate editor!");
         }
         had_editor = editor;

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

      had_editor = editor;

      // Debug.Log("Refresh");
      return true;
   }
   private void OnGUI() {
      in_window = true;
      try {

         if (SceneSubsystem.dirty || RemoveDesstroyedEditor()) {
            Refresh();
         }

         if (editor == null) {

            var asc = EditorSceneManager.GetActiveScene();

            if (GUILayout.Button($"Create Scene Subsystem in {asc.name}")) {

               var go = new GameObject("SceneSubsystem");
               var s = go.AddComponent<SceneSubsystem>();

               Undo.RegisterCreatedObjectUndo(go, "Created " + go.name);
            }
            return;
         }


         if (editor) editor.OnInspectorGUI();
      }
      finally {
         in_window = false;
      }
   }
}