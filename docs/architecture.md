# Architektura

Jak aplikacja jest zbudowana i dokąd zmierza. Przy rozbieżności z kodem prawdą jest kod — popraw ten
dokument w tym samym commicie. Powody i odrzucone kierunki: `docs/decisions.md`; kolejność pracy:
`docs/roadmap.md`.

## Czym to jest

Laptop MG przy stole: ile życia ma jeszcze goblin, co drużyna ma w plecaku, co żyje w świecie kampanii.
Gracze aplikacji nie widzą. To nie jest wirtualny stół, gra ani generator treści. Aplikacja prowadzi
księgę (wartości, liczniki, kolejka, przełomy tury i odpoczynku), zasady stosuje MG — granica
w `CLAUDE.md`, lista przełomów w `docs/decisions.md`.

Zakres: systemy z nazwanymi statystykami, rzutem z modyfikatorem i jakąś kolejnością działania (D&D,
Pathfinder, OSR, Call of Cthulhu). Aplikacja **wie, czym jest potwór i jak go pokazać**, bo karta jest
zaprojektowana; **nie wie, czym jest D&D**, bo wiedza o systemie — także przełomy — mieszka
w wymienialnym projekcie. Pole dostaje strukturę, gdy czyta je księga albo przełom; inaczej jest prozą.

## Słownik

Jedna nazwa na pojęcie. Słowa spoza tej tabeli znaczą to, co w zwykłej polszczyźnie.

| Termin | Znaczenie | Nie używaj |
|---|---|---|
| MG | Mistrz Gry, jedyny użytkownik aplikacji | — |
| autor | właściciel produktu; decyduje o produkcie i ogląda wynik, sam prowadzi aplikacją sesje | — |
| twórca paczki | ten, kto pisze pliki paczki (MG albo ktoś inny) | autor paczki |
| rama | `Core` i `Desktop` razem: kod wspólny dla wszystkich systemów | wspólny kod |
| system | projekt `Content.<system>`: typy treści, karty, przełomy jednej gry | — |
| paczka | katalog na dysku z `pack.json` i wpisami | — |
| wpis | plik JSON w paczce, jedna rzecz albo wiedza (stworzenie, przedmiot, stan) | — |
| pozycja prozy | element sekcji prozy (`entries`: nazwa, notatka, tekst) | wpis prozy |
| typ treści | rekord C# plus zaprojektowana karta; ustala kształt `values` wpisu | szablon |
| aspekt | nazwana grupa pól jednego konsumenta we wpisie i w entity (walka, przedmiot, ładunki) | paczka pól |
| konsument | narzędzie, przełom, filtr, sortowanie albo wartość liczona, które czyta pole | — |
| pole ze strukturą | pole typowane (liczba, kość, wybór z listy), które czyta konsument | — |
| entity | rzecz świata w kampanii: łącze do wpisu plus nakładka; w kodzie `CampaignEntity` | instancja, egzemplarz, rzecz świata |
| istota | entity z aspektem walki (stworzenie, postać gracza) | — |
| nakładka | rzadka łatka entity: same odchylenia od wpisu | łatka, patch |
| wiedza | typ bez entity, wskazywany identyfikatorem (zaklęcie, stan, zdolność) | — |
| katalog świata | drzewo katalogów z entity w stanie kampanii (`docs/spec/katalog.md`) | — |
| katalog | węzeł katalogu świata z nazwą i zawartością (katalogi, entity); w kodzie `WorldFolder` | folder |
| № | numer entity: unikalny w kampanii, nadawany po kolei, nigdy zmieniany ani używany ponownie | ID, indeks |
| biurko | pozycja kampanii w ramie z pływającymi oknami narzędzi | — |
| narzędzie | okno na biurku wniesione przez system | panel |
| księga | śledzone wartości i reguły ich zmiany: pole zmiany, rachunek, ostatnia zmiana | księgowość |
| pole zmiany | pole przyjmujące `-12`, `+5`, `=30` przy śledzonej wartości | — |
| przełom | akcja MG zmieniająca wiele wartości naraz według jawnej reguły; lista w `decisions.md` | — |
| okienko przełomu | boczna lista zmian przełomu z polami do odznaczenia | — |
| tyknięcie | obrażenia albo przypomnienie stanu na początek lub koniec tury | — |
| wariant zasad | opcjonalna reguła z podręcznika albo dodatku, włączana per kampania; niezbudowane | dodatek |
| slot | miejsce, które trzyma przedmioty z ilością i ceną (sklep, plecak); niezbudowane | — |
| odznaka | `Badge`: krótka liczba przy obiekcie („1/2”, „+3”) | plakietka |
| pigułka | `WordTag`: słowo przy obiekcie (kategoria, rzadkość, „magiczny”) | plakietka |
| blok wartości | podpis z ikoną, wartość i dopisek w nagłówku karty (KP, PW, Szybkość) | klocek |
| render | obraz widoku z `tools/render`, zrobiony bez ekranu | — |
| PW, KP, ST | punkty wytrzymałości, klasa pancerza, stopień trudności | PZ |
| SRD | System Reference Document 5.1: darmowe zasady D&D na licencji CC-BY 4.0 | — |

