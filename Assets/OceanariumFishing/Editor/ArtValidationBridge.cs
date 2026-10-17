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
                if(command=="home")EditorSceneManager.OpenScene(FoundationBuilder.Root+"/Scenes/OF_HomeIsland.unity");
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







