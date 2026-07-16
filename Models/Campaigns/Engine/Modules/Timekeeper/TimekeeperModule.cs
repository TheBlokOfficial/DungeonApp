using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DungeonApp.Models.Campaigns.Engine.Events;

namespace DungeonApp.Models.Campaigns.Engine.Modules.Timekeeper;

/// <summary>
/// Moduł odpowiedzialny za śledzenie upływu czasu w kampanii.
/// Używa kalendarza Fantasy: 12 miesięcy × 30 dni = 360 dni/rok.
/// </summary>
/// <remarks>
/// Moduł jest w 100% hermetyczny — nie wie, w którym widoku się wyświetla.
/// Jego propertiesy [ObservableProperty] są bindowane bezpośrednio przez TimekeeperView.axaml.
/// Zapis stanu odbywa się przez odziedziczony Storage i GetModuleStatePath() z CampaignModuleBase
/// — do pliku modules/Core.Timekeeper.json w folderze kampanii, bez dotykania campaign.json.
/// </remarks>
public partial class TimekeeperModule : CampaignModuleBase, IRecipient<ConsoleCommandEvent>
{
    // Stałe kalendarza Fantasy
    private const int MinutesPerHour = 60;
    private const int HoursPerDay    = 24;
    private const int DaysPerMonth   = 30;
    private const int MonthsPerYear  = 12;

    // Godzina 6:00 = wschód słońca, 18:00 = zachód
    private const int SunriseHour = 6;
    private const int SunsetHour  = 18;

    public override string ModuleId => "Core.Timekeeper";

    private TimeState _state = new();

    // ─── Propertiesy bindowane do UI ─────────────────────────────────────────

    [ObservableProperty]
    private string _formattedDate = string.Empty;

    [ObservableProperty]
    private string _formattedTime = string.Empty;

    /// <summary>
    /// Klucz ikony PathIcon — "sun" lub "moon" w zależności od pory dnia.
    /// Widok mapuje go do odpowiedniego StaticResource.
    /// </summary>
    [ObservableProperty]
    private string _dayPhaseIconKey = "IconSun";

    // ─── Inicjalizacja ───────────────────────────────────────────────────────

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // Ładowanie stanu z dysku lub użycie domyślnego
        var loaded = Storage?.Load<TimeState>(GetModuleStatePath());
        _state = loaded ?? new TimeState();

        RefreshDisplayProperties();

        Messenger?.Register<ConsoleCommandEvent>(this);

        System.Diagnostics.Debug.WriteLine($"[TimekeeperModule] Zainicjalizowano. Data: {FormattedDate}, Czas: {FormattedTime}");
    }

    protected override void OnShutdown()
    {
        SaveState();
        System.Diagnostics.Debug.WriteLine("[TimekeeperModule] Zamknięto i zapisano stan.");
    }

    // ─── Logika Czasu ────────────────────────────────────────────────────────

    /// <summary>
    /// Przesuwa czas kampanii o podaną liczbę minut, zawijając poprawnie
    /// minuty→godziny→dni→miesiące→lata. Po przesunięciu publikuje log do Konsoli.
    /// </summary>
    public void AdvanceTime(int minutes)
    {
        if (minutes <= 0) return;

        int totalMinutes = _state.Minute + minutes;

        _state.Minute = totalMinutes % MinutesPerHour;
        int hoursToAdd = totalMinutes / MinutesPerHour;

        if (hoursToAdd > 0)
        {
            int totalHours = _state.Hour + hoursToAdd;
            _state.Hour = totalHours % HoursPerDay;
            int daysToAdd = totalHours / HoursPerDay;

            if (daysToAdd > 0)
            {
                // Dni 1-indeksowane: dzień 30 + 1 = dzień 1 nowego miesiąca
                int totalDays = (_state.Day - 1) + daysToAdd;
                _state.Day = (totalDays % DaysPerMonth) + 1;
                int monthsToAdd = totalDays / DaysPerMonth;

                if (monthsToAdd > 0)
                {
                    int totalMonths = (_state.Month - 1) + monthsToAdd;
                    _state.Month = (totalMonths % MonthsPerYear) + 1;
                    _state.Year += totalMonths / MonthsPerYear;
                }
            }
        }

        RefreshDisplayProperties();
        SaveState();
        PublishTimeAdvancedLog(minutes);
    }

    // ─── Komendy UI ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void AdvanceOneHour() => AdvanceTime(60);

    [RelayCommand]
    private void AdvanceLongRest() => AdvanceTime(60 * 8);

    [RelayCommand]
    private void AdvanceOneDay() => AdvanceTime(60 * 24);

    // ─── ConsoleCommand Handler ──────────────────────────────────────────────

    /// <summary>
    /// Nasłuchuje komend z konsoli zaczynających się od "/time".
    /// Obsługiwane formaty: /time +Xm, /time +Xh, /time +Xd
    /// </summary>
    public void Receive(ConsoleCommandEvent message)
    {
        var input = message.RawInput.Trim();
        if (!input.StartsWith("/time", StringComparison.OrdinalIgnoreCase)) return;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            PublishError(Translate("module_timekeeper_error_usage"));
            return;
        }

        var arg = parts[1].Trim().ToLowerInvariant();
        if (arg.Length < 2 || !arg.StartsWith("+"))
        {
            PublishError(Translate("module_timekeeper_error_format"));
            return;
        }

        char unit   = arg[^1];
        string numStr = arg[1..^1];

        if (!int.TryParse(numStr, out int value) || value <= 0)
        {
            PublishError(string.Format(Translate("module_timekeeper_error_value"), numStr));
            return;
        }

        int minutesToAdd = unit switch
        {
            'm' => value,
            'h' => value * MinutesPerHour,
            'd' => value * MinutesPerHour * HoursPerDay,
            _   => -1
        };

        if (minutesToAdd < 0)
        {
            PublishError(string.Format(Translate("module_timekeeper_error_unit"), unit));
            return;
        }

        AdvanceTime(minutesToAdd);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void RefreshDisplayProperties()
    {
        FormattedDate = string.Format(Translate("module_timekeeper_date_format"), _state.Year, _state.Month, _state.Day);
        FormattedTime = $"{_state.Hour:D2}:{_state.Minute:D2}";
        DayPhaseIconKey = (_state.Hour >= SunriseHour && _state.Hour < SunsetHour)
            ? "IconSun"
            : "IconMoon";
    }

    private void SaveState()
    {
        if (Storage == null) return;
        Storage.Save(GetModuleStatePath(), _state);
    }

    private void PublishTimeAdvancedLog(int minutes)
    {
        string humanized = HumanizeMinutes(minutes);
        string msg = string.Format(Translate("module_timekeeper_advanced"), humanized, FormattedDate, FormattedTime);
        Publish(new NotificationEvent(msg, "Info") { SenderModuleId = ModuleId });
    }

    private void PublishError(string msg)
    {
        Publish(new NotificationEvent($"[Timekeeper] {msg}", "Warning") { SenderModuleId = ModuleId });
    }

    private string HumanizeMinutes(int minutes)
    {
        if (minutes < 60)    return string.Format(Translate("module_timekeeper_unit_min"), minutes);
        if (minutes < 60*24) return string.Format(Translate("module_timekeeper_unit_h"), minutes / 60);
        return string.Format(Translate("module_timekeeper_unit_d"), minutes / (60*24));
    }
}
