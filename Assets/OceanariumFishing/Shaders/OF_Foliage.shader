Shader "OceanariumFishing/Coastal Grass"
{
 Properties { _BaseColor("Grass tint",Color)=(0.55,0.68,0.23,1) }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;};
   V Vert(A i)
   {
    V o;float3 p=TransformObjectToWorld(i.positionOS.xyz);
    p.x+=sin(_Time.y*1.3+p.x*.5+p.z)*.035*i.uv.y;
    o.positionCS=TransformWorldToHClip(p);o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
   }
   half4 Frag(V i):SV_Target{return half4(MixFog(_BaseColor.rgb*lerp(.65,1.15,i.uv.y),i.fog),1);}
   ENDHLSL
  }
 }
}
