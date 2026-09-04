# Wizja

## Czym to jest

Desktopowy panel Mistrza Gry. Narzędzie do prowadzenia sesji przy stole —
nie stół wirtualny, nie gra dla graczy, nie arbiter narracji.

Czym świadomie **nie** jest:

- **Nie VTT.** Brak mapy taktycznej, brak figurek, brak trybu dla graczy.
- **Nie sieciowe.** Jedna maszyna, jeden użytkownik, żadnego kodu
  sieciowego ani synchronizacji.
- **Nie arbiter.** Silnik liczy i przedstawia wynik, decyzję podejmuje MG.
  Aplikacja nie blokuje niczego poza operacjami nieodwracalnymi.

Kryterium sukcesu jest jedno: przy stole, w trakcie sesji, odczytanie stanu
i wprowadzenie zmiany ma być szybsze niż sięgnięcie po kartkę.

## Decyzje fundamentalne

**Core nie zna świata gry.** Rdzeń wie, czym jest kampania, moduł, dziennik
i zapis — nie wie, czym jest miecz, NPC ani zaklęcie. Tożsamość rzeczy w
świecie należy do modułów i do treści, nigdy do rdzenia. Dzięki temu
zmiana systemu gry nie dotyka fundamentu.

**Moduły wbudowane, nie pluginy.** Funkcjonalność dzieli się na moduły
włączane per kampania, ale wszystkie są skompilowane w aplikację. Nie ma
dynamicznego ładowania z dysku i nie planujemy go — cena magii ładowania
przewyższa zysk w projekcie jednoosobowym.

**Stan jako snapshot, nie event sourcing.** Kampania trzyma bieżący stan
modułów. Dziennik jest kroniką dla człowieka, nie źródłem prawdy do
odtworzenia stanu. Odwrotna decyzja kosztowałaby migracje przy każdej
zmianie kształtu zdarzenia.

**Kampania to katalog, nie plik.** Manifest, stany modułów, dziennik i
układ biurka jako osobne pliki. Rozerwany zapis daje się wtedy wykryć,
zamiast po cichu uszkodzić całość.

**Zdarzenia tylko dla faktów dokonanych.** Moduł ogłasza to, co się już
stało — nie prosi zdarzeniem o wykonanie czegoś. Zdarzenia bez słuchacza
nie powstają.

**Notacja kości zna kości i jeden płaski modyfikator.** Co ten modyfikator
znaczy, wie system gry, nie moduł rzutu. Moduł jest bezstanowy.

Szczegóły techniczne każdej z tych decyzji — w `docs/architecture.md`.

## Odłożone świadomie

Poniższe nie są porzucone, tylko odsunięte. Kod miejscami ma już na nie
miejsce, ale nikt z niego nie korzysta.

- **Tryb aktywnej sesji.** Sesja jako byt trwały, z początkiem, końcem i
  własną historią. Dziś czas biegnie bez ramy sesji.
- **Konsola sesji.** Wolny wpis MG-a do kroniki. `JournalEntry` dopuszcza
  już wpis bez modułu, ale nie ma interfejsu, który by go tworzył. To
  najtańsze domknięcie pierwszego wycinka.
- **Przedmioty i paczki treści.** Przedmiot jako referencja do definicji z
  paczki plus nadpisania instancji; warianty jako osobne definicje.
  `CampaignManifest.ContentPacks` istnieje i jest puste.
- **Pełna obsługa klawiatury.** Skróty i nawigacja bez myszy — wymóg
  wynikający z użycia przy stole, jeszcze niezrealizowany.

## Nierozstrzygnięte

**Gdzie mieszka system gry.** Pierwszy ruleset miał powstać w całości w
kodzie, z obliczeniami adresowanymi po nazwie. W repozytorium nie ma go w
żadnej postaci. To najdroższa otwarta decyzja w projekcie i nic jej nie
przesądza — dopóki nie zapadnie, `Core` pozostaje wolny od systemu gry, co
jest stanem pożądanym, nie tymczasowym.

## Czego nie wskrzeszamy

Poprzednie podejście dzieliło kod na `domain` / `application` /
`infrastructure`. Zostało porzucone świadomie: przy jednym użytkowniku i
jednym procesie te warstwy generowały ceremonię bez zysku. Obecny podział —
`Core` i `Desktop`, moduły w środku — jest odpowiedzią na tamten koszt.
Nie wracamy do tego, nawet gdy pojawi się pokusa „porządku”.
