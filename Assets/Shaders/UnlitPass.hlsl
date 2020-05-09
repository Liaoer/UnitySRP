﻿

#include "../ShaderLibrary/Common.hlsl"

float4 _BaseColor;

float4 UnlitPassVertex (float3 positionOS : POSITION) : SV_POSITION {
    float3 positionWS = TransformObjectToWorld(positionOS.xyz);
	return TransformWorldToHClip(positionWS);
}

float4 UnlitPassFragment () : SV_TARGET {
    //return float4(1.0, 1.0, 0.0, 1.0);
    return _BaseColor;
}