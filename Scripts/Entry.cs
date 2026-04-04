using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace TenshiMod.Scripts;

[ModInitializer("Init")]
public static class Entry
{
	public static void Init()
	{
		GD.Print("\n====================================");
		GD.Print("Hinana Tenshi Project: 核心大脑成功点火喵！准备劫持引擎！");
		GD.Print("====================================\n");

		TenshiGlobals.TenshiScene = ResourceLoader.Load<PackedScene>(TenshiGlobals.TenshiScenePath);
		new Harmony(TenshiGlobals.HarmonyId).PatchAll();
	}
}
