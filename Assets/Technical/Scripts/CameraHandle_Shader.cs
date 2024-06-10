using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

   IEnumerable<(Transform tr, Mesh m)> GetMeshes(Transform t) {
      foreach (var a in t.GetComponentsInChildren<MeshFilter>()) {
         if (a.sharedMesh) yield return (a.transform, a.sharedMesh);
      }

      foreach (var a in t.GetComponentsInChildren<SkinnedMeshRenderer>()) {
         if (!a.sharedMesh) continue;
         var m = new Mesh();
         

         a.BakeMesh(m);
         yield return (a.transform, m);
      }
   }


   void Update() {
      if (cam && shader) {
         // am.RenderWithShader(shader, "");
      }

      if (mat) {
         Debug.Log($"mat shader: {mat.shader.name}");
      }

      if (base_render_mesh) {
         RenderMeshes(base_render_mesh);
         // FlatRenderMeshes(ms.Where(x => x).ToArray());
      }
   }
   Dictionary<(int, int), RenderTexture> render_textures = new();

   public bool fun_colors;


   public void RenderMeshes(Transform tr) {
      var ms = GetMeshes(tr).ToArray();

      FlatRenderMeshes(ms);
   }

   public void CaptureTo(Transform tr, Texture2D texture) {

      var size = (texture.width, texture.height);

      var tex = cam.targetTexture;
      var rt = tex;


      if (!rt || ( rt.width, rt.height) != size) {
         
         if (!render_textures.TryGetValue(size, out rt)) {
            rt = new RenderTexture(size.width, size.height, 32);
            render_textures[size] = rt;
         }
      }
      


      var ot = cam.targetTexture;

      try {

         cam.targetTexture = rt;
         RenderMeshes(tr);

         RenderTexture.active = rt;
         texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

         texture.Apply();
      }
      finally {
         
         RenderTexture.active = null;
         cam.targetTexture = ot;
      }

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


         mb.SetColor("_Color", CameraHandle.ColorFrom(i, fun_colors));
         rp.matProps = mb;
         rp.layer = 15;
         Graphics.RenderMesh(rp, m, 0, mat);
      }

      cam.Render();

      RenderTexture.active = null;
   }


   void OnPreRender() {
   }
}