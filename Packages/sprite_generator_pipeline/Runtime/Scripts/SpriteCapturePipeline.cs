using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.Mathf;
using FilterMode = UnityEngine.FilterMode;


[DefaultExecutionOrder(10)]
public class SpriteCapturePipeline : MonoBehaviour {
   public interface IBeforeSpriteRender {
      void Handle();
      void Cleanup();
   }

   [Tooltip("Stops the run with an error in case the model clips out of the rendered view..")]
   public bool check_model_clipping;

   [Tooltip("How many extra pixels are rendered and then merged into a single pixel, creating AA effect.")]
   public int export_resolution_downscale = 2;


   [NonSerialized] public int size = 256;
   [Header("Old late")] public bool mirror_output;
   public bool outline_depth = true;
   public bool outline_parts = true;

   [Tooltip("Outline color for the edges against the background, alpha makes it weaker/stronger.")]
   public Color black_outline_color = Color.black;

   [Tooltip("Outline color for the edges between different parts of the model, alpha makes it weaker/stronger.")]
   public Color parts_outline_color_internal = Color.black;

   [Tooltip("Outline color for the edges between different depths of the model, alpha makes it weaker/stronger.")]
   public Color black_outline_color_internal = Color.black;

   [Header("Unity Bindings")] public ModelHandle model;

   public DisplayHandle display_handle;
   public CameraHandle camera_handle;

   [Header("Outputs")] public Texture2D result_rexture;

   public RenderTexture render_result;
   public RenderTexture partial_render_result;

   public Texture2D downsampled_result_rexture;
   public RenderTexture downsampled_render_result;

   public RenderTexture basic_shading_texture;
   public RenderTexture marker_texture;
   public RenderTexture front_depth_texture;
   public RenderTexture back_depth_texture;


   public Shader front_depth_shader;
   public Shader back_depth_shader;

   [Button(false)]
   public void RunNow() {
      if (!front_depth_shader) front_depth_shader = Shader.Find("Unlit/DepthForward");
      if (!back_depth_shader) back_depth_shader = Shader.Find("Unlit/DepthBackward");
      InitTextures(false);
      if (!model.isSet) model.SetActiveModel(true);
      if (model.isActiveAndEnabled) model.UpdateModelAnimationPos(0);
      RunPipeline();
      PushToResultTextures();
   }

   // Start is called before the first frame update
   void Start() {
      if (exporting) return;
      InitTextures(false);

      if (!model || !model.isSet) {
         return;
      }

      RunPipeline();
   }

   void Awake() {
      result_rexture = null;
      front_depth_shader = Shader.Find("Unlit/DepthForward");
      back_depth_shader = Shader.Find("Unlit/DepthBackward");
   }

   ModelHandle[] other_models = Array.Empty<ModelHandle>();


   public bool exporting;

   [Header("Shading")] public bool shade_bottom;
   [Range(0, 1)] public float shade_bottom_start;
   [Range(0, 1)] public float shade_bottom_end;

   [Range(0, 1)] public float shade_bottom_mag;


   [Range(0, 3)] public float relative_model_height_for_shading = 1;

   public Action GetMoveBackFunk() {
      var neg = model.negate_root_motion;
      model.negate_root_motion = false;
      var cv = camera_handle.camera_pivot;
      var pos_before_root = model.transform.localPosition;
      var rot_before = model.transform.localRotation;
      return () => {
         camera_handle.camera_pivot = cv;
         model.negate_root_motion = neg;
         model.transform.localPosition = pos_before_root;
         model.transform.localRotation = rot_before;
      };
   }

