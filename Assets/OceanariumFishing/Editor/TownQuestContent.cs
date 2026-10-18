using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static partial class TownBuilder
    {
        static void AddQuests(TownWorld world)
        {
            string[] firstTitles={"A Welcome at the Tackle Shop","Meet the Botanist","Tea and Introductions","A Captain's Story","Between the Tides","Nothing Goes to Waste","A Photographer's Eye","A Stitch in Time","Tiny Underwater Stars","The Local Edition","A Fisherman's Tale","The Carpenter's Welcome"};
            string[] titles={"A Stronger Line","A Green Beginning","Tea for the Harbor","Shells with a Story","A Quiet Exchange","Salvage for the Workshop","A Frame for the Sea","Inspiration in the Square","An Underwater Garden","Know Your Harbor","Three Fishing Stories","Timber for Tomorrow"};
            string[] descriptions={"Purchase braided line from Oliver. Owned purchases also count.","Purchase Java moss from Marina for your aquarium.","Bring Rosie one island tea from her shop.","Bring Morris two rare shells from the town shoreline.","Bring the night merchant two rare shells. Visits are on alternate nights, 20:00–04:00.","Bring Bruno three scrap pieces from the salvage trail.","Purchase a picture frame from Iris for the aquarium.","Visit the fountain plaza and return to Lily.","Place any purchased decoration or furnishing in your home aquarium, then return.","Visit the town harbor and report back to Theo.","Discover three fish species through fishing. Existing discoveries count.","Bring Henry three driftwood pieces from the town shoreline."};
            TownObjective[] types={TownObjective.Purchase,TownObjective.Purchase,TownObjective.Deliver,TownObjective.Deliver,TownObjective.Deliver,TownObjective.Deliver,TownObjective.Purchase,TownObjective.Visit,TownObjective.PlaceDecoration,TownObjective.Visit,TownObjective.Discover,TownObjective.Deliver};
            string[] targets={"braided_line","java_moss","island_tea","rare_shell","rare_shell","scrap","harbor","plaza","any","harbor","any","driftwood"};int[] amounts={1,1,1,2,2,3,1,1,1,1,3,3};types[6]=TownObjective.Photo;descriptions[6]="Buy a pocket camera from Iris. Stand at the town harbor and press P to save a photo, then return.";titles[6]="A Portrait of the Harbor";
            for(int i=0;i<world.residents.Length;i++)
            {
                var npc=world.residents[i];var intro=Asset<TownQuestDefinition>(npc.id+"_welcome");intro.id=npc.id+"_welcome";intro.giver=npc.id;intro.titleKey="town.quest."+intro.id;intro.descriptionKey=intro.titleKey+".description";intro.objective=TownObjective.Talk;intro.target=npc.id;intro.amount=1;intro.coins=2;intro.friendship=20;Text("quest."+intro.id,firstTitles[i]);Text("quest."+intro.id+".description","Have a daily conversation with "+en.GetEntry(npc.nameKey).Value+". Return to claim your welcome reward.");
                var quest=Asset<TownQuestDefinition>(npc.id+"_help");quest.id=npc.id+"_help";quest.giver=npc.id;quest.titleKey="town.quest."+quest.id;quest.descriptionKey=quest.titleKey+".description";quest.objective=types[i];quest.target=targets[i];quest.amount=amounts[i];quest.coins=5;quest.friendship=40;quest.prerequisite=intro.id;Text("quest."+quest.id,titles[i]);Text("quest."+quest.id+".description",descriptions[i]);npc.quests=new[]{intro,quest};EditorUtility.SetDirty(intro);EditorUtility.SetDirty(quest);
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/CoastalFinds.mat");if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.85f,.72f,.48f);AssetDatabase.CreateAsset(material,Dir+"/CoastalFinds.mat");}
            for(int i=0;i<18;i++){float x=i<9?-5-i%3*1.2f:5+i%3*1.2f,z=-18-i/3*1.3f;var item=GameObject.CreatePrimitive(PrimitiveType.Sphere);item.name="Town coastal find "+i;item.transform.SetParent(world.transform);item.transform.position=Ground(x,z)+Vector3.up*.15f;item.transform.localScale=new Vector3(.35f,.2f,.25f);item.GetComponent<Renderer>().sharedMaterial=material;var pickup=item.AddComponent<TownCollectible>();pickup.id="town_find_"+i;pickup.resource=i%3==0?"rare_shell":i%3==1?"scrap":"driftwood";pickup.amount=2;}
        }
    }
}
