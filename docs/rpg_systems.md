# Asystent DM (TTRPG) — Koncepcja Architektoniczna
## Wersja 6 — dokument decyzji projektowych

> **Status: źródło historyczne. Nie obowiązuje i nie rozstrzyga niczego.**
>
> Dokument powstał w oderwaniu od tego repozytorium i jest pierwotnym źródłem pojęć, na których
> stoi projekt. Każda jego decyzja została od tamtej pory albo przyjęta, albo jawnie odrzucona —
> przyjęte są w [architecture.md](architecture.md), odrzucone w [decisions.md](decisions.md),
> zawsze w brzmieniu tamtych dokumentów, nie tego.
>
> Zachowany, bo nadal najlepiej ze wszystkich tłumaczy, **skąd wziął się problem**: dlaczego
> asystent zbudowany pod jeden system i asystent w pełni konfigurowalny zawodzą z przeciwnych
> powodów. Reszta jest historią i czyta się ją jako historię.
>
> Najważniejsze rzeczy, które nie przetrwały: skrypty Lua (poziom 3), sceny jako byt, podział
> narzędzi na „narzędzia bytu" i „narzędzia sceny", oraz zamknięty katalog narzędzi wybieranych
> przez dane. Uzasadnienia są w `decisions.md`.

---

## 1. Problem i zakres

**Problem.** Istniejące asystenty DM są zbudowane pod jeden system i twardo
kodują jego pojęcia. Alternatywa — silnik w pełni konfigurowalny — kończy
się tym, że użytkownik pisze własny interfejs, a aplikacja staje się słabym
frameworkiem UI. Szukamy trzeciej drogi.

**Klasa gier „trad".** Aplikacja nie obsługuje dowolnej gry fabularnej, lecz
zdefiniowaną klasę systemów mających łącznie: dyskretne, nazwane statystyki;
rozstrzyganie akcji rzutem kośćmi z modyfikatorem; jakąś formę kolejności
działania. Obejmuje m.in. D&D, Pathfindera, większość OSR, Call of Cthulhu,
Savage Worlds.

**Świadomie poza zakresem.** Gry bez kości, systemy oparte w rdzeniu na puli
sukcesów, gry karciane. Pojedynczą nieregularną regułę spoza klasy da się
obsłużyć skryptem; mechanika będąca rdzeniem całej gry wymaga nowej wersji
katalogu narzędzi, nie naginania obecnego. To granica produktowa, nie
techniczna.

**Śledzenie stanu zamiast egzekwowania zasad.** Aplikacja automatyzuje
rachunkowość. Interpretacja zasad zostaje przy DM-ie. Ta zasada rozstrzyga
większość sporów o zakres funkcji.

---

## 2. Decyzja centralna: zamknięty katalog narzędzi domenowych

Warstwa silnika **nie** udostępnia generycznych prymitywów UI (przycisk,
pasek, etykieta) do składania interfejsu z zewnątrz. Udostępnianie klocków —
choćby najprostszych — przenosi projektowanie interfejsu do warstwy danych i
w praktyce oznacza napisanie silnika UI cudzymi rękami.

Zamiast tego silnik udostępnia **skończony katalog gotowych narzędzi RPG** o
z góry ustalonym kształcie. Warstwa danych wybiera narzędzia i wypełnia je
treścią; nigdy nie decyduje o kształcie interfejsu.

Konsekwencje, które trzeba zaakceptować świadomie:

* **Różnice między systemami wyrażamy parametrem istniejącego narzędzia, nie
  nowym narzędziem.** Automatyczna a ręczna kolejność tur to dwa tryby
  jednego trackera, nie dwa trackery.
* **Nowy *rodzaj* narzędzia = nowe wydanie aplikacji.** Katalog nie jest
  rozszerzalny przez użytkownika. To jest cena i jest zamierzona.
* **Reguła rozstrzygająca kształt narzędzia:** zmienna liczba **jednorodnych**
  elementów jest dozwolona (dowolnie długi statblok); zmienny skład **różnych
  typów** elementów obok siebie — nie. To jedyne kryterium odróżniające
  „konfigurowalne narzędzie" od „przebranego silnika UI".

---

## 3. Trzy poziomy logiki, w kolejności pierwszeństwa

1. **Czyste dane** — pole wskazuje ścieżkę w danych bytu. Zero logiki.
2. **Deklaratywna formuła** — jeden, współdzielony silnik formuł stojący za
   *każdym* polem obliczanym w katalogu. Formuła czyta dane i zwraca wynik:
   nie ma pętli, gałęzi ani efektów ubocznych, więc jest bezpieczna z
   definicji i wymaga tylko walidacji składni przy wczytywaniu paczki — nie
   piaskownicy. **To domyślna ścieżka** dla rzutu na trafienie, modyfikatora
   cechy, inicjatywy, maksimum zasobu.
