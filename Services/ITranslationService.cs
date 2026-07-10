using System.ComponentModel;

namespace DungeonApp.Services;

public interface ITranslationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; set; }
    string this[string key] { get; }
    string Translate(string? key);
    void ChangeLanguage(string languageCode);
}
