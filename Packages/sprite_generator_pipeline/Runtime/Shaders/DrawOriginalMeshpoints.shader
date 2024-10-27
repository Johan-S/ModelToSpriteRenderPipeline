Shader "Unlit/DrawOriginalMeshpoints"
{
   Properties
   {
      _MainTex ("Texture", 2D) = "white" {}
   }
   SubShader
   {
      Tags
      {
         "RenderType"="Opaque"
      }
      LOD 100

      Pass
      {
         Cull Off
         ColorMask RGBA
         Blend One Zero
         
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
         // make fog work
         #pragma multi_compile_fog

         #include "UnityCG.cginc"
         #include "KlockanShader.cginc"

         struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
            
            float2 uv4 : TEXCOORD4;
            float2 uv5 : TEXCOORD5;
         };

         struct v2f {
            float2 uv : TEXCOORD0;
            
            float2 uv4 : TEXCOORD4;
            float2 uv5 : TEXCOORD5;
            
            UNITY_FOG_COORDS(1)
            float4 vertex : SV_POSITION;
         };

         sampler2D _MainTex;
         float4 _MainTex_ST;

         v2f vert(appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            o.uv4 = v.uv4;
            o.uv5 = v.uv5;
            UNITY_TRANSFER_FOG(o, o.vertex);
            return o;
         }

         float4 frag(v2f i) : SV_Target {
            // sample the texture
            fixed4 col = tex2D(_MainTex, i.uv);
            // apply fog
            float z = i.vertex.z;
            return float4(i.uv4.xy, i.uv5.x, z);
         }
         ENDCG
      }
   }
}