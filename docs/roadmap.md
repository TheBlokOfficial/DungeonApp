# Co dalej

Kolejność od góry. Pozycja jest opisem potrzeby, nie zleceniem — przed pracą sprawdź w kodzie, czy
nadal jest aktualna. Pozycję zrobioną usuwa się w commicie, który ją zamyka.

## Teraz: zakładki treści — karty potwora i przedmiotu

Gotowe: lista z wyszukiwaniem, filtrami i sortowaniem, szczegół wpisu, paczka `dnd5e-srd` (na razie
pusta), pole obrazka z ramką. Projekt kart (układ, wymiary, co wyróżnione) jest punktem wyjścia, nie
specyfikacją: `docs/archive/zadania/zakladki-tresci.md`, sekcja *C. Projekt szkieletu i kart*.

1. **Karta potwora, pełna.**
   - Poprawki ramki obrazka: tylko „Brak pliku” w kolorze niebezpieczeństwa, a ścieżka drugorzędna;
     linia ramki nad obrazkiem; ikony zastępcze z Lucide (czaszka — potwór, plecak — przedmiot), razem
     z licencją ISC w `Assets/Licenses`.
   - Styk karta–paczka: `CreateCard(Entry)` nie zna paczki, a obrazek rozwiązuje się względem niej.
   - Nowe pola, opcjonalne: PD, rzuty obronne, podatności, odporności i niewrażliwości, akcje
     dodatkowe, reakcje, akcje legendarne, rzucanie czarów.
   - Sekcje rozwijane przy długiej prozie.
   - Zaznaczanie tekstu przeciąganiem rozpoczętym obok tekstu.
   - Kilka–kilkanaście wpisów przykładowych w `dnd5e-srd`, każdy pokazujący inny wariant karty.
2. **Karta przedmiotu:** kategoria, podtyp, dostrojenie, cena, waga z ułamkami, obrażenia
   i właściwości broni, KP zbroi, wymagana Siła, ukrywanie się, ładunki i odnawianie. Filtry
   Kategoria i Dostrojenie. Do tego wpisy przykładowe.
3. **Wczytanie paczek od nowa** bez restartu; wybór zostaje, jeśli wpis o tym id nadal istnieje.
4. **Zaklęcia** — opcjonalnie. Czas rzucania, zasięg i czas trwania wyłącznie jako napisy; filtry
   i sortowanie po poziomie i szkole; koncentracja i rytuał jako tagi.

Dane przykładowe pochodzą z SRD 5.1 (CC-BY 4.0), tłumaczone. Tylko jako przykłady, nie wzorzec rekordów.

## Następny kamień milowy: pierwsza sesja przy stole

Zanim wejdą formuły i sloty, aplikacja ma się nadawać do poprowadzenia prawdziwej sesji. Zapytaj
autora, czego potrzebuje przy stole. Znane braki:
- nie da się nazwać okazu: operacja zmiany nazwy instancji istnieje i jest przetestowana, ale nie ma
  pola w interfejsie;
- panel „Świat kampanii” jest testowy — do przeprojektowania pod realne użycie.

## Porządki w kodzie

- **Pytanie do autora:** po nieobsłużonym błędzie aplikacja zapisuje go w logu i się zamyka. Czy ma
  zamiast tego pokazać komunikat i działać dalej?
- **Komentarze po polsku** w ok. 40 plikach — przetłumaczyć na angielski (konwencja: kod i komentarze
  po angielsku). `tools/comment-hits.py` wskazuje też komentarze z historią.
- **Komentarze nieaktualne w treści:** `CampaignRowViewModel` (powód niedostępności, którego wiersz nie
  pokazuje), `AppShellView.axaml` (host rozgrzewki „także po wyborze systemu”), `PanelCatalog`
  (kolejność „panele powłoki, potem narzędzia”), karty potwora i przedmiotu odsyłają po uzasadnienie
  do siebie nawzajem.
- **CommunityToolkit.Mvvm** zamiast ręcznych `ObservableObject`, `RelayCommand` i `AsyncCommand`.
- Piąty zakaz nie ma strażnika w kodzie — pilnuje go przegląd.

## Drobne uwagi autora

- Suwak: uchwyt w spoczynku trochę za ciemny. Pomysł: pod myszą obwódka zamiast rozjaśnienia.
- Menu: skrót nie stoi w jednej linii ze strzałką podmenu, podmenu nachodzi na menu.

## Po stronie autora

- Przenieść stare kampanie do `Dokumenty\DungeonApp\dnd5e\campaigns\` (stare `Packs`/`Campaigns` nie są
  czytane) i dopisać potworom pole `group`.

## Odłożone, z wyzwalaczem

- **Formuły, sloty, dokument** — po kamieniu milowym „przy stole”.
- **Dodatki (warianty zasad)** — pierwszy wariant, który autor chce mieć w konkretnej kampanii.
- **Dodanie paczki przeciągnięciem do okna** — kolizja nazwy daje odmowę z komunikatem, nigdy
  nadpisanie.
- **Kopie zapasowe kampanii** — MG chce wrócić do wcześniejszego stanu.
- **Pomiar startu** — pierwsze zacięcie zauważone przez autora; wtedy najpierw liczby, potem poprawka.
