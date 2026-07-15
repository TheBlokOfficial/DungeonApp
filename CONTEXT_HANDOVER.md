# Kontekst Projektu: DungeonApp (Handover)

Ten dokument zawiera podsumowanie aktualnego stanu prac, najważniejszych decyzji architektonicznych oraz pomysłów na przyszłość. Służy jako "punkt startowy" dla nowych konwersacji z agentem.

## 1. Ostatnia Sesja (Najświeższe Zmiany)

W trakcie ostatniej konwersacji skupiliśmy się na stabilizacji środowiska testowego (Workbench / Sandbox) oraz na ostatecznej hermetyzacji modułu Konsoli:

- **Naprawa wyświetlania Workbenchu (Black Screen i Drżenie)**:
  - Usunięto błędy parsera Avalonia (WPF-owe nazwy kursorów i wadliwe `RelativeSource`), które powodowały crashe w `FloatingPanel` i renderowanie czarnego ekranu.
  - Opracowano idealny mechanizm pozycjonowania modułów w Workbenchu przy użyciu `Canvas`. Zamiast używać `HorizontalAlignment="Center"` (co powodowało drżenie UI podczas resize), panel centrowany jest jednorazowo w code-behind (`CenterPanel()`), po czym manipulowana jest tylko jego szerokość/wysokość.
- **Naprawa cyklu życia silnika w Sandboxie**:
  - `SandboxTabViewModel` był niszczony (`_engine.StopEngine()`) w trakcie sekwencji rozgrzewkowej (warmup) przy starcie aplikacji. Usunięto Sandbox z sekwencji warmup i zlikwidowano `StopEngine` przy nawigacji, dzięki czemu środowisko w tle żyje cały czas, póki użytkownik korzysta z Workbenchu.
- **Hermetyzacja ConsoleModule (Wzorzec Architektoniczny)**:
  - Zidentyfikowano problem duplikacji kodu UI konsoli w kampanii i w workbenchu.
  - Wyekstrahowano widok z logiką przewijania (auto-scroll) do osobnego pliku `ConsoleView.axaml`.
  - Logika obsługi komend (`ExecuteConsoleCommand`) oraz zarządzania systemem Propozycji (`AcceptProposal`, `RejectProposal`) została usunięta z ViewModeli (`CampaignDashboardViewModel` i `SandboxTabViewModel`) i w całości zamknięta wewnątrz klasy bazowej modułu `ConsoleModule.cs`.
  - Obie konsole są teraz wywoływane w sposób czysty przez system `DataTemplate` za pomocą zwykłego `<ContentControl>`.

## 2. Architektura Modułów i Event Bus

- **Całkowita Niezależność (Event Bus)**: Moduły (np. `ConsoleModule`, `TimekeeperModule`) nie wiedzą o swoim istnieniu. Komunikacja odbywa się przez globalną szynę wiadomości (magistralę `CampaignEngine.Messenger`).
- **Komendy Systemowe**: Konsola nie parsuje komend. Wypluwa na szynę `ConsoleCommandEvent` zawierający surowy tekst (np. `/time +8h`). Odpowiednie moduły nasłuchują na te eventy i decydują o ich odrzuceniu lub wykonaniu akcji.
- **Zapis Stanu (Persistence)**: Każdy moduł sam implementuje zapis i odczyt z dysku przez `IStorageService`. Główny plik `campaign.json` jest lekki i zawiera tylko metadane.

## 3. Innowacje w Interfejsie Użytkownika

- **Zdarzenia Propozycji (Hover Actions)**: 
  - Wdrożono koncepcję znaną z Minecrafta (Tellraw / Hover action). Moduł może wyemitować `ProposalEvent`, co sprawia, że w konsoli wyświetlają się przyciski `[accept]` oraz `[reject]`. Kliknięcie na interfejsie automatycznie wykonuje przekazaną przez moduł lambdę (`AcceptAction`), co zwalnia DM'a z konieczności wpisywania np. `/accept` z klawiatury.
- **Anti Pixel-Jumping**: 
  - Zamiast podmieniać XAML lub ukrywać widoczność kontrolek, zmieniamy ich `Opacity="0"` oraz `IsHitTestVisible="False"`. Tło i wymiary zostają na swoim miejscu, zachowując żelazną strukturę layoutu.
  - Stosujemy `TextTrimming="CharacterEllipsis"` w tabelach i `VerticalContentAlignment="Center"` w stylach globalnych.

## 4. Planowane Zadania i Kolejne Kroki (Roadmap)

1. **Testowanie Timekeepera w Sandboxie**: 
   - Konieczne jest przetestowanie połączonej integracji `TimekeeperModule` (czasu) wraz z `ConsoleModule` (konsolą) po ostatnich refaktoryzacjach, by upewnić się, że przepływ zdarzeń i komend w sterylnym Sandboxie funkcjonuje w 100% z zamierzeniami projektowymi.
2. **Kolejne Moduły Kampanii**:
   - Silnik kampanii jest hermetyczny i w pełni gotowy na przyjmowanie nowych systemów. Najbliższe plany to moduł pogody, system awaryjnych notatek przypinanych, oraz podwaliny pod długo wyczekiwany **Combat Tracker**.
3. **Pełnoprawny Dashboard**:
   - `CampaignDashboardView` używa prostego bento-boxa z testowymi wizytówkami. Trzeba przemyśleć i zaprojektować elastyczny system dynamicznego ładownia kafelków poszczególnych modułów.

## 5. Zasady AI (Zgodnie z `.agents/AGENTS.md`)

- **Asertywność i Ochrona Architektury**: AI ma nakaz wstrzymywania i sprzeciwiania się poleceniom naruszającym integralność architektoniczną lub stabilność siatki układu (layoutu).
- **Tryb Operacyjny (Mode-Driven)**: AI musi klasyfikować swoje działania na Tryb Architektoniczny (ARCH), Tryb Szlifowania (POLISH) lub Tryb Szybki (FAST) i zgodnie z nimi generować odpowiednie Plany Implementacji i pliki Task.md.
- **Dokumentacja Kodu (Why over How)**: Komentarze w XAML i C# mają odpowiadać na pytanie "dlaczego" tak to zbudowano, aby zapobiec modyfikacjom "psującym" np. dedykowaną logikę pozycjonowania (Canvas).

---

> **Instrukcja dla Agenta startującego z tym plikiem:** 
> Przeanalizuj powyższy stan z naciskiem na Sekcję 1 (Ostatnia Sesja). Zapytaj użytkownika, czym chce się teraz zająć – prawdopodobnie testowaniem Sandboxa, nowymi modułami lub pracą nad głównym Dashboardem kampanii.
