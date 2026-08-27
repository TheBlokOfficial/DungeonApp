# Plan implementacji UI

> **Status:** plan wykonawczy — wersja 0.1
> **Punkt odniesienia:** [Kontrakt UI](contract.md), [Architektura komponentów UI](component-architecture.md)

## Zasada realizacji

Interfejs powstaje pionowymi, działającymi przyrostami: kompozycja widoków, stan prezentacyjny, podłączenie do przypadków użycia, weryfikacja geometrii. Nie budujemy z góry pustych ekranów przyszłych modułów.

## Faza 1 — shell i biblioteka kampanii

**Status: odbudowana na nowym rdzeniu (2026-08-27).** Pierwotny ekran biblioteki został usunięty razem z prototypem domeny (2026-08-26). Wrócił oparty na `DungeonApp.Core`: `CampaignLibraryViewModel` czyta listę przez `ICampaignRepository.ListAsync`, tworzy przez `CreateCampaign`, a otwarcie kampanii przełącza zawartość workspace'u na pulpit. Tworzenie jest statycznym formularzem na powierzchni zaplecza, nie nakładką — patrz [kierunek wizualny](visual-direction.md), „Zaplecze i stół”.

- wspólne tokeny kolorów, typografii i bazowych wymiarów;
- `MainWindow` ograniczony do osadzenia `AppShellView`;
- hermetyczne komponenty topbara, globalnego sidebara, paska statusu;
- ekran biblioteki z listą o stałej wysokości wiersza;
- tworzenie kampanii, lista, empty state i status scalone w jedną podniesioną kartę zaplecza, z zarezerwowanym slotem walidacji;
- odczyt/utworzenie/otwarcie kampanii podłączone do use case'ów `Core`;
- stabilny empty state i lokalny viewport listy.

Otwarcie kampanii przełącza kontekst shellu: topbar pokazuje nazwę kampanii i akcję „Zamknij kampanię”, obszar roboczy odsłania pulpit. Zamknięcie zapisuje układ biurka i wraca do biblioteki.

## Faza 1.2 — gotowość i cold path

**Status: zaimplementowana (2026-08-27).**

- shell renderuje lekką pierwszą klatkę, potem przechodzi przez jawną bramkę `Starting → Ready`;
- `CampaignWorkspacePreparationCache` przygotowuje wszystkie kampanie z biblioteki z maksymalnie dwoma równoległymi odczytami i współdzieli zadanie z kliknięciem, jeśli operacje się spotkają;
- kampania, layout i pierwsze 100 wpisów kroniki są gotowe przed utworzeniem `CampaignWorkspaceViewModel`;
- `WorkspaceLayoutStore.LoadAsync` usuwa synchroniczne I/O z konstruktora workspace'u;
- początkowa kronika jest mapowana poza UI thread i publikowana jako gotowa lista, nie przez serię zmian `ObservableCollection`;
- przygotowana kampania jest niekonsumująco pożyczana z cache'u, a pełny `CampaignWorkspaceView` z realnym `CampaignWorkspaceViewModel` przechodzi jeden layout/render przed odblokowaniem nawigacji; syntetyczny `WorkspaceWarmupView` pozostaje fallbackiem dla pustej biblioteki;
- nieudane przygotowanie pojedynczej kampanii nie blokuje startu; jej otwarcie ponawia odczyt i korzysta z istniejącej obsługi błędów;
- Desktop jest publikowany z ReadyToRun;
- testy Desktop obejmują wykorzystanie rozgrzanego cache'u, brak blokady przez usuniętą kampanię i asynchroniczne przywracanie layoutu.

## Faza 1.1 — stabilizacja wizualna

**Status: zaimplementowana.**

- jawne `PerMonitorV2` i profile `Small`/`Medium`/`Large`;
- lokalny font Alegreya dla sygnetu, tytułów, inicjałów;
- kuratorowany, przypięty do commita podzbiór Lucide z licencją;
- wektorowe ikony w stałych, wycentrowanych slotach;
- rozłączne stany hover/active sidebara, bez domyślnych powierzchni Fluent;
- uproszczone ramy topbara, sidebara, statusbara;
- biblioteka jako jedna karta o wysokości wynikającej z treści, bez ramy obejmującej pusty viewport;
- wariant Wide master–detail zamiast rozciągania metadanych na całą szerokość.

## Faza 2 — nawigacja i trwały stan okna