   public void HandleLoopingRootMotion(AnimationClip clip, float t) {
      bool root = model.model_root.applyRootMotion;
      try {
         model.model_root.applyRootMotion = true;
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
      finally {
         model.model_root.applyRootMotion = root;
      }
   }

   Texture2D MakeTex(int size) {
      var r = new Texture2D(size, size);
      r.filterMode = FilterMode.Point;
      r.hideFlags = Std.NoSave;
      return r;
   }

   public void InitTextures(bool force) {
      if (result_rexture && !force) return;

      other_models = FindObjectsOfType<ModelHandle>().Where(x => x != model).ToArray();
      // Debug.Log($"Init texture with size {size}");
      result_rexture = MakeTex(size);
      render_result = result_rexture.GetRenderTextureFor();
      partial_render_result = result_rexture.GetRenderTextureFor();
      basic_shading_texture = result_rexture.GetRenderTextureFor();
      marker_texture = result_rexture.GetRenderTextureFor();
      marker_texture.name = "marker_texture";
      front_depth_texture = result_rexture.GetRenderTextureFor(hdr: true);
      back_depth_texture = result_rexture.GetRenderTextureFor(hdr: true);

      ltex = new Texture2D(back_depth_texture.width, back_depth_texture.height, back_depth_texture.graphicsFormat,
         TextureCreationFlags.None);
      ltex.name = "See wtf is there";

      downsampled_result_rexture = MakeTex(size / export_resolution_downscale);
      downsampled_result_rexture.filterMode = FilterMode.Trilinear;
      downsampled_result_rexture.name = "rt + " + EditorApplication.timeSinceStartup;
      downsampled_render_result = downsampled_result_rexture.GetRenderTextureFor();

      if (display_handle) display_handle.DisplayTex(downsampled_result_rexture, result_rexture);
   }

   public TimeBenchmark time_benchmark;

   static readonly ProfilerMarker _m_RunPipeline = Std.Profiler<SpriteCapturePipeline>("RunPipeline");

   public void HdrDepth() {
   }

   public Texture2D ltex;

   public void RunPipelineOnRootTransform(Transform tr) {
      using var _m = _m_RunPipeline.Auto();
      InitTextures(false);

      var handlers = tr.GetComponentsInChildren<IBeforeSpriteRender>();

      foreach (var h in handlers) {
         h.Handle();
      }

      time_benchmark?.Begin();
      if (outline_parts) {
         marker_texture.Clear(Color.white);
         camera_handle.CaptureTo(tr, marker_texture);
      } else {
         marker_texture.Clear(Color.white);
      }

      if (outline_depth) {
         camera_handle.CaptureTo(front_depth_texture, front_depth_shader, new Color(1, 0, 0, 0));
         camera_handle.CaptureTo(back_depth_texture, back_depth_shader, new Color(1, 0, 0, 0));

         void Runlogs() {
            
            ltex.ReadPixelsFrom(back_depth_texture);
            ltex.Apply();
            var cl = ltex.GetPixels();
            var cols = back_depth_texture.ReadPixels_Expensive();
            cols = cl;
            var hs = cols.map(x => x.r).ToSet();
            var st = hs.sorted();
            Debug.Log($"hs {hs.Count}: , {st[1..].enumerate().map(iv => iv.value - st[iv.i]).Min()}\n\n{st.join("\n")}\n");
            Debug.Log($"Max g: {cols.Max(x => x.r)}, {cols.Max(x => x.g)}, {cols.Max(x => x.b)}, {cols.Max(x => x.a)}");
            Debug.Log($"Min g: {cols.Min(x => x.r)}, {cols.Min(x => x.g)}, {cols.Min(x => x.b)}, {cols.Min(x => x.a)}");
         }

         // Runlogs();
      } else {
         front_depth_texture.Clear(Color.black);
         back_depth_texture.Clear(Color.black);
      }

      camera_handle.CaptureTo(basic_shading_texture);

      foreach (var h in handlers.Reversed()) {
         h.Cleanup();
      }


      time_benchmark?.Lap("Unity Render");

      partial_render_result.Clear(default);

      if (shade_bottom) {
         ShadeBottom(basic_shading_texture, partial_render_result, tr);
      } else {
         Graphics.Blit(basic_shading_texture, partial_render_result);
      }

      AddBlackOutline(partial_render_result, marker_texture);

      if (check_model_clipping) {
         CheckModelClipping(partial_render_result);
      }

      ComputeShaderUtils.CopyAndDownsampleTo(render_result, downsampled_render_result, mirror: mirror_output);
      time_benchmark?.Lap("Black Outline");
      foreach (var om in other_models) {
         om.SetActive(true);
      }
   }

   public void RunPipeline() {
      foreach (var om in other_models) {
         om.SetActive(false);
      }

      RunPipelineOnRootTransform(model.render_obj.transform);
   }

   public void CheckModelClipping(RenderTexture rt) {
      var tex = new Texture2D(rt.width, rt.height);

      tex.ReadPixelsFrom(rt);

      tex.Apply();
      var cols = tex.GetPixels();

      for (int i = 0; i < cols.Length; i++) {
         int y = i / rt.width;
         int x = i % rt.width;

         if (y == 0 || y == rt.height - 1 || x == 0 || x == rt.width - 1) {
            var c = cols[i];
            if (c.a != 0 && (c.r != 0 || c.g != 0 || c.b != 0)) {
               Debug.LogError($"Clipping found! {x}, {y}: {c}");
               PushToResultTextures();
               throw new Exception($"Clipping error found!");
            }
         }
      }

      Destroy(tex);
   }

   public void PushToResultTextures() {
      result_rexture.ReadPixelsFrom(render_result);
      result_rexture.Apply();


      downsampled_result_rexture.ReadPixelsFrom(downsampled_render_result);
      downsampled_result_rexture.Apply();
   }

   float MapShadingHeight(float h, Transform tr) {
      var x = h - 0.25f;
      if (x < 0) return x;

      var angle_fac = tr.up.Dot(camera_handle.transform.up);

      return (x * relative_model_height_for_shading * angle_fac + 0.25f);
   }

   public void ShadeBottom(RenderTexture src, RenderTexture dest, Transform tr) {
      var be = MapShadingHeight(Min(shade_bottom_end, shade_bottom_start), tr);

      float he = be * src.height;
      float hs = MapShadingHeight(shade_bottom_start, tr) * src.height * relative_model_height_for_shading;

      int kernelHandle = shader.SetKernel("ShadeBottom");


      shader.SetTexture(kernelHandle, "Result", dest);
      shader.SetTexture(kernelHandle, "ImageInput", src);

      shader.SetFloat("shade_bottom_mag", shade_bottom_mag);
      shader.SetFloat("shade_bottom_hs", hs);
      shader.SetFloat("shade_bottom_he", he);
      shader.SetInt("res_width", dest.width);
      shader.SetInt("res_height", dest.height);

      shader.Dispatch(kernelHandle, dest.width / 8, dest.height / 8, 1);
   }

   public void ShadeBottom(Texture2D t, Transform tr) {
      var px = t.GetPixels();

      var be = MapShadingHeight(Min(shade_bottom_end, shade_bottom_start), tr);

      float he = be * t.height;
      float hs = MapShadingHeight(shade_bottom_start, tr) * t.height * relative_model_height_for_shading;

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


   void AddBlackOutlineGOU(RenderTexture result, RenderTexture marker) {
      RenderTexture render_tex;
      int kernelHandle = shader.SetKernel("CSMain");

      float depth_to_z = camera_handle.TotalDepth();
      float dm = 0.024f;
      float depth_margin = dm / depth_to_z;

      shader.SetFloat("depth_margin", depth_margin);
      shader.SetInt("res_width", result.width);
      shader.SetInt("res_height", result.height);
      shader.SetVector("outlines_color", black_outline_color);
      shader.SetVector("parts_outlines_color", parts_outline_color_internal);
      shader.SetVector("inner_outlines_color", black_outline_color_internal);

      shader.SetTexture(kernelHandle, "Result", render_result);
      shader.SetTexture(kernelHandle, "ImageInput", partial_render_result);
      shader.SetTexture(kernelHandle, "ImageMarker", marker);
      shader.SetTexture(kernelHandle, "front_depth_texture", back_depth_texture);
      shader.SetTexture(kernelHandle, "back_depth_texture", front_depth_texture);

      shader.Dispatch(kernelHandle, marker.width / 8, marker.height / 8, 1);
   }

   float DepthFromCol(Color c) {
      return c.r + (c.g + c.b * (1.0f / 255)) * (1.0f / 255);
   }

   void AddBlackOutline(RenderTexture t, RenderTexture marker) {
      if (try_shader_outlining) {
         // Debug.Log($"Experimental shader outlining");
         AddBlackOutlineGOU(t, marker);

         return;
      }

      Debug.LogError($"Implement new black outline for render texture!");
   }

   void AddBlackOutline_Old(Texture2D t, Texture2D marker) {
      /*
      var px = t.GetPixels();
      var px_c = marker.GetPixels();

      float depth_to_z = camera_handle.TotalDepth();

      float dm = 0.024f;
      double depth_margin = dm / depth_to_z;

      static float DepthFromCol(Color c) {
         return c.g + c.b * (1f / 256);
      }

      float depth_space = DepthFromCol(Color.white);

      var pix_fr = front_depth_texture.GetPixels();

      var pix_b = back_depth_texture.GetPixels();
      var depth_fr = pix_fr.map(DepthFromCol);
      var depth_b = pix_b.map(DepthFromCol);

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


         if (di_f <= dj_f) {
            return false;
         } else {
            if (dj_f > di_b + depth_margin) {
               return false;
            }

            if (di_f > dj_b + depth_margin) {
               // Only check one way.
               return true;
            }
         }


         return false;
      }

      for (int i = 1 + t.width; i + 1 + t.width < px.Length; i++) {
         if (DiffArea(i, i - 1) || DiffArea(i, i + 1) || DiffArea(i, i - t.width) || DiffArea(i, i + t.width)) {
            var c = depth_fr[i] == depth_space ? black_outline_color : black_outline_color_internal;


            float a = c.a;
            c.a = 1;
            px[i] = Color.Lerp(px[i], c, a);
         }
      }

      Debug.Log($"depth f: {depth_fr[0]}, {depth_fr.Max()}, {depth_fr.Min()}");
      Debug.Log($"pix f: {pix_fr[0]}");
      Debug.Log($"depth b: {depth_b[0]}, {depth_b.Max()}, {depth_b.Min()}");

      t.SetPixels(px);
      */
   }

   // Update is called once per frame
   void Update() {
      if (exporting) {
         PushToResultTextures();
         return;
      }

      if (!model || !model.isSet) {
         return;
      }

      RunPipeline();
      PushToResultTextures();
   }

   void LateUpdate() {
   }
}