# Architektura informacji i layout aplikacji

> **Status:** zatwierdzony fundament UX i nawigacji  
> **Wersja:** 0.1  
> **Cel:** określenie stabilnego shellu, zakresów nawigacji i reguł włączania kolejnych modułów do interfejsu.

Techniczny sposób podziału tego layoutu na widoki, kontrolki i ViewModele opisuje dokument [Architektura komponentów UI](04-architektura-komponentow-ui.md).
Normatywne wymiary shellu, breakpointy workspace'u i zasady maksymalizacji zawiera [Kontrakt UI v1](05-kontrakt-ui-v1.md).

## 1. Główna zasada

DungeonApp używa klasycznego układu **sidebar + workspace**. Nie jest on traktowany jako generyczny szablon, lecz jako stały układ współrzędnych dla rozbudowanego narzędzia Mistrza Gry.

Oryginalność aplikacji wynika z języka wizualnego, zachowania paneli i prezentacji danych. Nawigacja ma pozostać znajoma, szybka i przewidywalna.

Shell składa się z czterech trwałych stref:

```text
┌──────────────────────────────────────────────────────┐
│ pasek górny: marka + bieżący kontekst + szybkie akcje│
├───────────────┬──────────────────────────────────────┤
│               │                                      │
│ sidebar       │ workspace                            │
│ bieżącego     │ aktywnego ekranu                     │
│ zakresu       │                                      │
│               │                                      │
├───────────────┴──────────────────────────────────────┤
│ pasek statusu: zapis, tryb lokalny, skróty           │
└──────────────────────────────────────────────────────┘
```

Geometria shellu jest stabilna. Zmienia się wyłącznie wskutek jawnej zmiany kontekstu, wariantu rozmiaru okna albo działania użytkownika.

## 2. Trzy konteksty aplikacji

Nie stosujemy jednego sidebara zawierającego jednocześnie zasoby globalne, kroki kreatora i moduły otwartej kampanii. Aplikacja ma trzy rozłączne konteksty.

### 2.1. Poziom globalny

Poziom globalny jest biblioteką Mistrza Gry niezależną od konkretnej kampanii.

```text
Kampanie
Bohaterowie
Paczki zawartości
────────────
Ustawienia
```

- **Kampanie** — lista kampanii, szkiców, ostatnio otwieranych światów oraz akcja utworzenia nowej kampanii.
- **Bohaterowie** — trwałe profile postaci graczy dostępne pomiędzy kampaniami.
- **Paczki zawartości** — instalacja, walidacja, wersje i kompatybilność globalnych źródeł danych.
- **Ustawienia** — konfiguracja samej aplikacji: język, skala UI, typografia, motyw, skróty, lokalizacja biblioteki i kopie bezpieczeństwa.

Sidebar globalny ma pozostać krótki. Nie trafiają do niego narzędzia aktywnej kampanii.

### 2.2. Kreator kampanii

Kreator jest przejściowym, liniowym procesem, a nie stałą sekcją nawigacji.

```text
1. Podstawy
2. Ruleset i zawartość
3. Moduły
4. Podsumowanie
```

Kroki są prezentowane w stabilnym panelu bocznym. Bieżąca zawartość kroku zajmuje workspace, a akcje `Wstecz` i `Dalej` mają stałą lokalizację.

Kampania jest zapisywana jako szkic od początku procesu. Przerwanie kreatora nie może powodować utraty poprawnie wprowadzonych danych.

Ruleset jest wybierany przed paczkami i modułami. Kolejne kroki pokazują tylko elementy kompatybilne z dotychczasową konfiguracją. Podsumowanie musi jawnie wskazywać brakujące zależności i konsekwencje konfiguracji.

### 2.3. Workspace kampanii

Po otwarciu kampanii sidebar globalny zostaje zastąpiony nawigacją konkretnego świata. Widoczne pozostaje stałe, jednoznaczne przejście do biblioteki kampanii.

Docelowe grupy nawigacji:

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

Pozycje zależne od nieaktywnych modułów mogą być niewidoczne, ale kolejność grup i podstawowych elementów pozostaje stabilna. Zmiana stanu domenowego nie może reorganizować sidebara.

