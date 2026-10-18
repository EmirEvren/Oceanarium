using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
namespace OceanariumFishing
{
    public sealed class TownActivities:MonoBehaviour
    {
        TownWorld world;float roundStarted;bool playing,capturing;int hits,attempts;TownArcadeRound round;
        public bool Playing=>playing;string T(string key)=>GameSession.Current.Languages.Text(key);
        void Awake(){world=GetComponent<TownWorld>();}
        public void BeginArcade(){if(playing)return;playing=true;roundStarted=Time.time;hits=attempts=0;round=new();}
        public void StopArcade(){playing=false;}
        void Update()
        {
            if(playing&&!world.UI.IsOpen)playing=false;
            if(Keyboard.current?.pKey.wasPressedThisFrame==true&&world.Ready&&!world.UI.IsOpen&&world.Player.InputEnabled&&TownTransactions.Count(world.Inventory.Collection,"camera")>0&&!capturing)StartCoroutine(Capture());
        }
        public IEnumerator Capture()
        {
            if(capturing||!world.Ready||TownTransactions.Count(world.Inventory.Collection,"camera")<1||!world.Player.InputEnabled)yield break;
            capturing=true;yield return new WaitForEndOfFrame();string id=Guid.NewGuid().ToString("N");string directory=Path.Combine(Application.persistentDataPath,"Photos");Texture2D texture=null;
            try{Directory.CreateDirectory(directory);texture=ScreenCapture.CaptureScreenshotAsTexture();if(world.Inventory.Collection.town.flags.Contains("photo-filter:warm")){var colors=texture.GetPixels32();for(int i=0;i<colors.Length;i++){var c=colors[i];c.r=(byte)Mathf.Min(255,c.r*1.05f);c.b=(byte)(c.b*.92f);colors[i]=c;}texture.SetPixels32(colors);texture.Apply();}File.WriteAllBytes(Path.Combine(directory,id+".png"),texture.EncodeToPNG());var data=world.Inventory.Collection.Copy();string subject=world.Player.transform.position.z< -25?"harbor":"town";data.town.flags.Add("photo-file:"+id);if(!data.town.flags.Contains("photo:"+subject))data.town.flags.Add("photo:"+subject);world.Inventory.CommitAquarium(data);}catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("Photo could not be saved: "+e.Message);}finally{if(texture)Destroy(texture);capturing=false;}
        }
        public void Draw(float x,CozyUI ui)
        {
            GUI.Label(new Rect(x+45,275,990,100),T("town.arcade_rules"),ui.text);float marker=Mathf.PingPong((Time.time-roundStarted)*.7f,1);GUI.Box(new Rect(x+80,410,900,45),"");var old=GUI.color;GUI.color=new Color(.3f,.7f,.5f);GUI.DrawTexture(new Rect(x+80+900*.4f,410,900*.2f,45),Texture2D.whiteTexture);GUI.color=Color.white;GUI.DrawTexture(new Rect(x+80+marker*890,405,10,55),Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(new Rect(x+45,490,990,45),hits+" / 3 · "+attempts+" / 5",ui.heading);
            if(playing&&GUI.Button(new Rect(x+300,560,500,60),T("town.arcade_reel"),ui.button)){round.Reel(Time.time-roundStarted);hits=round.Hits;attempts=round.Attempts;roundStarted=Time.time;if(round.Finished){playing=false;var data=world.Inventory.Collection.Copy();if(round.Claim(data,world.Clock.Day))world.Inventory.CommitAquarium(data);}}
            if(!playing)GUI.Label(new Rect(x+45,640,990,60),T(hits>=3?"town.arcade_win":"town.arcade_retry"),ui.heading);
        }
    }
}


