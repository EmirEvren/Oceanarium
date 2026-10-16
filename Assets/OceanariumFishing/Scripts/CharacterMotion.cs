using UnityEngine;
namespace OceanariumFishing
{
    public sealed class CharacterMotion : MonoBehaviour
    {
        public LocalPlayerController controller;
        public Transform leftArm,rightArm,leftLeg,rightLeg,head;
        private float phase;
        private void LateUpdate()
        {
            float amount=Mathf.Clamp01(controller.Speed/3.4f);
            phase+=Time.deltaTime*(controller.Speed>4?13:9);
            float swing=Mathf.Sin(phase)*amount*32;
            leftArm.localRotation=Quaternion.Euler(swing,0,5);rightArm.localRotation=Quaternion.Euler(-swing,0,-5);
            leftLeg.localRotation=Quaternion.Euler(-swing,0,0);rightLeg.localRotation=Quaternion.Euler(swing,0,0);
            head.localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*.8f)*2*(1-amount),0);
        }
    }
}
