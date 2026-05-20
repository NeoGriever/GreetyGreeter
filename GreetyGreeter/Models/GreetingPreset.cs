using System;
using System.Collections.Generic;

namespace GreetyGreeter.Models;

public class GreetingPreset
{
    public string Id = Guid.NewGuid().ToString();
    public string Name = "New Preset";
    public int SortOrder = 0;
    public List<CommandLine> Lines = new();
}
