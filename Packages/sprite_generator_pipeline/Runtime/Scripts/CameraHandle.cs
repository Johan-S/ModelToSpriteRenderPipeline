using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[DefaultExecutionOrder(-150)]
public class CameraHandle : MonoBehaviour {
   [Range(0, 90)] public float camera_pitch_angle;

   [Range(-180, 180)] public float camera_yaw;

   public Vector2 camera_pivot = new Vector2(0.5f, 0.15f);

   public float camera_size = 4;

   public static Transform camera_tr {
      get {
         var a = GetInstance();
         if (!a) return null;
         return a._my_cam.transform;
      }
   }

   static CameraHandle GetInstance() {
      if (!instance) {
         var sc = GameObject.FindWithTag("SpriteCamera");
         instance = sc.GetComponent<CameraHandle>();
      }

      return instance;
   }

   static CameraHandle instance;

   void OnValidate() {
      UpdateCamera();
   }

   void Update() {
      UpdateCamera();
   }

   void OnEnable() {
      instance = this;
      if (Application.isPlaying) my_cam.enabled = false;
      UpdateCamera();
   }


   public void UpdateCamera() {
      if (camera_size / 2 != my_cam.orthographicSize) {
         my_cam.orthographicSize = camera_size / 2;
      }

      float size = my_cam.orthographicSize;
      transform.localRotation = Quaternion.Euler(camera_pitch_angle, -camera_yaw, 0);

      var bl = new Vector2(size, size);
      var mid = bl - camera_pivot * size * 2;
      transform.localPosition = transform.localRotation * new Vector3(mid.x, mid.y, 0);
   }

   public static Color ColorFrom(int i, bool fun = false) {
      if (!fun) return new Color32(255, 255, (byte)i, 255);
      Color cb = new Color();
      cb.a = 1;


      float scale = 0.5f;
      i += 3;

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

   Camera my_cam => _my_cam ? _my_cam : (_my_cam = GetComponentInChildren<Camera>());

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


   public void CaptureTo(RenderTexture render_texture, Shader shader, Color? clear_color = null) {
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
      foreach (var a in t.GetComponentsInChildren<Renderer>()) {
         if (a is SkinnedMeshRenderer mr) {
            if (!mr.sharedMesh) continue;
            var m = new Mesh();

            mr.BakeMesh(m, true);
            yield return (a.transform, m);
         } else if (a.GetComponent<MeshFilter>() is MeshFilter mf) {
            yield return (a.transform, mf.sharedMesh);
         }
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

      var oc = my_cam.backgroundColor;
      try {
         my_cam.backgroundColor = Color.black;
         my_cam.cullingMask = 1 << 15;
         Color last_c = ColorFrom(0);
         Transform last_part = null;
         int color_id = 0;
         for (int i = 0; i < meshes.Length; i++) {
            var (tr, m) = meshes[i];

            var submesh_sort = new[] { 0 };
            if (m.subMeshCount > 1) submesh_sort = m.subMeshCount.times().sorted(x => m.GetSubMesh(x).vertexCount);

            foreach (int j in submesh_sort) {
               var par = tr.GetComponent<PartsOutlineController>();

               var mat = tr.localToWorldMatrix;


               if (par && par.renderAsPartOfLastPart) {
                  mb.SetColor("_Color", last_c);
               } else {
                  last_part = tr;
                  last_c = ColorFrom(color_id++, false);
                  mb.SetColor("_Color", last_c);
               }

               rp.matProps = mb;
               rp.layer = 15;

               Graphics.RenderMesh(rp, m, j, mat);
            }
         }

         my_cam.Render();
      }
      finally {
         my_cam.cullingMask = omask;
         my_cam.backgroundColor = oc;
      }
   }
}