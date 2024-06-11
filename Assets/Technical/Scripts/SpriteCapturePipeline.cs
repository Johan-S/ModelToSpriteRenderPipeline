using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using FilterMode = UnityEngine.FilterMode;

[DefaultExecutionOrder(10)]
public class SpriteCapturePipeline : MonoBehaviour {
   public int size = 128;

   public bool outline_depth = true;
   public bool outline_parts = true;

   [Header("Unity Bindings")]
   public ModelHandle model;

   public DisplayHandle display_handle;
   public CameraHandle camera_handle;

   [Header("Outputs")] public Texture2D result_rexture;
   
   public Texture2D basic_shading_texture;
   public Texture2D marker_texture;
   public Texture2D front_depth_texture;
   public Texture2D back_depth_texture;


   public Shader front_depth_shader;
   public Shader back_depth_shader;

   void Awake() {
      front_depth_shader = Shader.Find("Unlit/DepthForward");
      back_depth_shader = Shader.Find("Unlit/DepthBackward");

      other_models = FindObjectsOfType<ModelHandle>().Where(x => x != model).ToArray();
   }

   ModelHandle[] other_models = Array.Empty<ModelHandle>();


   public bool exporting;

   [Header("Shading")] public bool shade_bottom;
   [Range(0, 1)]
   public float shade_bottom_start;
   [Range(0, 1)]
   public float shade_bottom_end;

   [Range(0, 1)]
   public float shade_bottom_mag;


   [Range(0, 3)]
   public float relative_model_height_for_shading = 1;

   public Action GetMoveBackFunk() {

      var neg = model.negate_root_motion;
      model.negate_root_motion = false;
      var pos_before_root = model.transform.localPosition;
      var rot_before = model.transform.localRotation;
      return () => {
         model.negate_root_motion = neg;
         model.transform.localPosition = pos_before_root;
         model.transform.localRotation = rot_before;
      }; 
   }
   
   public void HandleLoopingRootMotion(AnimationClip clip, float t) {

      model.SetAnimationNow_Float(clip, 0);

      var pstart = model.render_obj.transform.position;
      
      model.SetAnimationNow_Float(clip, 1);
      
      var pend = model.render_obj.transform.position;

      var diff = pstart - pend;

      var dt = diff * t;
      
      // Debug.Log($"Diff: {clip.name} - {dt}  ({diff})");
      
      var pos_before_root = model.transform.position;

      model.transform.position = pos_before_root + dt;
   }

   public void ResetPos() {
      
   }

   Texture2D MakeTex() {
      var r = new Texture2D(size, size);
      r.filterMode = FilterMode.Point;
      return r;
   }

   public void InitTextures() {
      if (result_rexture) return;
      result_rexture = MakeTex();
      basic_shading_texture = MakeTex();
      marker_texture = MakeTex();
      front_depth_texture = MakeTex();
      back_depth_texture = MakeTex();

      display_handle.DisplayTex(result_rexture);
   }

   public ExportPipeline.TimeBenchmark time_benchmark;

   public void RunPipeline() {
      InitTextures();
      foreach (var om in other_models) {
         om.SetActive(false);
      }
      time_benchmark?.Begin();

      if (outline_parts) {
         camera_handle.CaptureTo(model.render_obj.transform, marker_texture);  
      }
      if (outline_depth) {
         camera_handle.CaptureTo(front_depth_texture, front_depth_shader);
         camera_handle.CaptureTo(back_depth_texture, back_depth_shader);
      }
     
      camera_handle.CaptureTo(basic_shading_texture);
      
      time_benchmark?.Lap("Unity Render");
      
      result_rexture.SetPixels(basic_shading_texture.GetPixels());
      result_rexture.Apply();

      if (shade_bottom) {
         ShadeBottom(result_rexture);
         result_rexture.Apply();
      }

      AddBlackOutline(result_rexture, marker_texture);
      
      result_rexture.Apply();
      
      
      
      time_benchmark?.Lap("Black Outline");
      foreach (var om in other_models) {
         om.SetActive(true);
      }
   }

