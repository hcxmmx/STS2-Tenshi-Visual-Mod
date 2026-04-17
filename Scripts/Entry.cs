using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace TenshiMod.Scripts;

[ModInitializer("Init")]
public static class Entry
{
	public static void Init()
	{
		TenshiGlobals.Log("\n====================================");
		TenshiGlobals.Log("Hinana Tenshi Project: 核心大脑成功点火喵！准备劫持引擎！");
		TenshiGlobals.Log("====================================\n");

		TenshiGlobals.TenshiScene = TenshiGlobals.GetPackedScene(TenshiGlobals.TenshiScenePath);
		new Harmony(TenshiGlobals.HarmonyId).PatchAll();
	}
}
