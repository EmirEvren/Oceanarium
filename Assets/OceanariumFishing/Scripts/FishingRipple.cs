using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanariumFishing
{
    public sealed class FishingRipple:MonoBehaviour
    {public float duration=1;float age;void Update(){age+=Time.deltaTime;transform.localScale=Vector3.one*(1+age*3);var line=GetComponent<LineRenderer>();line.widthMultiplier=Mathf.Max(0,1-age/duration);if(age>=duration)Destroy(gameObject);}}
}
