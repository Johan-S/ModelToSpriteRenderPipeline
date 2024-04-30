using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FixedEditorCPUThrottler {
   static double last_editor_time;
   static double last_ms;

   [UnityEditor.InitializeOnLoadMethod]
   static void ThrottleEditorUpdates() {
      static void MySleep() {
         double lt = UnityEditor.EditorApplication.timeSinceStartup;

         last_ms = ((lt - last_editor_time) * 1000);
         int ms = (int)last_ms;


         if (ms <= 10 && Application.isPlaying) {
            System.Threading.Thread.Sleep(12 - ms);
         }

         last_editor_time = UnityEditor.EditorApplication.timeSinceStartup;
      }

      UnityEditor.EditorApplication.update -= MySleep;
      UnityEditor.EditorApplication.update += MySleep;
   }
}