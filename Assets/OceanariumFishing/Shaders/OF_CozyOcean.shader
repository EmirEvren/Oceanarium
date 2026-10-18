Shader "OceanariumFishing/Cozy Ocean"
{
 Properties {
 _Shallow("Shallow turquoise",Color)=(.14,.58,.53,1) _Deep("Deep ocean",Color)=(.035,.24,.34,1) _Foam("Foam",Color)=(.8,.9,.83,1)
 _WaveHeight("Wave height",Range(0,.5))=.09 _WaveSpeed("Wave speed",Range(0,3))=.6 _WaveScale("Wave scale",Range(.05,2))=.55
 _FoamWidth("Contact foam width",Range(.1,3))=.65 _Reflection("Sky reflection",Range(0,1))=.35 _SunGlint("Sun or moon glint",Range(0,2))=.65
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+20"}
 Pass {
 Name "OceanForward" Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Shallow,_Deep,_Foam;float _WaveHeight,_WaveSpeed,_WaveScale,_FoamWidth,_Reflection,_SunGlint;
 CBUFFER_END
 float _OFNight;
 struct A {float4 p:POSITION;float4 c:COLOR;};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float4 c:COLOR;float fog:TEXCOORD1;float4 screen:TEXCOORD2;};
 V Vert(A i){V o;float3 p=TransformObjectToWorld(i.p.xyz);float t=_Time.y*_WaveSpeed;
 p.y+=(sin(p.x*_WaveScale+t)+sin(p.z*_WaveScale*.73+t*1.2))*.5*_WaveHeight;
 o.w=p;o.p=TransformWorldToHClip(p);o.screen=ComputeScreenPos(o.p);o.c=i.c;o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 Frag(V i):SV_Target {
 float t=_Time.y*_WaveSpeed;
 float3 n=normalize(float3(-cos(i.w.x*_WaveScale+t)*_WaveHeight*_WaveScale*.5,1,-cos(i.w.z*_WaveScale*.73+t*1.2)*_WaveHeight*_WaveScale*.365));
 n=normalize(n+float3(sin(i.w.z*2.2+t)*.014,0,cos(i.w.x*1.8-t)*.014));
 float3 v=SafeNormalize(GetWorldSpaceViewDir(i.w));Light sun=GetMainLight();
 float depth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);float waterDepth=-TransformWorldToView(i.w).z;
 float contact=max(0,depth-waterDepth);float shallow=saturate(i.c.r*.8+exp(-contact*.35)*.2);
 half3 color=lerp(_Deep.rgb,_Shallow.rgb,shallow);
 // Two smoothly warped interference fields suggest sunlight on a shallow sandy bottom.
 float2 q=i.w.xz*.95+float2(sin(i.w.z*.7+t*.36),cos(i.w.x*.6-t*.31))*.28;
 float a=sin(q.x+sin(q.y*1.14+t*.17));float b=sin(q.y+cos(q.x*1.07-t*.14));
 float caustic=pow(saturate(1-abs(a+b)*2.4),14);
 float beachLight=pow(saturate(i.c.r),5);
 color+=_Shallow.rgb*caustic*.16*beachLight;
 float fresnel=.12+.72*pow(1-saturate(dot(n,v)),4);
 half3 sky=GlossyEnvironmentReflection(reflect(-v,n),.2,1);sky=lerp(max(sky,half3(.28,.57,.74)),half3(.05,.1,.21),_OFNight);
 color=lerp(color,sky,saturate(fresnel*_Reflection));
 float glint=pow(saturate(dot(n,SafeNormalize(sun.direction+v))),85);
 color+=sun.color*glint*_SunGlint*.6;
 float glimmer=pow(saturate(sin(i.w.x*2.8+i.w.z*1.9+t)*sin(i.w.z*3.4-i.w.x*.7-t*.8)),18);color+=sun.color*glimmer*.035;
 float wave=sin(i.w.x*2+i.w.z*1.4-t*2)*.5+.5;
 float foam=(1-smoothstep(.04,_FoamWidth,contact))*lerp(.45,1,smoothstep(.2,.8,wave))*.72;
  float angle=atan2(i.w.z/.872,i.w.x);
 float coast=22+2.3*sin(angle*3+.4)+1.4*sin(angle*5-1)+.7*cos(angle*9);
 float shoreDistance=length(float2(i.w.x,i.w.z/.872))-coast;
 float wash=sin(angle*36-t*1.3)*.13+sin(angle*83+t)*.07;
 float shoreline=(1-smoothstep(.06,_FoamWidth,abs(shoreDistance+.12+wash)))*.78;
 foam=max(foam,shoreline);
 color=lerp(color,_Foam.rgb,foam);return half4(MixFog(color,i.fog),1);
 }
 ENDHLSL
 }
 }
}


