Shader "OceanariumFishing/Tropical Sky"
{
 Properties { _Zenith("Blue sky",Color)=(.17,.46,.83,1) _Horizon("Horizon",Color)=(.62,.82,.88,1) _Cloud("Cloud",Color)=(.95,.94,.87,1) }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
 Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Zenith,_Horizon,_Cloud;
 CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 dir:TEXCOORD0;};
 V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.dir=i.p.xyz;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);}
 half4 Frag(V i):SV_Target{float3 d=normalize(i.dir);float y=saturate(d.y);half3 c=lerp(_Horizon.rgb,_Zenith.rgb,pow(y,.45));float2 p=float2(atan2(d.z,d.x)*3,asin(clamp(d.y,-1,1))*7);float n=noise(p)*.65+noise(p*2.1)*.25+noise(p*4.3)*.1;float clouds=smoothstep(.61,.76,n)*smoothstep(.04,.16,y)*(1-smoothstep(.65,.95,y));c=lerp(c,_Cloud.rgb,clouds*.9);Light sun=GetMainLight();float disc=pow(saturate(dot(d,sun.direction)),1300);c+=sun.color*disc*.65;return half4(c,1);}
 ENDHLSL }
 }
}