Słowa pracy (wycinek, etap, kamień milowy, szlif, wykonawca) definiuje `CLAUDE.md` i `docs/roadmap.md`.

## Warstwy

| Projekt | Co wie | Czego nie wie |
|---|---|---|
| `Core` | kampanie, stan, zapis, wpisy, paczki, entity | Avalonia, jakikolwiek system |
| `Desktop` | okno, pasek boczny, motyw, zakładki treści, biurko | jakikolwiek system (np. „potwór”) |
| `Content.<system>` | typy treści, karty, zakładki, narzędzia biurka | inne systemy |
| `App` | które systemy są wkompilowane | — |

- Systemy ładuje się statycznie, referencją projektu. Systemy się nawzajem nie referencują.
- Granic pilnują testy w `tests/DungeonApp.Architecture.Tests`; reguły: `CLAUDE.md`, „Struktura”.
- System mówi ramie (`Desktop/Systems/IGameSystem`): kim jest, jakie ma zakładki kategorii System
  (fabryka bez argumentu — zakładka nie widzi kampanii) i kategorii Kampania (fabryka dostaje
  `CampaignTabContext`), jakie modele stanu zapisuje jego kampania i jakie kroki startowe zgłasza.
- Rama zachowuje jedyną drogę zmiany stanu. System przejmuje wygląd i zawartość, nigdy zapisu.
- Wkompilowany jest jeden system (`Dnd5e`); ekran wyboru systemu i tak się pokazuje.

## Treść: paczki, wpisy, typy treści

**Kształt jest kodem, wartości są danymi.**

