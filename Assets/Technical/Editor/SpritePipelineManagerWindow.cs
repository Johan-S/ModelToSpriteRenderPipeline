using System;
using Shared;
using UnityEditor;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.EditorGUI;
using Object = UnityEngine.Object;

namespace NightfallEditor {
   public class SpritePipelineManagerWindow : EditorWindow {
      [MenuItem("Nightfall/Sprite Pipeline Runner", priority = -200)]
      private static void ShowWindow() {
         var window = GetWindow<SpritePipelineManagerWindow>();
         window.titleContent = new GUIContent("Sprite Pipeline Runner");
         window.Show();
      }


      [Serializable]
      public class Settings {
      }

      public bool auto_run;

      public bool full_auto;
      public bool auto_start_on_play;
      public bool auto_export;


      public ExportPipeline pipeline;


      void Update() {
      }

      

      void OnPlaymodeChange(PlayModeStateChange change) {
         if (change is PlayModeStateChange.EnteredEditMode) {
            full_auto = false;
            ExportPipeline.export_override = false;
         }
         if (change is PlayModeStateChange.EnteredPlayMode) {
            if ((auto_run || full_auto) && pipeline.isActiveAndEnabled) {
               pipeline.onPipelineDone.AddListener(() => {
                  if (full_auto) pipeline.export_files_action?.Invoke();
               });
               pipeline.omExportDone.AddListener(() => {
                  if (full_auto) {
                     MainObject.DelayCall(1, () => {
                        EditorApplication.ExitPlaymode();
                     });
                  }
               });

               pipeline.start_gen?.Invoke();
            }
         }
      }

      void OnEnable() {
         EditorApplication.playModeStateChanged += OnPlaymodeChange;
         
         ExportPipeline.export_override = full_auto;
      }

      void OnDisable() {
         EditorApplication.playModeStateChanged -= OnPlaymodeChange;
         
         ExportPipeline.export_override = false;
      }

      private void OnGUI() {
         using (new DisabledScope()) {
            var o = GameObject.FindWithTag("ExportPipeline");

            if (o) pipeline = o.GetComponent<ExportPipeline>();


            pipeline = (ExportPipeline)EditorGUILayout.ObjectField("Pipeline", pipeline, typeof(ExportPipeline));
         }
         
         EditorGUILayout.Separator();

         auto_run = EditorGUILayout.Toggle("Auto", auto_run);

         using (new EditorGUI.DisabledScope(pipeline == null)) {
            if (GUILayout.Button("Run full pipeline Export")) {
               full_auto = true;
               ExportPipeline.export_override = true;
               if (!EditorApplication.isPlaying) {
                  EditorApplication.EnterPlaymode();
               }
               EditorApplication.isPaused = false;
            }
         }
         
         EditorGUILayout.Separator();
         
      }
   }
}