Shader "Circus/ArenaWarning"
{
    Properties {
        _BaseColor("Danger",Color)=(1,.15,.1,.2)
        _CenterRadius("Center XZ / Radius",Vector)=(0,0,14,0)
        _SafeA("Safe A XZ",Vector)=(0,0,0,0)
        _SafeB("Safe B XZ",Vector)=(0,0,0,0)
        _SafeRadius("Safe Radius",Float)=3
    }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float4 _CenterRadius,_SafeA,_SafeB;float _SafeRadius;
            CBUFFER_END
            Varyings vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;}
            half4 frag(Varyings i):SV_Target {
                float2 p=i.positionWS.xz;
                clip(_CenterRadius.z-distance(p,_CenterRadius.xy));
                clip(distance(p,_SafeA.xy)-_SafeRadius);
                clip(distance(p,_SafeB.xy)-_SafeRadius);
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
