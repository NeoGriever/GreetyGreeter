using System;

namespace GreetyGreeter.Models;

public class ScheduleEntry
{
    public string Id = Guid.NewGuid().ToString();
    public int FromHour;
    public int FromMinute;
    public int ToHour   = 20;
    public int ToMinute;
    public string PresetId = string.Empty;
}
