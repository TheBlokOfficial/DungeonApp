# Architektura

Jak aplikacja jest zbudowana dziś i dokąd zmierza. Przy rozbieżności z kodem prawdą jest kod —
popraw ten dokument w tym samym commicie.

## Czym to jest

Laptop MG przy stole: ile życia ma jeszcze goblin, co drużyna ma w plecaku, co żyje w świecie kampanii.
Gracze aplikacji nie widzą. To nie jest wirtualny stół, gra ani generator treści. Aplikacja prowadzi
księgowość (wartości, liczniki, kolejka, przełomy tury i odpoczynku), zasady stosuje MG — granica
w `CLAUDE.md`, lista przełomów w `docs/decisions.md`.

Zakres: systemy „trad” — nazwane statystyki, rzut z modyfikatorem, jakaś kolejność działania (D&D,
Pathfinder, OSR, Call of Cthulhu). Aplikacja **wie, czym jest potwór i jak go pokazać**, bo karta jest
zaprojektowana; **nie wie, czym jest D&D**, bo wiedza o systemie — także przełomy — mieszka
w wymienialnym projekcie. Pole dostaje strukturę, gdy czyta je księga albo przełom; inaczej jest prozą.

## Warstwy

| Projekt | Co wie | Czego nie wie |
|---|---|---|
| `Core` | kampanie, stan, zapis, wpisy, paczki, instancje | Avalonia, jakikolwiek system |
| `Desktop` | okno, pasek boczny, motyw, zakładki treści, biurko | jakikolwiek system (np. „potwór”) |
| `Content.<system>` | typy treści, karty, zakładki, narzędzia biurka | inne systemy |
| `App` | które systemy są wkompilowane | — |

- Systemy ładuje się statycznie, referencją projektu. Systemy się nawzajem nie referencują.
- System mówi ramie (`Desktop/Systems/IGameSystem`): kim jest, jakie ma zakładki kategorii System
  (fabryka bez argumentu — zakładka nie widzi kampanii) i kategorii Kampania (fabryka dostaje
  `CampaignTabContext`), jakie modele stanu zapisuje jego kampania i jakie kroki startowe zgłasza.
- Rama zachowuje jedyną drogę zmiany stanu. System przejmuje wygląd i zawartość, nigdy zapisu.
- Dziś jest jeden system (`Dnd5e`), a ekran wyboru systemu i tak istnieje.

## Treść: paczki, wpisy, typy treści

**Kształt jest kodem, wartości są danymi.**

