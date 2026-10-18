using UnityEngine;
namespace OceanariumFishing
{
    public sealed class CaptainMotion:MonoBehaviour
    {
        public AppearanceData style;public BoatHarbor harbor;CharacterAppearance appearance;Vector3 previous;float step;
        public float scheduleStart=6,scheduleEnd=22;
        void Start(){appearance=GetComponent<CharacterAppearance>();if(style!=null)appearance.Apply(style);previous=transform.position;}
        void LateUpdate(){if(appearance?.Bones==null)return;float speed=Mathf.Clamp01(Vector3.Distance(previous,transform.position)/Mathf.Max(.001f,Time.deltaTime));previous=transform.position;step+=Time.deltaTime*6;float swing=Mathf.Sin(step)*speed*20;appearance.Bones[12].localRotation=Quaternion.Euler(swing,0,0);appearance.Bones[15].localRotation=Quaternion.Euler(-swing,0,0);appearance.Bones[9].localRotation=Quaternion.Euler(harbor&&harbor.Busy?-75:Mathf.Sin(Time.time)*3,0,harbor&&harbor.Busy?Mathf.Sin(Time.time*5)*15:4);}
    }
}