   float MapShadingHeight(float h) {

      var x = h - 0.25f;
      if (x < 0) return x;

      return (x * relative_model_height_for_shading + 0.25f);

   }

   public void ShadeBottom(Texture2D t) {
      
      var px = t.GetPixels();

      var be = MapShadingHeight(Mathf.Min(shade_bottom_end, shade_bottom_start));

      float he = be * t.height;
      float hs = MapShadingHeight(shade_bottom_start) * t.height * relative_model_height_for_shading;
      
      for (int i = 0; i < px.Length; i++) {


         int h = i / t.height;
         
         if (h > hs) break;
         
         
         
         
         
         var sh = h - he;
         float a = 1;
         if (sh > 0) a -= sh / (hs - he);

         if (px[i].a != 0) px[i] = Color.Lerp(px[i], Color.black, a * this.shade_bottom_mag);

      }
      
      t.SetPixels(px);
      
   }

   public ComputeShader shader;
   
   

   public bool try_shader_outlining;
   void AddBlackOutlineGOU(Texture2D result, Texture2D marker) {
      RenderTexture render_tex;
      int kernelHandle = shader.FindKernel("CSMain");
      {
         render_tex = new RenderTexture(marker.width, marker.height, 24);
         
         render_tex.enableRandomWrite = true;
         render_tex.Create();
      }
      
      float depth_to_z = camera_handle.TotalDepth();
      float dm = 0.024f;
      float depth_margin = dm / depth_to_z;
      
      shader.SetFloat("depth_margin", depth_margin);

      shader.SetTexture(kernelHandle, "Result", render_tex);
      shader.SetTexture(kernelHandle, "ImageInput", result);
      shader.SetTexture(kernelHandle, "ImageMarker", marker);
      shader.SetTexture(kernelHandle, "front_depth_texture", back_depth_texture );
      shader.SetTexture(kernelHandle, "back_depth_texture",front_depth_texture);
      
      
      shader.Dispatch(kernelHandle, marker.width/8 , marker.height / 8, 1);

      RenderTexture.active = render_tex;
      result.ReadPixels(new Rect(0, 0, render_tex.width, render_tex.height), 0, 0);
      result.Apply();
      
   }
   
   void AddBlackOutline(Texture2D t, Texture2D marker) {
      if (try_shader_outlining) {
         // Debug.Log($"Experimental shader outlining");
         AddBlackOutlineGOU(t, marker);
         return;
      }
      var px = t.GetPixels();
      var px_c = marker.GetPixels();

      float depth_to_z = camera_handle.TotalDepth();

      float dm = 0.024f;
      double depth_margin = dm / depth_to_z;
      static float DepthFromCol(Color c) {
         return c.g + c.b * (1f / 256);
      }


      var depth_fr = front_depth_texture.GetPixels().map(DepthFromCol);
      var depth_b = back_depth_texture.GetPixels().map(DepthFromCol);

      bool reverse_depth = true;
      if (reverse_depth) {
         (depth_fr, depth_b) = (depth_b, depth_fr);
      }

      float d_max = 0;
      float d_min = 0;

      bool DiffArea(int i, int j) {
         if (px_c[i] != px_c[j]) {
            return true;
         }

         double di_f = depth_fr[i];
         double di_b = depth_b[i];

         double dj_f = depth_fr[j];
         double dj_b = depth_b[j];



         if (di_f > dj_b +depth_margin) {
            // Debug.Log($"{di_f}, {di_b}");
            return true;
         }
         if (dj_f > di_b + depth_margin) {
            return true;
         }

         return false;
      }

      for (int i = 1; i + 1 < px.Length; i++) {
         if (DiffArea(i, i - 1)) {
            px[i - 1] = Color.black;
         }
      }

      for (int i = t.width; i + t.width < px.Length; i++) {
         if (DiffArea(i, i - t.width)) {
            px[i - t.width] = Color.black;
         }
      }

      t.SetPixels(px);
   }

   // Start is called before the first frame update
   void Start() {
      if (exporting) return;
      InitTextures();

      RunPipeline();

   }

   // Update is called once per frame
   void Update() {
      if (exporting) return;
      RunPipeline();
   }
}