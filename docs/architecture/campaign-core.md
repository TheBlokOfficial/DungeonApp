# Modułowy rdzeń kampanii

> **Status:** zaimplementowany pierwszy pionowy wycinek — wersja 0.1

## Cel

Kampania jest hostem dla opcjonalnych modułów. Core zna ich trwałe identyfikatory, wersje stanu, uporządkowaną historię zdarzeń i sposób przeprowadzenia jednej operacji — nie interpretuje mechaniki konkretnych modułów.

Pierwszy moduł: opcjonalny `core.clock`. Kampania może być utworzona bez niego; wtedy komenda przesunięcia czasu jest odrzucana jawnym błędem zamiast tworzyć niepełny stan.

## Przepływ operacji

```text
komenda MG-a
  -> wskazany moduł
  -> opublikowane zdarzenia domenowe
  -> deterministyczne reakcje aktywnych modułów
  -> wspólne zatwierdzenie stanu modułów i historii kampanii
```

Brak globalnej lub asynchronicznej szyny zdarzeń. Router zdarzeń żyje wewnątrz jednej kampanii i obsługuje jedną komendę synchronicznie — zdarzenia zachowują kolejność, powiązanie z komendą i przyczynę (poprzedzające zdarzenie).

Host pracuje na kopiach roboczych modułów. Błąd dowolnego subskrybenta odrzuca kopie i nowe wpisy historii — stan kampanii pozostaje bez zmian. Jedna komenda może wywołać maksymalnie 1000 zdarzeń (ochrona przed pętlą pub/sub).

## Kontrakty

- `CampaignCommand` — jawna prośba do konkretnego `ModuleId`, z identyfikatorem i powodem.
- `ICampaignEventPayload` — niezmienny fakt domenowy.
- `CampaignEvent` — koperta audytowa: własne ID, numer kolejności, moduł źródłowy, korelacja z komendą, przyczyna, czas świata.
- `ICampaignModule` — obsługa komend, reakcje na zdarzenia, utworzenie izolowanej kopii roboczej.
- `CampaignModuleDescriptor` — trwałe ID modułu i wersja jego stanu.

Aktywne moduły to konfiguracja tworzenia/odtworzenia kampanii. Włączenie modułu w istniejącej kampanii to komenda rdzenia `EnableCampaignModule`, realizowana przez zarejestrowaną fabrykę — UI nie tworzy modułów bezpośrednio. Host zapisuje `CampaignModuleEnabled`. Ponowne włączenie tego samego modułu jest odrzucane.

## Moduł zegara (`core.clock`)

`ClockModule` przechowuje `CampaignTime` — dodatni upływ od początku kampanii, nie kalendarz gregoriański (ruleset może później przedstawić go jako własne dni/miesiące/pory roku).

Komenda `AdvanceWorldTime` wymaga dodatniej wartości i powodu. Publikuje `WorldTimeAdvanced`, moduł zegara stosuje fakt do własnego stanu.

## Harmonogram zdarzeń świata (`core.scheduler`)

Opcjonalny moduł, jawnie wymaga aktywnego `core.clock`. Stan: lista `ScheduledWorldEvent` (stabilny identyfikator, termin w czasie kampanii, nazwa, powód).

Komenda `ScheduleWorldEvent` planuje `WorldEventScheduled` na dodatnie opóźnienie względem aktualnego czasu świata. Gdy moduł otrzyma `WorldTimeAdvanced` przekraczające termin zdarzenia, usuwa je z oczekującego stanu i publikuje `ScheduledWorldEventDue` — jedno przesunięcie zegara może poprawnie utworzyć wiele zdarzeń wynikowych, w stałej kolejności.

## Trwałość

`JsonCampaignRepository` zapisuje kampanię lokalnie, rozdzielając serializację od Domain przez adaptery:

- `ICampaignModulePersistenceAdapter` — zapisuje/odtwarza prywatny stan jednego modułu.
- `ICampaignEventPersistenceAdapter` — zapisuje/odtwarza wersjonowany payload jednego typu zdarzenia.

Adapter zegara: stan `core.clock` v1, zdarzenie `core.clock.world-time-advanced.v1`. Harmonogram: własny adapter stanu, adaptery zdarzeń `core.scheduler.world-event-scheduled.v1` i `core.scheduler.world-event-due.v1`. Nowy moduł dodaje własne adaptery zamiast zmieniać host kampanii lub format zapisu.

Repozytorium udostępnia też lekką listę kampanii, czytaną bez ładowania pełnego stanu każdej — używaną przez ekran wyboru/otwarcia kampanii.

Kontrakt trwałości opisuje [ADR-0002](adr/0002-lokalny-zapis-kampanii.md).
