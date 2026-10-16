using UnityEngine;
namespace OceanariumFishing
{
    public enum Destination { Boot, MainMenu, CharacterCreator, HomeIsland, TownIsland, ExplorationIsland01, ExplorationIsland02, ExplorationIsland03 }
    [CreateAssetMenu(menuName="Oceanarium/Scene catalog")]
    public sealed class SceneCatalog : ScriptableObject
    {
        public string[] sceneNames;
        public string Resolve(Destination destination) => sceneNames[(int)destination];
    }
}
