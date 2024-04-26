Shader "Custom/CellSurfaceShader"
{
   Properties
   {
      _Color ("Color", Color) = (1,1,1,1)
      _CellSteps ("CellSteps", Range(1, 6)) = 3
      
      _BrightMargin("BrightMargin", Range(0, 1)) = 0
   }
   SubShader
   {
      Tags
      {
         "RenderType"="Opaque"
      }
      LOD 200

      CGPROGRAM

      int _CellSteps;
      float _BrightMargin;
      fixed4 _Color;
      
      // Physically based Standard lighting model, and enable shadows on all light types
      #pragma surface surf SimpleLambert

      #pragma target 3.0

      struct Input {
         float2 uv_MainTex;
      };


      // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
      // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
      // #pragma instancing_options assumeuniformscaling
      UNITY_INSTANCING_BUFFER_START(Props)
      // put more per-instance properties here
      UNITY_INSTANCING_BUFFER_END(Props)

      void surf(Input IN, inout SurfaceOutput o) {
         o.Albedo = 0;
      }
      half4 LightingSimpleLambert(SurfaceOutput s, half3 lightDir, half atten) {
         float size = 2 + _BrightMargin;
         half NdotL = (dot(s.Normal, lightDir) + 1+ _BrightMargin) / size;

         float steps = _CellSteps;
         
         int step = floor(NdotL * steps + 0.99);
         if (step < 1) step = 1;
         if (step > _CellSteps) step = _CellSteps;
         half cell_step = step / steps;
         

         half4 c;
         c.rgb = _Color * _LightColor0.rgb * (cell_step * atten);

         
         c.a = 1;
         return c;
      }
      ENDCG
   }
   FallBack Off
}