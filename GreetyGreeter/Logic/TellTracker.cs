using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace GreetyGreeter.Logic;

public sealed unsafe class TellTracker : IDisposable
{
    private delegate void ProcessChatBoxEntryDelegate(UIModule* self, Utf8String* message, nint a2, byte a3);

    private readonly Hook<ProcessChatBoxEntryDelegate>? _hook;

    public static DateTime LastExternalTellAt = DateTime.MinValue;
    public const double ExternalTellCooldownSeconds = 1.5;

    public TellTracker(IGameInteropProvider interop)
    {
        try
        {
            var addr = (nint)(void*)UIModule.MemberFunctionPointers.ProcessChatBoxEntry;
            _hook = interop.HookFromAddress<ProcessChatBoxEntryDelegate>(addr, OnProcessChatBoxEntry);
            _hook.Enable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[GreetyGreeter] TellTracker hook failed – external tell detection disabled.");
        }
    }

    private void OnProcessChatBoxEntry(UIModule* self, Utf8String* message, nint a2, byte a3)
    {
        if (!ChatBox.IsPluginInitiated)
        {
            try
            {
                var text = message->ToString();
                if (IsTell(text))
                    LastExternalTellAt = DateTime.Now;
            }
            catch { }
        }

        _hook!.Original(self, message, a2, a3);
    }

    private static bool IsTell(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.TrimStart();
        return t.StartsWith("/tell ", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("/t ", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldWait()
        => (DateTime.Now - LastExternalTellAt).TotalSeconds < ExternalTellCooldownSeconds;

    public void Dispose()
    {
        _hook?.Disable();
        _hook?.Dispose();
    }
}