3. **Skrypt (Lua)** — wyłącznie dla reguł, których nie da się zapisać jako
   formuła, bo wymagają pętli lub warunku (przykład kanoniczny: eksplodujące
   kości).

Kluczowe jest to, że poziom 2 pokrywa zdecydowaną większość mechanik. Jeśli
w praktyce okaże się inaczej, założenie o klasie „trad" jest błędne i trzeba
wrócić do sekcji 1 — nie dopisywać skryptów.

---

## 4. Podział na warstwy

| Warstwa | Charakter | Odpowiedzialność |
| :--- | :--- | :--- |
| **1. Silnik** | kompilowana (C# / Avalonia) | Katalog narzędzi, silnik formuł, piaskownica skryptów, menedżer stanu, białalista API. |
| **2. Szablony** | dane (JSON + rzadkie skrypty) | Deklaruje, których narzędzi użyć i jak je sparametryzować. |
| **3. Byty i sceny** | dane + stan runtime | Rejestr konkretnych bytów oraz sceny referencjujące je przez ID. |

**Silnik nie zna pojęć domenowych.** Nie ma w kodzie klasy `Monster`, `Spell`
ani enuma typu bytu, ani dispatchu po typie. Ścieżka renderowania jest
identyczna dla każdego bytu: byt wskazuje szablon, szablon wskazuje
narzędzia. To, czym byt „jest", wynika wyłącznie z zestawu narzędzi
deklarowanego przez jego szablon. To jest weryfikowalne kryterium
agnostycyzmu — każde `switch` po rodzaju bytu w Warstwie 1 jest naruszeniem
architektury.

**Kierunek zależności.** Byt istnieje niezależnie od sesji, w której jest
używany. Scena referencjuje byty przez ID i nigdy ich nie duplikuje; byt nie
wie, w jakiej scenie występuje. Zależność jest jednokierunkowa.

---

## 5. Katalog narzędzi — zakres pierwszego wydania

„Zamknięty" z sekcji 2 znaczy: **zamknięty wobec warstwy danych** — szablon
nie może dołożyć narzędzia. Nie znaczy „kompletny na zawsze". Poniższa lista
jest zobowiązaniem zakresowym wersji 1; kolejne wydania ją poszerzają, każde
narzędzie przechodząc test z sekcji 8.

Katalog dzieli się według tego, **ile bytów narzędzie obsługuje**. Ten
podział jest architektoniczny, nie kosmetyczny: narzędzie pokazujące kolejkę
wielu uczestników nie może być elementem karty pojedynczego bytu.

**Narzędzia bytu** — instancjonowane raz na byt, deklarowane w układzie
szablonu:

| Narzędzie | Odpowiedzialność |
| :--- | :--- |
| statblok | Lista cech; każda z opcjonalną wartością pochodną z formuły. |
| lista wpisów | Jednorodna lista; opcjonalna ilość na wpis (tagi, cechy, ekwipunek, amunicja). |
| pasek zasobu | Wartość bieżąca i maksymalna; maksimum stałe **albo** liczone formułą — nigdy oba naraz. |
| akcja rzutu | Etykieta plus dowolnie długa lista nazwanych formuł. Pokrywa i pojedynczy rzut rozstrzygający, i akcję wieloetapową (trafienie + obrażenia). |
| akcja skryptowa | Jedyne miejsce, w którym skrypt podłącza się pod interfejs. Przycisk o stałym kształcie — nie generyczny event system. |

**Narzędzia sceny** — instancjonowane raz na scenę, zasilane deklaracjami
uczestników:

| Narzędzie | Odpowiedzialność |
| :--- | :--- |
| tracker tur | Wskaźnik aktywnego uczestnika, cykl tur i podświetlenie — zawsze wbudowane. Tryb wynika z danych: uczestnicy deklarujący klucz sortowania są układani automatycznie, pozostałych porządkuje DM ręcznie. Pokrywa inicjatywę D&D, stronową, popcorn i brak formalnej inicjatywy. |

Szablon deklaruje osobno swój **układ** (narzędzia bytu) i swoje
**zdolności** (udział w narzędziach sceny wraz z parametrami). Zdolności są
jedynym interfejsem bytu wobec sceny.

**Znane luki wersji 1.** Powyższe nie pokrywa jeszcze klasy „trad" w całości.
Dwa braki są pewne i przechodzą test z sekcji 8 bez dyskusji: **blok prozy**
(opis, taktyka, treść zaklęcia — obecny w statbloku każdego systemu klasy) i
**znacznik binarny** (bennies, przygotowane zaklęcie, odhaczona umiejętność,
rzuty na śmierć). Nie ma ich w wersji 1 świadomie — pierwsze wydanie
weryfikuje architekturę, nie kompletność. Trzeci brak, etykiety grup akcji
(Akcje / Legendarne / Reakcje), jest rozstrzygnięty inaczej: to parametr
akcji rzutu, nie narzędzie, bo osobne narzędzie „nagłówek" byłoby dokładnie
tym generycznym prymitywem UI, którego zakazuje sekcja 2.

---

## 6. Model bezpieczeństwa

Skrypty są jedynym wektorem ryzyka i cała reszta architektury ustawiona jest
tak, żeby ten wektor był wąski:

* Paczka wbudowana jest jedyną zaufaną bezwarunkowo. Reszta zawsze w
  piaskownicy, izolowana per paczka, instalowana za jawną zgodą.
* Dostęp do hosta przez **białą listę** funkcji, nigdy czarną.
* Odczyt i zapis stanu są **względne wobec bytu wywołującego** — skrypt
  fizycznie nie potrafi zaadresować cudzego stanu. Zapis dodatkowo
  ograniczony do stanu zmiennego; dane deklaratywne pozostają tylko do
  odczytu.
* Twarde limity czasu i liczby instrukcji na wywołanie. Skoro poziom 3
  istnieje właśnie po to, żeby dopuścić pętle, limit instrukcji jest jedyną
  obroną przed pętlą nieskończoną i nie jest opcjonalny.
* Błędy skryptu nigdy nie przerywają aplikacji.
* Efekty międzybytowe (zaklęcie obszarowe) są poza zasięgiem skryptu i
  wymagałyby osobnej, jawnie dopuszczonej funkcji hosta. Świadomie odłożone.

Powierzchnia ataku jest ograniczona z dwóch niezależnych powodów: skrypt
uruchamia się wyłącznie z akcji skryptowej, a zapis stanu nie sięga poza byt
wywołujący. Awaria jednego z tych mechanizmów nie wystarcza do eskalacji.

---

## 7. Przepływ

Przy starcie silnik wczytuje paczki, **waliduje składnię formuł i parametry
narzędzi** i ładuje byty. Widok powstaje przez złożenie narzędzi
zadeklarowanych przez szablon. Akcja użytkownika jest w typowym przypadku
rozwiązywana przez silnik formuł **bez uruchamiania skryptu**; skrypt wchodzi
tylko przy akcji skryptowej. Wynik trafia do stanu transakcyjnie i odświeża
widok.

Jedyny punkt wymagający decyzji na tym etapie to walidacja przy wczytywaniu:
paczka z błędną formułą lub nieznanym parametrem musi być odrzucona
natychmiast, a nie ujawnić się przy pierwszym kliknięciu w środku sesji.

---

## 8. Wersjonowanie i rozszerzanie

Wersja szablonu i wersja katalogu narzędzi to dwie niezależne osie. Byt wiąże
się z wersją szablonu (i tą ścieżką migruje). Wersję katalogu deklaruje
szablon — byt dziedziczy ją przez szablon i nigdy nie deklaruje samodzielnie.

W obrębie tej samej wersji major katalog rośnie **wyłącznie addytywnie**:
nowe opcjonalne parametry istniejących narzędzi. Zmiana łamiąca wymaga bumpa
wersji katalogu, wpisu w changelogu i jawnej ścieżki migracji dla paczek już
istniejących w terenie.

**Test przed dodaniem nowego rodzaju narzędzia** (nie parametru):

1. Czy da się to wyrazić parametryzacją narzędzia, które już istnieje?
2. Czy zachowana jest reguła jednorodności z sekcji 2?
3. Czy **wbudowane zachowanie** narzędzia — nie same parametry — miałoby sens
   bez modyfikacji w co najmniej 2–3 różnych systemach klasy „trad"?

Odpowiedź „nie" na którekolwiek pytanie oznacza, że narzędzie jest przebraną
funkcją jednego systemu.

---

## 9. Pytania otwarte

1. **Kronika zdarzeń.** Czy aktualizacja stanu emituje zdarzenie do wspólnego
   dziennika kampanii, czy warstwa narzędzi pozostaje świadomie niższa i o
   dzienniku nie wie? Decyzja wpływa na kontrakt menedżera stanu, więc
   powinna zapaść przed implementacją Warstwy 1.
2. **Kształt sceny.** Scena jest w tym dokumencie zarysowana minimalnie
   (lista ID uczestników + stan narzędzi sceny). Czy to wystarczy, zależy od
   tego, ile narzędzi sceny finalnie powstanie — na razie jest jedno.
