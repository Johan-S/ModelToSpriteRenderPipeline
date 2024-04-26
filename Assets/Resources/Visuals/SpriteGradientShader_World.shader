Shader "Nightfall/Sprite Gradient WorldSpace"
{
   Properties
   {
      [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
      _Color ("Tint", Color) = (1,1,1,1)


      _w_Gradient_Start ("w_Gradient_Start", Vector) = (0, 0, 0, 0)
      _w_Gradient_Vector ("w_Gradient_Vector", Vector) = (0, 1, 0, 1)
      _w_Gradient_Start_C ("w_Gradient_Start_Color", Color) = (1,1,1,1)
      _w_Gradient_End_C ("w_Gradient_End_Color", Color) = (0,0,0,1)

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
         #pragma vertex SpriteVert_Custom
         #pragma fragment SpriteFrag_Custom
         #pragma target 2.0
         #pragma multi_compile_instancing
         #pragma multi_compile_local _ PIXELSNAP_ON
         #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
         #include "UnitySprites.cginc"
         struct v2f_custom {
            float4 vertex : SV_POSITION;
            fixed4 color : COLOR;
            float2 texcoord : TEXCOORD0;
            float3 vpos : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
         };


         v2f_custom SpriteVert_Custom(appdata_t IN) {
            v2f_custom OUT;

            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

            OUT.vpos = mul(unity_ObjectToWorld, IN.vertex);
            OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
            OUT.vertex = UnityObjectToClipPos(OUT.vertex);
            OUT.texcoord = IN.texcoord;
            OUT.color = IN.color * _Color * _RendererColor;

            #ifdef PIXELSNAP_ON
          OUT.vertex = UnityPixelSnap (OUT.vertex);
            #endif

            return OUT;
         }


         fixed4 _w_Gradient_Start_C;
         fixed4 _w_Gradient_End_C;
         float4 _w_Gradient_Start;
         float4 _w_Gradient_Vector;

         fixed4 SpriteFrag_Custom(v2f_custom IN) : SV_Target {
            fixed4 gc = fixed4(1, 1, 1, 1);
            {
               float mag = _w_Gradient_Vector.w + (_w_Gradient_Vector.w == 0);
               float3 gv = _w_Gradient_Vector.xyz * mag;
               float x_cur = dot(IN.vpos - _w_Gradient_Start, gv) / dot(
                  gv, gv);

               x_cur = clamp(x_cur, 0, 1);


               gc *= lerp(_w_Gradient_Start_C, _w_Gradient_End_C, x_cur);
            }


            fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color * gc;
            c.rgb *= c.a;
            return c;
         }
         ENDCG
      }
   }
}