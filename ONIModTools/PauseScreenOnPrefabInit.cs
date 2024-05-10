using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OxygenNotIncluded.Mods.ONIModTools
{
  [HarmonyPatch(typeof(PauseScreen))]
  [HarmonyPatch("OnPrefabInit")]
  public static class PauseScreenOnPrefabInit
  {
    public static PauseScreen Instance;

    public static void Postfix(PauseScreen __instance)
    {
      Traverse obj = Traverse.Create((object)__instance);
      List<KButtonMenu.ButtonInfo> list = obj.Field("buttons").GetValue<KButtonMenu.ButtonInfo[]>().ToList();
      Instance = __instance;
      list.Insert(list.Count - 2, new KButtonMenu.ButtonInfo("ModTools", (Action)278, delegate
      {
        __instance.Show(false);
        Util.KInstantiateUI<InfoDialogScreen>(
          ScreenPrefabs.Instance.InfoDialogScreen.gameObject, 
         GameScreenManager.Instance.ssOverlayCanvas.gameObject, 
         force_active: true
        ).SetHeader("Choose")
           .AddOption("RuntimeScenceDebugger", (screen) =>
           {
             GameObject go = GameObject.Find("RuntimeScenceDebugger");
             if (go == null)
             {
               go = new GameObject("RuntimeScenceDebugger");
               go.AddComponent<RuntimeScenceDebugger>();
             }
             go.GetComponent<RuntimeScenceDebugger>().Show();
             screen.Show(false);
           })
           .AddOption("RuntimeConsole", (screen) =>
           {

             var go = GameObject.Find("RuntimeConsole");
             if (go == null)
             {
               go = new GameObject("RuntimeConsole");
               go.AddComponent<RuntimeConsole>();
             }
             go.GetComponent<RuntimeConsole>().Show();
             screen.Show(false);
           })
           .AddDefaultCancel();
      }));
      obj.Field("buttons").SetValue((object)list.ToArray());
    }
  }
}
