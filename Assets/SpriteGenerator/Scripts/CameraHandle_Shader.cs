using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class CameraHandle_Shader : MonoBehaviour {
   public Shader shader;

   [SerializeField] Camera cam;

   public Material mat;

   void OnEnable() {
      if (cam && shader) {
         cam.SetReplacementShader(shader, "");
      } else {
         cam.ResetReplacementShader();
      }
   }

   public Transform base_render_mesh;

   void Start() {
      if (base_render_mesh) {
         var tl = GetMeshes(base_render_mesh).ToList();
         
         Debug.Log($"meshes: {tl.Count}\n{tl.join("\n", x => x.tr.name)}");
      }
   }

   Dictionary<object, Mesh> _cach = new();

   IEnumerable<(Transform tr, Mesh m)> GetMeshes(Transform t) {

      foreach (var a in base_render_mesh.GetComponentsInChildren<MeshFilter>()) {
         if (a.sharedMesh)yield return (a.transform, a.sharedMesh);
      }
      foreach (var a in base_render_mesh.GetComponentsInChildren<SkinnedMeshRenderer>()) {
         if (!a.sharedMesh) continue;
         var m = _cach.Get(a) ?? (_cach[a] = new Mesh());
         
         a.BakeMesh(m);
         yield return (a.transform, m);
      }
   }
   

   void Update() {
      if (cam && shader) {
         // am.RenderWithShader(shader, "");
      }

      if (base_render_mesh) {

         var ms = GetMeshes(base_render_mesh).ToArray();

         FlatRenderMeshes(ms);
         // FlatRenderMeshes(ms.Where(x => x).ToArray());
      }
   }


   Color ColorFrom(int i) {
      Color cb = new Color();
      cb.a = 1;


      float scale = 0.5f;

      while (i > 0) {
         if ((i & 1) == 1) cb.r += scale;
         i >>= 1;
         if ((i & 1) == 1) cb.g += scale;
         i >>= 1;
         if ((i & 1) == 1) cb.b += scale;
         i >>= 1;

         scale /= 2;
      }


      return cb;
   }


   public void FlatRenderMeshes((Transform tr, Mesh m)[] meshes) {
      RenderParams rp = new RenderParams(mat);
      rp.camera = cam;

      MaterialPropertyBlock mb = rp.matProps;
      if (mb == null) {
         mb = new MaterialPropertyBlock();
      }

      mb.SetColor("_Color", new Color32(255, 255, (byte)0, 255));

      cam.SetReplacementShader(mat.shader, "");

      float ortho_level = 2;

      var ortho_boxx = Matrix4x4.Ortho(-ortho_level, ortho_level, -ortho_level, ortho_level, -ortho_level, ortho_level);


      var cmat = cam.projectionMatrix;

      var id = Matrix4x4.identity;

      RenderTexture.active = cam.targetTexture;

      for (int i = 0; i < meshes.Length; i++) {
         var (tr, m) = meshes[i];
         // Debug.Log($"mesh: {meshes[i].name}");

         var mat = tr.localToWorldMatrix;
         
         
         mb.SetColor("_Color", ColorFrom(i));
         rp.matProps = mb;
         rp.layer = 15;
         Debug.Log("Render");
         Graphics.RenderMesh(rp, m, 0, mat);
      }

      cam.Render();

      RenderTexture.active = null;
   }


   void OnPreRender() {
   }
}