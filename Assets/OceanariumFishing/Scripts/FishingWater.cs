using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class FishingWater:MonoBehaviour
    {
        public Material ocean;public float baseHeight=.05f;
        public float Height(Vector3 p,float time)
        {float h=ocean?ocean.GetFloat("_WaveHeight"):.09f,s=ocean?ocean.GetFloat("_WaveScale"):.55f,t=time*(ocean?ocean.GetFloat("_WaveSpeed"):.6f);return baseHeight+(Mathf.Sin(p.x*s+t)+Mathf.Sin(p.z*s*.73f+t*1.2f))*.5f*h;}
        public bool Valid(Vector3 p)
        {
            if(FishingZone.At(p)==null)return false;
            float water=Height(p,Time.time);foreach(var hit in Physics.RaycastAll(p+Vector3.up*30,Vector3.down,60,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.CompareTag("Player") && hit.point.y>water-.05f)return false;
            return true;
        }
    }
}
