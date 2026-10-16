using System.Collections.Generic;

namespace Oceanarium
{
    // English source keys; replace this small adapter with Unity String Tables in phase 2.
    public static class PrototypeText
    {
        private static readonly Dictionary<string, string> English = new()
        {
            ["title"] = "OCEANARIUM",
            ["subtitle"] = "HOME REEF  /  FIRST STEPS",
            ["start"] = "Explore the island",
            ["resume"] = "Return to island",
            ["menu"] = "Main menu",
            ["intro"] = "A little island. A whole ocean of possibilities.",
            ["scope"] = "Exploration prototype • Solo • Progress is not saved",
            ["controls"] = "WASD  Move    SHIFT  Run    Hold RMB  Orbit    E  Interact    ESC  Menu",
            ["goal.rod"] = "FIRST STEPS\nFind one of the four fishing rods on the pier.",
            ["goal.chest"] = "FIRST STEPS\nRod collected! Visit the supply chest beside the cabin.",
            ["goal.done"] = "ISLAND EXPLORED\nNext milestone: cast, reel and catch your first fish.",
            ["rod"] = "[E] Pick up fishing rod",
            ["chest"] = "[E] Inspect supply chest",
            ["rod.owned"] = "You already have a rod. Leave these for your future crew.",
            ["rod.collected"] = "Fishing rod collected. Fishing arrives in the next milestone.",
            ["chest.empty"] = "Supplies checked: the boat repair and inventory systems are planned next.",
            ["rods"] = "Fishing rods remaining: {0} / 4",
            ["close"] = "Close",
            ["chest.title"] = "SUPPLY CHEST",
            ["chest.body"] = "Your future shared storage.\n\nThis milestone supports exploration and rod pickup.\nItem storage and persistent saves are not implemented yet."
        };
        public static string Get(string key) => English.TryGetValue(key, out var value) ? value : "Text unavailable";
    }
}
