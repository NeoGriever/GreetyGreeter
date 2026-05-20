using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using GreetyGreeter.Models;

namespace GreetyGreeter.Logic;

public class GreetExecutor
{
    private readonly Configuration _config;
    private readonly ScheduleService _scheduleService;
    private readonly object _queueLock = new();
    private readonly Queue<GreetQueueEntry> _queue = new();
    private bool _workerRunning = false;
    private bool _isExecuting = false;
    private string? _currentKey = null;

    private static DateTime _lastTellSentAt = DateTime.MinValue;
    private const double TellCooldownSeconds = 1.1;

    public int QueueCount
    {
        get
        {
            lock (_queueLock)
                return _queue.Count + (_isExecuting ? 1 : 0);
        }
    }

    public GreetExecutor(Configuration config, ScheduleService scheduleService)
    {
        _config = config;
        _scheduleService = scheduleService;
    }

    public void Enqueue(string name, string world, IPlayerCharacter? pc)
    {
        lock (_queueLock)
        {
            _queue.Enqueue(new GreetQueueEntry(name, world, pc));

            if (_workerRunning)
                return;

            _workerRunning = true;
        }

        _ = ProcessQueueAsync();
    }

    public bool IsCurrentlyGreeting(string key)
    {
        lock (_queueLock)
            return _isExecuting && _currentKey == key;
    }

    public bool IsQueued(string key)
    {
        lock (_queueLock)
            return _queue.Any(e => e.Key == key);
    }

    public void Tick()
    {
        EnsureWorkerRunning();
    }

    private void EnsureWorkerRunning()
    {
        lock (_queueLock)
        {
            if (_workerRunning || _queue.Count == 0)
                return;

            _workerRunning = true;
        }

        _ = ProcessQueueAsync();
    }

    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            GreetQueueEntry entry;

            lock (_queueLock)
            {
                if (_queue.Count == 0)
                {
                    _workerRunning = false;
                    return;
                }

                entry = _queue.Dequeue();
                _isExecuting = true;
                _currentKey = entry.Key;
            }

            while (TellTracker.ShouldWait())
                await Task.Delay(100);

            await ExecuteAsync(entry);
        }
    }

    private async Task ExecuteAsync(GreetQueueEntry entry)
    {
        try
        {
            var preset = GetActivePreset();
            if (preset == null || preset.Lines.Count == 0) return;

            if (_config.TargetPlayerDuringExecution && entry.Character != null)
            {
                var charRef = entry.Character;
                _ = Plugin.Framework.RunOnTick(() =>
                {
                    if (charRef.IsValid())
                        Plugin.TargetManager.Target = charRef;
                });
            }

            foreach (var line in preset.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Text)) continue;

                string text = line.Text.Replace("<t>", $"{entry.Name}@{entry.World}");
                bool isTell = IsTellCommand(text);

                if (isTell)
                {
                    var elapsed = (DateTime.Now - _lastTellSentAt).TotalSeconds;
                    if (elapsed < TellCooldownSeconds)
                        await Task.Delay(TimeSpan.FromSeconds(TellCooldownSeconds - elapsed));
                    _lastTellSentAt = DateTime.Now;
                }

                var textCopy = text;
                _ = Plugin.Framework.RunOnTick(() => ChatBox.Send(textCopy));

                float delay = isTell ? Math.Max(1.1f, line.Delay) : line.Delay;
                if (delay > 0f)
                    await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[GreetyGreeter] Error during greet execution.");
        }
        finally
        {
            lock (_queueLock)
            {
                _currentKey = null;
                _isExecuting = false;
            }
        }
    }

    private static bool IsTellCommand(string text)
        => text.Contains("/tell ", StringComparison.OrdinalIgnoreCase)
        || text.Contains("/t ", StringComparison.OrdinalIgnoreCase);

    private GreetingPreset? GetActivePreset()
    {
        var effectiveId = _scheduleService.GetEffectivePresetId();
        if (string.IsNullOrEmpty(effectiveId)) return null;
        return _config.Presets.Find(p => p.Id == effectiveId);
    }
}

public class GreetQueueEntry
{
    public string Name;
    public string World;
    public IPlayerCharacter? Character;
    public string Key => $"{Name}@{World}";

    public GreetQueueEntry(string name, string world, IPlayerCharacter? character)
    {
        Name = name;
        World = world;
        Character = character;
    }
}
