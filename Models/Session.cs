using System;

namespace DungeonApp.Models;

public class Session
{
    public string Id { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;
}