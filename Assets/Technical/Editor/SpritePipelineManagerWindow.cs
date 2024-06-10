using System;
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

      public bool auto_start_on_play;
      public bool auto_export;
      public bool auto_exit_playmode;


      public ExportPipeline pipeline;


      void Update() {
      }

      void OnPlaymodeChange(PlayModeStateChange change) {
         if (change is PlayModeStateChange.EnteredPlayMode) {
            if (auto_run && pipeline.isActiveAndEnabled) {
               pipeline.onPipelineDone.AddListener(() => { });
               pipeline.omExportDone.AddListener(() => {
                  if (auto_run) {
                     EditorApplication.delayCall += () => {
                        EditorApplication.ExitPlaymode();
                     };
                  }
               });

               pipeline.start_gen?.Invoke();
            }
         }
      }

      void OnEnable() {
         EditorApplication.playModeStateChanged += OnPlaymodeChange;
      }

      void OnDisable() {
         EditorApplication.playModeStateChanged -= OnPlaymodeChange;
      }

      private void OnGUI() {
         using (new DisabledScope()) {
            var o = GameObject.FindWithTag("ExportPipeline");

            if (o) pipeline = o.GetComponent<ExportPipeline>();


            pipeline = (ExportPipeline)EditorGUILayout.ObjectField("Pipeline", pipeline, typeof(ExportPipeline));
         }

         auto_run = EditorGUILayout.Toggle("Auto", auto_run);
      }
   }
}