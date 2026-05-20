using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using GreetyGreeter.Logic;
using Dalamud.Bindings.ImGui;

namespace GreetyGreeter.Windows;

public class MainWindow : Window
{
    private readonly Configuration _config;
    private readonly SettingsWindow _settings;
    private readonly GreetExecutor _executor;

    private enum PlayerStatus { Greeting, AgainToGreet, Greeted }

    public MainWindow(Configuration config, SettingsWindow settings, GreetExecutor executor)
        : base("Greety Greeter###GreetyGreeterMain")
    {
        _config = config;
        _settings = settings;
        _executor = executor;
        Size = new Vector2(300, 180);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public override void Draw()
    {
        var enabledColor = _config.ZoneEnabled
            ? new Vector4(0.2f, 0.85f, 0.3f, 1f)
            : new Vector4(0.85f, 0.25f, 0.25f, 1f);

        float totalWidth = ImGui.GetContentRegionAvail().X;
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        ImGui.Text($"{Lang.T("greeted")}: {_config.TotalGreetedCount:N0}");

        float btnRight = totalWidth - (spacing + 18f) * 2;
        ImGui.SameLine(btnRight);

        if (ImGui.SmallButton($"{FontAwesomeIcon.Cogs.ToIconString()}##settings"))
            _settings.Toggle();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Lang.T("settings"));

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, enabledColor);
        var toggleIcon = _config.ZoneEnabled ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Circle;
        if (ImGui.SmallButton($"{toggleIcon.ToIconString()}##t"))
        {
            _config.ZoneEnabled = !_config.ZoneEnabled;
            _config.Save();
        }
        ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(_config.ZoneEnabled ? Lang.T("zoneEnabled") : Lang.T("zoneDisabled"));

        ImGui.Separator();

        if (_config.PlayerMemory.Count == 0)
        {
            ImGui.TextDisabled("  —");
            return;
        }

        var now = DateTime.Now;

        var entries = _config.PlayerMemory.Values
            .OrderByDescending(p => p.LastSeen)
            .Take(10)
            .ToList();

        foreach (var entry in entries)
        {
            var key = entry.FullKey;
            var status = DetermineStatus(key, entry, now);
            var color = status switch
            {
                PlayerStatus.Greeting     => _config.ColorGreeting,
                PlayerStatus.AgainToGreet => _config.ColorAgainToGreet,
                _                         => _config.ColorGreeted
            };

            var elapsed = now - entry.LastSeen;
            string timeStr = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}";

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text($" {timeStr}  {entry.Name}");
            ImGui.PopStyleColor();
        }
    }

    private PlayerStatus DetermineStatus(string key, Models.GreetedPlayer entry, DateTime now)
    {
        if (_executor.IsCurrentlyGreeting(key))
            return PlayerStatus.Greeting;
        if (_executor.IsQueued(key))
            return PlayerStatus.AgainToGreet;
        if ((now - entry.LastSeen).TotalMinutes >= _config.CooldownMinutes)
            return PlayerStatus.AgainToGreet;
        return PlayerStatus.Greeted;
    }
}
