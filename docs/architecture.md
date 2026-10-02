# Architektura

Jak aplikacja jest zbudowana dziś i dokąd zmierza. Przy rozbieżności z kodem prawdą jest kod —
popraw ten dokument w tym samym commicie.

## Czym to jest

Laptop MG przy stole: ile życia ma jeszcze goblin, co drużyna ma w plecaku, co żyje w świecie kampanii.
Gracze aplikacji nie widzą. To nie jest wirtualny stół, gra, generator treści ani silnik reguł.

Zakres: systemy „trad” — nazwane statystyki, rzut z modyfikatorem, jakaś kolejność działania (D&D,
Pathfinder, OSR, Call of Cthulhu). Aplikacja **wie, czym jest potwór i jak go pokazać**, bo karta jest
zaprojektowana; **nie wie, czym jest D&D**, bo wiedza o systemie mieszka w wymienialnym projekcie.
Wierność zasadom systemu nie jest celem — pole trafia do rekordu, gdy pomaga MG.

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
- **Typ treści** to para: rekord C# (np. `Monster`) i zaprojektowana karta (np. `MonsterCardView`).
  Deklaruje go system w swoim katalogu typów (`IContentTypeCatalog`) — to jedyne legalne miejsce
  rozgałęzienia po identyfikatorze typu. `values` deserializuje się wprost w rekord; deserializator
  (`required`, brak nieznanych kluczy) jest walidatorem. Wartości są płaskie: napisy, liczby, znaczniki.
- **Zepsuta treść jest widoczna, nie znika**: wadliwy wpis dostaje powód, a reszta paczki się
  wczytuje. Zepsuty manifest odrzuca całą paczkę z powodem.
- Obrazek wpisu: pole wskazane przez deskryptor typu (`ImageProperty`); ścieżka względem katalogu
  paczki, tylko PNG/JPG/WebP. Ścieżka poza paczkę odrzuca wpis; brak pliku pokazuje się na karcie.
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
- Zbyt nowy format kampanii oznacza odmowę odczytu; migracji nie ma, dopóki nie będzie potrzebna.

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
- **Błędy:** błąd złapany i niepokazany MG trafia do logu (`AppLog`, statyczny, bo globalne handlery
  i kroki startowe systemów nie mają wspólnego właściciela). Nieobsłużony wyjątek jest tylko
  zapisywany — program kończy działanie jak bez logu.
- **Nawigacja:** ekran wyboru systemu, potem pasek boczny z trzema kategoriami: **Kampania** (półka
  kampanii, a po otwarciu kampanii jej zakładki), **System** (zakładki treści), **Aplikacja** (Galeria,
  Ustawienia). Zakładki wypełnia skompilowany system, nigdy paczka. Kryterium okna czy zakładki: czy
  patrzysz na to obok innych rzeczy (okno na biurku), czy w tym przebywasz (zakładka).
- **Zakładka treści** (`Desktop/Entries/ContentTab`): układ lista–szczegół, wspólny dla typów treści.
  System podaje profil typu: kategorię, tagi, odznakę wiersza, filtry wartości i sortowania. Szczegół
  to nagłówek z biblioteki (ścieżka, nazwa, tagi) plus karta systemu o stałej szerokości.
- **Biurko** (`Desktop/Workspace`): zakładka kampanii z pływającymi oknami narzędzi wnoszonych przez
  system. Układ zapisuje się per kampania. Dziś jest jedno narzędzie: „Świat kampanii” (instancje, PZ).
- **Motyw** (`Desktop/Themes`) jest kompletny i własny, bez Fluenta pod spodem. Tokeny kolorów mają
  zapisane znaczenie; skale należące do systemu (rzadkość przedmiotu) mają własne kolory w systemie.
  Galeria pokazuje każdą kontrolkę motywu w każdym stanie.
- Konwencje interakcji: strzałka kursora wszędzie poza polem tekstu (ręki nie ma); stan zmienia się
  od razu, animacje najwyżej 150 ms i wyłączają się razem z animacjami systemu; zmiana stanu nie
  zmienia wymiarów kontrolki; tekst klikany ma co najmniej 12 pt; kontrast tekstu co najmniej 4,5:1;
  fokus klawiatury nie ma jeszcze widocznego wyglądu.

## Granica automatyzacji w praktyce

Aplikacja księguje decyzje MG — także jednym kliknięciem i na kilku rzeczach naraz — ale ich nie
podejmuje. Przykład: MG postanawia, że drużyna sprzedaje miecz, i wskazuje sakiewkę; aplikacja
przenosi miecz i dopisuje złoto. Aplikacja może **zaproponować** wartość z danych (np. „połowa
ceny”, bo to arytmetyka), ale MG ją poprawia przed zapisem. Brak złota pokazuje, nie blokuje.
Wyliczenie jest propozycją, akcja jest zapisem.

## Kierunek (jeszcze nie zbudowane)

Szczegóły dawnego projektu docelowego: `docs/archive/architecture.md`. Każde z poniższych powstaje
dopiero razem z pierwszym konsumentem.

- **Formuły:** jeden deklaratywny silnik za polami obliczanymi — bez pętli, gałęzi i efektów
  ubocznych. Wartość pochodna (bez kości, przeliczana przy odczycie) i rzut (na żądanie, wynik jest
  zdarzeniem dla MG). Zasięg: własna instancja plus jeden skok do slotów, bez przechodniości.
- **Sloty:** pole, którego wartością jest lista referencji (we wpisie) albo instancji (w kampanii).
- **Dokument:** długi tekst przygody z markerami pól interaktywnych; własny renderer, bez karty.
- **Dodatki i warianty:** przełączniki wariantów zasad na stronie kampanii. System dowiaduje się
  o nich tylko przy składaniu kampanii; żaden kontekst zakładki ani okna ich nie niesie.
- **Autorstwo treści w aplikacji:** najpierw „skopiuj i zmień” do własnej paczki MG, potem tworzenie
  od zera — formularz projektowany per typ, zapisujący ten sam plik wpisu.
