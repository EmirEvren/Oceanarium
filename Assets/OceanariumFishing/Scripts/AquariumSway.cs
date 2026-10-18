using System;
using UnityEngine;
namespace OceanariumFishing {
    public sealed class AquariumSway:MonoBehaviour
    {Quaternion basis;void Awake(){basis=transform.localRotation;}void Update(){transform.localRotation=basis*Quaternion.Euler(0,0,Mathf.Sin(Time.time*1.3f+transform.position.x)*3);}}
}
