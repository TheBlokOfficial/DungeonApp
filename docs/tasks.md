# DungeonApp — kolejka pracy

**Status: stan na 2026-09-24.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-state.md](code-state.md) w jakim stanie jest kod,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

**Zakres: wyłącznie to, co trzeba zrobić przed zamknięciem bieżącego etapu.** Nie jest spisem funkcji
aplikacji i nie zapisuje się tu pracy koncepcyjnej na zapas — całość docelowa mieszka
w [architecture.md](architecture.md), a pytania niezamknięte wraz z warunkami powrotu tam oraz
w [decisions.md](decisions.md). Pozycja wpisana tu przed swoim czasem starzeje się po cichu: nic nie
zmusza do jej przeliczenia, a sam fakt, że stoi zapisana, z czasem zaczyna uchodzić za uzasadnienie.

> **Ten dokument nie prowadzi archiwum.** Praca domknięta znika stąd, gdy tylko przestanie być
> potrzebna do zrozumienia następnego kroku. Co zostało zrobione, mówi historia gita; **dlaczego** —
> `decisions.md` i `architecture.md`; **w jakim stanie jest kod** — `code-state.md`. Do 2026-09-14 stała tu
> sesyjna kronika na dziewięćdziesiąt linii, wbrew temu zdaniu, które w tym dokumencie już wtedy było;
> 2026-09-23 dokument znów miał 271 linii, z czego trzy czwarte było zamkniętą historią etapów.

Gałąź: `master`. Build bez ostrzeżeń, 330 testów zielonych (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

---

## Następne: krok 10 — zakładki treści zamiast zakładki rejestru

Docelowy kształt — [architecture.md](architecture.md), *Zakładki treści*; wygląd — mockup autora
`docs/images/mockup_rejestr.png`. Zakres pierwszej wersji ustalony z autorem 2026-09-24.

* **Dwie zakładki w kategorii System: potwory i przedmioty**, z jednego szkieletu biblioteki wpisów.
  Zakładka „Rejestr" znika razem z nimi.
* **Wchodzi:** wyszukiwanie po nazwie; sortowanie po nazwie i po kluczu systemu; filtry po
  kategorii, po paczce i po wartości wskazanej przez system; czyszczenie filtrów; odznaka wiersza;
  licznik wpisów; nagłówek szczegółu (kategoria, nazwa, tagi) rysowany przez szkielet, pod nim karta
  systemu; sekcje po paczce z rzeczami zepsutymi na dole; powód błędu w miejscu karty; przycisk
  wczytania paczek od nowa.
* **Nie wchodzi:** tworzenie, edycja, kopiowanie, usuwanie; zaklęcia i NPC; filtr po typie treści
  (bez zakładki z kilkoma typami nie ma konsumenta); pasek stanu i Ctrl+K z mockupu.
* **Wymiary z `docs/images/mockup_rejestr.html`**, nie z obrazka. Pasek tytułu okna w mockupie nie
  jest projektem paska górnego. **Mockup jest prawie kwadratowy** — na ekranie 16:9 lista trzyma
  szerokość z mockupu, treść szczegółu swoją największą szerokość z mockupu, wyrównana do listy.
* **Kolory po znaczeniu:** treść niewczytana bierze kolor „niebezpieczeństwa"; kolory stanów
  dostają komentarze ze znaczeniem (niebezpieczeństwo — zepsute albo nieodwracalne; ostrzeżenie —
  wymaga uwagi, ale działa; sukces — udało się); kolory rzadkości — własne tokeny w systemie.

**W toku (stan na koniec sesji 2026-09-24)** — dwóch wykonawców równolegle, na prośbę autora; kopie
w `.claude/worktrees/agent-*`, gałęzie `worktree-agent-*` (`git worktree list`). Jeśli sesja urwała się
przed ich raportem: sprawdzić commity w gałęziach (`git log master..<gałąź>`), zweryfikować jak zwykle
(build bez ostrzeżeń kompilatora, testy ≥ 330, testy granic), scalić po kolei, usunąć kopie. Gałąź bez
commitów = praca przerwana; zlecić od nowa z briefu opisanego niżej.
* **Katalogi i półka** (zlecenie 3 niżej) — z dołączoną poprawką tła kampanii niedostępnej.
* **Poprawki zakładek treści** — lista autora po obejrzeniu aplikacji: szczegół przy liście (nie
  wyśrodkowany), paski przewijania, linia nagłówka na całą szerokość, lupa, strzałki chipów i sortowania,
  „Wyczyść filtry" podkreślone, widoczna kreska zaznaczenia, zaznaczony wiersz pogrubiony, nagłówki
  sekcji wersalikami, odznaka z samym wyzwaniem, okruszki z „›", pole `group` potwora jako kategoria,
  karta bez dublujących wierszy, inna ikona „Potworów", lista rozwijana (pod chipem, czcionka wierszy,
  wybór = przygaszony akcent, obramowanie jak pole wyszukiwania). Testy renderujące mierzą położenie
  i widoczność. **Rozszerzenie w osobnym commicie:** karta potwora według mockupu (rząd KP/PZ/szybkość,
  cechy w ramkach z modyfikatorami, właściwości, wyzwanie jako chip, nagłówki sekcji z kreską, opis
  kursywą) — bez parsowania akcji z prozy (to krok 11, sloty).
