using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace GreetyGreeter;

public static unsafe class ChatBox
{
    private const int MaxBytes = 500;

    internal static bool IsPluginInitiated = false;

    public static void Send(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        string sanitised;
        try { sanitised = Plugin.PluginInterface.Sanitizer.Sanitize(message); }
        catch { sanitised = message; }
        if (string.IsNullOrEmpty(sanitised)) return;

        var bytes = Encoding.UTF8.GetByteCount(sanitised);
        if (bytes > MaxBytes)
        {
            Plugin.Log.Warning($"[GreetyGreeter] Chat message too long ({bytes} bytes, max {MaxBytes}), dropped.");
            return;
        }

        var module = UIModule.Instance();
        if (module == null)
        {
            Plugin.Log.Warning("[GreetyGreeter] UIModule not available; chat send skipped.");
            return;
        }

        Utf8String* utf = null;
        try
        {
            utf = Utf8String.FromString(sanitised);
            IsPluginInitiated = true;
            module->ProcessChatBoxEntry(utf, IntPtr.Zero, false);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[GreetyGreeter] Chat send failed.");
        }
        finally
        {
            IsPluginInitiated = false;
            if (utf != null) utf->Dtor(true);
        }
    }
}
