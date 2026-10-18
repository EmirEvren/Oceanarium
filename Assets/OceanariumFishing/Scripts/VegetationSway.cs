using UnityEngine;
namespace OceanariumFishing
{
    public sealed class VegetationSway : MonoBehaviour
    {
        public float amplitude=1.3f;
        private Quaternion initial;
        private float phase;
        private void Awake(){initial=transform.localRotation;phase=transform.position.x*.7f+transform.position.z;}
        private void Update()=>transform.localRotation=initial*Quaternion.Euler(Mathf.Sin(Time.time*.8f+phase)*amplitude*(1+(WorldTimeService.Current?.Simulation.Current?.wind??0)),0,Mathf.Cos(Time.time*.65f+phase)*amplitude*(1+(WorldTimeService.Current?.Simulation.Current?.wind??0)));
    }
}
