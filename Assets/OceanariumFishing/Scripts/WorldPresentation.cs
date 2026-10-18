using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OceanariumFishing
{
    // Scene-local render resources driven by one session clock; no simulation RNG here.
    public sealed class WorldPresentation:MonoBehaviour
    {
        Light sun;Material sky,ocean;ParticleSystem precipitation;AudioSource ambience;Camera cameraView;
        float rain,wind,clouds,wetness,nextFlash;Color foliage=Color.white;int scene=-1;
        readonly List<Material> owned=new();
        void Start(){ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.volume=.18f;}
        void Bind()
        {
            scene=SceneManager.GetActiveScene().handle;sun=null;foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(RenderSettings.skybox){sky=new Material(RenderSettings.skybox);owned.Add(sky);RenderSettings.skybox=sky;}
            var water=FindFirstObjectByType<FishingWater>();ocean=null;
            if(water&&water.ocean){var original=water.ocean;ocean=new Material(original);owned.Add(ocean);water.ocean=ocean;foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.sharedMaterial==original)r.sharedMaterial=ocean;}
            cameraView=Camera.main;if(precipitation)Destroy(precipitation.gameObject);
            var go=new GameObject("Camera-local weather");precipitation=go.AddComponent<ParticleSystem>();precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=precipitation.main;main.maxParticles=900;main.startLifetime=1.6f;main.startSpeed=0;main.startSize=.035f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=precipitation.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(18,1,18);
            var velocity=precipitation.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=-14;velocity.x=1;
            var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetColor("_BaseColor",new Color(.65f,.83f,.9f,.65f));owned.Add(material);go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            var collision=precipitation.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.lifetimeLoss=1;collision.quality=ParticleSystemCollisionQuality.Low;
            precipitation.Play();
        }
        void LateUpdate()
        {
            var service=WorldTimeService.Current;if(service?.Simulation==null)return;var sim=service.Simulation;
            if(scene!=SceneManager.GetActiveScene().handle)Bind();if(!cameraView)cameraView=Camera.main;if(!sun||!sim.Current)return;
            var a=sim.Previous;var b=sim.Current;float blend=sim.State.forcedUntil>sim.State.minute?1:sim.Blend;
            float smooth=1-Mathf.Exp(-Time.deltaTime/3);rain=Mathf.Lerp(rain,Mathf.Lerp(a.rain,b.rain,blend),smooth);wind=Mathf.Lerp(wind,Mathf.Lerp(a.wind,b.wind,blend),smooth);clouds=Mathf.Lerp(clouds,Mathf.Lerp(a.clouds,b.clouds,blend),smooth);wetness=Mathf.MoveTowards(wetness,rain,Time.deltaTime*.04f);
            float solar=Mathf.Sin((sim.Hour-6)/24*Mathf.PI*2);float daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.15f,.45f,solar));float golden=Mathf.Exp(-solar*solar*30);
            sun.transform.rotation=Quaternion.Euler(sim.Hour*15-90+(solar<-.12f?180:0),-32,0);sun.intensity=Mathf.Lerp(.28f,1.25f,daylight)*Mathf.Lerp(1,.6f,clouds);sun.color=Color.Lerp(new Color(.55f,.68f,1),Color.Lerp(new Color(1,.96f,.85f),new Color(1,.61f,.36f),golden),daylight);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
            var horizon=Color.Lerp(new Color(.075f,.12f,.23f),new Color(.64f,.82f,.87f),daylight);horizon=Color.Lerp(horizon,new Color(.91f,.49f,.44f),golden*.7f);horizon=Color.Lerp(horizon,new Color(.35f,.42f,.49f),clouds*.65f);
            var zenith=Color.Lerp(new Color(.014f,.025f,.08f),new Color(.16f,.45f,.78f),daylight);zenith=Color.Lerp(zenith,new Color(.2f,.27f,.35f),clouds*.7f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=Color.Lerp(new Color(.17f,.23f,.36f),new Color(.55f,.66f,.7f),daylight);RenderSettings.ambientEquatorColor=RenderSettings.ambientSkyColor*.8f;RenderSettings.ambientGroundColor=RenderSettings.ambientSkyColor*.55f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=Mathf.Lerp(a.fog,b.fog,blend);RenderSettings.fogColor=horizon;
            if(sky){sky.SetColor("_Zenith",zenith);sky.SetColor("_Horizon",horizon);sky.SetColor("_Cloud",Color.Lerp(new Color(.13f,.18f,.27f),new Color(.9f,.9f,.86f),daylight));}
            Shader.SetGlobalFloat("_OFNight",1-daylight);Shader.SetGlobalFloat("_OFCloud",clouds);Shader.SetGlobalFloat("_OFWind",wind);Shader.SetGlobalFloat("_OFWetness",wetness);
            Shader.SetGlobalVector("_OFMoonDirection",Quaternion.Euler(sim.Hour*15+90,-32,0)*Vector3.back);
            if(sim.Settings.seasons?.Length==4){foliage=Color.Lerp(foliage,sim.Settings.seasons[sim.Month].foliage,smooth*.15f);Shader.SetGlobalColor("_OFFoliage",foliage);}
            float snow=b.snow*sim.Settings.tropicalSnowMultiplier;Shader.SetGlobalFloat("_OFSnow",snow);
            if(ocean){ocean.SetColor("_Shallow",Color.Lerp(new Color(.035f,.16f,.23f),new Color(.14f,.58f,.53f),daylight));ocean.SetColor("_Deep",Color.Lerp(new Color(.018f,.055f,.14f),new Color(.035f,.24f,.34f),daylight));ocean.SetColor("_Foam",Color.Lerp(new Color(.26f,.38f,.5f),new Color(.8f,.9f,.83f),daylight));ocean.SetFloat("_WaveHeight",.09f*Mathf.Lerp(a.waves,b.waves,blend));ocean.SetFloat("_WaveSpeed",.6f+wind*.35f);ocean.SetFloat("_Reflection",.35f-clouds*.12f);}
            if(cameraView&&precipitation){precipitation.transform.position=cameraView.transform.position+Vector3.up*9;bool roof=Physics.Raycast(cameraView.transform.position,Vector3.up,12,~0,QueryTriggerInteraction.Ignore);var emission=precipitation.emission;emission.rateOverTime=roof?0:rain*400+snow*100;var velocity=precipitation.velocityOverLifetime;velocity.x=wind*3;velocity.y=b.snow>0?-2:-14;var main=precipitation.main;main.startSize=b.snow>0?.08f:.025f;main.startLifetime=b.snow>0?5:1.6f;}
            if(ambience&&ambience.clip!=b.ambient){ambience.clip=b.ambient;if(b.ambient)ambience.Play();else ambience.Stop();}
            if(b.id=="thunderstorm"&&sim.Settings.lightning&&Time.time>nextFlash){nextFlash=Time.time+43;sun.intensity+=.25f;if(b.thunder)ambience.PlayOneShot(b.thunder,.25f);}
        }
        void OnDestroy(){if(precipitation)Destroy(precipitation.gameObject);foreach(var m in owned)if(m)Destroy(m);Shader.SetGlobalFloat("_OFNight",0);Shader.SetGlobalFloat("_OFWind",0);Shader.SetGlobalFloat("_OFWetness",0);Shader.SetGlobalColor("_OFFoliage",Color.white);}
    }
}
