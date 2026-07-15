# Kontekst Projektu: DungeonApp (Handover)

Ten dokument zawiera podsumowanie aktualnego stanu prac, najważniejszych decyzji architektonicznych oraz pomysłów na przyszłość. Służy jako "punkt startowy" dla nowych konwersacji z agentem.

## 1. Ostatnio zaimplementowane funkcje

- **Architektura Modułów i Zapisywanie (Persistence)**: 
  - `ICampaignModule.Initialize` przyjmuje teraz `IStorageService` oraz `CampaignDataPath`.
  - Moduły same dbają o swój zapis stanu (np. w folderze `modules/Core.Timekeeper.json`). Plik `campaign.json` jest "lekki" i trzyma tylko podstawowe metadane kampanii.
- **Console Event Bus i Hermetyzacja Konsoli**: 
  - `ConsoleModule` został zrefaktoryzowany. Posiada teraz własny stan wejścia (`ConsoleInputText`) oraz logikę komend (`ExecuteConsoleCommand`, `AcceptProposal`, `RejectProposal`).
  - Zamiast ręcznie parsować komendy, emituje zdarzenie `ConsoleCommandEvent` przez wbudowany `IMessenger`. Każdy moduł nasłuchuje szyny.
  - Interfejs konsoli został wyekstrahowany do w pełni hermetycznego pliku `ConsoleView.axaml` (ze zintegrowanym systemem auto-scroll). Ten sam widok jest renderowany dynamicznie przez `DataTemplate` zarówno w głównej kampanii, jak i w Sandboxie, eliminując całkowicie duplikację kodu UI i logiki.
- **Timekeeper Module (Czas i Kalendarz)**:
  - Zbudowany hermetyczny moduł oparty o stały kalendarz fantasy: 12 miesięcy, 30 dni w miesiącu (360 dni w roku).
  - Posiada obsługę komend: `/time +Xh`, `/time +Xd`, `/time +Xm` itp.
  - Zbudowano widok `TimekeeperView.axaml` ze wskaźnikami, dynamicznym słońcem/księżycem i przyciskami szybkiego przewijania.
- **Tryb Deweloperski i Środowisko Sandbox (Workbench)**:
  - Sandbox korzysta teraz z `Canvas` do bezdrganiowego (anti-jitter) renderowania prototypowanego modułu przy swobodnym resizowaniu panelu przez użytkownika.
  - Środowisko posiada zakotwiczoną na dole konsolę, która teraz wykorzystuje hermetyczny `ConsoleView` (działa tam m.in. auto-scroll, historia zdarzeń oraz zatwierdzanie akcji z `ProposalEvent`).
- **Nawigacja i Cykl Życia**:
  - Implementacja `OnNavigatedTo()` oraz `OnNavigatedFrom()` w `ViewModelBase`. W przypadku `SandboxTabViewModel` usunięto automatyczne zatrzymywanie silnika, dzięki czemu środowisko jest od razu rozgrzane przy przełączeniu zakładki.

## 2. Innowacje w Interfejsie Użytkownika

- **Zdarzenia Propozycji (Hover Actions)**: 
  - Wdrożono koncepcję znaną z Minecrafta (Tellraw / Hover action).
  - Konsola może renderować `ProposalEvent`, gdzie pojawia się zwykły tekst z ukrytymi/dodanymi przyciskami `[accept]` oraz `[reject]`. Dzięki temu Mistrz Gry nie musi wpisywać `/accept` z palca — wystarczy jedno kliknięcie na log w konsoli.
- **Safe-Shrink i Wyrównania**: 
  - Wszystkie widoki stosują blokadę przed "pixel jumpingiem". Zamiast podmieniać XAML, używa się przezroczystości `Opacity="0"` i blokady kliknięć `IsHitTestVisible="False"`.
  - Stosujemy `TextTrimming="CharacterEllipsis"` w tabelach i `VerticalContentAlignment="Center"` w stylach globalnych kontrolki tekstowej.

## 3. Planowane Zadania i Kolejne Kroki (Roadmap)

1. **Testowanie Systemu Komend w Sandboxie**: 
   - Moduł konsoli zyskał w Workbenchu pełną funkcjonalność. Można teraz bez problemu testować zaawansowane komendy (jak `/time +8h`) i ich skutki (Pojawienie się propozycji akceptacji) w czasie rzeczywistym na panelu Timekeepera obok.
2. **Kolejne Moduły Kampanii**:
   - Skoro fundament silnika jest gotowy (zdarzeniowa konsola, niezależny zapis, hermetyczne widoki `XxxView.axaml` wpinane przez DataTemplate i stabilne Sandbox UI), można przejść do wdrażania kolejnych, bardziej złożonych modułów: np. moduł pogody, notatek awaryjnych, czy podwalin pod **Combat Tracker**.
3. **Pełnoprawny Dashboard**:
   - `CampaignDashboardView` używa obecnie prostej nakładki kafelkowej (bento-box). Docelowo będzie musiał składać się z w pełni konfigurowalnych kafelków modułów bazujących na `ContentControl` ułatwiającym zarządzanie pozycją danego widoku.

## 4. Zasady AI (Przypomnienie reguł `.agents/AGENTS.md`)

- **Brak Pixel Jumpingu**: Zakaz zamykania i otwierania nowych kontrolek wpływających na rozmiar siatki (np. ukrywanie TextBlock, by pokazać TextBox).
- **Hermetyczność**: Każdy moduł UI musi być niezależny i możliwy do przetestowania w oknie Workbench bez ładowania całego systemu.
- **Pancerne Siatki**: Zamiast elastycznych marginów stosować MaxWidth i centralne ułożenie zapobiegające rozciąganiu na monitorach ultrawide. Zakaz używania spacji "Wrap" dla statycznych tekstów (używamy Ellipsis).

---

> **Instrukcja dla Agenta startującego z tym plikiem:** 
> Przeanalizuj powyższy stan. Następnie zapytaj użytkownika, czym chcemy się zająć w tej sesji (np. nowe moduły, dalszy szlif UI, debugowanie).
