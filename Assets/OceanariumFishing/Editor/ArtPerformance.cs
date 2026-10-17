using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace OceanariumFishing.Editor
{
 public static class ArtPerformance
 {
  static readonly List<float> frames=new();static readonly List<int> batches=new(),triangles=new(),calls=new();static double start;static int lastFrame;
  [MenuItem("Oceanarium Fishing/Measure ASTRA 0.1 Performance")]
  public static void Begin(){if(!EditorApplication.isPlaying)return;frames.Clear();batches.Clear();triangles.Clear();calls.Clear();start=EditorApplication.timeSinceStartup;lastFrame=-1;EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
  static void Tick()
  {
   if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
   if(EditorApplication.timeSinceStartup-start<3 || Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
   frames.Add(Time.unscaledDeltaTime*1000);batches.Add(UnityEditor.UnityStats.batches);triangles.Add(UnityEditor.UnityStats.triangles);calls.Add(UnityEditor.UnityStats.drawCalls);
   if(frames.Count<600 && EditorApplication.timeSinceStartup-start<33)return;
   EditorApplication.update-=Tick;var sorted=frames.OrderBy(x=>x).ToArray();
   string text=$"ASTRA 0.1 editor measurement — {DateTime.UtcNow:O}\nGPU: {SystemInfo.graphicsDeviceName}, VRAM {SystemInfo.graphicsMemorySize} MB, API {SystemInfo.graphicsDeviceType}\nCPU: {SystemInfo.processorType}, threads {SystemInfo.processorCount}; RAM {SystemInfo.systemMemorySize} MB\nRender resolution: {Screen.width}x{Screen.height}; samples {frames.Count}; vSync {QualitySettings.vSyncCount}; targetFrameRate {Application.targetFrameRate}\nMean frame {frames.Average():F2} ms ({1000/frames.Average():F1} FPS); median {sorted[sorted.Length/2]:F2} ms; p95 {sorted[(int)((sorted.Length-1)*.95f)]:F2} ms\nMean batches {batches.Average():F1}; draw calls {calls.Average():F1}; triangles {triangles.Average():F0}\nTexture memory (all loaded textures, editor included): {UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver()/1048576f:F1} MB graphics driver allocation (not isolated texture memory).\nEditor Play Mode, stationary spawn viewpoint, Game and Scene view may both render. Not a standalone or mid-range hardware certification; no claim of stable 60 FPS from this sample alone.\n";
   var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();
   var mats=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
   var textures=mats.SelectMany(m=>m.GetTexturePropertyNames().Select(n=>m.GetTexture(n))).Where(t=>t!=null).Distinct().ToArray();
   text+=$"Active mesh renderers {renderers.Length}; unique scene materials {mats.Length}; transparent materials {mats.Count(m=>m.renderQueue>=3000)}; referenced material textures {textures.Length}, runtime bytes {textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t))}. Excludes pipeline render targets, probes, sky and UI.\n";
   File.WriteAllText("Docs/ASTRA_01_PERFORMANCE.txt",text);Debug.Log(text);
  }
 }
}


