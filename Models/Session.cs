using System;
using System.Collections.Generic;

namespace DungeonApp.Models;

public class Session
{
    public string Id { get; set; } = string.Empty;
    public int Number { get; init; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    
    // Zmieniamy string na listę obiektów
    public List<Note> NotesList { get; set; } = []; 
    
    public bool IsArchived { get; set; } = false; 
    
    public List<string> ParticipatingCharacterIds { get; set; } = new();
    
    // Zapisywanie stanu potyczki / trackera inicjatywy
    public List<Combatant> Combatants { get; set; } = new();
}
