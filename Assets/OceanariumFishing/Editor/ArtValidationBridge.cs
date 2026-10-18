using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace OceanariumFishing.Editor
{
    // Local editor command inbox for repeatable art validation; never included in builds.
    [InitializeOnLoad]
    public static class AstraEditorBridge
    {
        static AstraEditorBridge(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            const string path="Temp/OF_ArtCommand.txt";
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(path))return;
            string command;try{command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try
            {
                if(command=="world-build")WorldBuilder.Build();
                else if(command=="aquarium-build")AquariumBuilder.Build();
                else if(command=="aquarium-checks")AquariumChecks.Run();
                else if(command=="aquarium-restart-checks")AquariumChecks.Restart();
                else if(command=="aquarium-polish-checks")AquariumChecks.Polish();
                else if(command=="building-checks")AquariumBuildChecks.Run();
                else if(command=="building-restart-checks")AquariumBuildChecks.Restart();
                else if(command=="travel-checks")BoatTravelChecks.Run();
                else if(command=="town-transaction-checks")TownTransactionChecks.Run();
                else if(command=="town-build")TownBuilder.Build();
                else if(command=="town-runtime-checks")TownRuntimeChecks.Run();
                else if(command=="town-activity-checks")TownActivityChecks.Run();
                else if(command=="town-content-checks")TownContentChecks.Run();
                else if(command=="travel-restart-checks")BoatTravelChecks.Restart();
                else if(command=="travel-build")BoatWorldBuilder.Build();
                else if(command=="building-generate")AquariumBuildGenerator.Build();
                else if(command=="building-open")Object.FindFirstObjectByType<AquariumBuildHUD>().Open();
                else if(command=="building-close")Object.FindFirstObjectByType<AquariumBuildHUD>().Close();
                else if(command=="aquarium-panel")Object.FindFirstObjectByType<AquariumWorld>().Panel.Open(null);
                else if(command=="aquarium-close")Object.FindFirstObjectByType<AquariumWorld>().Panel.Close();
                else if(command=="capture05"){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_05_Current.png");}
                else if(command=="cozy-build")CozyFishingBuilder.Build();
                else if(command=="cozy-checks")CozyFishingChecks.Run();
                else if(command=="cozy-demo")CozyFishingDemo.Run();
                else if(command=="cozy-runtime-checks")CozyRuntimeChecks.Run();
                else if(command=="capture04"){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_04_Current.png");}
                else if(command=="inventory")Object.FindFirstObjectByType<CollectionHUD>().Open(false);
                else if(command=="journal")Object.FindFirstObjectByType<CollectionHUD>().Open(true);
                else if(command=="close-collection")Object.FindFirstObjectByType<CollectionHUD>().Close();
                else if(command=="fishing-localize"){FishingBuilder.Localize();AssetDatabase.SaveAssets();}
                else if(command=="fishing-demo")FishingInputDemo.Run();
                else if(command=="fishing-checks")FishingChecks.Run();
                else if(command=="fishing-build")FishingBuilder.Build();
                else if(command=="capture03"){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_03_Fishing.png");}
                else if(command=="creator-localize")CreatorBuilder.AddLocalization();
                else if(command=="creator-checks")CreatorChecks.Run();
                else if(command=="creator-build")CreatorBuilder.Build();
                else if(command.StartsWith("cosmetic:")){var parts=command.Split(':');Object.FindFirstObjectByType<CharacterCreator>().Select(parts[1],parts[2]);}
                else if(command.StartsWith("category:"))Object.FindFirstObjectByType<CharacterCreator>().SetCategory(command.Substring(9));
                else if(command=="creator-confirm")Object.FindFirstObjectByType<CharacterCreator>().Confirm();
                else if(command=="travel-creator")GameSession.Current.Travel(Destination.CharacterCreator);
                else if(command=="capture02"){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_02_"+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+".png");}
                else if(command=="creator")EditorSceneManager.OpenScene(FoundationBuilder.Root+"/Scenes/OF_CharacterCreator.unity");
                else if(command=="home")EditorSceneManager.OpenScene(FoundationBuilder.Root+"/Scenes/OF_HomeIsland.unity");
                else if(command=="play")EditorApplication.isPlaying=true;
                else if(command=="stop")EditorApplication.isPlaying=false;
                else if(command=="checks")AstraChecks.Run();
                else if(command=="localize")LocalizationContent.Apply();
                else if(command=="language-checks")LocalizationChecks.Run();
                else if(command.StartsWith("language:"))GameSession.Current.Languages.Select(command.Substring(9));
                else if(command=="capture"){Directory.CreateDirectory("Docs/Screenshots");ScreenCapture.CaptureScreenshot("Docs/Screenshots/ASTRA_01_HomeIsland.png");}
                else if(command=="diagnose"){var mesh=GameObject.Find("Organic island").GetComponent<MeshFilter>().sharedMesh;Debug.Log("Terrain colors="+mesh.colors.Length+" first="+mesh.colors[0]+" mid="+mesh.colors[3000]+" shader="+GameObject.Find("Organic island").GetComponent<MeshRenderer>().sharedMaterial.shader.name);}
                else if(command=="optimize")AstraArtPass.Optimize();
                else if(command.StartsWith("view:")){var p=Object.FindFirstObjectByType<LocalPlayerController>();var parts=command.Split(':' );p.Teleport(new Vector3(float.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture),float.Parse(parts[2],System.Globalization.CultureInfo.InvariantCulture),float.Parse(parts[3],System.Globalization.CultureInfo.InvariantCulture)));if(parts.Length>5)p.SetViewAngles(float.Parse(parts[4],System.Globalization.CultureInfo.InvariantCulture),float.Parse(parts[5],System.Globalization.CultureInfo.InvariantCulture));}
                else if(command=="validate")ArtDelivery.Validate();
                else if(command=="performance")ArtPerformance.Begin();
                else if(command=="reference")AstraArtPass.ReferenceDirection();
                else if(command=="polish")EditorApplication.ExecuteMenuItem("Oceanarium Fishing/Apply ASTRA 0.1 Art Pass");
                Debug.Log("ASTRA editor command: "+command);
            }
            catch(System.Exception e){Debug.LogException(e);}
        }
    }
}
// ASTRA 0.1 editor automation.