## 3. Pasek górny

Pasek górny odpowiada na pytanie „gdzie jestem?” i nie pełni roli drugiego rozbudowanego menu.

Zawiera:

- znak `D` oraz nazwę aplikacji;
- nazwę bieżącego kontekstu globalnego, kreatora albo kampanii;
- w kampanii: skróconą informację o stanie sesji i czasie świata, jeżeli moduł czasu jest aktywny;
- wyszukiwanie lub paletę poleceń;
- niewielką liczbę globalnych akcji kontekstowych.

Dane w pasku mają ustalone sloty. Zmiana wartości czasu, nazwy kampanii lub statusu nie może zmieniać jego wysokości.

## 4. Pasek statusu

Dolny pasek statusu ma stałą wysokość i służy do informacji technicznych, które nie powinny wypychać głównej treści:

- stan lokalnego zapisu;
- trwająca operacja lub ostatni wynik operacji;
- informacja o trybie offline/lokalnym;
- podpowiedź najważniejszego skrótu klawiaturowego.

Błędy wymagające działania mogą otwierać szczegóły lub powiadomienie ponad layoutem. Sam pasek nie rośnie wraz z długością komunikatu.

## 5. Pulpit aktywnej sesji

Pulpit jest ekranem orientacyjnym, a nie miejscem prezentacji całej kampanii. Powinien odpowiadać przede wszystkim na cztery pytania:

1. Kto bierze udział w bieżącej sesji?
2. Jaki jest aktualny czas i istotny stan świata?
3. Co wydarzy się wkrótce?
4. Co ostatnio uległo zmianie?

Zatwierdzony zestaw startowych paneli:

- drużyna i jej najważniejsze parametry;
- czas świata oraz jawna akcja jego przesunięcia;
- nadchodzące zdarzenia;
- krótka notatka sesyjna;
- ostatnie wyjaśnialne zmiany świata.

Tracker spotkania, pełna mapa, baza przedmiotów i rozbudowany edytor notatek mają własne ekrany. Pulpit może prezentować skróty lub podglądy, lecz nie może stać się nieskończonym dashboardem kafelków.

Panele pulpitu mają jawne pozycje i rozmiary w gridzie. Nowe dane przewijają się albo są ograniczane wewnątrz panelu; nie zmieniają geometrii pulpitu.

## 6. Moduły a nawigacja

Moduły nie mogą dowolnie dopisywać równorzędnych pozycji do głównego sidebara. Każdy moduł UI może wnosić:

- ekran przypisany do jednej zatwierdzonej grupy nawigacji;
- panel lub skrót pulpitu o określonym kontrakcie rozmiaru;
- akcje dostępne w palecie poleceń;
- typy prezentacji i edycji własnych danych.

Stabilne grupy to `Sesja`, `Świat`, `Wiedza` i `Kampania`. Utworzenie nowej grupy wymaga jawnej decyzji projektowej, a nie tylko zarejestrowania modułu.

Nazwa techniczna modułu nie musi być nazwą pozycji nawigacyjnej. Przykładowo `core.scheduler` współtworzy ekran „Czas i zdarzenia”, zamiast automatycznie tworzyć pozycję „Scheduler”.

## 7. Globalni bohaterowie i stan kampanii

Globalny bohater ma trwałą tożsamość i dane profilu, ale jego udział w kampanii jest osobnym pojęciem. Kampania nie może bezwarunkowo modyfikować globalnej karty.

Przy dodawaniu bohatera do kampanii interfejs powinien w przyszłości rozróżniać:

- utworzenie kampanijnej kopii;
- powiązanie uczestnika kampanii z globalnym profilem;
- świadomą synchronizację wybranych danych.

HP, ekwipunek, efekty, historia i inne wartości zależne od przebiegu świata należą domyślnie do kampanii. UX musi jawnie komunikować zakres każdej synchronizacji.

## 8. Paczki zawartości w UX

Menedżer globalny odpowiada za instalację, wersje, źródła i kompatybilność paczek. Workspace kampanii pokazuje wyłącznie paczki przypięte do danego świata.

