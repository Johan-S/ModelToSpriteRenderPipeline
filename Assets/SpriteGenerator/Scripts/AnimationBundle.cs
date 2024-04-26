

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// [CreateAssetMenu(fileName = "New_AnimationBundle", menuName = "Animation Bundle", order = 0)]
public class AnimationBundle : ScriptableObject {

   public AnimationClip[] animation_clips;
   
   
   #if UNITY_EDITOR

   [CustomEditor(typeof(AnimationBundle))]
   public class MyEditor : Editor {


      int last_check = -1;

      int duplicate_num = 0;
      
      void Init() {
         
         AnimationBundle t = (AnimationBundle)target;
         if (t.animation_clips.Length == last_check) return;
         last_check = t.animation_clips.Length;

         duplicate_num = last_check - t.animation_clips.Select(x => x.name).ToHashSet().Count;
      }
      
      public override void OnInspectorGUI() {

         Init();
         AnimationBundle t = (AnimationBundle)target;

         if (duplicate_num > 0) {
            
            if (GUILayout.Button($"Remove {duplicate_num} Duplicates")) {

               AnimationClip[] clips = t.animation_clips.GroupBy(x => x.name).Select(x => x.First()).ToArray();
               if (clips.Length != t.animation_clips.Length) {
               
                  Undo.RecordObject(t, "AnimationBundle filter clips");
                  t.animation_clips = clips;
               
               }

            }
         }
         
         
         base.OnInspectorGUI();
         
         
      }
   }
   
   #endif

}
