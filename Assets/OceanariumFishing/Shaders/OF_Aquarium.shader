Shader "OceanariumFishing/Aquarium Glass and Water"
{
 Properties { _BaseColor("Tint",Color)=(0.25,0.75,0.78,0.12) _Water("Surface waves",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;float _Water;
   CBUFFER_END
   struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;};
   V Vert(A i){V o;float3 p=i.p.xyz;p.y+=_Water*sin(p.x*4+_Time.y*.7)*cos(p.z*3+_Time.y*.6)*.009;o.w=TransformObjectToWorld(p);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    float3 n=normalize(i.n+_Water*float3(sin(i.w.x*5+_Time.y)*.03,0,cos(i.w.z*4+_Time.y)*.03));
    float3 v=normalize(GetWorldSpaceViewDir(i.w));float fresnel=pow(1-abs(dot(v,n)),3);
    half3 reflection=SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0,samplerunity_SpecCube0,reflect(-v,n),4).rgb;
    float edge=1-smoothstep(0,.055,min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y)));
    half3 color=_BaseColor.rgb+reflection*.18+fresnel*.18+edge*.08;
    return half4(color,saturate(_BaseColor.a+fresnel*.16+edge*.08));
   }
   ENDHLSL
  }
 }
}
