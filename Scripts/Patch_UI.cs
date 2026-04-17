using System;
using Godot;
using HarmonyLib;

namespace TenshiMod.Scripts;

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen), "SelectCharacter")]
internal static class NCharacterSelectScreen_SelectCharacter_Patch
{
    private static void Prefix(object characterModel, ref string __state)
    {
        var entryName = TenshiGlobals.GetCharacterEntry(characterModel);
        if (!string.Equals(entryName, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        __state = Traverse.Create(characterModel).Property("CharacterSelectSfx").GetValue<string>()
            ?? Traverse.Create(characterModel).Field("CharacterSelectSfx").GetValue<string>();

        try { Traverse.Create(characterModel).Property("CharacterSelectSfx").SetValue(""); } catch { }
        try { Traverse.Create(characterModel).Field("CharacterSelectSfx").SetValue(""); } catch { }
        try { Traverse.Create(characterModel).Field("<CharacterSelectSfx>k__BackingField").SetValue(""); } catch { }
    }

    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen __instance, Node charSelectButton, object characterModel, ref string __state)
    {
        var entryName = TenshiGlobals.GetCharacterEntry(characterModel);
        if (!string.Equals(entryName, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TenshiGlobals.Log("\n====== 🎯 选人界面雷达：侦测到天子大小姐登场！启动视觉劫持！ ======");

        var bgContainer = Traverse.Create(__instance).Field("_bgContainer").GetValue<Control>();
        var nameLabel = Traverse.Create(__instance).Field("_name").GetValue();
        var descLabel = Traverse.Create(__instance).Field("_description").GetValue<RichTextLabel>();

        if (bgContainer != null)
        {
            foreach (Node child in bgContainer.GetChildren())
            {
                if (child is CanvasItem canvasItem)
                {
                    canvasItem.Hide();
                }
            }

            var tenshiScreenScene = TenshiGlobals.GetPackedScene("res://mods/TenshiHinanawi/visuals/TenshiSelectScreen.tscn");
            if (tenshiScreenScene != null)
            {
                var tenshiScreen = tenshiScreenScene.Instantiate<Control>();
                tenshiScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                bgContainer.AddChild(tenshiScreen);
                TenshiGlobals.Log("✅ 蔚蓝有顶天背景铺设完毕！");
            }
        }

        if (nameLabel != null)
        {
            Traverse.Create(nameLabel).Method("SetTextAutoSize", new object[] { "比那名居 天子" }).GetValue();
        }

        if (descLabel != null)
        {
            descLabel.Text = "傲娇的不良天人。\n手持绯想之剑，可以操纵大地的气象与地震。";
        }

        if (__state != null)
        {
            try { Traverse.Create(characterModel).Property("CharacterSelectSfx").SetValue(__state); } catch { }
            try { Traverse.Create(characterModel).Field("CharacterSelectSfx").SetValue(__state); } catch { }
            try { Traverse.Create(characterModel).Field("<CharacterSelectSfx>k__BackingField").SetValue(__state); } catch { }
        }

        TenshiGlobals.Log("🎉 UI 篡改与防崩溃战术静音协议极其完美地执行完毕！");
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectButton), "Init")]
internal static class NCharacterSelectButton_Init_Patch
{
    private static void Postfix(object __instance, object character)
    {
        var entryName = TenshiGlobals.GetCharacterEntry(character);
        if (!string.Equals(entryName, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TenshiGlobals.Log("\n====== 🎯 头像雷达：锁定天子选人按钮！启动物理换脸！ ======");

        var customAvatar = TenshiGlobals.GetTexture("res://mods/TenshiHinanawi/visuals/Tenshi_Avatar.png");
        if (customAvatar == null)
        {
            GD.PrintErr("💥 找不到天子的头像图片！长官检查一下路径和文件名喵？");
            return;
        }

        var iconNode = Traverse.Create(__instance).Field("_icon").GetValue();
        if (iconNode == null)
        {
            return;
        }

        Traverse.Create(iconNode).Property("Texture").SetValue(customAvatar);
        TenshiGlobals.Log("✅ 天子头像极其完美地贴上去了！");
    }
}
