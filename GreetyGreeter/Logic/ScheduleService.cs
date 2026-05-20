using System;

namespace GreetyGreeter.Logic;

public class ScheduleService
{
    private readonly Configuration _config;

    public ScheduleService(Configuration config)
    {
        _config = config;
    }

    public string GetEffectivePresetId()
    {
        var now = DateTime.Now;
        int nowMins = now.Hour * 60 + now.Minute;

        foreach (var s in _config.Schedules)
        {
            if (string.IsNullOrEmpty(s.PresetId)) continue;

            int from = s.FromHour * 60 + s.FromMinute;
            int to   = s.ToHour   * 60 + s.ToMinute;

            bool inRange = from <= to
                ? nowMins >= from && nowMins < to
                : nowMins >= from || nowMins < to;

            if (inRange) return s.PresetId;
        }

        return _config.ActivePresetId;
    }
}
