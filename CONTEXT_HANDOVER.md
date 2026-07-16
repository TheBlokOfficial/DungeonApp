# Kontekst Projektu: DungeonApp (Handover)

Ten dokument zawiera podsumowanie aktualnego stanu prac, najważniejszych decyzji architektonicznych oraz pomysłów na przyszłość. Służy jako "punkt startowy" dla nowych konwersacji z agentem.

## 1. Ostatnia Sesja (Najświeższe Zmiany)

W trakcie tej sesji skupiliśmy się na szlifowaniu interfejsu i umiędzynarodowieniu (i18n) istniejących modułów (tryb POLISH/FAST MODE):

- **Tłumaczenie (i18n) dla Console i Timekeeper**:
  - `TimekeeperModule`: Zastąpiono "hardkodowane" polskie komunikaty w kodzie C# użyciem metody `Translate()`. Data, czas i błędy są formowane dynamicznie przez `string.Format`. Dodano odpowiednie klucze do plików `pl.json` i `en.json`.
  - `ConsoleTemplateFactory`: Wdrożono dynamiczne tłumaczenie identyfikatorów modułów (`SenderModuleId`). Kodowe nazwy jak `Core.Timekeeper` są teraz przechwytywane i tłumaczone na czytelne nazwy (np. `[CZAS & KALENDARZ]`).

- **Unifikacja nagłówków paneli modułów**:
  - Utworzono uniwersalne style XAML w `CommonStyles.axaml`: klasa `StackPanel.moduleHeader` dla układu oraz `TextBlock.moduleHeaderLabel` dla typografii (FontSize 12, Bold, LetterSpacing 2).
  - Wyczyszczono XAML w `TimekeeperView` oraz `ConsoleView` z inline-atrybutów, przypinając je do scentralizowanych klas. Zapewni to 100% spójności dla wszystkich nowych modułów (np. Combat Trackera).
  - Teksty w nagłówkach również przeszły pod kontrolę `i18n:Translate`.

- **Logika powiadomień w Konsoli**:
  - Usunięto błąd, w którym każdy komunikat był poprzedzony sztywnym przedrostkiem `[INFO]`. 
  - Wdrożono wizualną separację poziomów logowania (`Level` z `NotificationEvent`):
    - `Warning` -> `[WARN]` (żółty)
    - `Error` -> `[ERROR]` (czerwony)
    - Puste (lub `Info`) -> `[INFO]` (szary)
  - Notatki wpisywane ręcznie przez Mistrza Gry (DM) nie posiadają już tagów systemowych. Otrzymały formę "lini czasu" ze znacznikiem `>` i wyróżniającym się jasnoszarym kolorem tekstu.

- **Drobne porządki kompilatora**:
  - Poprawiono 6 ostrzeżeń `AVLN5001` we wszystkich widokach (`CharacterDetailView`, `CreateCharacterView`, `CreateSessionView`) zamieniając przestarzały atrybut `TextBox.Watermark` na `PlaceholderText`.

## 2. Architektura Modułów i Event Bus

- **Całkowita Niezależność (Event Bus)**: Moduły (np. `ConsoleModule`, `TimekeeperModule`) nie wiedzą o swoim istnieniu. Komunikacja odbywa się przez globalną szynę wiadomości (magistralę `CampaignEngine.Messenger`).
- **Komendy Systemowe**: Konsola nie parsuje komend. Wypluwa na szynę `ConsoleCommandEvent` zawierający surowy tekst (np. `/time +8h`). Odpowiednie moduły nasłuchują na te eventy i decydują o ich odrzuceniu lub wykonaniu akcji.
- **Zapis Stanu (Persistence)**: Każdy moduł sam implementuje zapis i odczyt z dysku przez `IStorageService`. Główny plik `campaign.json` jest lekki i zawiera tylko metadane.

## 3. Innowacje w Interfejsie Użytkownika

- **Animowane Logi Konsoli (ChatAnimation)**:
  - Każdy nowy log wjeżdża z dołu ze slide-in + fade-in, a stare logi płynnie przesuwają się ku górze.
  - Czysto wizualne animacje (RenderTransform) — brak wpływu na layout.
  - Logi **nie** blaknią — świadoma decyzja: konsola jest aktywnym narzędziem DM-a, nie pływającym chatem.

- **Zdarzenia Propozycji (Hover Actions)**:
  - Wdrożono koncepcję Tellraw / Hover action. Moduł może wyemitować `ProposalEvent`, co sprawia, że w konsoli wyświetlają się przyciski `[accept]` oraz `[reject]`.
  - Kliknięcie automatycznie wykonuje przekazaną lambdę (`AcceptAction`), zwalniając DM-a z wpisywania komend.

- **Anti Pixel-Jumping**:
  - `Opacity=0` + `IsHitTestVisible=False` zamiast `IsVisible=False` dla zachowania wymiarów layoutu.
  - `TextTrimming="CharacterEllipsis"` w tabelach.
  - `VerticalContentAlignment="Center"` w globalnych stylach.

- **Workbench / FloatingPanel**:
  - Moduł pozycjonowany przez `Canvas.SetLeft/Top` (jednorazowo w `CenterPanel()`), nie przez `HorizontalAlignment="Center"` — zapobiega drżeniu podczas resize.
  - Po każdym resize modułu (zdarzenie `ResizeCompleted`) panel re-centruje się automatycznie.

## 4. Planowane Zadania i Kolejne Kroki (Roadmap)

1. **Testowanie Timekeepera w Sandboxie**:
   - Przetestowanie połączonej integracji `TimekeeperModule` (czasu) z nową animowaną `ConsoleModule` (konsolą) po wszystkich refaktoryzacjach.

2. **Kolejne Moduły Kampanii**:
   - Silnik kampanii jest hermetyczny i w pełni gotowy na nowe systemy.
   - Kolejne w kolejce: moduł pogody, system awaryjnych notatek przypinanych, **Combat Tracker** (długo wyczekiwany).

3. **Pełnoprawny Dashboard**:
   - `CampaignDashboardView` używa prostego bento-boxa z testowymi wizytówkami.
   - Do przemyślenia: elastyczny system dynamicznego ładowania kafelków poszczególnych modułów.

## 5. Zasady AI (Zgodnie z `.agents/AGENTS.md`)

- **Asertywność i Ochrona Architektury**: AI ma nakaz wstrzymywania i sprzeciwiania się poleceniom naruszającym integralność architektoniczną lub stabilność siatki layoutu.
- **Tryb Operacyjny (Mode-Driven)**: AI musi klasyfikować działania na ARCH / POLISH / FAST i odpowiednio generować Plany Implementacji i pliki Task.md.
- **Dokumentacja Kodu (Why over How)**: Komentarze w XAML i C# odpowiadają na "dlaczego" tak to zbudowano.

---

> **Instrukcja dla Agenta startującego z tym plikiem:**
> Przeanalizuj powyższy stan z naciskiem na Sekcję 1 (Ostatnia Sesja). Zapytaj użytkownika, czym chce się teraz zająć — prawdopodobnie testowaniem Sandboxa z nową animowaną konsolą, nowymi modułami (Combat Tracker?) lub sprzątaniem ostrzeżeń Watermark.
