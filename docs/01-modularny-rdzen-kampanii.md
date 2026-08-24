# Modułowy rdzeń kampanii

> **Status:** zaimplementowany pierwszy pionowy wycinek  
> **Wersja:** 0.1

## Cel

Kampania jest hostem dla opcjonalnych modułów. Core zna ich trwałe identyfikatory, wersje stanu, uporządkowaną historię zdarzeń oraz sposób przeprowadzenia jednej operacji. Nie interpretuje jednak mechaniki zegara ani przyszłych modułów.

Pierwszą implementacją jest opcjonalny moduł `core.clock`. Kampania może być utworzona bez niego; wówczas komenda przesunięcia czasu jest odrzucana jednoznacznym błędem zamiast tworzyć niepełny stan.

## Przepływ operacji

```text
komenda MG-a
  -> wskazany moduł
  -> opublikowane zdarzenia domenowe
  -> deterministyczne reakcje aktywnych modułów
  -> wspólne zatwierdzenie stanu modułów i historii kampanii
```

Nie ma globalnej ani asynchronicznej szyny zdarzeń. Router zdarzeń żyje wewnątrz jednej kampanii i obsługuje jedną komendę synchronicznie. Dzięki temu zdarzenia zachowują kolejność, powiązanie z komendą oraz ewentualną przyczynę w postaci wcześniejszego zdarzenia.

Host pracuje na kopiach roboczych modułów. Gdy którykolwiek subskrybent zgłosi błąd, kopie i nowe wpisy historii są odrzucane; stan kampanii pozostaje bez zmian. Jedna komenda może spowodować najwyżej 1000 zdarzeń, co chroni przed niezamierzoną pętlą pub/sub.

## Kontrakty

- `CampaignCommand` jest jawną prośbą skierowaną do konkretnego `ModuleId`; zawiera identyfikator i powód.
- `ICampaignEventPayload` opisuje niezmienny fakt domenowy.
- `CampaignEvent` jest kopertą audytową: ma własne ID, numer kolejności, moduł źródłowy, korelację z komendą, przyczynę i czas świata.
- `ICampaignModule` implementuje obsługę komend, reakcje na zdarzenia i utworzenie izolowanej kopii roboczej.
- `CampaignModuleDescriptor` zawiera trwałe ID modułu oraz wersję jego stanu.

Aktywne moduły są konfiguracją tworzenia lub odtworzenia kampanii. Włączenie modułu w istniejącej kampanii jest komendą rdzenia `EnableCampaignModule`, realizowaną przez zarejestrowaną fabrykę modułu. Host zapisuje wówczas `CampaignModuleEnabled`; UI nie tworzy modułów bezpośrednio. Ponowne włączenie tego samego modułu jest odrzucane.

## Moduł zegara

`ClockModule` przechowuje `CampaignTime` — dodatni upływ od początku kampanii, a nie kalendarz gregoriański. Ruleset może później przedstawić go jako własne dni, miesiące i pory roku.

Komenda `AdvanceWorldTime` wymaga dodatniej wartości czasu i powodu. Publikuje `WorldTimeAdvanced`, a moduł zegara stosuje ten fakt do własnego stanu. Historia kampanii pozostaje więc wyjaśnialna.

## Harmonogram zdarzeń świata

`core.scheduler` jest opcjonalnym modułem, który jawnie wymaga aktywnego `core.clock`. Jego stan jest listą `ScheduledWorldEvent`: stabilny identyfikator, termin w czasie kampanii, nazwa oraz powód.

Komenda `ScheduleWorldEvent` planuje fakt `WorldEventScheduled` na dodatnie opóźnienie względem aktualnego czasu świata. Gdy moduł otrzyma `WorldTimeAdvanced` przekraczające termin zdarzenia, usuwa je z własnego oczekującego stanu i publikuje `ScheduledWorldEventDue`. Dzięki temu pojedyncze przesunięcie zegara może poprawnie utworzyć wiele zdarzeń wynikowych, w stałej kolejności.

## Trwałość

`JsonCampaignRepository` zapisuje kampanię lokalnie i rozdziela serializację od Domain za pomocą adapterów:

- `ICampaignModulePersistenceAdapter` zapisuje i odtwarza prywatny stan jednego modułu;
- `ICampaignEventPersistenceAdapter` zapisuje i odtwarza wersjonowany payload jednego typu zdarzenia.

Adapter zegara obsługuje stan `core.clock` v1 oraz zdarzenie `core.clock.world-time-advanced.v1`. Nowy moduł dodaje własne adaptery zamiast zmieniać host kampanii lub ogólny format zapisu.

Harmonogram ma własny adapter stanu oraz osobne adaptery zdarzeń `core.scheduler.world-event-scheduled.v1` i `core.scheduler.world-event-due.v1`.

Repozytorium udostępnia także lekką listę kampanii, odczytywaną z lokalnych dokumentów bez ładowania pełnego stanu każdej kampanii. Ekran aplikacji korzysta z niej do wyboru i otwarcia zapisanej kampanii.
