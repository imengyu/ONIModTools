using HarmonyLib;
using KMod;
using OxygenNotIncluded.Mods.ONIModTools;
using UnityEngine;

public class ONIModToolsMod : UserMod2
{ 
  public override void OnLoad(Harmony harmony)
  {
    base.OnLoad(harmony);
    new GameObject("RuntimeConsole").AddComponent<RuntimeConsole>();
  }
}

