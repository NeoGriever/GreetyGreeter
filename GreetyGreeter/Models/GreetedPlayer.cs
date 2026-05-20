using System;

namespace GreetyGreeter.Models;

public class GreetedPlayer
{
    public string Name = string.Empty;
    public string World = string.Empty;
    public DateTime FirstSeen;
    public DateTime LastSeen;
    public string FullKey => $"{Name}@{World}";
}