- **Paczka** to katalog z `pack.json` i `entries/*.json`. Paczki MG leżą w
  `Dokumenty\DungeonApp\<system>\packs\`, paczki dostarczane z programem — w
  `<katalog programu>\<system>\packs\`. Oba źródła czyta się jednym przebiegiem do jednego rejestru.
  Powtórzone id paczki odrzuca obie.
- Z programem idzie paczka `dnd5e-srd`: przykłady z SRD 5.1 po polsku (CC-BY 4.0); ikony stanów to
  SVG z game-icons.net (CC BY 3.0, autorzy w `LICENSE.txt` paczki).
- **Wpis** to plik JSON: `id`, `name`, `template` (`system:typ`), `templateVersion`, `values`.
  Format koperty jest stały; zmieniają się tylko pola `values` danego typu.
- **Typ treści** to para: rekord C# (np. `Creature`) i zaprojektowana karta (np. `CreatureCardView`).
  Deklaruje go system w swoim katalogu typów (`IContentTypeCatalog`) — to jedyne legalne miejsce
  rozgałęzienia po identyfikatorze typu. `values` deserializuje się wprost w rekord; deserializator
  (`required`, brak nieznanych kluczy) jest walidatorem. Pole to napis, liczba, znacznik, **aspekt**
  albo **sekcja prozy**.
- **Aspekt** to obiekt w `values` z polami jednego konsumenta: walka stworzenia (`combat`: KP, PW,
  bieżące PW, cechy), przedmiot (`item`: waga, wartość), ładunki (`charges`). Każdy to rekord
  w systemie; walka i przedmiot są wymagane we wpisie, ładunki nie. Narzędzie pyta o aspekt, nie
  o typ. Aspekt nie ma wyglądu — pola rozmieszcza karta typu.
- **Sekcja prozy** (u stworzenia cechy szczególne, akcje, rzucanie czarów, akcje dodatkowe, reakcje,
  akcje legendarne; u stanu zasady) to `{ "intro": "…", "entries": [{ "name", "note", "text" }] }`.
  `intro` i `note` są opcjonalne; sekcja bez wstępu i bez pozycji, pozycja bez nazwy albo tekstu,
  a proza zapisana zwykłym napisem odrzucają wpis.
- **Znaczniki w prozie:** w `intro` i `text` `**…**` wyróżnia fragment (premia, obrażenia, ST),
  a `[[id|tekst]]` wskazuje inny wpis — karta pokazuje sam tekst. Strukturę pisze twórca paczki;
  karta niczego nie wyczytuje z tekstu i nic jej nie liczy.
- **Zepsuta treść jest widoczna, nie znika**: wadliwy wpis dostaje powód, a reszta paczki się
  wczytuje. Zepsuty manifest odrzuca całą paczkę z powodem.
- **Obrazek wpisu:** pole wskazane przez deskryptor typu (`ImageProperty`); ścieżka względem katalogu
  paczki, tylko PNG/JPG/WebP/SVG. Ścieżka poza paczkę odrzuca wpis; brak pliku pokazuje się na karcie.
  Plik czyta biblioteka przy pokazaniu karty i podaje go karcie gotowy (`EntryPicture`); karta stawia
  ramkę i ikonę zastępczą, ale nie wie, gdzie leży paczka.
- **Ikona jednobarwna** (SVG, u stanu; `SvgIcon` czyta podzbiór: `path` z `viewBox`): ramka rysuje sam
  kształt w miejscu ikony zastępczej, tym samym rozmiarem i kolorem. Profil typu może kazać wierszom
  listy stawiać tę ikonę przed nazwą; obrazka rastrowego wiersz listy nie rysuje.
- Rejestr (`ContentRegistry`) jest tylko do odczytu. Każdy system wczytuje własny.

## Kampania i stan

- **Kampania to sesja, świat i zapis naraz.** Nie ma warstwy scen — podziału kampanii na sceny
  z własnym stanem i operacjami na zawartości (`docs/decisions.md`, „Odrzucone”).
- Stan kampanii to niemutowalna migawka (`CampaignStateSnapshot`) złożona z modeli stanu
  (`StateModelDeclaration`). Modele **ramy** — entity, katalogi (`world.folders`) i licznik № —
  deklaruje `WorldModels`; modele **systemu** dochodzą z `IGameSystem.StateModels`. Listę kampanii
  składa jedno miejsce, `WorldModels.Combine`, i z niego korzystają otwieranie, tworzenie i sesja. Zmiana to `CampaignChange` (upsert/delete rekordów),
  przechodząca przez jedne drzwi: `CampaignSession.ChangeAsync` → zapis → powiadomienie widoków.
- Zapis jest natychmiastowy i atomowy (`AtomicWrite`): pliki modeli najpierw, manifest zatwierdza
  generację jako ostatni, więc rozdarty zapis jest wykrywalny. Format pilnuje test bajt w bajt na
  wzorcowej kampanii.
- **Entity** (`CampaignEntity`) to łącze do wpisu (`paczka:id`) plus **nakładka** — rzadka łatka
  z samymi odchyleniami (np. aktualne PW, nazwa własna). Rozwiązuje się ją od nowa przy każdym
  odczycie, więc poprawka wpisu w paczce dociera do istniejących kampanii.
- **Katalogi i №.** Katalog (`WorldFolder`: nazwa, katalog nadrzędny; katalogu głównego „Świat” nie ma
  jako rekordu) i entity (`FolderId`, `Number`) tworzą drzewo, którego rama nie pyta o typ. Licznik № to
  jeden rekord (`WorldCounter`) z ostatnim nadanym numerem: usunięcie entity go nie cofa. Zmiany
  drzewa (dodanie N entity, przeniesienie, usunięcie, nazwy) to czyste fabryki w `WorldChanges`, jedna
  akcja MG = jedna zmiana; odmowę (katalog w siebie, niepusty katalog) opisuje polski tekst z funkcji
  `...Problem`. Widok czyta drzewo przez `WorldTree` (katalogi przed entity, nazwy porównywane
  naturalnie). Co do typu entity, ikony i podpowiedzi wiersza pyta ramę o system `IGameSystem.EntityTypes`
  i `RowHint`. Kampania zapisana przed katalogami otwiera się bez plików nowych modeli; jej entity
  dostają № przy odczycie, w kolejności identyfikatorów (te same przy każdym otwarciu), licznik
  startuje za nimi, a trwale zapisują się przy pierwszym zapisie.
- Nakładka scala obiekty po kluczu: zmiana PW zapisuje tylko `{"combat":{"currentHp":3}}`,
  a późniejsza poprawka KP we wpisie dociera do entity.
- Format kampanii inny niż bieżący (zbyt nowy albo zbyt stary) oznacza odmowę odczytu, a wpis paczki
  w innej wersji typu niż bieżąca jest oznaczany. Kiedy wersje rosną i kiedy pojawią się migracje:
  `docs/decisions.md`, „Wersje formatów nie rosną do pierwszej sesji przy stole”.

## Gdzie mieszka stan

| Co | Gdzie |
|---|---|
| Paczki MG | `Dokumenty\DungeonApp\<system>\packs\<paczka>\` |
| Kampanie | `Dokumenty\DungeonApp\<system>\campaigns\<id>\` |
| Układy biurka | `%LocalAppData%\DungeonApp\layouts\` |
| Log | `%LocalAppData%\DungeonApp\logs\dungeonapp.log`; powyżej 1 MB poprzedni jako `dungeonapp.old.log` |

## Interfejs

- **Start:** kurtyna startowa zasłania okno, dopóki biegną kroki startowe systemów (wczytanie
  paczek, rozgrzewka kart), a potem ramy (półka kampanii, rozgrzewka ekranów). Rozgrzewka buduje
  każdą kartę i ekran raz za kurtyną, żeby pierwsze kliknięcie nie budowało ich na wątku interfejsu.
  Awaria kroku nigdy nie zatrzymuje startu — kończy się ostrzeżeniem na pasku stanu i wpisem w logu.
- **Błędy:** wyjątek na wątku interfejsu — z komendy, kliknięcia, timera interfejsu czy układu
  (komenda asynchroniczna oddaje swój błąd na ten wątek) — łapie jeden handler
  (`Desktop/Diagnostics/UiThreadErrors`, podpięty w `App`): wpis w logu, jedno powiadomienie w oknie
  głównym, program działa dalej. Błąd złapany i niepokazany MG trafia do logu przez `AppLog`
  (statyczny, bo globalne handlery i kroki startowe systemów nie mają wspólnego właściciela). Błąd
  w tle poza interfejsem trafia tylko do logu. W testach bez ekranu handlera nie ma — wyjątek oblewa
  test.
- **Modele widoków:** CommunityToolkit.Mvvm — `[ObservableProperty]` na właściwościach częściowych
  i `[RelayCommand]`. Komenda nie sprawdza `CanExecute` w `Execute`; robi to przycisk.
- **Nawigacja:** ekran wyboru systemu, potem pasek boczny z trzema kategoriami: **Kampania** (jedna
  pozycja ramy: półka kampanii, a po otwarciu kampanii jej biurko; zakładki kampanii spoza biurka,
  jeśli system je deklaruje, stoją pod nim), **System** (zakładki treści), **Aplikacja** (Galeria,
  Ustawienia). Zakładki wypełnia skompilowany system, nigdy paczka.
- **Okno czy zakładka:** to, na co MG patrzy obok innych rzeczy, jest oknem na biurku; to, w czym
  przebywa, jest zakładką.

### Zakładka treści i karty

- **Zakładka treści** (`Desktop/Entries/ContentTab`): układ lista–szczegół, wspólny dla typów treści.
  System podaje profil typu: kategorię, tagi, odznakę wiersza, filtry wartości i sortowania. Szczegół
  to nagłówek z biblioteki (ścieżka, nazwa, tagi) plus karta systemu o stałej szerokości.
- **Nagłówek karty:** karta może pożyczyć nagłówkowi cztery kontrolki (`IEntryCardHeader`): obraz na
  lewo od nazwy, wartość na końcu pierwszej linii nazwy (nazwa i tagi zawijają się przed nią), słowo
  przed tagami i blok pod tagami. Obraz ma 150 px szerokości, tyle co trzy komórki tabeli cech:
  kwadrat u przedmiotu, portret 3:4 (150×200) u stworzenia.
- **Bloki wartości** stoją pod tagami, dnem równo z dolną krawędzią obrazu; gdy kolumna tytułu jest
  wyższa, nagłówek rośnie, nic nie jest przycinane. Blok ma zawsze trzy rzędy (podpis z ikoną,
  wartość, dopisek); rząd dopisku stoi także pusty, więc bloki mają równą wysokość.
- **Karta stworzenia:** w nagłówku portret oraz bloki KP, PW i Szybkość. Pod nagłówkiem dwie tabele
  cech (SIŁ/ZRĘ/KON pod obrazem, INT/MDR/CHA od linii tytułu), każda cecha w kolumnie — skrót nad
  kratką, pod nim kwadratowe komórki wartości i modyfikatora. Dalej pary pól, sekcje prozy zawsze
  otwarte (nagłówek z paskiem akcentu, nazwa pozycji wyróżniona, notatka przygaszona), opis jako
  stopka. Nazwa stworzenia się zawija.
- **Karta przedmiotu — nagłówek:** kategoria w ścieżce (jak grupa stworzenia), kwadratowy obrazek,
  pigułki rzadkości, „magiczny” i podtypu; na końcu linii nazwy waga z ikoną odważnika, pod nią
  mniejsza, przygaszona wartość (obie obowiązkowe, zero też widać). U dołu kolumny tytułu bloki KP,
  Obrażenia i Ładunki — ta sama kontrolka co u stworzenia.
- **Nazwa przedmiotu** stoi w jednej linii, ucięta wielokropkiem na dowolnej literze
  (`RevealingTextBlock`). Pod wskaźnikiem pełna nazwa pojawia się w tym samym miejscu na kryjącym tle
  i zakrywa wagę i wartość; przenika tylko dopisana końcówka, początek nazwy się nie zmienia.
- **Karta przedmiotu — treść:** za separatorami (pierwszy w odstępie nagłówka od karty, jak u tabel
  cech stworzenia) pary bez ikon (Dostrojenie, Właściwości, Siła, Skradanie się, Odnawianie) i sekcja
  „Opis” jak sekcje prozy stworzenia. Skala rzadkości i jej kolory: `docs/decisions.md`.
- **Zaznaczanie tekstu:** proza karty jest tekstem do zaznaczenia; przeciągnięcie zaczęte obok tekstu
  zaznacza najbliższy blok (`TextSelectionArea`). Zaznaczenie przez kilka bloków naraz to osobna,
  większa funkcja i go nie ma.

### Zasady wyglądu kart

Obowiązują każdą kartę. Wygląd sprawdza się renderem (`tools/render`) przed oddaniem autorowi.

- Układ stoi na kilku pionowych liniach, do których wyrównuje się wszystko (portret ma szerokość
  bloku pod nim, wartości par zaczynają się na linii nazwy).
- Sekcje są statyczne — bez rozwijania, strzałek i wcięć; to, co puste, się nie pokazuje.
- Karta nie reaguje na mysz. Bloki z obramowaniem mają ostre rogi.
- Liczby krojem interfejsu z cyframi tabelarycznymi, bez kroju o stałej szerokości.
- Ikona tylko tam, gdzie niesie znaczenie przez konwencję (tarcza przy KP), nigdy jako ozdoba; stoi
  w bloku wartości, nigdy w tabelce par.
- Wysokość wiersza wyznacza linia tekstu — pigułka ani odznaka jej nie podnosi.
- Nazwa pozycji prozy pogrubiona, w osobnej linii, z kropką; wyróżnienia w tekście zaznacza paczka.
- Odcień wartości: nasycona barwa przy małej nieprzezroczystości, nie barwa przygaszona.
- Opis, który jest tylko kolorytem (stworzenie), to mała stopka; opis niosący zasady (przedmiot) to
  sekcja prozy „Opis”.

### Biurko, motyw, interakcja

- **Biurko** (`Desktop/Workspace`): pozycja kampanii w ramie, z pływającymi oknami narzędzi. Buduje je
  rama (`ActiveSystemSession`, raz na otwartą kampanię); system podaje tylko narzędzia
  (`IGameSystem.CreateDeskTools`). Układ zapisuje się per kampania. Nieudany zapis zmiany rama
  pokazuje powiadomieniem, niezależnie od narzędzia, które zmieniało.
- **Katalog świata** (`Workspace/World`, spec: `docs/spec/katalog.md`): drzewo katalogów i entity
  w prawym górnym rogu biurka, warstwa pod oknami (okno nachodzi na katalog). Pozycja, szerokość,
  zwinięcie korzenia i rozwinięte katalogi zapisują się w pliku układu biurka (`WorldCatalogLayout`),
  nie w kampanii; rozwinięcia znikłych katalogów są zapominane przy zapisie. Rama bierze wpisy
  z `IGameSystem.GetWorldCatalogSource()` (`WorldCatalogSource`: rejestr paczek, typy, profile),
  ikony typów z `IGameSystem.EntityTypes`, a dopisek przy wierszu (np. PW) z `IGameSystem.RowHint`.
  Kliknięcie zaznacza (Ctrl, Shift), dwuklik albo Enter na entity zgłasza zdarzenie
  `OpenEntityRequested`; ostatnio klikniętą entity podaje `LastClickedEntity` (podgląd idzie za nią).
- **Listek** (`Workspace/Leaf`): pasek poleceń w lewym górnym rogu biurka, nad wszystkimi oknami
  (okna mogą wjechać pod niego): zamknięcie kampanii oraz zaślepki Cofnij, Ponów i Polecenia. Ciemniejsze
  tło, mocniejszy cień i większe zaokrąglenie niż u okna to tokeny elementu ramy nad oknami, wspólne
  z przyszłym hotbarem.
- **Maksymalizacja okna:** tylko okno bez górnej granicy rozmiaru (inne nie mają przycisku). Okno
  zmaksymalizowane wypełnia biurko bez obwódki, tytuł stoi pośrodku, przyciski nagłówka znikają;
  przywraca dwuklik w tytuł albo Esc. Stan zmaksymalizowany nie trafia do zapisanego układu.
- **Paleta** (`Desktop/Controls/Palette.cs`, motyw `Themes/Controls/Palette.axaml`): okienko u góry
  pośrodku okna, w `WindowOverlay`, bez wiedzy o systemie. Woła się `Palette.ShowAsync(origin,
  PaletteOptions)` — wywołujący podaje tekst celu, funkcję wyszukiwania (paleta pokazuje tylko jej wynik)
  i akcję wyboru `(PaletteItem, ilość)`; zadanie kończy się przy zamknięciu, druga paleta zamyka
  pierwszą. Z okienka wysuwanego (menu kontekstowe) nakładkę znajduje przez okno główne. Liczbę sztuk
  („4 gob”) czyta `PaletteQuery.Parse`, gdy wywołujący na to pozwala; Ctrl+Enter zostawia paletę otwartą.
- **Motyw** (`Desktop/Themes`) jest kompletny i własny, bez Fluenta (domyślnego motywu Avalonii) pod
  spodem. Tokeny kolorów mają zapisane znaczenie; skale należące do systemu (rzadkość przedmiotu) mają
  własne kolory w systemie. Galeria pokazuje każdą kontrolkę motywu w każdym stanie.
- **Konwencje interakcji:**
  - kursor to strzałka wszędzie poza polem tekstu, także nad przyciskami;
  - stan zmienia się od razu; animacje trwają najwyżej 150 ms i wyłączają się razem z animacjami
    systemu;
  - zmiana stanu nie zmienia wymiarów kontrolki, także zmiana formy z tekstu na pole edycji;
  - tekst klikany ma co najmniej 12 pt, kontrast tekstu co najmniej 4,5:1;
  - fokus klawiatury nie ma widocznego wyglądu (roadmapa, „Szlif”).

## Kierunek (niezbudowane)

Każde z poniższych powstaje dopiero razem z pierwszym konsumentem; kolejność: `docs/roadmap.md`.

- **Granica automatyzacji na przykładzie.** Atak liczy MG: rzuca kośćmi albo kalkulatorem („17 = 13
  na k20 + 4”), porównuje z KP z karty i wpisuje celowi `-12`. Pole zmiany odejmuje najpierw PW
  tymczasowe i zostawia przy wartości „30, było 42”. „Następna tura” kończy turę goblina (jego
  Ogłuszenie schodzi z 1 na 0 i znika) i zaczyna turę trolla (przypomnienie o regeneracji); okienko
  przełomu wymienia obie zmiany, a odznaczenie przywraca Ogłuszenie. „Sprzedaj” przy mieczu bierze
  cenę z wartości wpisu (sprzedaż za połowę), MG ją zmienia, a zatwierdzenie przenosi miecz i złoto.
- **Przełomy i wartości pochodne:** kod systemu. Wartość pochodna (modyfikator cechy, pasywna
  Percepcja) liczy się przy odczycie z pól ze strukturą. Formuły w danych paczki są odłożone.
- **Katalog świata:** `docs/spec/katalog.md`.
- **Postacie graczy:** obok kampanii, w katalogu systemu na dysku; zmieniane formularzem w aplikacji.
  Postać jest źródłem entity tak jak wpis, więc identyfikator `paczka:id` rozszerza się do
  `źródło:id`, gdzie źródło to paczka albo postać.
- **Sloty:** miejsce z listą przedmiotów, ich ilością i ceną (sklep, plecak). Czy plecak to slot
  w istocie, czy entity w entity w drzewie świata, rozstrzyga etap 9 roadmapy.
- **Dokument:** długi tekst przygody albo lore z markerami pól interaktywnych i linkami do stron
  i identyfikatorów; mieszka w paczce, odhaczenia w kampanii; własny renderer, bez karty.
- **Warianty zasad:** przełączniki na stronie kampanii. System dowiaduje się o nich tylko przy
  składaniu kampanii; żaden kontekst zakładki ani okna ich nie niesie.
- **Autorstwo treści w aplikacji:** najpierw „skopiuj i zmień” do własnej paczki MG, potem tworzenie
  od zera — formularz projektowany per typ, zapisujący ten sam plik wpisu.
- **Podział księgi między ramę i system:** pole zmiany, tryb edycji karty, kalkulator kości, okienko
  przełomu i znaczniki zmian w turze należą do ramy i nie znają systemu. Reguły rachunku (PW
  tymczasowe schodzą pierwsze) i treść przełomów (co tyka na końcu tury, co odnawia odpoczynek) są
  kodem systemu.
