# DungeonApp — kolejka pracy

**Status: stan na 2026-09-23.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
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

Gałąź: `master`. Build bez ostrzeżeń, 282 testy zielone (w tym testy renderujące okno bez ekranu, `DungeonApp.Desktop.RenderingTests`).

---

## Następne: krok 10 — zakładki treści zamiast zakładki rejestru

Decyzja autora z 2026-09-23. Docelowy kształt — [architecture.md](architecture.md), *Zakładki
treści*; wygląd — mockup autora `docs/images/mockup_rejestr.png`. Zakładka „Rejestr" znika w dniu,
w którym stają pierwsze zakładki treści; sam rejestr zostaje jako ich źródło.

* **Każdy typ treści to osobna zakładka na pasku, z jednego szkieletu biblioteki wpisów** — mockup
  pokazuje jeden ekran z typami jako przełącznikiem, i tym się wynik od niego różni (autor).
* **Wymiary z `docs/images/mockup_rejestr.html`**, nie z obrazka. **Pasek tytułu okna w mockupie nie
  jest projektem paska górnego** i się nim nie sugerować.
* **Przy projektowaniu rozważyć** dwie pierwsze pozycje z „Czekają na miejsce na ekranie" (niżej)
  oraz pytanie otwarte „Paczka a system" w architekturze — dopiero przy projekcie interfejsu, nie
  przed nim.
* **Przed briefem ustalić z autorem, co z mockupu wchodzi do pierwszej wersji** — mockup pokazuje
  rzeczy, których architektura w pierwszej wersji nie przewiduje albo których dziś nie ma: tworzenie
  i edycję wpisów, typy zaklęć i NPC, pełny blok statystyk potwora.
* **Kandydat do pierwszej wersji z kierunku autorstwa treści** (architektura, pytanie otwarte
  „Autorstwo treści w aplikacji", krok 1): ponowne wczytanie paczek bez restartu, „otwórz plik" przy
  wpisie, zepsute wpisy widoczne z powodem. Kandydat, nie postanowienie.

**Plan następnej sesji:**
1. Rozmowa z autorem przy otwartym mockupie: zakres pierwszej wersji — architekt przynosi propozycję
   z jedną rekomendacją, łącznie z kandydatem wyżej i miejscem na rzeczy zepsute.
2. Zielone światło, podział na zlecenia, brief(y).
3. Pierwsze zlecenie idzie przez własny rodzaj agenta `wykonawca` — sprawdza przy okazji, że
   definicje z `.claude/agents/` działają (zakazy, krok 0, format raportu).

Po kroku 10 — krok 11: formuły, sloty i dokument, od razu w bibliotece. **Nic nie wchodzi na
zapas** między krokami; dodatki powstają z pierwszym prawdziwym dodatkiem (niżej).

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

### Czekają na miejsce na ekranie — trzy pozycje o interfejsie

Wszystkie czekają, aż autor zechce zaprojektować dla nich miejsce na ekranie. Dwie pierwsze stanęłyby
na ekranie, który zastąpią zakładki treści — rozważyć je przy kroku 10, nie osobno.

1. **Odrzucone paczki nigdzie się nie pokazują.** Loader je odnotowuje, ekran rejestru ich nie
   wyświetla — świadoma decyzja autora z 2026-09-12. Jedyna pozycja z tabeli „Co się dzieje, gdy
   treść jest zepsuta", o której Mistrz Gry nie dowiaduje się z aplikacji: paczka odrzucona za
   literówkę w manifeście znika dziś po cichu.
2. **Nagłówek `NIE WCZYTANE` czyni pierwszy zepsuty wiersz wyższym od pozostałych**, bo niesie go ten
   wiersz, a nie prawdziwy nagłówek sekcji. Cena za „jedna lista, jeden szablon", zostawiona
   świadomie 2026-09-13.
3. **Nie da się nazwać okazu.** Operacja zmiany nazwy własnej instancji istnieje, jest przetestowana
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
