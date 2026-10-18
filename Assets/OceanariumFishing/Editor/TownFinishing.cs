using UnityEngine;
using UnityEditor;
namespace OceanariumFishing.Editor
{
    public static partial class TownBuilder
    {
        static void FinishTown(TownWorld world)
        {
            var source=AssetDatabase.LoadAssetAtPath<AquariumBuildItem>(Root+"/Building/decor_1.asset");
            var glow=Asset<AquariumBuildItem>("luminous_coral");EditorUtility.CopySerialized(source,glow);glow.id="luminous_coral";
            var specimen=Object.Instantiate(source.prefab);specimen.name="Luminous tide coral";
            foreach(var r in specimen.GetComponentsInChildren<Renderer>())
            {
                var path=Dir+"/Luminous coral.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(r.sharedMaterial);AssetDatabase.CreateAsset(m,path);}m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.12f,.65f,.55f));r.sharedMaterial=m;EditorUtility.SetDirty(m);
            }
            glow.prefab=PrefabUtility.SaveAsPrefabAsset(specimen,Dir+"/LuminousCoral.prefab");Object.DestroyImmediate(specimen);EditorUtility.SetDirty(glow);
            var catalog=AssetDatabase.LoadAssetAtPath<AquariumBuildCatalog>(Root+"/Building/BuildCatalog.asset");var items=new System.Collections.Generic.List<AquariumBuildItem>(catalog.items);if(!items.Exists(i=>i.id==glow.id))items.Add(glow);catalog.items=items.ToArray();EditorUtility.SetDirty(catalog);
            world.residents[8].rewardBuildItem=glow;world.residents[8].products[0].buildItem=glow;EditorUtility.SetDirty(world.residents[8].products[0]);
            en.AddEntry("build.luminous_coral","Luminous tide coral");tr.AddEntry("build.luminous_coral","Işıldayan gelgit mercanı");
            string[] welcome={"Welcome to Tidal Tackle. I'm Oliver. Tell me what you hope to catch, and we'll start with a steady line.","I'm Marina! Freshwater roots and salty reefs need different care. Let's find your aquarium something green.","A new face! I'm Rosie. Sit a moment; the first thing a harbor needs is a good kitchen.","Captain Morris, retired from rushing. If you find something washed ashore, bring its story along.","Call me Gull. I trade in things the tide remembers. Look carefully; I won't be here every night.","Bruno. Mind the tools! That scrap might be tomorrow's finest cabinet.","I'm Iris. The harbor changes every time you look. Would you help me collect its little moments?","Lily, at your service. Comfortable seams first, splendid hats second.","Luna. I study the tiny lights living beneath the waves. Would you like a closer look?","Theo Current, Tidal Press. New neighbor? Excellent. There's always room for another story.","Eddie! Pull up a chair. We tell fishing stories here, then test whether our timing lives up to them.","Henry Oak. Good wood needs patience. So does a good neighbor. Welcome."};
            string[] friends={"I set aside my best knot advice for you. A quiet cast and an even reel go a long way.","Your visits make this little nursery feel like a garden shared with friends.","There's always a place for you near my kettle. Tell me about your latest catch.","Some treasures are better shared. I'm glad my old stories found a patient listener.","You kept coming back. Few people notice what waits between the tides. I remember that.","I've been saving the useful offcuts. A friend with an aquarium can always use good timber.","I used to chase perfect sunsets. Now I photograph the people who make this harbor home.","I know your style now: ready for an adventure, but never too busy to wave hello.","I saved my brightest notes for you. A little light feels warmer when someone shares it.","You have become part of the harbor's story. That's the best kind of news.","Our table feels empty when you're away. Another round, or another impossible fish story?","I've put extra care into your next piece. That's what working for a friend feels like."};
            for(int i=0;i<world.residents.Length;i++)
            {
                var d=world.residents[i];d.firstMeetingKey="town.first."+d.id;d.friendKey="town.friend."+d.id;d.shopIntroKey="town.intro."+d.id;d.farewellKey="town.farewell";
                Text("first."+d.id,welcome[i]);Text("friend."+d.id,friends[i]);Text("intro."+d.id,i==9?"Read the bulletin for harbor news and fishing hints.":i==10?"Try a free timing round from our conversation. Three catches win the daily prize.":"Choose something for your next island day. The price is shown before you confirm.");
            }
            Text("farewell","See you by the water.");Text("reaction.loved","You remembered my favorite! Thank you. +35 friendship.");Text("reaction.liked","How thoughtful. I'll put this to good use. +20 friendship.");Text("reaction.neutral","Thank you for thinking of me. +5 friendship.");Text("reaction.disliked","I appreciate the thought, but this isn't really for me. -5 friendship.");Text("sea_pearl","Sea pearls");Text("driftwood","Driftwood");
            var wood=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Shop timber.mat");var cream=AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Shop linen.mat");
            GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material mat,bool solid=true){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
            // Small open-front shop rooms preserve clear public paths and avoid sealed placeholder doors.
            for(int n=0;n<world.residents.Length;n++)
            {
                if(n==4)continue;var d=world.residents[n];var room=new GameObject("Open shop interior - "+d.id).transform;room.SetParent(world.transform);room.position=d.work+new Vector3(1.2f,0,1.3f);
                Part(room,"Rear timber panel",new(0,1.05f,.8f),new(2.3f,2.1f,.12f),wood);
                Part(room,"Left linen wall",new(-1.15f,1.05f,.1f),new(.1f,2.1f,1.5f),cream);
                Part(room,"Roof canopy",new(0,2.2f,.1f),new(2.6f,.16f,1.9f),cream);
                for(int shelf=0;shelf<2;shelf++){Part(room,"Display shelf",new(0,.8f+shelf*.6f,.65f),new(1.9f,.08f,.35f),wood,false);for(int jar=0;jar<4;jar++)Part(room,"Craft display",new(-.65f+jar*.43f,1+shelf*.6f,.6f),new(.2f,.3f,.2f),jar%2==0?cream:wood,false);}
            }
            var tea=new GameObject("Tea garden seating").transform;tea.SetParent(world.transform);tea.position=Ground(5,-6);
            Part(tea,"Tea table",new(0,.7f,0),new(1.2f,.12f,.75f),wood);Part(tea,"Table support",new(0,.35f,0),new(.15f,.7f,.15f),wood);
            for(int side=-1;side<=1;side+=2){Part(tea,"Garden bench",new(side*1.1f,.4f,0),new(.4f,.12f,1.1f),wood);Part(tea,"Bench legs",new(side*1.1f,.2f,0),new(.2f,.4f,.7f),wood);}
            var look=new GameObject("Harbor scenic overlook").transform;look.SetParent(world.transform);look.position=Ground(8,-24);
            for(int plank=0;plank<10;plank++)Part(look,"Boardwalk plank",new(plank*.32f,.08f,0),new(.3f,.16f,2.2f),wood);
            for(int step=0;step<3;step++)Part(look,"Approach stair",new(1.4f,.02f*step,1.2f+step*.23f),new(1.2f,.06f,.25f),wood);
            for(int post=0;post<4;post++)Part(look,"Seaward railing post",new(post*.95f,.6f,-1),new(.1f,1.2f,.1f),wood);
            Part(look,"Seaward handrail",new(1.45f,1.15f,-1),new(3.1f,.08f,.08f),cream);
        }
    }
}

