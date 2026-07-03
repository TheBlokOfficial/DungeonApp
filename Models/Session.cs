using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DungeonApp.Models;

public partial class Note : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    // --- Pola wspierające UI (nie są istotne z punktu widzenia danych, ale sterują widokiem) ---
    
    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private string _draftText = string.Empty;
}

public class Session
{
    public string Id { get; set; } = string.Empty;
    public int Number { get; init; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    
    // Zmieniamy string na listę obiektów
    public List<Note> NotesList { get; set; } = []; 
    
    public bool IsArchived { get; set; } = false; 
}