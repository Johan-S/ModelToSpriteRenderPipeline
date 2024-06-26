using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CameraHandle : MonoBehaviour {
   public static Color ColorFrom(int i, bool fun = false) {
      if (!fun) return new Color32(255, 255, (byte)i, 255);
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

   Camera my_cam => _my_cam ? _my_cam : (_my_cam = GetComponent<Camera>());

   Camera _my_cam;
   public Vector2 OrthographicRectSize => new(my_cam.orthographicSize * 2, my_cam.orthographicSize * 2);

   public float TotalDepth() => my_cam.farClipPlane - my_cam.nearClipPlane;


   public Material color_mat;


   public void FlatRenderMeshes(Texture2D texture, Mesh[] meshes) {
      var size = (texture.width, texture.height);

      var render_texture = texture.GetCachedRenderTextureFor();

      my_cam.targetTexture = render_texture;
      RenderTexture.active = render_texture;
      RenderParams rp = new RenderParams();
      rp.camera = my_cam;

      var ortho_boxx = Matrix4x4.Ortho(-2, 2, -2, 2, -2, 2);

      foreach (var m in meshes) {
         Graphics.RenderMesh(rp, m, 0, ortho_boxx);
      }


      RenderTexture.active = render_texture;
      texture.ReadPixels(new Rect(0, 0, render_texture.width, render_texture.height), 0, 0);

      texture.Apply();
   }


   public void CaptureTo(RenderTexture render_texture, Shader shader, Color? clear_color=null) {
      Color bg = my_cam.backgroundColor;
      try {
         my_cam.backgroundColor = clear_color ?? bg;
         my_cam.targetTexture = render_texture;
         my_cam.RenderWithShader(shader, "");
         RenderTexture.active = render_texture;
      }
      finally {
         my_cam.backgroundColor = bg;
      }


   }

   RenderTexture GetRenderFor(Texture2D texture) {
      return texture.GetCachedRenderTextureFor();
   }

   public void CaptureTo(Texture2D texture) {
      var render_texture = GetRenderFor(texture);
      CaptureTo(render_texture);
      RenderTexture.active = render_texture;
      texture.ReadPixels(new Rect(0, 0, render_texture.width, render_texture.height), 0, 0);

      texture.Apply();
   }

   public void CaptureTo(RenderTexture render_texture) {

      my_cam.targetTexture = render_texture;
      my_cam.Render();

   }

   public void CaptureTo(Transform tr, RenderTexture rt) {


      var ot = my_cam.targetTexture;

      try {
         my_cam.targetTexture = rt;
         RenderTexture.active = null;
         RenderMeshes(tr);
      }
      finally {
         RenderTexture.active = null;
         my_cam.targetTexture = ot;
      }
   }


   IEnumerable<(Transform tr, Mesh m)> GetMeshes(Transform t) {
      foreach (var a in t.GetComponentsInChildren<MeshFilter>()) {
         if (a.sharedMesh) yield return (a.transform, a.sharedMesh);
      }

      foreach (var a in t.GetComponentsInChildren<SkinnedMeshRenderer>()) {
         if (!a.sharedMesh) continue;
         var m = new Mesh();

         a.BakeMesh(m, true);
         yield return (a.transform, m);
      }
   }

   public void RenderMeshesToMe(Transform root_trans) {
      var ms = GetMeshes(root_trans).ToArray();

      RenderTexture.active = my_cam.targetTexture;
      FlatRenderMeshes(ms);

      RenderTexture.active = null;
   }

   public void RenderMeshes(Transform root_trans) {
      var ms = GetMeshes(root_trans).ToArray();

      FlatRenderMeshes(ms);
   }

   public void FlatRenderMeshes((Transform tr, Mesh m)[] meshes) {
      RenderParams rp = new RenderParams(color_mat);
      rp.camera = my_cam;

      MaterialPropertyBlock mb = rp.matProps;
      if (mb == null) {
         mb = new MaterialPropertyBlock();
      }

      mb.SetColor("_Color", new Color32(255, 255, (byte)0, 255));

      float ortho_level = 2;

      var cmat = my_cam.projectionMatrix;

      var id = Matrix4x4.identity;

      var omask = my_cam.cullingMask;

      try {
         my_cam.cullingMask = 1 << 15;
         for (int i = 0; i < meshes.Length; i++) {
            var (tr, m) = meshes[i];

            var mat = tr.localToWorldMatrix;


            mb.SetColor("_Color", ColorFrom(i));
            rp.matProps = mb;
            rp.layer = 15;

            Graphics.RenderMesh(rp, m, 0, mat);
         }

         my_cam.Render();
      }
      finally {
         my_cam.cullingMask = omask;
      }

   }
   void Awake() {
      my_cam.enabled = false;
   }
}