Shader "Nightfall/Sprite Gradient Shader"
{
   Properties
   {
      [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
      _Color ("Tint", Color) = (1,1,1,1)

      _Gradient_Start ("Gradient_Start", Vector) = (0, 0, 0, 0)
      _Gradient_Vector ("Gradient_Vector", Vector) = (0, 1, 0, 0)
      _Gradient_Start_C ("Gradient_Start_Color", Color) = (1,1,1,1)
      _Gradient_End_C ("Gradient_End_Color", Color) = (0,0,0,1)

      [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
      [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
      [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
      [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
      [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
   }

   SubShader
   {
      Tags
      {
         "Queue"="Transparent"
         "IgnoreProjector"="True"
         "RenderType"="Transparent"
         "PreviewType"="Plane"
         "CanUseSpriteAtlas"="True"
      }

      Cull Off
      Lighting Off
      ZWrite Off
      Blend One OneMinusSrcAlpha

      Pass
      {
         CGPROGRAM
         #pragma vertex SpriteVert
         #pragma fragment SpriteFrag_Custom
         #pragma target 2.0
         #pragma multi_compile_instancing
         #pragma multi_compile_local _ PIXELSNAP_ON
         #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
         #include "UnitySprites.cginc"

         fixed4 _Gradient_Start_C;
         fixed4 _Gradient_End_C;
         float4 _Gradient_Start;
         float4 _Gradient_Vector;
         fixed4 SpriteFrag_Custom(v2f IN) : SV_Target {
            float mag = _Gradient_Vector.w + (_Gradient_Vector.w == 0);
            float3 gv = _Gradient_Vector.xyz * mag;

            float x_cur = dot(IN.texcoord - _Gradient_Start, gv) / dot(gv, gv);

            x_cur = clamp(x_cur, 0, 1);


            fixed4 gc = lerp(_Gradient_Start_C, _Gradient_End_C, x_cur);


            fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color * gc;
            c.rgb *= c.a;
            return c;
         }
         ENDCG
      }
   }
}