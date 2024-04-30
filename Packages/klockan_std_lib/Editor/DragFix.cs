using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class DragFix {
   
   [InitializeOnLoadMethod]
   static void OnLoad() {
      DisableCompletely();
   }
   
   static void DisableCompletely() {
      
      EditorApplication.update += () => { EditorGUIUtility.SetWantsMouseJumping(0); };
      SceneView.duringSceneGui += OnSceneGUI;
   }

   static void FixSelectionOnly() {
      
      SceneView.duringSceneGui += OnSceneGUI_SelectionOnky;
   }

   static void OnSceneGUI(SceneView SceneView) {
      EditorGUIUtility.SetWantsMouseJumping(0);
   }
   static void OnSceneGUI_SelectionOnky(SceneView SceneView) {
      var ct = Event.current;

      if (ct.delta != default) {
         if (ct.type == EventType.Used) {
            EditorGUIUtility.SetWantsMouseJumping(0);
         }
      }

      if (ct.type == EventType.MouseLeaveWindow) {
         EditorGUIUtility.SetWantsMouseJumping(0);
      }
   }
}