* Po scaleniu obu: **autor sprawdza w aplikacji** i przenosi ręcznie paczki do `dnd5e\packs\`,
  kampanie do `dnd5e\campaigns\`, dopisuje `group` w swoich paczkach. Potem zlecenie 4 (zwiad rejestru,
  przycisk wczytania od nowa).

**Zlecenia, po kolei:**
1. ~~Logika szkieletu bez okna~~ — zrobione 2026-09-24 (model listy i profile typów w bibliotece
   wpisów, profile D&D w systemie; porządek wyświetlania po polsku).
2. ~~Widok według mockupu i dwie zakładki systemu w miejsce rejestru~~ — scalone 2026-09-24.
   **Czeka na sprawdzenie przez autora w aplikacji.**
3. Katalogi (Sonnet — jest reguła niezgodności): paczki z `Dokumenty\DungeonApp\<system>\packs\`,
   kampanie z `Dokumenty\DungeonApp\<system>\campaigns\`, małe litery; paczki dostarczane z systemem
   tym samym układem. Reguły półki — architektura, *Gdzie mieszka stan*. Autor przenosi swoje paczki
   i kampanie ręcznie po scaleniu. Dołączona poprawka z tego samego obszaru: przygaszone tło
   kampanii niedostępnej (niżej, *Poprawki czekające na obszar*).
4. Zwiad: jak żyje rejestr (kto go trzyma, kto dostaje przy zakładkach i biurku); potem zlecenie
   wczytania paczek od nowa.

**Po kroku 10, osobnymi etapami:** dodanie paczki przeciągnięciem do okna (do rozstrzygnięcia: katalog
czy archiwum; kolizja nazwy — odmowa z komunikatem, nigdy nadpisanie); karty potwora i przedmiotu
według mockupu.

Po kroku 10 — krok 11: formuły, sloty i dokument, od razu w bibliotece. **Nic nie wchodzi na
zapas** między krokami; dodatki powstają z pierwszym prawdziwym dodatkiem (niżej).

---

## Poprawki czekające na obszar

Drobne poprawki nie dostają własnego zlecenia. Czekają, aż wykonawca będzie pracował w ich obszarze,
albo aż zbierze się ich tyle, że warto dać im osobnego — reguła w [collaboration.md](collaboration.md),
*Jak zapadają decyzje*. Zlecenie, które wchodzi w dany obszar, zabiera stąd wszystko, co do niego należy.

**Rama — półka kampanii** (zabiera zlecenie 3 kroku 10)
* Tło wiersza kampanii niedostępnej przygaszone względem dostępnej (dziś identyczne): istniejący kolor
  tła o stopień ciemniejszy, obramowanie zostaje. Autor, 2026-09-24.

**Rama — pasek boczny**
* Kreska zaznaczenia jako osobny element odsunięty w lewo od tła pozycji, krótszy od niej,
  zaokrąglony — jak `.sidebar-item.active::before` w `docs/images/mockup_rejestr.html`. Kolor kreski —
  intensywny akcent, jak na liście wpisów; tło zaznaczonej pozycji zostaje szare. Autor, 2026-09-24.

---

## Odłożone

### Czeka na pierwszy prawdziwy dodatek: mechanizm dodatków

Model i reguły — [architecture.md](architecture.md), *Dodatki*; forma dodatku należy do ramy,
a jednostką włączania jest wariant pod nagłówkiem dodatku. Dziś nie ma ani jednego dodatku.
Przykład, na którym rozmawiano — złoto w sakiewkach zamiast na postaci — wymaga licznika złota,
którego też jeszcze nie ma.

**Czekanie nie kosztuje nic, i to jest własność repozytorium, nie prognoza.** Deserializacja
manifestu kampanii jest celowo pobłażliwa, więc dołożenie listy włączonych dodatków później nie
podnosi wersji formatu i nie wymaga migracji — patrz [decisions.md](decisions.md), „Zarezerwowane
pola `Ruleset` i `ContentPacks` w manifeście kampanii".

**Wyzwalacz:** pierwszy wariant zasad, który autor chce mieć w konkretnej kampanii.

### Czeka na miejsce na ekranie: nazwa okazu

Czeka, aż autor zechce zaprojektować dla niej miejsce na ekranie.

* **Nie da się nazwać okazu.** Operacja zmiany nazwy własnej instancji istnieje, jest przetestowana
   i **nikt jej nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, mimo
   że lista pokazuje właśnie nazwę własną, gdy jest. Konsument jest jednym polem tekstowym stąd.

### Czeka na zacięcie: zacięcia mierzyć, nie oglądać

2026-09-22 autor potwierdził, że wejście w system działa płynnie — i sam zauważył, że dowodu nie ma:
animacja rozwijania paska, która dawniej czyniła zacięcie widocznym, już nie gra przy wejściu.
2026-09-23 etap 4 przestawił kolejność rozgrzewki przy starcie. **Wyzwalacz:** pierwsze zacięcie
zauważone przez autora. Wtedy najpierw logowane liczby — czas rozgrzewki i czas od kliknięcia
systemu do pierwszej narysowanej klatki — dopiero potem poprawka.

### Czeka na potrzebę: kopie zapasowe kampanii, zapis ręczny, wersjonowanie

Zapis od razu zostaje — decyzja autora z 2026-09-22, uzasadnienie w [decisions.md](decisions.md),
*Gdzie mieszka stan*. **Wyzwalacz:** MG chce wrócić do wcześniejszego stanu kampanii albo zapis po
każdej zmianie staje się odczuwalnie wolny. Wtedy pierwszym kandydatem są rotujące kopie zapasowe,
nie zapis ręczny.
