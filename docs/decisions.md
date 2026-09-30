# Rozstrzygnięcia

Co przesądzone i czego nie robimy. Czytaj **przed** propozycją zmiany architektury. Pomysł z listy
odrzuconych wraca tylko wtedy, gdy jego wyzwalacz się spełnił albo powód przestał obowiązywać — wtedy
zmień wpis. Pełne argumenty sprzed przebudowy obiegu pracy: `docs/archive/decisions.md`.

## Obowiązuje

- **Cztery projekty, nie siedem.** Warstwa „bibliotek” (wpisy, biurko) miała po jednym konsumencie,
  a każda funkcja przechodziła przez 3–4 projekty. Zasady „rama nie zna D&D” pilnują referencje i skan
  słownictwa. *Wyzwalacz powrotu:* drugi system, który nie chce części wspólnego kodu.
- **Avalonia zostaje, motyw własny zostaje, nie wracamy do Fluenta.** Koszt interfejsu siedział
  w procesie (małe porcje, zatwierdzanie kroków, pikselowe rundy), nie w bibliotece; zmiana na HTML
  odtworzyłaby ten sam rachunek. Nowych kontrolek motywu nie buduje się na zapas.
- **Karta jest projektowana, nie składana z danych.** Układ karty należy do kodu systemu; dane niosą
  wyłącznie wartości.
- **Kształt jest kodem, wartości są danymi.** Literówka w nazwie pola to błąd kompilacji albo powód
  odrzucenia wpisu, nie cicho zignorowana wartość.
- **Nakładka instancji to rzadka łatka rozwiązywana przy odczycie**, nie kopia wpisu: zmiana wpisu
  działa jak patch balansujący grę i dociera do zapisanych kampanii.
- **Zapis po każdej zmianie**, bez ręcznego „Zapisz”. *Wyzwalacz:* MG chce wrócić do wcześniejszego
  stanu → najpierw rotujące kopie zapasowe.

## Odrzucone

- **Skrypty w paczkach (Lua, „poziom 3”)** — formuła deklaratywna bez gałęzi wystarcza i nie wymaga
  piaskownicy.
- **Osobny system efektów, kaskady zmian, cofanie jako wymóg silnika** — łamią zakazy 3–5; efekt jest
  wkładem do sumy, nie mechanizmem.
- **Zakładki, nawigacja albo kategorie z danych paczki** — pasek wypełnia skompilowany system.
- **Karta z listy elementów podanej w danych, generyczne prymitywy UI dla danych, jedna uniwersalna
  forma pośrednia** — to decyzje o układzie przebrane za dane.
- **Dziedziczenie i osadzanie szablonów, typy treści jako plik danych** — typ treści jest kodem.
- **Zagnieżdżone wartości w polu wpisu** — wartości są płaskie; strukturę da slot.
- **Wpisy lokalne dla kampanii, rejestr wewnątrz kampanii** — treść mieszka w paczkach; autorstwo
  idzie przez własną paczkę MG.
- **Materializacja wpisu w instancji, nakładka jako miejsce na warianty rzeczy** — patrz nakładka wyżej.
- **Odrzucanie całej paczki za jeden wadliwy wpis** — wadliwy wpis oznacza się, reszta się wczytuje.
- **Rozgałęzianie po rodzaju wpisu w Core/Desktop i introspekcja typu treści przez narzędzie** —
  jedynym miejscem, gdzie wolno wiedzieć, czym jest wpis, jest system.
- **Ładowanie systemów w czasie wykonania, systemy zależne od innych systemów** — statycznie,
  równolegle, bez zależności.
- **Dodatek jako przełącznik sprawdzany w logice** — wariant, który potrzebuje przełącznika w środku
  logiki, zwykle wykonuje regułę, którą powinien wykonać MG.
- **Warstwa scen** — kampania jest sesją i światem naraz.
- **Migracja formatu i pola zarezerwowane budowane z wyprzedzeniem** — manifest jest pobłażliwy, więc
  dołożenie pola nie wymaga migracji; migracja powstanie z pierwszą niezgodną zmianą.
- **Śledzenie tur jako element karty albo licznik rund w danych** — zakaz 2.
