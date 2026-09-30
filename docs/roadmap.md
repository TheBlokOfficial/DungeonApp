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

- **Komentarze:** usunąć odwołania do briefów, porcji i kroków (ok. 100) oraz do sekcji starych
  dokumentów (ok. 140 odwołań do `docs/`); włączyć walidację `<see cref>` (`GenerateDocumentationFile`
  z wyłączonym tylko CS1591) i poprawić zerwane odwołania, np. `Dnd5eSystem.cs` → `CreateRegistryTab`.
- **Rozbić `Themes/DungeonControls.axaml`** (ok. 4200 linii, 63 motywy) na plik per kontrolka.
- **Logowanie do pliku** (`%LocalAppData%\DungeonApp\logs`) i globalna obsługa nieobsłużonych
  wyjątków; kroki startowe połykają dziś błędy bez śladu.
- **`Dnd5eSystem`:** tabela rejestracji typów zamiast rozgałęzień po napisie w `TryGet`,
  `TryValidate` i `CreateCard` oraz zamiast `CreateContentTab(0/1)`.
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
