# Co dalej — stan prac i kontekst

> **Status:** żywy dokument roboczy — aktualizacja 2026-08-27
> **Punkt odniesienia:** [Wizja i fundamenty](vision.md), [Plan implementacji UI](../ui/implementation-plan.md)

Ten dokument istnieje po to, żeby wracając do projektu po przerwie nie trzeba było odtwarzać kontekstu z historii gita. Opisuje, co już działa, co jest następne i **dlaczego akurat to** — decyzje, nie tylko listę zadań.

## Gdzie jesteśmy

Pętla z wizji domyka się po raz pierwszy end-to-end:

```text
zmiana czasu → działanie → konsekwencja → wyjaśnialna historia
   zegar         kości       harmonogram        kronika
```

Kampania jest katalogiem: manifest, plik stanu na moduł, dziennik i układ biurka. Manifest zapisywany jako ostatni jest znacznikiem commitu, a licznik generacji czyni rozerwany zapis wykrywalnym. Cztery moduły wbudowane (`core.clock`, `core.scheduler`, `core.party`, `core.dice`) włączane per kampania, z zależnościami domykanymi przy tworzeniu.

Wszystko, co MG robi na biurku, trafia do kroniki razem z powodem. Nic w Desktop nie zna reguł — panele proszą moduł i pokazują to, czym moduł odpowie, łącznie z treścią odmowy.

## Następne kroki

### 1. Konsola sesji — najtańsze domknięcie

MG nie ma dziś sposobu, żeby zapisać fakt własnymi słowami. Jedyne „działania” to te, które produkują moduły. Tymczasem zasada 4 wizji mówi, że ręczne korekty są dozwolone, ale **jako zapisane zdarzenia, nie ciche omijanie reguł** — a infrastruktura już na to czeka: `JournalEntry.Module == null` oznacza „MG działa bezpośrednio” i nikt z tego nie korzysta.

Zakres: pole wpisu + powód, wpis idzie do kroniki bez modułu. To panel, nie moduł — kronika należy do kampanii, nie do żadnego modułu.

### 2. Rejestry i pierwszy ruleset — wymaga decyzji przed kodem

Najdroższa decyzja w projekcie i jedyna pozycja, której **nie należy zaczynać od kodu**. Trzy rzeczy do rozstrzygnięcia:

**Gdzie mieszkają paczki zawartości.** Katalog obok biblioteki kampanii (`Documents/DungeonApp/Packs`) czy w danych aplikacji? Paczka jest dokumentem do wymiany między ludźmi, co przemawia za pierwszym — tak samo jak kampania trafiła do `Documents`, a nie do `%LocalAppData%`.

**Czy rejestr jest modułem.** Roboczo: nie. Paczka jest *poza* kampanią, kampania tylko wskazuje, których używa. Konsekwencja jest jednak realna — rdzeń musi wiedzieć o paczkach, czyli `CampaignManifest.ContentPacks` (dziś pole istnieje, ale zawsze puste) staje się prawdziwym wiązaniem, a otwarcie kampanii bez wymaganej paczki musi mieć zdefiniowane zachowanie.

**Zakres pierwszego rulesetu.** Prototyp do weryfikacji rdzenia czy zalążek autorskiego systemu? To wciąż otwarta decyzja w [wizji](vision.md), a wpływa na obie powyższe.

Granica, którą już postawiliśmy i której trzeba pilnować: zapis rzutu zna kości i jeden płaski modyfikator. **Znaczenie** modyfikatora należy do rulesetu. Jeżeli notacja kości zacznie rozumieć atrybuty, premie albo przewagę — ruleset wszedł tylnymi drzwiami i decyzja powyżej została podjęta przypadkiem.

### 3. Tryb aktywnej sesji — świadomie odłożone

Rozróżnienie „sesja trwa / nie trwa”, czas sesji, podsumowanie na koniec. Odłożone, bo wprowadza **nowe pojęcie do rdzenia** (sesja jako byt trwały), a pełny przepływ z wizji działa bez niego. Wracamy do tego, kiedy pojawi się rzecz, której bez tego pojęcia nie da się zrobić — nie wcześniej.

## Dług i drobiazgi

| Rzecz | Kontekst |
| --- | --- |
| Ikona kości | Panel używa `DungeonIconBoxes` jako zastępnika — w [Icons.axaml](../../src/DungeonApp.Desktop/Themes/Icons.axaml) nie ma kości. |
| Kreator kampanii jako kreator | Wybór modułów żyje w karcie biblioteki. [Faza 3](../ui/implementation-plan.md) przewiduje kroki (podstawy → ruleset → moduły → podsumowanie); pełny kreator ma sens dopiero z paczkami i rulesetem, czyli po punkcie 2. |
| Router kontekstów | `AppShellViewModel.OnSectionSelected` rozgałęzia się na `if` po identyfikatorze sekcji. Wystarczy dziś, nie wystarczy przy trzecim kontekście. |
| Ekran Ustawień | Placeholder — profil skali UI zapisuje się, ale nie da się go zmienić z aplikacji. |
| Zapis rozmiaru i pozycji okna | `AppSettingsStore` trzyma tylko profil skali i wariant sidebara. |
| Testy ViewModeli shellu | `DungeonApp.Desktop.Tests` pokrywa cache przygotowania, layout i wybór modułów. Reszta shellu — nie. |
| Wyłączanie modułów w istniejącej kampanii | Manifest to potrafi (stan nieznanego modułu jest zachowywany nietknięty), ale nie ma UI. |

## Czego nie robić bez rozmowy

- **Nie wskrzeszać** starego prototypu domain/application/infrastructure z historii gita.
- **Nie dodawać abstrakcji, których jedynym uzasadnieniem jest wymienność** — podział na projekty jest zabiegiem higienicznym, nie przygotowaniem na wymianę frontendu.
- **Nie publikować zdarzeń, których nikt nie słucha.** Kanał ogłoszeń powstał dopiero wtedy, gdy drugi moduł realnie go potrzebował, i ta dyscyplina ma zostać: zdarzenie bez słuchacza to zgadywanie przyszłości.
- **Nie wydzielać projektu `Infrastructure`** przed wyzwalaczem opisanym w [wizji](vision.md).
- **Nie robić z kroniki źródła prawdy.** Jest kroniką dla MG-a; stan odtwarza się ze snapshotów modułów.
