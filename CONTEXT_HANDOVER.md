# Kontekst Projektu: DungeonApp (Handover)

Ten dokument zawiera podsumowanie aktualnego stanu prac, najważniejszych decyzji architektonicznych oraz pomysłów na przyszłość. Służy jako "punkt startowy" dla nowych konwersacji z agentem.

## 1. Ostatnia Sesja (Najświeższe Zmiany)

W trakcie tej sesji skupiliśmy się na szlifowaniu Workbenchu i wdrożeniu animowanej konsoli (POLISH MODE + ARCH MODE):

- **Workbench – usunięcie czarnego tła**:
  - Usunięto `Border` z tłem `#0A0A0A` otaczający Canvas w `SandboxTabView.axaml`.
  - `FloatingPanel` (moduł) wisi teraz bezpośrednio na szarym tle aplikacji bez dodatkowego "pudełka".

- **Workbench – centrowanie po resize modułu**:
  - `FloatingPanel` nie centrował się po przeciągnięciu uchwytu resize (trójkąt ◢).
  - Dodano zdarzenie `ResizeCompleted` w `FloatingPanel.axaml.cs`, emitowane po `PointerReleased`.
  - `SandboxTabView.axaml.cs` subskrybuje `ResizeCompleted` i wywołuje `CenterPanel()` — moduł centruje się po każdym resize.

- **Animowana konsola (`AnimatedFeedList`) — efekt ChatAnimation**:
  - Stworzono nową kontrolkę `Controls/AnimatedFeedList.axaml` + `.axaml.cs`.
  - Nowy log "wjeżdża" z dołu (slide-in + fade-in), a istniejące logi płynnie przesuwają się ku górze (push-up) — wzorowane na mod Minecraft Ezzenix/ChatAnimation.
  - **Architektura**: Canvas z ręcznym zarządzaniem pozycjami przez code-behind. `Canvas.Top` ustawiany raz na finalną wartość; animacje wyłącznie na `TranslateTransform.Y` + `Opacity` przez system `Transitions` (nie `Animation.RunAsync`, który crashuje na nie-Visual).
  - **Kluczowa naprawa ghostingu**: Guard `_subscribedCollection` + `UnsubscribeFromCurrent()` eliminuje podwójną subskrypcję `CollectionChanged` (która wcześniej powodowała, że `AddItem` wywoływano dwa razy per log).
  - **Dwuetapowy dispatch**: Element dodawany niewidocznie (`Opacity=0`, `Top=-9999`) → Canvas mierzy go w tle → `DispatcherPriority.Background` odczytuje `DesiredSize` i startuje animacje. Unika ręcznego `Arrange()`, który powodował ghosting.
  - Stworzono `Views/Campaigns/Modules/Templates/ConsoleTemplateFactory.cs` — fabryka C# budująca widoki dla `NotificationEvent` i `ProposalEvent` (wymagana przez `FuncDataTemplate` w `AnimatedFeedList`).
  - `ConsoleView.axaml` i `.axaml.cs` zaktualizowane: `ScrollViewer+ItemsControl` zastąpiony przez `<controls:AnimatedFeedList>`, stary auto-scroll usunięty.

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

4. **Drobne sprzątanie ostrzeżeń**:
   - 6 ostrzeżeń `AVLN5001` w `CharacterDetailView`, `CreateCharacterView`, `CreateSessionView` — `Watermark` → `PlaceholderText` (drobne, FAST MODE).

## 5. Zasady AI (Zgodnie z `.agents/AGENTS.md`)

- **Asertywność i Ochrona Architektury**: AI ma nakaz wstrzymywania i sprzeciwiania się poleceniom naruszającym integralność architektoniczną lub stabilność siatki layoutu.
- **Tryb Operacyjny (Mode-Driven)**: AI musi klasyfikować działania na ARCH / POLISH / FAST i odpowiednio generować Plany Implementacji i pliki Task.md.
- **Dokumentacja Kodu (Why over How)**: Komentarze w XAML i C# odpowiadają na "dlaczego" tak to zbudowano.

---

> **Instrukcja dla Agenta startującego z tym plikiem:**
> Przeanalizuj powyższy stan z naciskiem na Sekcję 1 (Ostatnia Sesja). Zapytaj użytkownika, czym chce się teraz zająć — prawdopodobnie testowaniem Sandboxa z nową animowaną konsolą, nowymi modułami (Combat Tracker?) lub sprzątaniem ostrzeżeń Watermark.