- **Paczka** to katalog z `pack.json` i `entries/*.json`. Paczki MG leżą w
  `Dokumenty\DungeonApp\<system>\packs\`, paczki dostarczane z programem — w
  `<katalog programu>\<system>\packs\` (dziś `dnd5e-srd`, przykłady z SRD 5.1, CC-BY 4.0). Oba źródła
  czyta się jednym przebiegiem do jednego rejestru. Powtórzone id paczki odrzuca obie.
- **Wpis** to plik JSON: `id`, `name`, `template` (`system:typ`), `templateVersion`, `values`.
  Format koperty jest stały; zmieniają się tylko pola `values` danego typu.
- **Typ treści** to para: rekord C# (np. `Creature`) i zaprojektowana karta (np. `CreatureCardView`).
  Deklaruje go system w swoim katalogu typów (`IContentTypeCatalog`) — to jedyne legalne miejsce
  rozgałęzienia po identyfikatorze typu. `values` deserializuje się wprost w rekord; deserializator
  (`required`, brak nieznanych kluczy) jest walidatorem. Pole to napis, liczba, znacznik, **aspekt**
  albo **sekcja prozy**.
- **Aspekt** to paczka pól jednego znaczenia, zapisana jako obiekt w `values`: walka stworzenia
  (`combat`: KP, PZ, bieżące PZ, cechy), przedmiot (`item`: waga, wartość), ładunki (`charges`).
  Każdy to rekord w `Content.Dnd5e`; walka i przedmiot są wymagane we wpisie, ładunki nie; narzędzie pyta
  o aspekt, nie o typ. Aspekt nie ma wyglądu — pola rozmieszcza karta typu. Łatka instancji scala
  obiekty po kluczu, więc zmiana PZ zapisuje tylko `{"combat":{"currentHp":3}}`, a późniejsza
  poprawka KP we wpisie dociera do instancji.
- **Sekcja prozy** (u stworzenia cechy szczególne, akcje, rzucanie czarów, akcje
  dodatkowe, reakcje, akcje legendarne) to `{ "intro": "…", "entries": [{ "name", "note", "text" }] }`.
  `intro` i `note` są opcjonalne; sekcja bez wstępu i bez wpisów, wpis bez nazwy albo tekstu, a proza
  zapisana zwykłym napisem odrzucają wpis. W `intro` i `text` `**…**` wyróżnia fragment (premia,
  obrażenia, ST). Strukturę pisze autor paczki — karta niczego nie wyczytuje z tekstu i nic jej nie liczy.
- **Zepsuta treść jest widoczna, nie znika**: wadliwy wpis dostaje powód, a reszta paczki się
  wczytuje. Zepsuty manifest odrzuca całą paczkę z powodem.
- Obrazek wpisu: pole wskazane przez deskryptor typu (`ImageProperty`); ścieżka względem katalogu
  paczki, tylko PNG/JPG/WebP. Ścieżka poza paczkę odrzuca wpis; brak pliku pokazuje się na karcie.
  Plik czyta biblioteka przy pokazaniu karty i podaje go karcie gotowy (`EntryPicture`); karta stawia
  ramkę i ikonę zastępczą, ale nie wie, gdzie leży paczka.
- Rejestr (`ContentRegistry`) jest tylko do odczytu. Każdy system wczytuje własny.

## Kampania i stan

- **Kampania to sesja, świat i zapis naraz.** Warstwy scen nie ma.
- Stan kampanii to niemutowalna migawka (`CampaignStateSnapshot`) złożona z modeli stanu, które
  zadeklarował system (`StateModelDeclaration`). Zmiana to `CampaignChange` (upsert/delete rekordów),
  przechodząca przez jedne drzwi: `CampaignSession.ChangeAsync` → zapis → powiadomienie widoków.
- Zapis jest natychmiastowy i atomowy (`AtomicWrite`): pliki modeli najpierw, manifest zatwierdza
  generację jako ostatni, więc rozdarty zapis jest wykrywalny. Format pilnuje test bajt w bajt na
  wzorcowej kampanii.
- **Instancja** to egzemplarz w kampanii: łącze do wpisu (`paczka:id`) plus **nakładka** — rzadka
  łatka z samymi odchyleniami (np. aktualne PZ, nazwa własna). Rozwiązuje się ją od nowa przy każdym
  odczycie, więc poprawka wpisu w paczce dociera do istniejących kampanii.
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

- **Start:** kurtyna startowa, podczas której biegną kroki startowe systemów (wczytanie paczek,
  rozgrzewka kart), a potem ramy (półka kampanii, rozgrzewka ekranów). Awaria kroku nigdy nie
  zatrzymuje startu — kończy się ostrzeżeniem na pasku stanu i wpisem w logu.
- **Błędy:** wyjątek na wątku interfejsu — z komendy, kliknięcia, timera czy układu (komenda
  asynchroniczna oddaje swój błąd na ten wątek) — łapie jeden handler
  (`Desktop/Diagnostics/UiThreadErrors`, podpięty w `App`): wpis w logu, jedno powiadomienie w oknie
  głównym, program działa dalej. Kod akcji MG nie łapie więc błędu po to, żeby go ukryć. Błąd złapany
  i niepokazany MG trafia do logu (`AppLog`, statyczny, bo globalne handlery i kroki startowe systemów
  nie mają wspólnego właściciela). Błąd w tle poza interfejsem trafia tylko do logu. W testach bez
  ekranu handlera nie ma — wyjątek oblewa test.
- **Modele widoków:** CommunityToolkit.Mvvm — `[ObservableProperty]` na właściwościach częściowych
  i `[RelayCommand]`. Komenda nie sprawdza `CanExecute` w `Execute`; robi to przycisk.
- **Nawigacja:** ekran wyboru systemu, potem pasek boczny z trzema kategoriami: **Kampania** (półka
  kampanii, a po otwarciu kampanii jej zakładki), **System** (zakładki treści), **Aplikacja** (Galeria,
  Ustawienia). Zakładki wypełnia skompilowany system, nigdy paczka. Kryterium okna czy zakładki: czy
  patrzysz na to obok innych rzeczy (okno na biurku), czy w tym przebywasz (zakładka).
- **Zakładka treści** (`Desktop/Entries/ContentTab`): układ lista–szczegół, wspólny dla typów treści.
  System podaje profil typu: kategorię, tagi, odznakę wiersza, filtry wartości i sortowania. Szczegół
  to nagłówek z biblioteki (ścieżka, nazwa, tagi) plus karta systemu o stałej szerokości. Karta może
  pożyczyć nagłówkowi cztery kontrolki (`IEntryCardHeader`): obraz na lewo od nazwy, wartość na końcu
  pierwszej linii nazwy (nazwa i tagi zawijają się przed nią), słowo przed tagami i blok pod tagami
  (u stworzenia portret oraz KP, PZ i Szybkość). Obraz ma 150 szerokości (własny token; trzy komórki
  tabeli cech po 50 mają tę samą szerokość): kwadrat u przedmiotu, portret 3:4 (150×200) u stworzenia — tyle, żeby obok zmieściły się nazwa
  w jednej linii, tagi i blok. Blok stoi dnem równo z dolną krawędzią obrazu; gdy kolumna tytułu
  jest wyższa, nagłówek rośnie, nic nie jest przycinane. Wartość w bloku to zawsze trzy rzędy:
  podpis z ikoną, wartość, dopisek — rząd dopisku stoi także pusty, więc wartości mają równą wysokość.
  Karta stworzenia pod nagłówkiem: dwie tabele cech (SIŁ/ZRĘ/KON pod obrazem, INT/MDR/CHA od linii
  tytułu), każda cecha w kolumnie — skrót nad kratką, pod nim kwadratowe komórki wartości
  i modyfikatora; pary pól, sekcje prozy zawsze otwarte (nagłówek
  z paskiem akcentu, nazwa wpisu wyróżniona, notatka przygaszona), opis jako stopka. Przedmiot:
  kategoria w ścieżce (jak grupa stworzenia), w nagłówku kwadratowy obrazek; nazwa w jednej linii,
  ucięta na dowolnej literze wielokropkiem bez spacji, a pod wskaźnikiem pokazana cała w tym samym
  miejscu, na nakładce okna z kryjącym tłem karty, z krótkim przenikaniem (dorysowuje tylko część
  od miejsca ucięcia, litery wspólne zostają tytułu); na ten czas zakrywa wagę i wartość
  (`RevealingTextBlock`; nazwa stworzenia się zawija); na końcu linii nazwy waga
  z ikoną odważnika, pod nią mniejsza, przygaszona wartość (obie obowiązkowe, zero też widać); pigułki rzadkość, „magiczny”, podtyp; u dołu kolumny tytułu blok główny KP / Obrażenia / Ładunki
  (ta sama kontrolka co KP / PZ / Szybkość stworzenia, tylko wypełnione). Pod nagłówkiem, za
  separatorami (pierwszy w odstępie nagłówka od karty, jak tabele cech stworzenia): pary bez ikon (Dostrojenie, Właściwości, Siła, Skradanie się, Odnawianie) i sekcja
  „Opis” jak sekcje prozy stworzenia. Rzadkość ma pięć stopni ze skali gier: Pospolity (jasnoszary),
  Niepospolity (zielony), Rzadki (niebieski), Epicki (fioletowy), Legendarny (pomarańczowy);
  magiczność to osobny znacznik. Proza karty jest tekstem do zaznaczenia; przeciągnięcie zaczęte
  obok tekstu zaznacza najbliższy blok (`TextSelectionArea`) — zaznaczenia przez kilka bloków naraz
  nie ma.
- **Zasady wyglądu kart** (przyjęte przez autora na karcie stworzenia; obowiązują każdą kartę):
  układ stoi na kilku pionowych liniach, do których wyrównuje się wszystko (portret ma szerokość
  bloku pod nim, wartości par zaczynają się na linii nazwy). Sekcje są statyczne — bez rozwijania,
  strzałek i wcięć; to, co puste, się nie pokazuje. Karta nie reaguje na mysz. Bloki z obramowaniem
  mają ostre rogi. Liczby krojem interfejsu z cyframi tabelarycznymi, bez kroju o stałej szerokości.
  Ikona tylko tam, gdzie niesie znaczenie przez konwencję (tarcza przy KP), nigdy jako ozdoba; stoi
  przy wartości pierwszego rzędu (blok główny), nigdy w tabelce par.
  Wysokość wiersza wyznacza linia tekstu — plakietka jej nie podnosi. Nazwa wpisu prozy pogrubiona,
  w osobnej linii, z kropką; wyróżnienia w tekście zaznacza paczka (`**…**`). Odcień wartości:
  nasycona barwa przy małej nieprzezroczystości, nie barwa przygaszona. Opis, który jest tylko
  kolorytem (stworzenie), to mała stopka; opis niosący zasady (przedmiot) to sekcja prozy „Opis”.
  Wygląd sprawdza się renderem (`tools/render`) przed oddaniem autorowi.
- **Biurko** (`Desktop/Workspace`): zakładka kampanii z pływającymi oknami narzędzi wnoszonych przez
  system. Układ zapisuje się per kampania. Dziś jest jedno narzędzie: „Świat kampanii” (instancje, PZ).
- **Motyw** (`Desktop/Themes`) jest kompletny i własny, bez Fluenta pod spodem. Tokeny kolorów mają
  zapisane znaczenie; skale należące do systemu (rzadkość przedmiotu) mają własne kolory w systemie.
  Galeria pokazuje każdą kontrolkę motywu w każdym stanie.
- Konwencje interakcji: strzałka kursora wszędzie poza polem tekstu (ręki nie ma); stan zmienia się
  od razu, animacje najwyżej 150 ms i wyłączają się razem z animacjami systemu; zmiana stanu nie
  zmienia wymiarów kontrolki, także zmiana formy z tekstu na pole edycji; tekst klikany ma co najmniej 12 pt; kontrast tekstu co najmniej 4,5:1;
  fokus klawiatury nie ma jeszcze widocznego wyglądu.

## Granica automatyzacji w praktyce

Atak liczy MG: rzuca kośćmi albo kalkulatorem („17 = 13 na k20 + 4”), porównuje z KP z karty
i wpisuje celowi `-12`. Pole zmiany odejmuje najpierw PZ tymczasowe i zostawia przy wartości „30,
było 42”. „Następna tura” kończy turę goblina (jego Ogłuszenie schodzi z 1 na 0 i znika) i zaczyna
turę trolla (przypomnienie o regeneracji); okienko boczne wymienia obie zmiany, a odznaczenie
przywraca Ogłuszenie. „Sprzedaj” przy mieczu bierze cenę z wartości wpisu (sprzedaż za połowę), MG
ją zmienia, a zatwierdzenie przenosi miecz i złoto jedną akcją.

## Kierunek (jeszcze nie zbudowane)

Szczegóły dawnego projektu docelowego: `docs/archive/architecture.md`. Każde z poniższych powstaje
dopiero razem z pierwszym konsumentem.

- **Przełomy i wartości pochodne:** kod systemu. Wartość pochodna (modyfikator cechy, bierna
  Percepcja) liczy się przy odczycie z pól ze strukturą. Formuły w danych paczki są odłożone
  (roadmapa).
- **Katalog świata:** drzewo katalogów w stanie kampanii. Instancja (w rozmowie z autorem: entity)
  leży w katalogu albo w innym entity — tak wygląda ekwipunek. Katalog niesie tylko nazwę.
- **Postacie graczy:** obok kampanii, w katalogu systemu; zmieniane formularzem w aplikacji. Postać
  jest źródłem entity tak jak wpis, więc identyfikator `źródło:id` wskazuje wpis albo postać.
- **Sloty:** pole, którego wartością jest lista referencji (we wpisie) albo instancji (w kampanii).
- **Dokument:** długi tekst przygody albo lore z markerami pól interaktywnych i linkami do stron
  i identyfikatorów; mieszka w paczce, odhaczenia w kampanii; własny renderer, bez karty.
- **Dodatki i warianty:** przełączniki wariantów zasad na stronie kampanii. System dowiaduje się
  o nich tylko przy składaniu kampanii; żaden kontekst zakładki ani okna ich nie niesie.
- **Autorstwo treści w aplikacji:** najpierw „skopiuj i zmień” do własnej paczki MG, potem tworzenie
  od zera — formularz projektowany per typ, zapisujący ten sam plik wpisu.
- **Podział księgi między ramę i system:** pole zmiany liczby, tryb edycji karty, kalkulator kości,
  okienko przełomu i znaczniki zmian w turze są ramy i nie znają systemu. Reguły rachunku (PZ
  tymczasowe schodzą pierwsze) i treść przełomów (co tyka na końcu tury, co odnawia odpoczynek) są
  kodem systemu.
