# Architektura informacji i layout aplikacji

> **Status:** zatwierdzony fundament UX i nawigacji — wersja 0.1

Techniczny podział na widoki/kontrolki/ViewModele: [Architektura komponentów UI](component-architecture.md). Normatywne wymiary i breakpointy: [Kontrakt UI](contract.md).

## Główna zasada

Klasyczny układ **sidebar + workspace**, traktowany jako stały układ współrzędnych, nie generyczny szablon. Oryginalność wynika z języka wizualnego, zachowania paneli i prezentacji danych — nawigacja pozostaje znajoma, szybka, przewidywalna.

```text
┌──────────────────────────────────────────────────────┐
│ pasek górny: marka + bieżący kontekst + szybkie akcje│
├───────────────┬──────────────────────────────────────┤
│ sidebar       │ workspace aktywnego ekranu            │
│ bieżącego     │                                       │
│ zakresu       │                                       │
├───────────────┴──────────────────────────────────────┤
│ pasek statusu: zapis, tryb lokalny, skróty           │
└──────────────────────────────────────────────────────┘
```

Geometria shellu zmienia się wyłącznie wskutek jawnej zmiany kontekstu, wariantu rozmiaru okna albo działania użytkownika.

## Trzy konteksty aplikacji

Nie ma jednego sidebara z zasobami globalnymi, krokami kreatora i modułami kampanii naraz — to trzy rozłączne konteksty.

### Poziom globalny

```text
Kampanie
Bohaterowie
Paczki zawartości
────────────
Ustawienia
```

- **Kampanie** — lista, szkice, akcja utworzenia nowej.
- **Bohaterowie** — trwałe profile postaci graczy między kampaniami.
- **Paczki zawartości** — instalacja, wersje, źródła, kompatybilność.
- **Ustawienia** — język, skala UI, typografia, motyw, skróty, lokalizacja biblioteki, kopie bezpieczeństwa.

Sidebar globalny ma pozostać krótki — bez narzędzi aktywnej kampanii.

### Kreator kampanii

Przejściowy, liniowy proces, nie stała sekcja nawigacji.

```text
1. Podstawy
2. Ruleset i zawartość
3. Moduły
4. Podsumowanie
```

Kroki w stabilnym panelu bocznym, akcje `Wstecz`/`Dalej` mają stałą lokalizację. Kampania zapisywana jako szkic od początku — przerwanie kreatora nie traci poprawnie wprowadzonych danych.

Ruleset wybierany przed paczkami i modułami; kolejne kroki pokazują tylko elementy kompatybilne z dotychczasową konfiguracją. Podsumowanie jawnie wskazuje brakujące zależności i konsekwencje.

### Workspace kampanii

Po otwarciu kampanii sidebar globalny zastępuje nawigacja świata, ze stałym przejściem do biblioteki kampanii.

```text
← Biblioteka kampanii

SESJA
  Pulpit
  Spotkanie
  Czas i zdarzenia

ŚWIAT
  Postacie
  Lokacje
  Bestiariusz
  Przedmioty

WIEDZA
  Notatki i lore
  Relacje

KAMPANIA
  Moduły
  Paczki
  Konfiguracja
```

Pozycje zależne od nieaktywnych modułów mogą być niewidoczne, ale kolejność grup i podstawowych elementów jest stabilna — zmiana stanu domenowego nie reorganizuje sidebara.

## Pasek górny

Odpowiada na „gdzie jestem?", nie jest drugim rozbudowanym menu. Zawiera: znak `D` + nazwę aplikacji, nazwę bieżącego kontekstu, w kampanii — skróconą informację o czasie świata (jeśli moduł czasu aktywny), wyszukiwanie/paletę poleceń, niewielką liczbę globalnych akcji. Wszystko w ustalonych slotach — zmiana wartości nie zmienia wysokości paska.

## Pasek statusu

Stała wysokość: stan lokalnego zapisu, trwająca/ostatnia operacja, tryb offline/lokalny, podpowiedź skrótu. Błędy wymagające działania otwierają nakładkę — pasek sam nie rośnie z długością komunikatu.

## Pulpit aktywnej sesji

Ekran orientacyjny, nie prezentacja całej kampanii. Odpowiada na: kto bierze udział w sesji, jaki jest czas i stan świata, co wydarzy się wkrótce, co ostatnio się zmieniło.

Zatwierdzone panele startowe: drużyna z kluczowymi parametrami, czas świata + akcja jego przesunięcia, nadchodzące zdarzenia, krótka notatka sesyjna, ostatnie wyjaśnialne zmiany. Tracker spotkania, pełna mapa, baza przedmiotów i edytor notatek mają własne ekrany — pulpit pokazuje co najwyżej skróty, nie staje się nieskończonym dashboardem kafelków. Panele mają jawne pozycje/rozmiary w gridzie; nowe dane przewijają się wewnątrz panelu, nie zmieniają geometrii pulpitu.

