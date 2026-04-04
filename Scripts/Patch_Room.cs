using System;
using System.Collections;
using Godot;
using HarmonyLib;

namespace TenshiMod.Scripts;

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom), nameof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom._Ready))]
internal static class NMerchantRoom_Ready_Patch
{
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom __instance)
    {
        GD.Print("\n====== 侦测到进入商店！启动精准鸠占鹊巢协议！ ======");
        TenshiGlobals.IsInShop = true;

        var players = Traverse.Create(__instance).Field("_players").GetValue<IList>();
        if (players == null)
        {
            GD.PrintErr("💥 商店雷达中断：找不到 _players 列表！");
            return;
        }

        bool hasTenshi = false;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var character = Traverse.Create(player).Property("Character").GetValue() ?? Traverse.Create(player).Field("Character").GetValue();
            var entryName = TenshiGlobals.GetCharacterEntry(character);

            if (string.Equals(entryName, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                hasTenshi = true;
                GD.Print("🎯 商店 DNA 匹配成功！发现天子大小姐！");
                break;
            }
        }

        if (!hasTenshi)
        {
            GD.Print("拦截：队伍里没有天子，保留原版队伍！");
            return;
        }

        var characterContainer = __instance.GetNodeOrNull<Control>("%CharacterContainer");
        if (characterContainer == null)
        {
            return;
        }

        var scene = TenshiGlobals.TenshiScene ?? ResourceLoader.Load<PackedScene>(TenshiGlobals.TenshiScenePath);
        TenshiGlobals.TenshiScene = scene;
        if (scene == null)
        {
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var character = Traverse.Create(player).Property("Character").GetValue() ?? Traverse.Create(player).Field("Character").GetValue();
            var entryName = TenshiGlobals.GetCharacterEntry(character);

            if (!string.Equals(entryName, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i >= characterContainer.GetChildCount())
            {
                continue;
            }

            var targetChild = characterContainer.GetChild(i);
            if (targetChild is CanvasItem canvasItem)
            {
                canvasItem.Hide();
            }

            Vector2 originalPos = Vector2.Zero;
            if (targetChild is Control c)
            {
                originalPos = c.Position;
            }
            else if (targetChild is Node2D n)
            {
                originalPos = n.Position;
            }

            var tenshiShopMecha = scene.Instantiate<Node2D>();
            tenshiShopMecha.Name = $"TenshiShopMecha_{i}";
            characterContainer.AddChild(tenshiShopMecha);

            tenshiShopMecha.Position = originalPos + new Vector2(0, -200f);
            tenshiShopMecha.Scale = new Vector2(0.7f, 0.7f);

            var combatSprite = TenshiGlobals.FindFirstNode<AnimatedSprite2D>(tenshiShopMecha);
            var shopSprite = TenshiGlobals.FindFirstNode<Sprite2D>(tenshiShopMecha, s => s.Name == "ShopSprite");
            var animPlayer = TenshiGlobals.FindFirstNode<AnimationPlayer>(tenshiShopMecha);

            if (combatSprite != null)
            {
                combatSprite.Visible = false;
            }

            if (shopSprite != null)
            {
                shopSprite.Visible = true;
            }

            if (animPlayer != null)
            {
                animPlayer.Play("Shop_Idle");
            }
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom), "HideScreen")]
internal static class NMerchantRoom_HideScreen_Patch
{
    private static void Prefix()
    {
        GD.Print("\n====== 侦测到离开商店！摘除物理锁！ ======");
        TenshiGlobals.IsInShop = false;
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom), nameof(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom._Ready))]
internal static class NRestSiteRoom_Ready_Patch
{
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom __instance)
    {
        GD.Print("\n====== 📡 篝火雷达：侦测到进入篝火！启动协议！ ======");
        TenshiGlobals.IsInShop = true;

        var runState = Traverse.Create(__instance).Field("_runState").GetValue();
        if (runState == null)
        {
            GD.PrintErr("💥 雷达中断：找不到 _runState！");
            return;
        }

        var players = Traverse.Create(runState).Property("Players").GetValue<IList>()
            ?? Traverse.Create(runState).Field("Players").GetValue<IList>();
        if (players == null)
        {
            GD.PrintErr("💥 雷达中断：找不到 Players 列表！");
            return;
        }

        var scene = TenshiGlobals.TenshiScene ?? ResourceLoader.Load<PackedScene>(TenshiGlobals.TenshiScenePath);
        TenshiGlobals.TenshiScene = scene;
        if (scene == null)
        {
            GD.PrintErr("💥 雷达中断：找不到天子的 Godot PCK 场景！");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var character = Traverse.Create(player).Property("Character").GetValue() ?? Traverse.Create(player).Field("Character").GetValue();
            var entryName = TenshiGlobals.GetCharacterEntry(character);

            GD.Print($"🔍 扫描玩家 {i} 的 DNA (Entry): {entryName ?? "NULL"}");
            if (!string.Equals(entryName, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string containerPath = $"BgContainer/Character_{i + 1}";
            var container = __instance.GetNodeOrNull<Control>(containerPath);
            if (container == null)
            {
                GD.PrintErr($"💥 雷达中断：找不到官方坑位 {containerPath}！");
                continue;
            }

            for (int j = 0; j < container.GetChildCount(); j++)
            {
                if (container.GetChild(j) is CanvasItem canvasItem)
                {
                    canvasItem.Hide();
                }
            }

            var tenshiCampMecha = scene.Instantiate<Node2D>();
            tenshiCampMecha.Name = $"TenshiCampMecha_{i}";
            container.AddChild(tenshiCampMecha);

            tenshiCampMecha.Scale = new Vector2(0.7f, 0.7f);
            tenshiCampMecha.Position = new Vector2(0, 0f);
            bool needsFlip = (i % 2 == 1);

            var combatSprite = TenshiGlobals.FindFirstNode<AnimatedSprite2D>(tenshiCampMecha);
            var shopSprite = TenshiGlobals.FindFirstNode<Sprite2D>(tenshiCampMecha, s => s.Name == "ShopSprite");
            var ikuSprite = TenshiGlobals.FindFirstNode<Sprite2D>(tenshiCampMecha, s => s.Name == "IkuSprite");
            var animPlayer = TenshiGlobals.FindFirstNode<AnimationPlayer>(tenshiCampMecha);

            if (combatSprite != null)
            {
                combatSprite.Visible = false;
            }

            if (shopSprite != null)
            {
                shopSprite.Visible = true;
                if (needsFlip)
                {
                    shopSprite.FlipH = !shopSprite.FlipH;
                }
            }

            if (ikuSprite != null)
            {
                ikuSprite.Visible = true;
                if (needsFlip)
                {
                    ikuSprite.FlipH = !ikuSprite.FlipH;
                }
            }

            if (animPlayer != null)
            {
                animPlayer.Play("Campfire_Idle");
            }
        }
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Rooms.NRestSiteRoom), "OnProceedButtonReleased")]
internal static class NRestSiteRoom_Exit_Patch
{
    private static void Prefix()
    {
        GD.Print("\n====== 侦测到玩家点击前进！极其优雅地摘除篝火物理锁！ ======");
        TenshiGlobals.IsInShop = false;
    }
}
