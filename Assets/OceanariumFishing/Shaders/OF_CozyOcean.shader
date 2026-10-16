Shader "OceanariumFishing/Cozy Ocean"
{
 Properties
 {
  _Shallow("Shallow turquoise",Color)=(0.12,0.68,0.62,1)
  _Deep("Deep ocean",Color)=(0.025,0.23,0.38,1)
  _Foam("Foam",Color)=(0.8,0.94,0.8,1)
  _WaveHeight("Wave height",Range(0,0.5))=0.1
  _WaveSpeed("Wave speed",Range(0,3))=0.6
  _WaveScale("Wave scale",Range(0.05,2))=0.6
  _FoamWidth("Foam width",Range(0.1,3))=0.8
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+20"}
  Pass
  {
   Name "OceanForward"
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Shallow,_Deep,_Foam;
   float _WaveHeight,_WaveSpeed,_WaveScale,_FoamWidth;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float4 color:COLOR;float fog:TEXCOORD1;};
   Varyings Vert(Attributes input)
   {
    Varyings o;float3 p=TransformObjectToWorld(input.positionOS.xyz);
    float t=_Time.y*_WaveSpeed;
    p.y+=(sin(p.x*_WaveScale+t)+sin(p.z*_WaveScale*.73+t*1.2))*.5*_WaveHeight;
    o.world=p;o.positionCS=TransformWorldToHClip(p);o.color=input.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;
   }
   half4 Frag(Varyings i):SV_Target
   {
    float t=_Time.y*_WaveSpeed;
    float3 n=normalize(float3(-cos(i.world.x*_WaveScale+t)*_WaveHeight*_WaveScale*.5,1,-cos(i.world.z*_WaveScale*.73+t*1.2)*_WaveHeight*_WaveScale*.365));
    Light sun=GetMainLight();float3 v=SafeNormalize(GetWorldSpaceViewDir(i.world));
    float spec=pow(saturate(dot(n,SafeNormalize(sun.direction+v))),140);
    float shore=i.color.r;
    float ripple=sin(i.world.x*1.7+i.world.z*1.2+t*1.6)*sin(i.world.z*2.3-i.world.x*.5+t);
    half3 color=lerp(_Deep.rgb,_Shallow.rgb,shore);
    float band=sin(shore*36-t*2+ripple*.5);
    float foam=smoothstep(.7,.95,shore)*smoothstep(.72,.95,band)*.7;
    color=lerp(color,_Foam.rgb,foam*_FoamWidth);
    color+=sun.color*spec*.75+pow(saturate(ripple),9)*.055*shore;
    color=MixFog(color,i.fog);return half4(color,1);
   }
   ENDHLSL
  }
 }
}