Kampania przypina konkretną wersję paczki. Aktualizacja globalnie zainstalowanego pakietu nie może automatycznie zmienić trwającej kampanii. Migracja albo zmiana wersji jest jawną operacją pokazującą jej konsekwencje.

W kreatorze kolejność zależności jest następująca:

```text
ruleset
  -> kompatybilne paczki zawartości
      -> kompatybilne moduły i konfiguracja
```

## 9. Baza wiedzy i Entity

Nie należy tworzyć konkurencyjnych źródeł prawdy w postaci osobnego NPC-a mechanicznego i niezależnej notatki o tym samym NPC-u.

`Entity` jest kanoniczną tożsamością świata, do której mogą być dołączone:

- statblock i stan mechaniczny;
- opis oraz notatki MG-a;
- relacje z innymi obiektami;
- zdarzenia i historia zmian;
- backlinki z dokumentów;
- ilustracje i assety kampanii.

Moduł „Postacie” pokazuje perspektywę mechaniczną, a „Notatki i lore” perspektywę dokumentacyjną. Oba prowadzą do tej samej tożsamości i nie duplikują danych.

## 10. Wyszukiwanie i paleta poleceń

W miarę wzrostu liczby modułów sidebar nie może być jedyną metodą dotarcia do funkcji. Pasek górny zapewnia jedno globalne wejście do wyszukiwania lub palety poleceń.

Docelowo mechanizm może obejmować:

- przejście do ekranu;
- otwarcie Entity albo dokumentu;
- wykonanie częstej, jawnej akcji;
- wyszukanie kampanii, bohatera lub definicji zawartości.

Paleta jest nakładką i nie wpływa na geometrię workspace’u.

## 11. Reguły stabilności layoutu

- Sidebar ma stałą szerokość w danym wariancie układu.
- Pasek górny i statusowy mają stałą wysokość.
- Nagłówek workspace’u ma przewidziany obszar na tytuł i akcje.
- Każdy panel ma stały nagłówek oraz ograniczony viewport treści.
- Przewijanie odbywa się wewnątrz właściwego panelu albo workspace’u.
- Pozycje nawigacji mają stałą wysokość i slot na ikonę.
- Długie nazwy są skracane, a pełna wartość dostępna bez zmiany geometrii.
- Ładowanie, brak danych i błąd zachowują rozmiar docelowego obszaru.
- Zwijanie sidebara, dokowanie i zmiana proporcji są wyłącznie jawnymi akcjami użytkownika.

## 12. Zatwierdzony charakter makiety

Za obowiązujący punkt odniesienia uznaje się następujące cechy zaakceptowanej makiety:

- wyraziste, ale subtelne obramowania;
- ciepła, zbalansowana paleta grafitów, kremu i bursztynu;
- elegancki, zwarty sidebar;
- minimalistyczne, monochromatyczne ikony liniowe;
- szeryfowe akcenty typograficzne w nazwach i nagłówkach;
- prosty znak `D` w kwadratowej ramie;
- płaskie powierzchnie bez ciężkich tekstur;
- grid pulpitu z panelami o stabilnych proporcjach;
- niewielkie promienie narożników i ograniczone użycie cienia;
- wyraźne rozdzielenie poziomu globalnego, kreatora i kampanii.

Makieta jest wzorcem hierarchii, proporcji i atmosfery, a nie gotową specyfikacją każdego piksela. Implementacja może doprecyzować tokeny pod warunkiem zachowania opisanych kontraktów.

## 13. Otwarte decyzje przed implementacją pełnych modułów

- Minimalna wspierana wielkość okna i jawne warianty układu.
- Dokładna szerokość sidebara oraz wysokości pasków i wierszy.
- Zachowanie sidebara w wariancie kompaktowym.
- Ostateczny krój szeryfowy.
- Zakres pierwszej wersji wyszukiwania i palety poleceń.
- Kolejność modułów po pierwszym pionowym wycinku sesji.
- Czy układ paneli pulpitu będzie w pierwszej wersji stały, czy konfigurowalny przez użytkownika.
