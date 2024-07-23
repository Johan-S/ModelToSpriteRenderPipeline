using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static System.Runtime.CompilerServices.MethodImplOptions;


[Serializable]
[ExecuteAlways]
public class MyPrefabRender : UnityEngine.MonoBehaviour {
   public GameObject prefab_render;


   void OnRenderObject() {
      ImmediateRender();
   }

   private void ImmediateRender() {
      // Not sure this check is needed?
      if (
         // Game view.
         Camera.current == Camera.main
         // Scene view.
         || Camera.current.name == "SceneCamera"
         // Renders the camera preview when you select a camera. Note the space.
         || Camera.current.name == "Preview Camera") {
         // Apply the material
         Std.lineMaterial.SetPass(0);


         if (prefab_render) {
            var my_tr = transform;
            var me_to_world = my_tr.localToWorldMatrix;
            var ptr = prefab_render.transform;
            var world_to_prefab = ptr.worldToLocalMatrix;
            var mat = me_to_world * world_to_prefab;
            foreach (var ms in prefab_render.GetComponentsInChildren<MeshRenderer>()) {
               var mf = ms.GetComponent<MeshFilter>();
               var curt = ms.transform;

               var local_to_world = curt.localToWorldMatrix;

               Graphics.DrawMeshNow(mf.sharedMesh, mat * local_to_world);
            }
         }

         // Graphics.DrawMeshNow(mesh, transform.position + new Vector3(2, 0), transform.rotation);
      }
   }
}