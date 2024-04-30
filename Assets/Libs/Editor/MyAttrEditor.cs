using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

[CustomEditor(typeof(Object), editorForChildClasses: true, isFallback = true), CanEditMultipleObjects]
public class MyAttrEditor : Editor {
   MethodInfo[] to_call;
   void OnEnable() {

      if (this.targets != null && targets.Length > 1) {
         return;
      }
      
      var t = target.GetType();

      to_call = t.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
         .Where(x => x.GetCustomAttribute<ButtonAttribute>() != null && x.GetParameters().Length == 0)
         .ToArray();
   }

   public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      if (this.targets != null && targets.Length > 1) {
         return;
      }

      foreach (var b in to_call) {
         if (GUILayout.Button(b.Name)) {

            if (target is Component c) {
               
               Undo.RegisterFullObjectHierarchyUndo(c.gameObject, "Called button " + b.Name);
               b.Invoke(target, Array.Empty<object>());
               Undo.RegisterFullObjectHierarchyUndo(c.gameObject, "Called button " + b.Name);
            } else {
               Undo.RegisterFullObjectHierarchyUndo(target, "Called button " + b.Name);
               b.Invoke(target, Array.Empty<object>());
               Undo.RegisterFullObjectHierarchyUndo(target, "Called button " + b.Name);
               
            }
            
         }
      }
   }
}