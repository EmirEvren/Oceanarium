Shader "OceanariumFishing/Coastal Grass"
{
 Properties { _BaseColor("Foliage tint",Color)=(.35,.52,.26,1) _Wind("Wind strength",Range(0,.2))=.055 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Pass {
 Tags {"LightMode"="UniversalForwardOnly"} Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;float _Wind;
 CBUFFER_END
 float _OFWind;float4 _OFFoliage;
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;float3 w:TEXCOORD2;float3 n:TEXCOORD3;};
 V Vert(A i){V o;float3 p=TransformObjectToWorld(i.p.xyz);p.x+=sin(_Time.y*1.3+p.x*.5+p.z)*_Wind*(1+_OFWind*2)*i.uv.y;o.w=p;o.n=TransformObjectToWorldNormal(i.n);o.p=TransformWorldToHClip(p);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 Frag(V i):SV_Target{Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));float shade=.68+.32*abs(dot(normalize(i.n),sun.direction));half3 c=_BaseColor.rgb*lerp(float3(1,1,1),_OFFoliage.rgb,step(.01,_OFFoliage.a))*lerp(.82,1.12,i.uv.y)*shade*(SampleSH(float3(0,1,0))*.75+sun.color*.5)*lerp(.62,1,sun.shadowAttenuation);return half4(MixFog(c,i.fog),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