## Moduły a nawigacja

Moduł nie dopisuje dowolnie równorzędnych pozycji do sidebara. Może wnieść: ekran przypisany do jednej zatwierdzonej grupy (`Sesja`, `Świat`, `Wiedza`, `Kampania`), panel/skrót pulpitu o określonym kontrakcie rozmiaru, akcje w palecie poleceń, typy prezentacji/edycji własnych danych. Nowa grupa wymaga jawnej decyzji projektowej. Nazwa techniczna modułu ≠ nazwa pozycji nawigacyjnej (np. `core.scheduler` współtworzy „Czas i zdarzenia").

## Globalni bohaterowie i stan kampanii

Globalny bohater ma trwałą tożsamość i dane profilu; udział w kampanii jest osobnym pojęciem — kampania nie modyfikuje bezwarunkowo globalnej karty. Docelowo UI rozróżnia: utworzenie kampanijnej kopii, powiązanie z globalnym profilem, świadomą synchronizację wybranych danych. HP, ekwipunek, efekty, historia domyślnie należą do kampanii; zakres synchronizacji musi być jawnie komunikowany.

## Paczki zawartości w UX

Menedżer globalny odpowiada za instalację/wersje/źródła/kompatybilność; workspace kampanii pokazuje wyłącznie paczki przypięte do danego świata. Kampania przypina konkretną wersję — aktualizacja globalnego pakietu nie zmienia automatycznie trwającej kampanii; migracja jest jawną operacją pokazującą konsekwencje.

```text
ruleset -> kompatybilne paczki zawartości -> kompatybilne moduły i konfiguracja
```

## Baza wiedzy i Entity

Brak konkurencyjnych źródeł prawdy (osobny NPC mechaniczny + niezależna notatka o tym samym NPC). `Entity` jest kanoniczną tożsamością świata, do której dołączają: statblock/stan mechaniczny, opis i notatki MG-a, relacje, zdarzenia/historia, backlinki, ilustracje/assety. Moduł „Postacie" pokazuje perspektywę mechaniczną, „Notatki i lore" — dokumentacyjną; oba prowadzą do tej samej tożsamości, bez duplikacji danych.

## Wyszukiwanie i paleta poleceń

Jedno globalne wejście w pasku górnym. Docelowo: przejście do ekranu, otwarcie Entity/dokumentu, wykonanie częstej akcji, wyszukanie kampanii/bohatera/definicji zawartości. Paleta jest nakładką — nie wpływa na geometrię workspace'u.

## Reguły stabilności layoutu

- Sidebar ma stałą szerokość w danym wariancie.
- Topbar i statusbar mają stałą wysokość.
- Nagłówek workspace'u ma przewidziany obszar tytułu i akcji.
- Każdy panel ma stały nagłówek i ograniczony viewport treści.
- Przewijanie wewnątrz panelu/workspace'u, nie strony.
- Pozycje nawigacji mają stałą wysokość i slot ikony.
- Długie nazwy skracane, pełna wartość dostępna bez zmiany geometrii.
- Loading/empty/error zachowują rozmiar docelowego obszaru.
- Zwijanie sidebara, dokowanie, zmiana proporcji — wyłącznie jawne akcje użytkownika.

## Zatwierdzony charakter makiety

Wyraziste, subtelne obramowania · ciepła paleta grafitów/kremu/bursztynu · elegancki zwarty sidebar · minimalistyczne monochromatyczne ikony liniowe · szeryfowe akcenty w nazwach i nagłówkach · znak `D` w kwadratowej ramie · płaskie powierzchnie bez ciężkich tekstur · grid pulpitu ze stabilnymi proporcjami · niewielkie promienie narożników, ograniczony cień · wyraźne rozdzielenie poziomu globalnego, kreatora i kampanii.

Makieta jest wzorcem hierarchii i proporcji, nie specyfikacją każdego piksela.

## Otwarte decyzje przed implementacją pełnych modułów

- Minimalna wspierana wielkość okna i warianty układu.
- Dokładna szerokość sidebara oraz wysokości pasków/wierszy.
- Zachowanie sidebara w wariancie kompaktowym.
- Ostateczny krój szeryfowy.
- Zakres pierwszej wersji wyszukiwania i palety poleceń.
- Kolejność modułów po pierwszym pionowym wycinku sesji.
- Czy układ paneli pulpitu jest w v1 stały czy konfigurowalny.
