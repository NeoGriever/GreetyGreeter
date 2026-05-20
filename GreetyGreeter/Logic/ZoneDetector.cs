using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace GreetyGreeter.Logic;

public class ZoneDetector
{
    private readonly Configuration _config;
    private DateTime _lastScan = DateTime.MinValue;
    private const double ScanIntervalMs = 500;

    private List<IPlayerCharacter> _cached = new();

    public ZoneDetector(Configuration config)
    {
        _config = config;
    }

    public IReadOnlyList<IPlayerCharacter> GetPlayersInZone()
    {
        if ((DateTime.Now - _lastScan).TotalMilliseconds < ScanIntervalMs)
            return _cached;

        _lastScan = DateTime.Now;
        _cached = ScanNow();
        return _cached;
    }

    private List<IPlayerCharacter> ScanNow()
    {
        var result = new List<IPlayerCharacter>();
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer == null) return result;

        foreach (var obj in Plugin.ObjectTable)
        {
            if (obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Pc) continue;
            if (obj is not IPlayerCharacter pc) continue;
            if (pc.GameObjectId == localPlayer.GameObjectId) continue;

            if (IsInZone(pc.Position))
                result.Add(pc);
        }

        return result;
    }

    public bool IsInZone(Vector3 pos)
    {
        var center = _config.ZoneCenter;

        if (_config.Shape == ZoneShape.Sphere)
        {
            return Vector3.Distance(pos, center) <= _config.SphereRadius;
        }
        else
        {
            // Translate to center-local space, then apply inverse Y-rotation
            float dx = pos.X - center.X;
            float dy = pos.Y - center.Y;
            float dz = pos.Z - center.Z;

            float rad = -_config.BoxRotationDegrees * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            float lx = cos * dx - sin * dz;
            float lz = sin * dx + cos * dz;

            return MathF.Abs(lx) <= _config.BoxWidth / 2f
                && MathF.Abs(dy)  <= _config.BoxHeight / 2f
                && MathF.Abs(lz) <= _config.BoxDepth / 2f;
        }
    }

    public void InvalidateCache() => _lastScan = DateTime.MinValue;
}
