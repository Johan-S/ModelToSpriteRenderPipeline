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
   public class SpritePipelineAssetManagement : EditorWindow {
      [MenuItem("Nightfall/Nightfall Asset Management", priority = -150)]
      private static void ShowWindow() {
         var window = GetWindow<SpritePipelineAssetManagement>();
         window.titleContent = new GUIContent("Nightfall Asset Management".ToTitleCase());
         window.Show();
      }

      public ExportPipelineSheets sheets_descriptor;

      private void OnGUI() {

         if (GUILayout.Button("Gen folder")) {
            var dir = StdEditor.GetLocalOut("export_anims");
            StdEditor.EnsureAssetDir(dir);
         }
         
         EditorGUILayout.Separator();
         sheets_descriptor = (ExportPipelineSheets)EditorGUILayout.ObjectField("Sheets", sheets_descriptor, typeof(ExportPipelineSheets));

         using (new EditorGUI.DisabledScope(sheets_descriptor == null)) {
            if (GUILayout.Button("Gen Animations From Sheets")) {
            
               sheets_descriptor.InitData();
               var anims = sheets_descriptor.animation_arr;

               var dir = StdEditor.GetLocalOut("export_anims");
               StdEditor.EnsureAssetDir(dir);

               foreach (var a in anims) {
                  var res = ScriptableObject.CreateInstance<AnimationTypeObject>();
                  res.name = a.full_name;

                  Std.CopyShallowDuckTyped(a, res);
                  AssetDatabase.CreateAsset(res, dir + res.name + ".asset");
               }
               AssetDatabase.SaveAssets();
               // Debug.Log($"hi: {anims.Length} \n{anims.join("\n", x => $"{x.animation_type}.{x.category}")}");
               
               
            }
         }
         
      }
   }
}