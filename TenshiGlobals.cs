using System;
using System.Collections.Generic;
using Godot;

namespace TenshiMod.Scripts; // 保持和你原来的命名空间一致

// 🚨 天子专属赛博数据中枢：所有全局状态、常量和弹药库全在这里！
public static class TenshiGlobals
{
    // ==========================================
    // 1. 动态状态监视器
    // ==========================================
    public static bool IsInShop = false;
    public static bool IsDead = false;
    
    // 联机卫星阵列：记录所有在场的天子
    public static HashSet<AnimatedSprite2D> ActiveTenshiSprites = new();

    // 默认关闭高频日志，避免热路径频繁字符串拼接和输出。
    public static bool EnableVerboseLog = false;

    // ==========================================
    // 2. 核心系统常量
    // ==========================================
    public const string TargetCharacterId = "IRONCLAD";
    public const string TenshiScenePath = "res://mods/TenshiHinanawi/visuals/TenshiVisuals.tscn";
    public const string HarmonyId = "sts2.tenshi.pixelreplacement";

    // ==========================================
    // 3. 全局随机数发生器
    // ==========================================
    public static readonly Random Rng = new Random();

    // 全局场景缓存：由 Entry.Init 预加载，Patch 中可直接复用。
    public static PackedScene? TenshiScene;

    private static readonly Dictionary<string, AudioStream> AudioStreamCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PackedScene> PackedSceneCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Texture2D> TextureCache = new(StringComparer.Ordinal);
    private static readonly Dictionary<Node2D, MechaComponents> MechaComponentsCache = new();

    // ==========================================
    // 4. 动作动画池
    // ==========================================
    public static readonly string[] AttackPool = { "Attack_1", "Attack_2", "Attack_3" };
    public static readonly string[] HitPool = { "Hit_1", "Hit_2", "Hit_3" };
    public static readonly string[] CastPool = { "Cast_1", "Cast_2", "Cast_3" };

    // ==========================================
    // 5. 语音光盘库
    // ==========================================
    public static readonly string[] IntroVoicePool = { 
        "res://mods/TenshiHinanawi/audio/Vo_ready_tenshi.wav", 
        "res://mods/TenshiHinanawi/audio/Vo_go_tenshi.wav" 
    };

    public static readonly string[] VictoryVoicePool = { 
        "res://mods/TenshiHinanawi/audio/Vo_tedium_tenshi.wav" 
    };

    public static readonly string[] AttackVoicePool = { 
        "res://mods/TenshiHinanawi/audio/Vo_attack_a_tenshi.wav", 
        "res://mods/TenshiHinanawi/audio/Vo_attack_b_tenshi.wav",
        "res://mods/TenshiHinanawi/audio/Vo_attack_s_tenshi.wav" 
    };

    public static readonly string[] HitVoicePool = { 
        "res://mods/TenshiHinanawi/audio/Vo_damage_s1_tenshi.wav", 
        "res://mods/TenshiHinanawi/audio/Vo_damage_s2_tenshi.wav" 
    };

    public static readonly string[] CastVoicePool = { 
        "res://mods/TenshiHinanawi/audio/Vo_spell_a_tenshi.wav", 
        "res://mods/TenshiHinanawi/audio/Vo_hightension_tenshi.wav" 
    };

    private sealed class MechaComponents
    {
        public required AnimatedSprite2D Sprite { get; init; }
        public AudioStreamPlayer2D? Voice { get; init; }
    }

    public static void Log(string message)
    {
        if (EnableVerboseLog)
        {
            GD.Print(message);
        }
    }

    public static AudioStream? GetAudioStream(string path)
    {
        if (AudioStreamCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var loaded = ResourceLoader.Load<AudioStream>(path);
        if (loaded != null)
        {
            AudioStreamCache[path] = loaded;
        }

        return loaded;
    }

    public static PackedScene? GetPackedScene(string path)
    {
        if (PackedSceneCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var loaded = ResourceLoader.Load<PackedScene>(path);
        if (loaded != null)
        {
            PackedSceneCache[path] = loaded;
        }

        return loaded;
    }

    public static Texture2D? GetTexture(string path)
    {
        if (TextureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var loaded = ResourceLoader.Load<Texture2D>(path);
        if (loaded != null)
        {
            TextureCache[path] = loaded;
        }

        return loaded;
    }

    public static void RegisterMecha(Node2D mechaRoot)
    {
        if (!GodotObject.IsInstanceValid(mechaRoot))
        {
            return;
        }

        var sprite = FindFirstNode<AnimatedSprite2D>(mechaRoot);
        if (sprite == null)
        {
            return;
        }

        var voice = FindFirstNode<AudioStreamPlayer2D>(mechaRoot, n => n.Name == "TenshiVoice");
        MechaComponentsCache[mechaRoot] = new MechaComponents
        {
            Sprite = sprite,
            Voice = voice,
        };
    }

    public static bool TryGetMechaComponents(Node2D mechaRoot, out AnimatedSprite2D? sprite, out AudioStreamPlayer2D? voice)
    {
        sprite = null;
        voice = null;

        if (!GodotObject.IsInstanceValid(mechaRoot))
        {
            return false;
        }

        if (MechaComponentsCache.TryGetValue(mechaRoot, out var cached)
            && GodotObject.IsInstanceValid(cached.Sprite)
            && (cached.Voice == null || GodotObject.IsInstanceValid(cached.Voice)))
        {
            sprite = cached.Sprite;
            voice = cached.Voice;
            return true;
        }

        RegisterMecha(mechaRoot);
        if (MechaComponentsCache.TryGetValue(mechaRoot, out cached) && GodotObject.IsInstanceValid(cached.Sprite))
        {
            sprite = cached.Sprite;
            voice = cached.Voice;
            return true;
        }

        MechaComponentsCache.Remove(mechaRoot);
        return false;
    }

    public static void CleanupSpriteRegistry()
    {
        ActiveTenshiSprites.RemoveWhere(s => !GodotObject.IsInstanceValid(s));
    }

    public static T? FindFirstNode<T>(Node root, Func<T, bool>? predicate = null) where T : Node
    {
        var queue = new System.Collections.Generic.Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is T matched && (predicate == null || predicate(matched)))
            {
                return matched;
            }

            for (int i = 0; i < current.GetChildCount(); i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }

    public static string? GetCharacterEntry(object? model)
    {
        if (model == null)
        {
            return null;
        }

        var idObj = HarmonyLib.Traverse.Create(model).Property("Id").GetValue()
            ?? HarmonyLib.Traverse.Create(model).Field("Id").GetValue();

        return HarmonyLib.Traverse.Create(idObj).Property("Entry").GetValue<string>()
            ?? HarmonyLib.Traverse.Create(idObj).Field("Entry").GetValue<string>();
    }
}