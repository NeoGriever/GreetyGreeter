using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using GreetyGreeter.Models;

namespace GreetyGreeter;

public enum ZoneShape { Sphere, Cube }

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public ZoneShape Shape = ZoneShape.Sphere;
    public Vector3 ZoneCenter = Vector3.Zero;
    public float SphereRadius = 5f;
    public float BoxWidth = 10f;
    public float BoxHeight = 5f;
    public float BoxDepth = 10f;
    public float BoxRotationDegrees = 0f;
    public bool ZoneEnabled = false;
    public bool ShowZoneVisual = true;
    public bool ShowRootBoneIndicators = true;

    public int CooldownMinutes = 60;
    public bool TargetPlayerDuringExecution = false;

    public List<GreetingPreset> Presets = new();
    public string ActivePresetId = string.Empty;

    public List<string> NameIgnoreList = new();
    public List<ScheduleEntry> Schedules = new();

    // Legacy field – kept for migration only, do not use directly
    public List<string> NameBlacklist = new();

    public void MigrateLegacyData()
    {
        if (NameBlacklist.Count == 0) return;
        foreach (var name in NameBlacklist)
        {
            if (!NameIgnoreList.Exists(n => n.Equals(name, System.StringComparison.OrdinalIgnoreCase)))
                NameIgnoreList.Add(name);
        }
        NameBlacklist.Clear();
        Save();
    }

    public Dictionary<string, GreetedPlayer> PlayerMemory = new();
    public int TotalGreetedCount = 0;
    public List<RecentGreet> RecentGreets = new();

    public int Language = 0;

    // Zone rendering colors
    public Vector4 ColorZoneXZ   = new(0.2f, 0.9f, 0.3f, 0.8f);
    public Vector4 ColorZoneXY   = new(0.3f, 0.5f, 1.0f, 0.8f);
    public Vector4 ColorZoneYZ   = new(1.0f, 0.3f, 0.3f, 0.8f);
    public Vector4 ColorZoneCube = new(0.3f, 0.9f, 0.7f, 0.8f);

    // Player list colors
    public Vector4 ColorGreeting     = new(0.2f, 0.85f, 0.3f, 1f);
    public Vector4 ColorGreeted      = new(0.6f, 0.6f, 0.6f, 1f);
    public Vector4 ColorAgainToGreet = new(1.0f, 0.6f, 0.2f, 1f);

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
