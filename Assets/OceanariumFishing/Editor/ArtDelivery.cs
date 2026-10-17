using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
namespace OceanariumFishing.Editor
{
 public static class ArtDelivery
 {
  [MenuItem("Oceanarium Fishing/Validate ASTRA 0.1 Assets")]
  public static void Validate()
  {
   var report=new System.Text.StringBuilder("ASTRA 0.1 asset validation\n");bool ok=true;
   foreach(string guid in AssetDatabase.FindAssets("t:Shader",new[]{FoundationBuilder.Root+"/Shaders"}))
   {
    var shader=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
    report.AppendLine(shader.name+": supported="+shader.isSupported+", compiler errors="+errors.Length);ok&=shader.isSupported && errors.Length==0;
    foreach(var e in errors)report.AppendLine(e.message);
   }
   foreach(string meshGuid in AssetDatabase.FindAssets("t:Mesh",new[]{FoundationBuilder.Root+"/Meshes"}))
   {
    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(meshGuid));
    bool finite=mesh.vertices.All(v=>!float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z));
    if(!finite){ok=false;report.AppendLine("FAIL non-finite vertices: "+mesh.name);}
   }
   report.AppendLine("Generated mesh vertex finiteness checked.");
   AssetDatabase.SaveAssets();File.WriteAllText("Docs/ASTRA_01_ASSET_VALIDATION.txt",(ok?"PASS\n":"FAIL\n")+report);
   Debug.Log(report.ToString());
  }
 }
}


