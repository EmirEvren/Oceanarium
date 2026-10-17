using UnityEngine;
namespace OceanariumFishing
{
    public sealed class CharacterMotion : MonoBehaviour
    {
        public LocalPlayerController controller;
        public Transform leftArm,rightArm,leftLeg,rightLeg,head;
        private float phase,blend;
        private Vector3 rest;
        private void Start(){rest=controller.visual.localPosition;}
        private void LateUpdate()
        {
            blend=Mathf.Lerp(blend,Mathf.Clamp01(controller.Speed/3.4f),1-Mathf.Exp(-12*Time.deltaTime));
            float running=Mathf.InverseLerp(3.4f,5.8f,controller.Speed);
            phase+=Time.deltaTime*Mathf.Lerp(8,13,running)*Mathf.Max(.25f,blend);
            float swing=Mathf.Sin(phase)*blend*Mathf.Lerp(27,39,running);
            leftArm.localRotation=Quaternion.Euler(swing,0,5);rightArm.localRotation=Quaternion.Euler(-swing,0,-5);
            leftLeg.localRotation=Quaternion.Euler(-swing,0,0);rightLeg.localRotation=Quaternion.Euler(swing,0,0);
            head.localRotation=Quaternion.Euler(running*3,Mathf.Sin(Time.time*.8f)*2*(1-blend),0);
            controller.visual.localPosition=rest+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.035f*blend+Mathf.Sin(Time.time*1.8f)*.008f*(1-blend));
        }
    }
}
