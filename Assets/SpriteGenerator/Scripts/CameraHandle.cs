using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraHandle : MonoBehaviour {

   Camera my_cam;

   public Vector2 OrthographicRectSize => new(my_cam.orthographicSize * 2, my_cam.orthographicSize * 2);
   

   public float TotalDepth() => my_cam.farClipPlane - my_cam.nearClipPlane;

   Dictionary<(int, int), RenderTexture> render_textures;


   public Material color_mat;
   

   public void FlatRenderMeshes(Texture2D texture, Mesh[] meshes) {
      
      var size = (texture.width, texture.height);

      if (!render_textures.TryGetValue(size, out var render_texture)) {
         render_texture = new RenderTexture(size.width, size.height, 32);
         render_textures[size] = render_texture;
      }

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
   

   public void CaptureTo(Texture2D texture, Shader shader) {

      var size = (texture.width, texture.height);

      if (!render_textures.TryGetValue(size, out var render_texture)) {
         render_texture = new RenderTexture(size.width, size.height, 32);
         render_textures[size] = render_texture;
      }

      my_cam.targetTexture = render_texture;
      my_cam.RenderWithShader(shader, "");

      RenderTexture.active = render_texture;
      texture.ReadPixels(new Rect(0, 0, render_texture.width, render_texture.height), 0, 0);

      texture.Apply();
   }
   public void CaptureTo(Texture2D texture) {

      var size = (texture.width, texture.height);

      if (!render_textures.TryGetValue(size, out var render_texture)) {
         render_texture = new RenderTexture(size.width, size.height, 32);
         render_textures[size] = render_texture;
      }

      my_cam.targetTexture = render_texture;
      my_cam.Render();

      RenderTexture.active = render_texture;
      texture.ReadPixels(new Rect(0, 0, render_texture.width, render_texture.height), 0, 0);

      texture.Apply();
   }

   void Awake() {
      my_cam = GetComponent<Camera>();
      my_cam.enabled = false;
      render_textures = new();
   }
}