- ✅ podstawowa nawigacja globalna: sidebar (grupy "Biblioteka"/"System") przełącza zawartość obszaru roboczego przez `AppShellViewModel.CurrentWorkspaceContent` + `DataTemplate`, na razie na proste placeholdery;
- jawny router pozostałych dwóch kontekstów (kreator, workspace kampanii) — do zrobienia, gdy wrócą;
- deskryptory ekranów zamiast warunków rozproszonych po widokach;
- obsługa klawiatury dla nawigacji;
- wariant kompaktowy sidebara świadomie usunięty (2026-08-26) — kod miał być jak najprostszy; wróci, jeśli będzie realna potrzeba;
- ✅ zapis i bezpieczne przywracanie profilu skali UI (`AppSettingsStore`, `%LocalAppData%\DungeonApp\settings.json`) — zapis rozmiaru/pozycji okna pozostaje do zrobienia; ekran Ustawień w sidebarze jest na razie placeholderem, bez pickera skali UI;
- pierwsze testy ViewModeli shellu i routingu.

## Faza 3 — kreator kampanii

**Status: częściowo, poza kreatorem (2026-08-27).** Wybór modułów działa, ale mieszka w karcie „Nowa kampania” na zapleczu, nie w wieloetapowym kreatorze: `CampaignLibraryViewModel` buduje listę z `ModuleCatalog.Manifests`, a moduł wciągnięty przez zależność jest pokazany jako zaznaczony i zablokowany, z podpowiedzią nazywającą, co go trzyma. Zamiana na pełny kreator ma sens dopiero razem z paczkami zawartości i rulesetem — patrz [Co dalej](../product/roadmap.md).

- stabilny układ kroków: podstawy, ruleset i zawartość, moduły, podsumowanie;
- zapis szkicu od pierwszego kroku;
- stała strefa akcji `Wstecz`/`Dalej`;
- zarezerwowane miejsca walidacji, bez przesuwania formularza;
- filtrowanie kompatybilnych paczek i modułów przez `Application`.

## Faza 4 — workspace kampanii i dashboard sesji

**Status: pulpit działa na realnych modułach (2026-08-27).** Biurko składa się z paneli budowanych per kampania (`PanelCatalog.For`): drużyna, czas świata, kości, harmonogram i kronika. Panel jest oferowany tylko wtedy, gdy kampania ma odpowiedni moduł, a zapisany układ nazywający panel nieistniejący jest pomijany, nie traktowany jako błąd. Żaden panel nie zna reguł — prosi moduł i pokazuje jego odpowiedź, łącznie z treścią odmowy. Zostają warianty dashboardu i notatki.

- przełączenie topbara i sidebara na kontekst otwartej kampanii;
- dashboard w wariantach `Compact`/`Standard`/`Wide`;
- panele drużyny, czasu, zdarzeń, notatki, historii jako osobne komponenty;
- formalne polityki `FixedMetric`/`Bounded`/`FluidData`;
- wykorzystanie istniejących modułów zegara i harmonogramu bez reguł domenowych w Desktop.

## Faza 5 — komponenty gęstych danych i baza wiedzy

- generyczna rama panelu dopiero po ≥2 realnych zastosowaniach;
- tabele i rejestry o stałych wysokościach wierszy;
- statblock Entity i widok read-first;
- układ dokument–drzewo–inspektor bez globalnego ograniczenia szerokości workspace'u;
- rozwój lokalnego podzbioru ikon Lucide wyłącznie o symbole wymagane przez kolejne moduły.

## Bramka jakości każdego przyrostu

1. Rozwiązanie kompiluje się bez ostrzeżeń.
2. Testy istniejącego rdzenia pozostają zielone.
3. Bindingi są kompilowane i typowane.
4. Loading/empty/ready/error nie zmieniają zewnętrznej geometrii komponentu.
5. Ekran sprawdzony co najmniej w `1024 × 680`, `1280 × 800` i jednym szerokim wariancie.
6. Nowe surowe kolory i wspólne metryki nie są rozproszone po plikach widoków.
7. Komponent ma jasno określonego właściciela stanu, bez wywoływania infrastruktury z code-behind.

## Najbliższy krok

Konsola sesji — jedyne ogniwo przepływu z wizji, którego brakuje: MG nie ma jak zapisać faktu własnymi słowami, choć kronika już rozróżnia wpis modułu od wpisu MG-a.

Router kontekstów i trwały stan okna z fazy 2 pozostają do zrobienia i są warunkiem pełnego kreatora — pominięcie tej granicy ponownie skupiłoby odpowiedzialności w jednym ViewModelu. Pełny obraz pozostałych prac wraz z kontekstem decyzji: [Co dalej](../product/roadmap.md).
