using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using GreetyGreeter.Logic;
using GreetyGreeter.Models;
using Dalamud.Bindings.ImGui;

namespace GreetyGreeter.Windows;

public class SettingsWindow : Window
{
    private readonly Configuration _config;
    private readonly ZoneDetector _detector;
    private readonly ZoneRenderer _renderer;

    public bool IsZoneTabActive { get; private set; } = false;

    private string _renameBuffer = string.Empty;
    private bool _renaming = false;
    private string _selectedPresetId = string.Empty;

    private string _ignoreListInput = string.Empty;

    private static readonly string[] LanguageNames = { "English", "Deutsch", "Français", "Русский", "日本語" };

    public SettingsWindow(Configuration config, ZoneDetector detector, ZoneRenderer renderer)
        : base("Greety Greeter — Settings###GreetyGreeterSettings")
    {
        _config = config;
        _detector = detector;
        _renderer = renderer;
        Size = new Vector2(580, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        IsZoneTabActive = false;

        if (!ImGui.BeginTabBar("##GGTabs")) return;

        DrawTabZone();
        DrawTabGreet();
        DrawTabPresets();
        DrawTabMemory();
        DrawTabIgnoreList();
        DrawTabSchedule();
        DrawTabColors();
        DrawTabLanguage();

        ImGui.EndTabBar();
    }

    private void DrawTabZone()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabZone"))) return;

        IsZoneTabActive = true;

        ImGui.Spacing();

        var shape = (int)_config.Shape;
        var shapeNames = new[] { Lang.T("sphere"), Lang.T("cube") };
        ImGui.Text(Lang.T("shape"));
        ImGui.SameLine();
        ImGui.SetNextItemWidth(130);
        if (ImGui.Combo("##shape", ref shape, shapeNames, shapeNames.Length))
        {
            _config.Shape = (ZoneShape)shape;
            _detector.InvalidateCache();
            _config.Save();
        }

        ImGui.Spacing();

        if (_config.Shape == ZoneShape.Sphere)
        {
            var r = _config.SphereRadius;
            ImGui.SetNextItemWidth(220);
            if (ImGui.SliderFloat(Lang.T("radius"), ref r, 0.5f, 50f, "%.1f yalm"))
            {
                _config.SphereRadius = r;
                _detector.InvalidateCache();
                _config.Save();
            }
        }
        else
        {
            var w = _config.BoxWidth;
            var h = _config.BoxHeight;
            var d = _config.BoxDepth;
            var rot = _config.BoxRotationDegrees;

            ImGui.SetNextItemWidth(220);
            if (ImGui.DragFloat(Lang.T("width"), ref w, 0.1f, 0.5f, 200f, "%.1f"))
            { _config.BoxWidth = w; _detector.InvalidateCache(); _config.Save(); }

            ImGui.SetNextItemWidth(220);
            if (ImGui.DragFloat(Lang.T("height"), ref h, 0.1f, 0.5f, 200f, "%.1f"))
            { _config.BoxHeight = h; _detector.InvalidateCache(); _config.Save(); }

            ImGui.SetNextItemWidth(220);
            if (ImGui.DragFloat(Lang.T("depth"), ref d, 0.1f, 0.5f, 200f, "%.1f"))
            { _config.BoxDepth = d; _detector.InvalidateCache(); _config.Save(); }

            ImGui.SetNextItemWidth(220);
            if (ImGui.DragFloat(Lang.T("rotation"), ref rot, 0.5f, -360f, 360f, "%.1f°"))
            { _config.BoxRotationDegrees = rot; _detector.InvalidateCache(); _config.Save(); }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted(Lang.T("zoneCenter"));

        var cx = _config.ZoneCenter.X;
        var cy = _config.ZoneCenter.Y;
        var cz = _config.ZoneCenter.Z;

        ImGui.SetNextItemWidth(120);
        if (ImGui.DragFloat("X##cx", ref cx, 0.05f, -10000f, 10000f, "%.2f"))
        { _config.ZoneCenter = new Vector3(cx, _config.ZoneCenter.Y, _config.ZoneCenter.Z); _detector.InvalidateCache(); _config.Save(); }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.DragFloat("Y##cy", ref cy, 0.05f, -10000f, 10000f, "%.2f"))
        { _config.ZoneCenter = new Vector3(_config.ZoneCenter.X, cy, _config.ZoneCenter.Z); _detector.InvalidateCache(); _config.Save(); }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.DragFloat("Z##cz", ref cz, 0.05f, -10000f, 10000f, "%.2f"))
        { _config.ZoneCenter = new Vector3(_config.ZoneCenter.X, _config.ZoneCenter.Y, cz); _detector.InvalidateCache(); _config.Save(); }

