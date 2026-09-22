# DungeonApp — kolejka pracy

**Status: stan na 2026-09-22.** Ten dokument jest jedynym miejscem, które mówi **co dalej**.
Pozostałe dokumenty go nie dublują: [CLAUDE.md](../CLAUDE.md) mówi, czego nie wolno,
[architecture.md](architecture.md) jak ma być, [code-map.md](code-map.md) jak jest,
[decisions.md](decisions.md) co już odrzucono, [collaboration.md](collaboration.md) jak pracować.

**Zakres: wyłącznie to, co trzeba zrobić przed zamknięciem bieżącego etapu.** Nie jest spisem funkcji
aplikacji i nie zapisuje się tu pracy koncepcyjnej na zapas — całość docelowa mieszka
w [architecture.md](architecture.md), a pytania niezamknięte wraz z warunkami powrotu tam oraz
w [decisions.md](decisions.md). Pozycja wpisana tu przed swoim czasem starzeje się po cichu: nic nie
zmusza do jej przeliczenia, a sam fakt, że stoi zapisana, z czasem zaczyna uchodzić za uzasadnienie.

> **Ten dokument nie prowadzi archiwum.** Praca domknięta znika stąd, gdy tylko przestanie być
> potrzebna do zrozumienia następnego kroku. Co zostało zrobione, mówi historia gita; **dlaczego** —
> `decisions.md` i `architecture.md`; **jak jest teraz** — `code-map.md`. Do 2026-09-14 stała tu
> sesyjna kronika na dziewięćdziesiąt linii, wbrew temu zdaniu, które w tym dokumencie już wtedy było.

Gałąź: `master`. Build bez ostrzeżeń, 239 testów zielonych.

---

## Gdzie jesteśmy

Kroki 1–8 z sekcji „Kolejność prac" [architecture.md](architecture.md) są **zrobione** — krok 8
domknięty 2026-09-14 usunięciem licznika i całej warstwy bloków danych, a 2026-09-15 dogonieniem
go przez dokumenty. Było to przejście z szablonów jako plików danych na skompilowane typy treści,
a następnie osadzenie treści w konkretnej kampanii. Stan, do którego to doprowadziło:

* **Typy treści są kodem.** Zestaw `DungeonApp.Content.Dnd5e` niesie `Monster` i `Gear`, ich
  zaprojektowane karty i jedno okno biurka. Deserializator jest jedynym walidatorem.
* **Granice są pilnowane mechanicznie**, nie deklarowane — cztery testy architektoniczne, w tym skan
  słownictwa, który poszerza się sam wraz z przybywającymi typami treści.
* **Kampania ma własny świat.** Instancje z rzadką łatką, zapis całej generacji na dysk w jednej
  transakcji, rozwiązywanie wskazania wobec rejestru jako osobna warstwa odczytu.
* **Zestaw treści wnosi własne okno biurka** — mechanizm pasa narzędzi. Okno „Świat kampanii" jest
  pierwszym prawdziwym narzędziem biurka i pierwszym konsumentem magazynu instancji.
* **Jeden prymityw zapisu atomowego** zamiast trzech ręcznie pisanych kopii.
* **Instancje są jedynym magazynem stanu kampanii.** Warstwa bloków danych odeszła bez następcy;
  kształt, w jakim wróci razem ze swoim konsumentem, stoi w [decisions.md](decisions.md), pozycja
  „Utrzymanie warstwy bloków danych po odejściu jej jedynego konsumenta".

Szczegóły każdej z tych rzeczy — [code-map.md](code-map.md).

**2026-09-22 zapadła przebudowa, której kod jeszcze nie dogonił:** dawny silnik i powłoka stają się
ramą, wspólny kod wychodzi do biblioteki, a system — dawniej zestaw — przejmuje wygląd i zawartość
aplikacji. Model — [architecture.md](architecture.md), *Rama, biblioteka, system*.

---

## Następne

**Krok 9 z „Kolejność prac" [architecture.md](architecture.md): przebudowa na ramę, bibliotekę
i system.** Docelowy kształt stoi w architekturze — sekcje *Rama, biblioteka, system*, *Nawigacja:
ekran wyboru systemu i pasek boczny*, *Gdzie mieszka stan*, *Narzędzia biurka i system okien*.

Następna czynność to **plan etapów przebudowy** — proponuje asystent, zatwierdza autor, zielone
światło etap po etapie. Dopóki plan nie zapadnie, ta sekcja celowo nie wylicza etapów.

Formuły, sloty i dokument są odtąd krokiem 10 i czekają na przebudowę, żeby powstać od razu
w bibliotece.

---

## Odłożone

### Czeka na pierwszy prawdziwy dodatek: mechanizm dodatków

Model i reguły — [architecture.md](architecture.md), *Dodatki*. Mechanizm powstaje razem
z pierwszym prawdziwym dodatkiem, nie wcześniej: dziś nie ma ani jednego. Przykład, na którym
rozmawiano — złoto w sakiewkach zamiast na postaci — wymaga licznika złota, którego też jeszcze nie
ma.

**Czekanie nie kosztuje nic, i to jest własność repozytorium, nie prognoza.** Deserializacja
manifestu kampanii jest celowo pobłażliwa, więc dołożenie listy włączonych dodatków później nie
podnosi wersji formatu i nie wymaga migracji — patrz [decisions.md](decisions.md), „Zarezerwowane
pola `Ruleset` i `ContentPacks` w manifeście kampanii".

**Wyzwalacz:** pierwszy wariant zasad, który autor chce mieć w konkretnej kampanii.

### Czekają na miejsce na ekranie — trzy pozycje o interfejsie

Wszystkie czekają, aż autor zechce zaprojektować dla nich miejsce na ekranie. Żadna nie blokuje
przebudowy — ale przebudowa zmienia ekran, na którym dwie pierwsze by stanęły: rejestr przechodzi
do zakładek treści systemu. Warto więc rozważyć je przy projektowaniu tych zakładek, nie osobno.

1. **Odrzucone paczki nigdzie się nie pokazują.** Loader je odnotowuje, ekran rejestru ich nie
   wyświetla — świadoma decyzja autora z 2026-09-12. Jedyna pozycja z tabeli „Co się dzieje, gdy
   treść jest zepsuta", o której Mistrz Gry nie dowiaduje się z aplikacji: paczka odrzucona za
   literówkę w manifeście znika dziś po cichu.
2. **Nagłówek `NIE WCZYTANE` czyni pierwszy zepsuty wiersz wyższym od pozostałych**, bo niesie go ten
   wiersz, a nie prawdziwy nagłówek sekcji. Cena za „jedna lista, jeden szablon", zostawiona
   świadomie 2026-09-13.
3. **Nie da się nazwać okazu.** Silnik umie zmienić nazwę własną instancji, jest to przetestowane
   i **nikt tego nie woła** — okno „Świat kampanii" umie dodać, zmienić punkty życia i usunąć, mimo
   że lista pokazuje właśnie nazwę własną, gdy jest. Konsument jest jednym polem tekstowym stąd.

---

## Archiwum

`archive/pre-pivot-content-layer` (`f5d5b7a`) — **nigdy nie scalana**. Praca sprzed
przeprojektowania warstwy treści: `SummaryContract`, `ContractFill`,
`SummaryContractResolution`, grupowanie rejestru, rozbudowane testy loadera i fixture'y.
Trzy fixture'y wpisów zostały stamtąd przywrócone; resztę trzyma się tylko po to, żeby nic
nie zginęło.
