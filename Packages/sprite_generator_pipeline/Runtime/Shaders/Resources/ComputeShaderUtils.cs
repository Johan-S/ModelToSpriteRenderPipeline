using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public static class ComputeShaderUtils {
   static Dictionary<(int, int), RenderTexture> render_textures = new();

   [RuntimeInitializeOnLoadMethod]
   static void Onl() {
      render_textures = new();
   }

   public static RenderTexture GetCachedRenderTextureFor(this Texture2D texture) {
      int2 size = new int2(texture.width.UpperDiv(8), texture.height.UpperDiv(8)) * 8;

      if (!render_textures.TryGetValue(size, out var render_texture) || !render_texture) {
         render_texture = GetRenderTextureFor(texture);
         render_textures[size] = render_texture;
      }

      return render_texture;
   }

   public static RenderTexture GetRenderTextureFor(this Texture2D texture, int bits=32) {
      int2 size = new int2(texture.width.UpperDiv(8), texture.height.UpperDiv(8)) * 8;

      var render_texture = new RenderTexture(size.width, size.height, bits);
      render_texture.enableRandomWrite = true;

      return render_texture;
   }

   static ComputeShader _shader;

   static ComputeShader shader {
      get {
         if (!_shader) {
            _shader = Resources.Load<ComputeShader>("ComputeShaderUtils");
         }

         return _shader;
      }
   }

   public static void Clear(this RenderTexture tex, Color color) {
      RenderTexture.active = tex;
      GL.Clear (true, true, color);
      RenderTexture.active = null;
   }

   public static void Clear(this Texture2D tex, Color color) {
      var rt = GetCachedRenderTextureFor(tex);
      
      rt.Clear(color);
      RenderTexture.active = rt;
      tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
      RenderTexture.active = null;
   }

   public static void CopyAndDownsampleTo(RenderTexture src, RenderTexture rt, bool mirror) {
      int kernelHandle = shader.FindKernel("CopyAndDownsampleTo");

      shader.SetTexture(kernelHandle, "Result", rt);
      shader.SetTexture(kernelHandle, "tex_in", src);
      shader.SetInt("N", src.width.UpperDiv(rt.width));
      shader.Dispatch(kernelHandle, rt.width / 8, rt.height / 8, 1);

      if (mirror) rt.MirrorInplace();
   }

   public static void MirrorInplace(this RenderTexture rt) {
      int kernelHandle = shader.FindKernel("MirrorInplace");
      shader.SetTexture(kernelHandle, "Result", rt);
      shader.SetInt("N", rt.width);
      // Only dispatch for half width.
      shader.Dispatch(kernelHandle, rt.width / 16, rt.height / 8, 1);
   }

   static readonly ProfilerMarker _m_ReadPixelsFrom = Std.Profiler("ComputeShaderUtils", "ReadPixelsFrom");
   public static void ReadPixelsFrom(this Texture2D t, RenderTexture rt, RectInt rect) {
      using var _m = _m_ReadPixelsFrom.Auto();
      RenderTexture.active = rt;
      t.ReadPixels(new Rect(new(), rect.size), rect.x, rect.y);
      RenderTexture.active = null;
   }

   public static void ReadPixelsFrom(this Texture2D t, RenderTexture rt) =>
      ReadPixelsFrom(t, rt, new (0, 0, rt.width, rt.height));

   public static void CopyToRenderTexture(this Texture2D t, RenderTexture rt) =>
      ReadPixelsFrom(t, rt, new (0, 0, rt.width, rt.height));
}