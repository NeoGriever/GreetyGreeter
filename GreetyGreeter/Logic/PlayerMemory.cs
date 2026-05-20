using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using GreetyGreeter.Models;

namespace GreetyGreeter.Logic;

public class PlayerMemory
{
    private readonly Configuration _config;
    private readonly GreetExecutor _executor;

    public PlayerMemory(Configuration config, GreetExecutor executor)
    {
        _config = config;
        _executor = executor;
    }

    public void CheckAndTrigger(IPlayerCharacter pc)
    {
        var name = pc.Name.TextValue;
        var world = pc.HomeWorld.Value.Name.ToString();

        if (_config.NameIgnoreList.Exists(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;
        var key = $"{name}@{world}";
        var now = DateTime.Now;

        if (_config.PlayerMemory.TryGetValue(key, out var entry))
        {
            var elapsedMinutes = (now - entry.LastSeen).TotalMinutes;
            entry.LastSeen = now;
            if (elapsedMinutes < _config.CooldownMinutes) return;
        }
        else
        {
            _config.PlayerMemory[key] = new GreetedPlayer
            {
                Name = name,
                World = world,
                FirstSeen = now,
                LastSeen = now
            };
        }

        _config.TotalGreetedCount++;
        _config.RecentGreets.Insert(0, new RecentGreet
        {
            PlayerName = name,
            HomeWorld = world,
            TriggeredAt = now
        });
        if (_config.RecentGreets.Count > 4)
            _config.RecentGreets.RemoveAt(4);

        _config.Save();
        _executor.Enqueue(name, world, pc);
    }
}
