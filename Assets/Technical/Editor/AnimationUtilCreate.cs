using System;
using Shared;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.EditorGUI;
using Object = UnityEngine.Object;

namespace NightfallEditor {
   public static class AnimationUtilCreate {
      public static string GetContextPath(Object o) {
         if (!o) return null;
         var p = AssetDatabase.GetAssetPath(o);
         if (p.IsEmpty()) return null;
         if (o is DefaultAsset) return p;
         return p.SubstringBeforeLast("/");
      }
      [MenuItem("Assets/Create/Animation Controller Programmatically", priority = -100)]
      public static void CreateAnimationController() {

         var dir = GetContextPath(Selection.objects.Get(0, null));

         if (dir == null) {
            
            Debug.LogError($"Need to create from asset menu!");
            return;
         }

         var man = Resources.Load<AnimationManager>("AnimationManager");

         var blink = man.animation_bundles[0];
         
         // Create the controller
         AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath($"{dir}/NewAnimatorController.controller");

         // Add parameters

         AnimatorState none_state = controller.layers[0].stateMachine.AddState("None");

         foreach (var clip in blink.animation_clips) {
            var name = clip.NormalizedName();
            
            AnimatorState state = controller.layers[0].stateMachine.AddState(name);
            state.motion = clip;
         }
      }
   }
}