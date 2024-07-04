#include "UnityCG.cginc"

float crunch_to_256(float f) {
   return floor(f * 255) / 255;
}

float4 FloatToCol(float z1r) {
   float z1 = crunch_to_256(z1r);
   float z2r = (z1r - z1) * 256;
   float z2 = crunch_to_256(z2r);
   float z3r = (z2r - z2) * 256;
   float z3 = crunch_to_256(z3r);
   

   return float4(z1, z2, z3, 1);
}

float FloatFromCol(float4 c) {
   return c.r + c.g / 256 + c.b / (256 * 256);
}