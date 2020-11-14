#ifndef SRP_DEPTH_ONLY_PASS_INCLUDED
#define SRP_DEPTH_ONLY_PASS_INCLUDED

#include "Assets/SRP/Runtime/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
};

struct Varyings
{
    float4 posCS : SV_Position;
};

Varyings vert(Attributes input)
{
    Varyings output;
    output.posCS = UnityObjectToClipPos(input.positionOS);
    return output;
}

void Frag(Varyings input)
{
    
}

#endif