        ImGui.SameLine();
        if (ImGui.Button(Lang.T("capturePos")))
        {
            var local = Plugin.ObjectTable.LocalPlayer;
            if (local != null)
            {
                _config.ZoneCenter = local.Position;
                _detector.InvalidateCache();
                _config.Save();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var showVisual = _config.ShowZoneVisual;
        if (ImGui.Checkbox(Lang.T("showVisual"), ref showVisual))
        { _config.ShowZoneVisual = showVisual; _config.Save(); }

        var showRootBone = _config.ShowRootBoneIndicators;
        if (ImGui.Checkbox(Lang.T("showRootBone"), ref showRootBone))
        { _config.ShowRootBoneIndicators = showRootBone; _config.Save(); }

        var enabled = _config.ZoneEnabled;
        if (ImGui.Checkbox(Lang.T("zoneEnabled"), ref enabled))
        { _config.ZoneEnabled = enabled; _config.Save(); }

        ImGui.EndTabItem();
    }

    private void DrawTabGreet()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabGreet"))) return;

        ImGui.Spacing();

        var cooldown = _config.CooldownMinutes;
        int hours = cooldown / 60;
        int mins = cooldown % 60;
        ImGui.Text(Lang.T("cooldown"));
        ImGui.SameLine();
        ImGui.TextDisabled($"({hours:D2}:{mins:D2})");
        ImGui.SetNextItemWidth(300);
        if (ImGui.SliderInt("##cooldown", ref cooldown, 1, 1440))
        { _config.CooldownMinutes = cooldown; _config.Save(); }
        ImGui.TextDisabled(Lang.T("cooldownHint"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var target = _config.TargetPlayerDuringExecution;
        if (ImGui.Checkbox(Lang.T("targetPlayer"), ref target))
        { _config.TargetPlayerDuringExecution = target; _config.Save(); }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text(Lang.T("activePreset"));
        ImGui.SameLine();

        var presetIds = _config.Presets.OrderBy(p => p.SortOrder).Select(p => p.Id).ToArray();
        var displayNames = new string[presetIds.Length + 1];
        displayNames[0] = Lang.T("noPreset");
        for (int i = 0; i < presetIds.Length; i++)
            displayNames[i + 1] = _config.Presets.First(p => p.Id == presetIds[i]).Name;

        int activeIdx = Array.IndexOf(presetIds, _config.ActivePresetId) + 1;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("##activePreset", ref activeIdx, displayNames, displayNames.Length))
        { _config.ActivePresetId = activeIdx == 0 ? string.Empty : presetIds[activeIdx - 1]; _config.Save(); }

        ImGui.Spacing();
        ImGui.TextDisabled(Lang.T("tokenHint"));

        ImGui.EndTabItem();
    }

    private void DrawTabPresets()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabPresets"))) return;

        ImGui.Spacing();

        var sorted = _config.Presets.OrderBy(p => p.SortOrder).ToList();

        if (ImGui.Button(Lang.T("addPreset")))
        {
            var p = new GreetingPreset { Name = "New Preset", SortOrder = _config.Presets.Count };
            _config.Presets.Add(p);
            _selectedPresetId = p.Id;
            _config.Save();
        }

        ImGui.Spacing();

        float listW = 170f;
        if (ImGui.BeginChild("##presetList", new Vector2(listW, 0), true))
        {
            foreach (var preset in sorted)
            {
                bool selected = preset.Id == _selectedPresetId;
                if (ImGui.Selectable(preset.Name + "##" + preset.Id, selected))
                    _selectedPresetId = preset.Id;
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();

        var current = _config.Presets.Find(p => p.Id == _selectedPresetId);
        if (current != null && ImGui.BeginChild("##presetEdit", new Vector2(0, 0), true))
        {
            if (_renaming)
            {
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText("##rename", ref _renameBuffer, 64, ImGuiInputTextFlags.EnterReturnsTrue))
                { current.Name = _renameBuffer; _renaming = false; _config.Save(); }
                ImGui.SameLine();
                if (ImGui.SmallButton("OK"))
                { current.Name = _renameBuffer; _renaming = false; _config.Save(); }
            }
            else
            {
                ImGui.Text(current.Name);
                ImGui.SameLine();
                if (ImGui.SmallButton(Lang.T("renamePreset")))
                { _renameBuffer = current.Name; _renaming = true; }
                ImGui.SameLine();
                if (ImGui.SmallButton(Lang.T("moveUp")))
                {
                    var idx = sorted.IndexOf(current);
                    if (idx > 0) { sorted[idx].SortOrder = idx - 1; sorted[idx - 1].SortOrder = idx; _config.Save(); }
                }
                ImGui.SameLine();
                if (ImGui.SmallButton(Lang.T("moveDown")))
                {
                    var idx = sorted.IndexOf(current);
                    if (idx < sorted.Count - 1) { sorted[idx].SortOrder = idx + 1; sorted[idx + 1].SortOrder = idx; _config.Save(); }
                }
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
                if (ImGui.SmallButton(Lang.T("deletePreset") + "##del") && ImGui.GetIO().KeyCtrl)
                {
                    if (_config.ActivePresetId == current.Id) _config.ActivePresetId = string.Empty;
                    _config.Presets.Remove(current);
                    _selectedPresetId = string.Empty;
                    _config.Save();
                    ImGui.PopStyleColor();
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                    return;
                }
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.T("deleteHint"));
            }

            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled(Lang.T("tokenHint"));
            ImGui.Spacing();

            int removeIdx = -1, moveUpIdx = -1, moveDnIdx = -1;

            for (int i = 0; i < current.Lines.Count; i++)
            {
                var line = current.Lines[i];
                ImGui.PushID(i);

                bool isTell = line.Text.Contains("/tell", StringComparison.OrdinalIgnoreCase)
                           || line.Text.Contains("/t ", StringComparison.OrdinalIgnoreCase);

                ImGui.SetNextItemWidth(270);
                var txt = line.Text;
                if (ImGui.InputText("##lt", ref txt, 512))
                {
                    line.Text = txt;
                    bool nowTell = line.Text.Contains("/tell", StringComparison.OrdinalIgnoreCase)
                                || line.Text.Contains("/t ", StringComparison.OrdinalIgnoreCase);
                    if (nowTell)
                        line.Delay = Math.Max(1.1f, line.Delay);
                    _config.Save();
                }
                if (isTell)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled(FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.T("tellHint"));
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(70);
                var delay = line.Delay;
                if (ImGui.DragFloat("##ld", ref delay, 0.1f, isTell ? 1.1f : 0f, 60f, "%.1fs"))
                { line.Delay = isTell ? Math.Max(1.1f, delay) : delay; _config.Save(); }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.T("lineDelay"));

                ImGui.SameLine();
                if (ImGui.SmallButton(FontAwesomeIcon.ArrowUp.ToIconString())) moveUpIdx = i;
                ImGui.SameLine();
                if (ImGui.SmallButton(FontAwesomeIcon.ArrowDown.ToIconString())) moveDnIdx = i;
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
                if (ImGui.SmallButton(FontAwesomeIcon.Times.ToIconString())) removeIdx = i;
                ImGui.PopStyleColor();

                ImGui.PopID();
            }

            if (removeIdx >= 0) { current.Lines.RemoveAt(removeIdx); _config.Save(); }
            if (moveUpIdx > 0) { (current.Lines[moveUpIdx], current.Lines[moveUpIdx - 1]) = (current.Lines[moveUpIdx - 1], current.Lines[moveUpIdx]); _config.Save(); }
            if (moveDnIdx >= 0 && moveDnIdx < current.Lines.Count - 1) { (current.Lines[moveDnIdx], current.Lines[moveDnIdx + 1]) = (current.Lines[moveDnIdx + 1], current.Lines[moveDnIdx]); _config.Save(); }

            if (current.Lines.Count == 0) ImGui.TextDisabled(Lang.T("noLines"));

            ImGui.Spacing();
            if (ImGui.Button(Lang.T("addLine")))
            { current.Lines.Add(new CommandLine()); _config.Save(); }
        }
        if (current != null) ImGui.EndChild();

