Shader "OceanariumFishing/Island Blend"
{
 Properties { _Grass("Meadow",Color)=(.34,.49,.22,1) _Sand("Warm sand",Color)=(.78,.68,.46,1) _Path("Trail",Color)=(.65,.49,.31,1) }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {
 Tags {"LightMode"="UniversalForwardOnly"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Grass,_Sand,_Path;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 c:COLOR;};
 struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 c:COLOR;float fog:TEXCOORD2;};
 V Vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.c=i.c;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 Frag(V i):SV_Target{
 Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));
 float grain=sin(i.w.x*2.7+sin(i.w.z*3.1))*sin(i.w.z*2.3)*.018+sin(i.w.x*.41+i.w.z*.32)*.025;
 half3 base=lerp(_Sand.rgb,_Grass.rgb,saturate(i.c.r));base=lerp(base,_Path.rgb,i.c.g*.88);base+=grain;
 float3 n=normalize(i.n);half3 light=SampleSH(n)*.8+sun.color*(saturate(dot(n,sun.direction))*.75+.12)*lerp(.42,1,sun.shadowAttenuation);
 return half4(MixFog(base*light,i.fog),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}


