#include "UnityCG.cginc"

float2 parts(float z, int d) {
   float z1 = (z * d) - floor(z * d);
   return float2(z - z1 / d, z1);
}

const float S_B = (1.0 / (255 * 256));
const float S_G = (1.0 / 255);
fixed4 DepthToCol(float z) {

   float2 p = parts(z, 256);

   float2 p2 = parts(p.x, 256);
   float z1 = (z * 256) - floor(z * 256);
   return fixed4(p.x, p2.x, p2.y, 1);
}


float DepthFromCol(float4 c) {
   float a = (1.0 / (255 * 255));
   float b = (1.0 / 255);
   return c.r + c.g * S_G + c.b * S_B;
}