        ImGui.EndTabItem();
    }

    private void DrawTabMemory()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabMemory"))) return;

        ImGui.Spacing();

        ImGui.Text($"{Lang.T("totalGreeted")}: {_config.TotalGreetedCount:N0}");
        ImGui.SameLine();
        if (ImGui.SmallButton(Lang.T("resetCount") + "##rt") && ImGui.GetIO().KeyShift)
        { _config.TotalGreetedCount = 0; _config.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.T("resetHint"));

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
        if (ImGui.Button(Lang.T("clearMemory") + "##ca") && ImGui.GetIO().KeyCtrl)
        { _config.PlayerMemory.Clear(); _config.RecentGreets.Clear(); _config.Save(); }
        ImGui.PopStyleColor();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.T("clearHint"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_config.PlayerMemory.Count == 0)
        {
            ImGui.TextDisabled(Lang.T("noMemory"));
        }
        else
        {
            if (ImGui.BeginTable("##mt", 5,
                ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg,
                new Vector2(0, ImGui.GetContentRegionAvail().Y - 10)))
            {
                ImGui.TableSetupColumn(Lang.T("memColName"),  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(Lang.T("memColWorld"), ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn(Lang.T("memColFirst"), ImGuiTableColumnFlags.WidthFixed, 110);
                ImGui.TableSetupColumn(Lang.T("memColLast"),  ImGuiTableColumnFlags.WidthFixed, 110);
                ImGui.TableSetupColumn("##del",               ImGuiTableColumnFlags.WidthFixed, 26);
                ImGui.TableHeadersRow();

                string? toRemove = null;
                foreach (var (key, entry) in _config.PlayerMemory)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.Name);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.World);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.FirstSeen.ToString("MM-dd HH:mm"));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(entry.LastSeen.ToString("MM-dd HH:mm"));
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
                    if (ImGui.SmallButton(FontAwesomeIcon.Times.ToIconString() + "##" + key) && ImGui.GetIO().KeyCtrl) toRemove = key;
                    ImGui.PopStyleColor();
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.T("deleteHint"));
                }
                if (toRemove != null) { _config.PlayerMemory.Remove(toRemove); _config.Save(); }

                ImGui.EndTable();
            }
        }

        ImGui.EndTabItem();
    }

    private void DrawTabIgnoreList()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabIgnoreList"))) return;

        ImGui.Spacing();
        ImGui.TextDisabled(Lang.T("ignoreListHint"));
        ImGui.Spacing();

        ImGui.SetNextItemWidth(240);
        bool entered = ImGui.InputTextWithHint("##ilInput", Lang.T("ignoreListInputHint"),
            ref _ignoreListInput, 64, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();

        bool canAdd = !string.IsNullOrWhiteSpace(_ignoreListInput)
            && !_config.NameIgnoreList.Exists(n => n.Equals(_ignoreListInput.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!canAdd) ImGui.BeginDisabled();
        if ((ImGui.Button(Lang.T("ignoreListAdd")) || entered) && canAdd)
        {
            _config.NameIgnoreList.Add(_ignoreListInput.Trim());
            _ignoreListInput = string.Empty;
            _config.Save();
        }
        if (!canAdd) ImGui.EndDisabled();

        ImGui.Separator();
        ImGui.Spacing();

        float listH = ImGui.GetContentRegionAvail().Y - 8;
        if (ImGui.BeginChild("##ilScroll", new Vector2(0, listH), false))
        {
            if (_config.NameIgnoreList.Count == 0)
            {
                ImGui.TextDisabled(Lang.T("ignoreListEmpty"));
            }
            else
            {
                int removeIdx = -1;
                for (int i = 0; i < _config.NameIgnoreList.Count; i++)
                {
                    ImGui.PushID(i);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
                    if (ImGui.SmallButton(FontAwesomeIcon.Times.ToIconString())) removeIdx = i;
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.TextUnformatted(_config.NameIgnoreList[i]);
                    ImGui.PopID();
                }
                if (removeIdx >= 0) { _config.NameIgnoreList.RemoveAt(removeIdx); _config.Save(); }
            }
        }
        ImGui.EndChild();

        ImGui.EndTabItem();
    }

    private void DrawTabSchedule()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabSchedule"))) return;

        ImGui.Spacing();
        ImGui.TextDisabled(Lang.T("scheduleHint"));
        ImGui.Spacing();

        if (_config.Presets.Count == 0)
        {
            ImGui.TextDisabled(Lang.T("scheduleNoPresets"));
            ImGui.EndTabItem();
            return;
        }

        if (ImGui.Button(Lang.T("scheduleAdd")))
        {
            var first = _config.Presets.OrderBy(p => p.SortOrder).First();
            _config.Schedules.Add(new ScheduleEntry
            {
                FromHour = 8, FromMinute = 0,
                ToHour   = 20, ToMinute  = 0,
                PresetId = first.Id
            });
            _config.Save();
        }

        ImGui.Separator();
        ImGui.Spacing();

        float listH = ImGui.GetContentRegionAvail().Y - 8;
        if (ImGui.BeginChild("##schedScroll", new Vector2(0, listH), false))
        {
            if (_config.Schedules.Count == 0)
            {
                ImGui.TextDisabled(Lang.T("scheduleEmpty"));
            }
            else
            {
                var sortedPresets = _config.Presets.OrderBy(p => p.SortOrder).ToList();
                var presetIds    = sortedPresets.Select(p => p.Id).ToArray();
                var presetNames  = sortedPresets.Select(p => p.Name).ToArray();

                int removeIdx = -1;
                for (int i = 0; i < _config.Schedules.Count; i++)
                {
                    var s = _config.Schedules[i];
                    ImGui.PushID(i);

                    ImGui.SetNextItemWidth(32);
                    var fh = s.FromHour;
                    if (ImGui.DragInt("##fh", ref fh, 0.15f, 0, 23, "%02d")) { s.FromHour = fh; _config.Save(); }
                    ImGui.SameLine(0, 2); ImGui.TextUnformatted(":");
                    ImGui.SameLine(0, 2);
                    ImGui.SetNextItemWidth(32);
                    var fm = s.FromMinute;
                    if (ImGui.DragInt("##fm", ref fm, 0.4f, 0, 59, "%02d")) { s.FromMinute = fm; _config.Save(); }

                    ImGui.SameLine(0, 6); ImGui.TextUnformatted("–"); ImGui.SameLine(0, 6);

                    ImGui.SetNextItemWidth(32);
                    var th = s.ToHour;
                    if (ImGui.DragInt("##th", ref th, 0.15f, 0, 23, "%02d")) { s.ToHour = th; _config.Save(); }
                    ImGui.SameLine(0, 2); ImGui.TextUnformatted(":");
                    ImGui.SameLine(0, 2);
                    ImGui.SetNextItemWidth(32);
                    var tm = s.ToMinute;
                    if (ImGui.DragInt("##tm", ref tm, 0.4f, 0, 59, "%02d")) { s.ToMinute = tm; _config.Save(); }

                    ImGui.SameLine(0, 10);

                    int presetIdx = Array.IndexOf(presetIds, s.PresetId);
                    if (presetIdx < 0) presetIdx = 0;
                    ImGui.SetNextItemWidth(150);
                    if (ImGui.Combo("##spreset", ref presetIdx, presetNames, presetNames.Length))
                    { s.PresetId = presetIds[presetIdx]; _config.Save(); }

                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
                    if (ImGui.SmallButton(FontAwesomeIcon.Times.ToIconString())) removeIdx = i;
                    ImGui.PopStyleColor();

                    ImGui.PopID();
                }
                if (removeIdx >= 0) { _config.Schedules.RemoveAt(removeIdx); _config.Save(); }
            }
        }
        ImGui.EndChild();

        ImGui.EndTabItem();
    }

    private void DrawTabColors()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabColors"))) return;

        ImGui.Spacing();

        ImGui.TextDisabled(Lang.T("sectionZoneColors"));
        ImGui.Separator();
        ImGui.Spacing();

        DrawColorEdit(Lang.T("colorZoneXZ"),   ref _config.ColorZoneXZ);
        DrawColorEdit(Lang.T("colorZoneXY"),   ref _config.ColorZoneXY);
        DrawColorEdit(Lang.T("colorZoneYZ"),   ref _config.ColorZoneYZ);
        DrawColorEdit(Lang.T("colorZoneCube"), ref _config.ColorZoneCube);

        ImGui.Spacing();
        ImGui.TextDisabled(Lang.T("sectionListColors"));
        ImGui.Separator();
        ImGui.Spacing();

        DrawColorEdit(Lang.T("colorGreeting"),     ref _config.ColorGreeting,     Lang.T("statusGreeting"));
        DrawColorEdit(Lang.T("colorGreeted"),      ref _config.ColorGreeted,      Lang.T("statusGreeted"));
        DrawColorEdit(Lang.T("colorAgainToGreet"), ref _config.ColorAgainToGreet, Lang.T("statusAgainToGreet"));

        ImGui.EndTabItem();
    }

    private void DrawColorEdit(string label, ref Vector4 color, string? previewText = null)
    {
        if (ImGui.ColorEdit4(label + "##" + label, ref color,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview))
        {
            _config.Save();
        }
        if (previewText != null)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(previewText);
            ImGui.PopStyleColor();
        }
    }

    private void DrawTabLanguage()
    {
        if (!ImGui.BeginTabItem(Lang.T("tabLanguage"))) return;

        ImGui.Spacing();
        ImGui.Text(Lang.T("languageLabel"));
        ImGui.SetNextItemWidth(200);

        var langIdx = _config.Language;
        if (ImGui.Combo("##language", ref langIdx, LanguageNames, LanguageNames.Length))
        {
            _config.Language = langIdx;
            Lang.Current = (GGLanguage)langIdx;
            _config.Save();
        }

        ImGui.EndTabItem();
    }
}
