using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandle : MonoBehaviour {

   Camera my_cam;

   public float TotalDepth() => my_cam.farClipPlane - my_cam.nearClipPlane;

   Dictionary<(int, int), RenderTexture> render_textures;

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
