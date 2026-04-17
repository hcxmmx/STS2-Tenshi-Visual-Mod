using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TenshiMod.Scripts;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class NCreature_Ready_Patch
{
    private static void Postfix(NCreature __instance)
    {
        TenshiGlobals.IsDead = false;
        TenshiGlobals.IsInShop = false;
        TenshiGlobals.Log($"\n---> Hinana Tenshi Project: 侦测到 NCreature 试图活化！节点名称 = {__instance.Name} <---");

        var scene = TenshiGlobals.TenshiScene ?? TenshiGlobals.GetPackedScene(TenshiGlobals.TenshiScenePath);
        TenshiGlobals.TenshiScene = scene;

        if (scene == null)
        {
            GD.PrintErr("拦截：天子图纸为空！");
            return;
        }

        if (__instance.Entity == null)
        {
            TenshiGlobals.Log("拦截：Entity 为空！_Ready 阶段灵魂尚未注入！");
            return;
        }

        var player = __instance.Entity.Player;
        if (player == null)
        {
            TenshiGlobals.Log("拦截：这是一个怪物，不是玩家！");
            return;
        }

        if (!string.Equals(player.Character?.Id?.Entry, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            TenshiGlobals.Log($"拦截：角色不匹配，当前角色是 {player.Character?.Id?.Entry}");
            return;
        }

        var visuals = __instance.Visuals;
        if (visuals == null)
        {
            TenshiGlobals.Log("拦截：Visuals 视觉中枢为空！");
            return;
        }

        TenshiGlobals.Log("====== 突破所有防线！强行挂载天子！ ======");
        // 极其优雅的底层节点抓取，完美规避官方测试版私有化 Body 的背刺
        var originalBody = visuals.GetNodeOrNull<Node2D>("%Visuals");
        originalBody?.Hide();

        var tenshiNode = scene.Instantiate<Node2D>();
        if (tenshiNode == null)
        {
            return;
        }

        tenshiNode.Name = "TenshiMecha";
        visuals.AddChild(tenshiNode);
        tenshiNode.Position = new Vector2(0, -160f);
        tenshiNode.Scale = new Vector2(3.0f, 3.0f);
        tenshiNode.Visible = true;

        TenshiGlobals.RegisterMecha(tenshiNode);
        _ = TenshiGlobals.TryGetMechaComponents(tenshiNode, out var tenshiSprite, out var tenshiVoice);

        if (tenshiSprite != null)
        {
            TenshiGlobals.CleanupSpriteRegistry();
            TenshiGlobals.ActiveTenshiSprites.Add(tenshiSprite);

            tenshiSprite.AnimationFinished += () =>
            {
                if (tenshiSprite.Animation != "Idle" && tenshiSprite.Animation != "Die" && tenshiSprite.Animation != "Victory")
                {
                    tenshiSprite.Play("Idle");
                    tenshiNode.Position = new Vector2(0, -160f);
                }
            };

            tenshiSprite.Play("Intro");

            if (tenshiVoice != null)
            {
                string chosenIntroVoice = TenshiGlobals.IntroVoicePool[TenshiGlobals.Rng.Next(TenshiGlobals.IntroVoicePool.Length)];
                tenshiVoice.Stream = TenshiGlobals.GetAudioStream(chosenIntroVoice);
                tenshiVoice.Play();
                TenshiGlobals.Log($"📢 入场播报：极其傲娇地播放了 {chosenIntroVoice} !");
            }
        }

        var syncTimer = new Godot.Timer();
        syncTimer.Name = "TenshiDirectionRadar";
        syncTimer.WaitTime = 0.05f;
        syncTimer.Autostart = true;
        tenshiNode.AddChild(syncTimer);

        // 雷达系统的目标也必须换成底层抓取！
        Node2D? bodyRef = visuals.GetNodeOrNull<Node2D>("%Visuals");
        Node2D? tenshiRef = tenshiNode;

        syncTimer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(bodyRef) && GodotObject.IsInstanceValid(tenshiRef))
            {
                float targetSign = Mathf.Sign(bodyRef.Scale.X);
                float currentSign = Mathf.Sign(tenshiRef.Scale.X);

                if (targetSign != currentSign && targetSign != 0)
                {
                    float absX = Mathf.Abs(tenshiRef.Scale.X);
                    tenshiRef.Scale = new Vector2(absX * targetSign, tenshiRef.Scale.Y);
                }
            }
        };

        TenshiGlobals.Log("天子物理矫正完毕！");
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
internal static class NCreature_SetAnimationTrigger_Patch
{
    private static void Postfix(NCreature __instance, string trigger)
    {
        if (TenshiGlobals.IsInShop)
        {
            return;
        }

        if (TenshiGlobals.IsDead)
        {
            TenshiGlobals.Log($"🔒 拦截死后诈尸信号：{trigger} 被物理屏蔽！");
            return;
        }

        var player = __instance?.Entity?.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var visuals = __instance?.Visuals;
        if (visuals == null)
        {
            return;
        }

        var tenshiMecha = visuals.GetNodeOrNull<Node2D>("TenshiMecha");
        if (tenshiMecha == null)
        {
            return;
        }

        if (!TenshiGlobals.TryGetMechaComponents(tenshiMecha, out var tenshiSprite, out var tenshiVoice) || tenshiSprite == null)
        {
            return;
        }

        TenshiGlobals.Log($"---> Hinana Tenshi Project: 收到动作指令: {trigger} <---");

        switch (trigger)
        {
            case "Attack":
            case "AttackSingle":
            case "AttackTriple":
            {
                string chosenAttack = TenshiGlobals.AttackPool[TenshiGlobals.Rng.Next(TenshiGlobals.AttackPool.Length)];
                string chosenAttackVoice = TenshiGlobals.AttackVoicePool[TenshiGlobals.Rng.Next(TenshiGlobals.AttackVoicePool.Length)];
                TenshiGlobals.Log($"极其华丽地抽中了: {chosenAttack} !");

                tenshiSprite.Stop();
                tenshiSprite.Play(chosenAttack);

                if (tenshiVoice != null)
                {
                    tenshiVoice.Stream = TenshiGlobals.GetAudioStream(chosenAttackVoice);
                    tenshiVoice.Play();
                }

                tenshiMecha.Position = new Vector2(0, -160f);
                break;
            }
            case "Hit":
            {
                string chosenHit = TenshiGlobals.HitPool[TenshiGlobals.Rng.Next(TenshiGlobals.HitPool.Length)];
                string chosenHitVoice = TenshiGlobals.HitVoicePool[TenshiGlobals.Rng.Next(TenshiGlobals.HitVoicePool.Length)];
                TenshiGlobals.Log($"极其心疼地触发了受击: {chosenHit}");

                tenshiSprite.Stop();
                tenshiSprite.Play(chosenHit);

                if (tenshiVoice != null)
                {
                    tenshiVoice.Stream = TenshiGlobals.GetAudioStream(chosenHitVoice);
                    tenshiVoice.Play();
                }

                tenshiMecha.Position = new Vector2(0, -160f);
                break;
            }
            case "Cast":
            {
                string chosenCast = TenshiGlobals.CastPool[TenshiGlobals.Rng.Next(TenshiGlobals.CastPool.Length)];
                string chosenCastVoice = TenshiGlobals.CastVoicePool[TenshiGlobals.Rng.Next(TenshiGlobals.CastVoicePool.Length)];
                TenshiGlobals.Log($"极其华丽地触发了施法: {chosenCast}");

                tenshiSprite.Stop();
                tenshiSprite.Play(chosenCast);

                if (tenshiVoice != null)
                {
                    tenshiVoice.Stream = TenshiGlobals.GetAudioStream(chosenCastVoice);
                    tenshiVoice.Play();
                }

                tenshiMecha.Position = new Vector2(0, -160f);
                break;
            }
            case "Die":
            case "Death":
            case "Dead":
                TenshiGlobals.IsDead = true;
                tenshiSprite.Stop();
                tenshiSprite.Play("Die");
                tenshiMecha.Position = new Vector2(0, -160f);
                break;
            default:
                if (tenshiSprite.Animation != "Idle")
                {
                    tenshiSprite.Play("Idle");
                }
                tenshiMecha.Position = new Vector2(0, -160f);
                break;
        }
    }
}

[HarmonyPatch(typeof(NCreature), "AnimDie")]
internal static class NCreature_AnimDie_Patch
{
    private static void Prefix(NCreature __instance)
    {
        var player = __instance.Entity?.Player;
        if (player == null || !string.Equals(player.Character?.Id?.Entry, TenshiGlobals.TargetCharacterId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var visuals = __instance.Visuals;
        if (visuals == null)
        {
            return;
        }

        var tenshiMecha = visuals.GetNodeOrNull<Node2D>("TenshiMecha");
        if (tenshiMecha == null)
        {
            return;
        }

        if (!TenshiGlobals.TryGetMechaComponents(tenshiMecha, out var tenshiSprite, out _) || tenshiSprite == null)
        {
            return;
        }

        TenshiGlobals.Log("---> Hinana Tenshi Project: 侦测到死亡信号！执行安息协议！ <---");
        TenshiGlobals.IsDead = true;
        tenshiSprite.Stop();
        tenshiSprite.Play("Die");
        tenshiMecha.Position = new Vector2(0, -160f);
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "EndCombatInternal")]
internal static class CombatManager_EndCombatInternal_Patch
{
    private static void Prefix()
    {
        TenshiGlobals.Log("\n====== 🏆 侦测到底层宣布战斗结束！通过卫星阵列呼叫全体天子！ ======");

        TenshiGlobals.CleanupSpriteRegistry();

        if (TenshiGlobals.ActiveTenshiSprites.Count <= 0)
        {
            GD.PrintErr("💥 卫星呼叫失败：雷达阵列中找不到任何存活的天子机甲！");
            return;
        }

        foreach (var sprite in TenshiGlobals.ActiveTenshiSprites)
        {
            sprite.Stop();
            sprite.Play("Victory");

            var mechaNode = sprite.GetParent();
            AudioStreamPlayer2D? voiceNode = null;
            if (mechaNode is Node2D mechaRoot)
            {
                _ = TenshiGlobals.TryGetMechaComponents(mechaRoot, out _, out voiceNode);
            }

            if (voiceNode != null)
            {
                string chosenVictoryVoice = TenshiGlobals.VictoryVoicePool[TenshiGlobals.Rng.Next(TenshiGlobals.VictoryVoicePool.Length)];
                voiceNode.Stream = TenshiGlobals.GetAudioStream(chosenVictoryVoice);
                voiceNode.Play();
            }
        }

        TenshiGlobals.Log($"🎉 全局广播成功：共 {TenshiGlobals.ActiveTenshiSprites.Count} 名天子极其傲娇地宣布了胜利！");
    }
}
