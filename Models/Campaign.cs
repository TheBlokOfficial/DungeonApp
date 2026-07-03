using System;

namespace DungeonApp.Models;

public class Campaign
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string System { get; set; } = "D&D 5e";
    public DateTime CreatedAt { get; set; }
    public DateTime LastSession { get; set; }
    public int SessionsCount { get; set; }
    
    public string Description { get; set; } = string.Empty;
}