using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GreetyGreeter.Logic;
using GreetyGreeter.Windows;

namespace GreetyGreeter;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;

    private const string CommandName = "/gg";

    public Configuration Configuration { get; }

    private readonly WindowSystem _windowSystem = new("GreetyGreeter");
    private readonly MainWindow _mainWindow;
    private readonly SettingsWindow _settingsWindow;

    private readonly ZoneDetector _detector;
    private readonly ZoneRenderer _renderer;
    private readonly GreetExecutor _executor;
    private readonly PlayerMemory _memory;
    private readonly TellTracker _tellTracker;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.MigrateLegacyData();
        Lang.Current = (GGLanguage)System.Math.Clamp(Configuration.Language, 0, 4);

        _tellTracker  = new TellTracker(GameInterop);
        var schedule  = new ScheduleService(Configuration);
        _executor     = new GreetExecutor(Configuration, schedule);
        _detector     = new ZoneDetector(Configuration);
        _renderer     = new ZoneRenderer(Configuration);
        _memory       = new PlayerMemory(Configuration, _executor);

        _settingsWindow = new SettingsWindow(Configuration, _detector, _renderer);
        _mainWindow     = new MainWindow(Configuration, _settingsWindow, _executor);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_settingsWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "/gg - Open Greety Greeter panel. /gg settings - Open settings."
        });

        PluginInterface.UiBuilder.Draw += OnUiDraw;
        PluginInterface.UiBuilder.OpenMainUi += _mainWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi += _settingsWindow.Toggle;

        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= OnUiDraw;
        PluginInterface.UiBuilder.OpenMainUi -= _mainWindow.Toggle;
        PluginInterface.UiBuilder.OpenConfigUi -= _settingsWindow.Toggle;
        CommandManager.RemoveHandler(CommandName);
        _windowSystem.RemoveAllWindows();
        _tellTracker.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Equals("settings", System.StringComparison.OrdinalIgnoreCase))
            _settingsWindow.Toggle();
        else
            _mainWindow.Toggle();
    }

    private void OnUiDraw()
    {
        _windowSystem.Draw();

        if (_config.ZoneEnabled || _config.ShowZoneVisual)
        {
            if (_settingsWindow.IsZoneTabActive)
                _renderer.ShowDebugDots = true;
            _renderer.Draw(_detector);
        }
        else if (_settingsWindow.IsZoneTabActive)
        {
            _renderer.ShowDebugDots = true;
            _renderer.Draw(_detector);
        }
    }

    private Configuration _config => Configuration;

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.ZoneEnabled) return;
        if (ObjectTable.LocalPlayer == null) return;

        var inZone = _detector.GetPlayersInZone();
        foreach (var pc in inZone)
            _memory.CheckAndTrigger(pc);

        _executor.Tick();
    }
}
