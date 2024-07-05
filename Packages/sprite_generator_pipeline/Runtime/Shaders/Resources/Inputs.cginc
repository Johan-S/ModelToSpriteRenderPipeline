#include "UnityCG.cginc"

RWTexture2D<float4> Result;

Texture2D<float4> tex_in;
int2 tex_in_size;
RWBuffer<float> result_buffer;

RWBuffer<float> buffer;
RWStructuredBuffer<float2> buffer2;
RWStructuredBuffer<float4> buffer4;

float4 color;

int N;