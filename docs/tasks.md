# DungeonApp — kolejka pracy

**Status: stan na 2026-09-26.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-state.md](code-state.md) w jakim stanie jest kod,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.
Duże zadanie w toku może mieć własny dokument w [zadania/](zadania/) — wtedy tutaj stoi o nim jedna
linia z odnośnikiem, a jego stan, plan i notki są tam (`collaboration.md`, *Dokument zadania*).

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

Gałąź: `master`. Build bez ostrzeżeń, 396 testów zielonych (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

---

## Następne: krok 10 — zakładki treści zamiast zakładki rejestru

1. ~~Fundament interfejsu~~ — zamknięty 2026-09-26 (reguły w `architecture.md`, *Niezmiennik
   interfejsu*; pomiar w `decisions.md`, pozycja 39).
2. **Zakładki treści** — [zadania/zakladki-tresci.md](zadania/zakladki-tresci.md): nowy wygląd listy
   i kart potworów i przedmiotów (dawne zlecenia A i B, karta przedmiotu), dane D&D 5e, wpisy
   przykładowe, ewentualnie zaklęcia, wczytanie paczek od nowa i punkt kontrolny o Avalonii.

**Po kroku 10, osobnym etapem:** dodanie paczki przeciągnięciem do okna (do rozstrzygnięcia: katalog
czy archiwum; kolizja nazwy — odmowa z komunikatem, nigdy nadpisanie).

Po kroku 10 — krok 11: formuły, sloty i dokument, od razu w bibliotece. **Nic nie wchodzi na
zapas** między krokami; dodatki powstają z pierwszym prawdziwym dodatkiem (niżej).

---

## Poprawki czekające na obszar

Drobne poprawki nie dostają własnego zlecenia. Czekają, aż wykonawca będzie pracował w ich obszarze,
albo aż zbierze się ich tyle, że warto dać im osobnego — reguła w [collaboration.md](collaboration.md),
*Jak zapadają decyzje*. Zlecenie, które wchodzi w dany obszar, zabiera stąd wszystko, co do niego należy.

* **Pola tekstowe (motyw ramy, `DungeonControls.axaml`, `Controls/IconField.cs`)** — uwagi autora po
  rundzie 1 porcji 2, 2026-09-25:
  * Zaznaczanie jak w przeglądarce: przeciągnięcie **rozpoczęte obok tekstu** (np. w pustym miejscu
    karty) i przeprowadzone nad tekstem do zaznaczenia ma go zaznaczać — dziś trzeba trafić w sam
    tekst. **Zabiera karta potwora** w [zadania/zakladki-tresci.md](zadania/zakladki-tresci.md) (autor, 2026-09-26) i tam rozstrzyga zasięg (obszar wokół jednego
    bloku tekstu czy cała karta; zaznaczenie przez kilka bloków naraz to osobna, większa rzecz).
* **Wyłączenie w motywie ramy (`Themes/DungeonControls.axaml`)** — zgłoszone przez autora po rundzie
  W porcji 3a, 2026-09-28: „Wyczyść filtry” w zakładce treści, wyłączone przez komendę (`CanExecute`
  = fałsz), wygląda jak włączone. Przyczyna widoczna w motywie: przygaszenie stoi na selektorze
  właściwości `[IsEnabled=False]` (30 miejsc w pliku), a komenda wyłącza kontrolkę bez zmiany
  `IsEnabled` — Avalonia daje wtedy tylko pseudoklasę `:disabled`. Galeria pokazuje wyłączone
  wyłącznie przez `IsEnabled="False"`, więc tego nie widać. Poprawka: `:disabled` zamiast
  `[IsEnabled=False]` we wszystkich 30 miejscach (uwaga na podwójne przygaszenie, gdy wyłączony jest
  też rodzic z własnym przygaszeniem); w galerii przykład „wyłączony przez komendę” obok
  „Wyłączony”. **Zabiera porcja 3c** zakładek treści (sortowanie i poprawki).
* **Suwak — tylko notka, bez korekty teraz** (autor, po rundzie 2 porcji 5, 2026-09-25): uchwyt
  w spoczynku (kolor tekstu drugorzędnego) lekko za ciemny. Pomysł autora na później: pod myszą
  obwódka wokół uchwytu zamiast rozjaśnienia — do zderzenia z wpisem o otoczce w `decisions.md`
  (otoczka tylko na przyciskach głównym i niszczącym, jej wygląd zarezerwowany dla fokusu klawiatury).

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
