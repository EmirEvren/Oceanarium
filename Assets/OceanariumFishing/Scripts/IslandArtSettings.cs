using UnityEngine;
namespace OceanariumFishing
{
    [CreateAssetMenu(menuName="Oceanarium/Island art settings")]
    public sealed class IslandArtSettings : ScriptableObject
    {
        public int seed=1847;
        [Range(0,3)] public float hillHeight=1.65f;
        [Range(.5f,2)] public float vegetationDensity=1;
        [Range(0,.2f)] public float windStrength=.055f;
        public Color sunlight=new Color(1,.88f,.7f);
        [Range(.3f,2)] public float sunIntensity=1.15f;
        public Vector3 sunEuler=new Vector3(38,-38,0);
        public Color skyAmbient=new Color(.57f,.7f,.77f);
        public Color horizon=new Color(.66f,.8f,.81f);
    }
}
