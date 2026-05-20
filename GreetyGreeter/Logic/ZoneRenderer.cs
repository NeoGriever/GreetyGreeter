using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace GreetyGreeter.Logic;

public class ZoneRenderer
{
    private readonly Configuration _config;
    public bool ShowDebugDots { get; set; } = false;

    private const int Segments = 64;
    private const float LineThickness = 2f;
    private const float DotRadius = 5f;

    private static readonly uint ColorDotInside  = ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.9f, 0.15f, 1f));
    private static readonly uint ColorDotOutside = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.15f, 0.15f, 1f));

    public ZoneRenderer(Configuration config)
    {
        _config = config;
    }

    public void Draw(ZoneDetector detector)
    {
        var drawList = ImGui.GetBackgroundDrawList();
        var center = _config.ZoneCenter;

        if (_config.ShowZoneVisual)
        {
            if (_config.Shape == ZoneShape.Sphere)
            {
                DrawCircle(drawList, center, _config.SphereRadius, Axis.XZ, ImGui.ColorConvertFloat4ToU32(_config.ColorZoneXZ));
                DrawCircle(drawList, center, _config.SphereRadius, Axis.XY, ImGui.ColorConvertFloat4ToU32(_config.ColorZoneXY));
                DrawCircle(drawList, center, _config.SphereRadius, Axis.YZ, ImGui.ColorConvertFloat4ToU32(_config.ColorZoneYZ));
            }
            else
            {
                DrawCube(drawList, center, _config.BoxWidth, _config.BoxHeight, _config.BoxDepth,
                         _config.BoxRotationDegrees, ImGui.ColorConvertFloat4ToU32(_config.ColorZoneCube));
            }
        }

        if (ShowDebugDots)
            DrawDebugDots(drawList, detector);

        ShowDebugDots = false;
    }

    private enum Axis { XZ, XY, YZ }

    private void DrawCircle(ImDrawListPtr drawList, Vector3 center, float r, Axis axis, uint color)
    {
        var worldPoints = new Vector3[Segments];
        var screenPoints = new Vector2[Segments];
        var visible = new bool[Segments];

        for (int i = 0; i < Segments; i++)
        {
            float angle = 2f * MathF.PI * i / Segments;
            float c = r * MathF.Cos(angle);
            float s = r * MathF.Sin(angle);

            worldPoints[i] = axis switch
            {
                Axis.XZ => new Vector3(center.X + c, center.Y, center.Z + s),
                Axis.XY => new Vector3(center.X + c, center.Y + s, center.Z),
                Axis.YZ => new Vector3(center.X, center.Y + s, center.Z + c),
                _ => center
            };

            visible[i] = Plugin.GameGui.WorldToScreen(worldPoints[i], out screenPoints[i]);
        }

        var stroke = new List<Vector2>();
        for (int i = 0; i < Segments; i++)
        {
            int j = (i + 1) % Segments;
            if (visible[i] && visible[j])
            {
                if (stroke.Count == 0) stroke.Add(screenPoints[i]);
                stroke.Add(screenPoints[j]);
            }
            else if (!visible[i] && !visible[j])
            {
                FlushStroke(drawList, stroke, color);
            }
            else if (visible[i] && !visible[j])
            {
                if (stroke.Count == 0) stroke.Add(screenPoints[i]);
                var edge = FindEdgePoint(worldPoints[i], worldPoints[j]);
                if (edge.HasValue) stroke.Add(edge.Value);
                FlushStroke(drawList, stroke, color);
            }
            else
            {
                FlushStroke(drawList, stroke, color);
                var edge = FindEdgePoint(worldPoints[j], worldPoints[i]);
                if (edge.HasValue) stroke.Add(edge.Value);
                stroke.Add(screenPoints[j]);
            }
        }
        FlushStroke(drawList, stroke, color);
    }

    private static void DrawCube(ImDrawListPtr drawList, Vector3 center, float w, float h, float d, float rotDeg, uint color)
    {
        float hw = w / 2f, hh = h / 2f, hd = d / 2f;
        float rad = rotDeg * MathF.PI / 180f;
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);

        Vector3 RotateCorner(float lx, float ly, float lz)
        {
            return new Vector3(
                center.X + cos * lx - sin * lz,
                center.Y + ly,
                center.Z + sin * lx + cos * lz);
        }

        var corners = new Vector3[8]
        {
            RotateCorner(-hw, -hh, -hd),
            RotateCorner(+hw, -hh, -hd),
            RotateCorner(+hw, -hh, +hd),
            RotateCorner(-hw, -hh, +hd),
            RotateCorner(-hw, +hh, -hd),
            RotateCorner(+hw, +hh, -hd),
            RotateCorner(+hw, +hh, +hd),
            RotateCorner(-hw, +hh, +hd),
        };

        int[,] edges =
        {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        for (int e = 0; e < 12; e++)
        {
            var a3 = corners[edges[e, 0]];
            var b3 = corners[edges[e, 1]];
            bool va = Plugin.GameGui.WorldToScreen(a3, out var a2);
            bool vb = Plugin.GameGui.WorldToScreen(b3, out var b2);

            if (va && vb)
                drawList.AddLine(a2, b2, color, LineThickness);
            else if (va && !vb)
            {
                var edge = FindEdgePoint(a3, b3);
                if (edge.HasValue) drawList.AddLine(a2, edge.Value, color, LineThickness);
            }
            else if (!va && vb)
            {
                var edge = FindEdgePoint(b3, a3);
                if (edge.HasValue) drawList.AddLine(edge.Value, b2, color, LineThickness);
            }
        }
    }

    private void DrawDebugDots(ImDrawListPtr drawList, ZoneDetector detector)
    {
        if (!_config.ShowRootBoneIndicators) return;

        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer == null) return;

        foreach (var obj in Plugin.ObjectTable)
        {
            if (obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Pc) continue;
            if (obj is not IPlayerCharacter pc) continue;
            if (pc.GameObjectId == localPlayer.GameObjectId) continue;

            bool inZone = detector.IsInZone(pc.Position);
            uint dotColor = inZone ? ColorDotInside : ColorDotOutside;

            if (Plugin.GameGui.WorldToScreen(pc.Position, out var screenPos))
            {
                drawList.AddCircleFilled(screenPos, DotRadius, dotColor);
                drawList.AddCircle(screenPos, DotRadius + 1f, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.7f)), 12, 1.5f);
            }
        }
    }

    private static Vector2? FindEdgePoint(Vector3 visible, Vector3 invisible)
    {
        var a = visible;
        var b = invisible;
        for (int k = 0; k < 6; k++)
        {
            var mid = (a + b) * 0.5f;
            if (Plugin.GameGui.WorldToScreen(mid, out _)) a = mid;
            else b = mid;
        }
        return Plugin.GameGui.WorldToScreen(a, out var result) ? result : null;
    }

    private static void FlushStroke(ImDrawListPtr drawList, List<Vector2> stroke, uint color)
    {
        if (stroke.Count >= 2)
        {
            for (int i = 0; i < stroke.Count - 1; i++)
                drawList.AddLine(stroke[i], stroke[i + 1], color, LineThickness);
        }
        stroke.Clear();
    }
}
