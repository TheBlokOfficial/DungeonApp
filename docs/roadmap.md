# Co dalej

Lista zadań w kolejności pracy. Pozycja opisuje potrzebę, nie zlecenie — przed startem sprawdź
w kodzie, czy nadal jest aktualna.

**Oznaczenia:** `[x]` zrobione · `[~]` w toku · `[ ]` do zrobienia · ❓ decyzja przed startem pozycji ·
⏸ odłożone, z wyzwalaczem.

**Prowadzenie listy:**
- Numer: etap `E2`, wycinek `2.3b`. Specyfikacje i rozmowa odwołują się do tych numerów.
- Pozycja to jedna myśl z odsyłaczem. Szczegóły mieszkają w specyfikacji (`docs/spec/`), opis tego, co
  zbudowane — w `docs/architecture.md`, powód — w `docs/decisions.md`.
- Zrobiona pozycja zostaje jako `[x]` z odsyłaczem do architektury. Po zakończeniu kamienia milowego
  jego zrobione pozycje zwijają się do jednej linii.
- Notatki do etapu nierozpoczętego to zapisane pomysły, nie uzgodniona koncepcja; specyfikacja
  powstaje przy starcie etapu.
- Listę aktualizuje się raz na wycinek, przy scaleniu.

## Kamień milowy: pierwsza sesja przy stole

Cel: MG prowadzi prawdziwą sesję bez podręcznika i bez własnych notatek na papierze (gracze grają
z kartek, aplikacja jest źródłem prawdy). Etapy w kolejności pracy; każdy jest użyteczny sam. W środku
punkt kontrolny „Pierwsza walka”. Rozstrzygnięcia, na których stoi plan: `docs/decisions.md`.

### [x] E1 · Treść

- [x] Biblioteka wpisów: paczki, wpisy, typy treści, zakładki treści, karty stworzenia, przedmiotu
  i stanu → architecture.md, „Treść: paczki, wpisy, typy treści”, „Zakładka treści i karty”. Projekt
  kart był punktem wyjścia, nie specyfikacją: `docs/archive/zadania/zakladki-tresci.md`, sekcja *C*.
- [~] Paczka `dnd5e-srd` (SRD 5.1 po polsku) rośnie na żądanie: zestaw startowy stworzeń na pierwsze
  sesje, reszta, gdy przygoda jej potrzebuje → decisions.md, „Paczka SRD rośnie na żądanie”,
  „Słownictwo z polskiego podręcznika”. Słownictwo, znaczniki `[[id|tekst]]`, metry, rzadkość i spis
  stworzeń: `tools/srd/`.
- ❓ Szkielet zakładki: każda zakładka ma ten sam układ (filtry, lista, szczegół) — za generyczny, do
  przemyślenia.

### [~] E2 · Katalog świata

Specyfikacja: `docs/spec/katalog.md`.

- [x] 2.1 Biurko w miejscu strony kampanii → architecture.md, „Biurko”, „Listek”, „Maksymalizacja okna”.
- [x] 2.2 Katalog na biurku i paleta: drzewo katalogów z entity, paleta dodawania, podgląd z pinezką →
  architecture.md, „Paleta”, „Katalog świata”, „Podgląd i przypięte karty”.
- [ ] 2.3 Karta entity zmienia się w miejscu: część żywa nad kreską, część stała jak w bibliotece →
  spec, od „Podgląd i karta” do „Projekt kontrolek”; decisions.md, „Karta entity zmienia się
  w miejscu”.
  - [x] 2.3a Arkusz rodziny kontrolek w Galerii, sekcja „Część żywa”: pasek PW, bieżące PW z polem
    zmiany, notatka, wiersz stanu i efektu, kleks, kolory względem bazy → spec, „Projekt kontrolek”;
    decisions.md, „Wartość do zmiany wygląda jak tekst”.
  - [ ] 2.3b Część żywa: ścieżka kategorii zamiast katalogów, bez linii „№” pod nazwą, tytuł okna
    z nazwą i №; bloki w kolejności PW, KP, Szybkość, nad nimi pasek PW przez całą kolumnę, bieżące
    PW „9 / 13” z polem zmiany po kliknięciu; model baza + modyfikatory; notatka → spec, „Podgląd i karta”, „Część żywa”,
    „Wartości z bazą”, „Edycja w części żywej”.
  - [ ] 2.3c Zatwierdzanie: punkt odniesienia, kleks, Zatwierdź i Odrzuć na wysokości ścieżki;
    poprawki w drzewie (zmiana nazwy, próg przeciągnięcia, przewinięcie po przeniesieniu) → spec,
    „Zatwierdzanie zmian”, „Poprawki w drzewie”.
  - [ ] 2.3d Stany: sekcja, dodawanie z palety, usuwanie, stany domyślne wpisu → spec, „Stany”.
  - [ ] 2.3e Efekty: sekcja, dodawanie, usuwanie, sumowanie, kolory względem bazy → spec, „Efekty”.
  - ❓ Przed 2.3e: efekt na PW, rodzaje modyfikatorów, dodawanie efektu → spec, „Otwarte pytania” 1–3.

### [ ] E3 · Kopie zapasowe kampanii

- [ ] Kopie rotujące — przed pierwszym prawdziwym użyciem, bo przy stole zapis biegnie na żywo po
  każdej zmianie.

### [ ] E4 · Postacie w minimum

- [ ] Postacie leżą obok kampanii, w katalogu systemu na dysku, i mogą grać w wielu kampaniach →
  decisions.md, „Postać gracza leży obok kampanii”.
- [ ] Nazwa i aspekt walki (KP, PW, cechy, inicjatywa) z formularza; w E6 postać rośnie o kolejne
  aspekty.
- [ ] Wejście postaci do kampanii tworzy jej entity. Identyfikator `paczka:id` staje się `źródło:id`:
  wpis w paczce albo postać.

### [ ] E5 · Inicjatywa i walka

- [ ] Okno inicjatywy: MG wskazuje uczestników i układa kolejkę; rundę i turę przesuwa MG.
- [ ] Zwarty widok istoty to część żywa karty z 2.3, bez części stałej.
- [ ] Przełom „Następna tura” z okienkiem przełomu i znacznikami zmian w turze — ten sam mechanizm
  co zatwierdzanie z 2.3c → decisions.md, „Znaczniki zmian w turze”.
- [ ] PW tymczasowe, liczniki i tyknięcia stanów → decisions.md, „Stany i efekty — dwie listy”,
  „Liczniki stanów tykają na końcu tury”.
- [ ] „Do walki” z menu albo przeciągnięciem; okno zmaksymalizowane zakrywa katalog, więc
  przeciąganie do kolejki trzeba rozwiązać w tym etapie.
- [ ] Kalkulator kości jako osobne okno biurka → decisions.md, „Kalkulator kości zamiast rzutu
  z karty”.
- ❓ Wyjątek od reguły 3 granicy (rzut istoty zostaje u MG): czy narzędzie rzuca stworzeniom
  inicjatywę (k20 + mod. ZRĘ z aspektu walki, z rozpisaniem, do poprawienia).
- ❓ „Następna tura” zatwierdza tylko istotę kończącą turę czy wszystkie → spec katalogu, „Otwarte
  pytania” 4.

**Punkt kontrolny „Pierwsza walka”:** jedna prawdziwa walka poprowadzona w całości z aplikacji.
Uwagi autora poprawiają księgę, przełom i karty, zanim powstanie na nich pełna postać.

### [ ] E6 · Pełna postać

- [ ] Pełna karta (aplikacja to jedyne źródło prawdy), wartości liczone z rozpisaniem → decisions.md,
  „Postać ma pełną kartę jak potwór”.
- [ ] Formularz do szybkiego przepisania istniejącej kartki gracza.
- [ ] Do paczki dochodzą pochodzenie i tło.

### [ ] E7 · Klasy i drzewka

- [ ] Klasa i każdy węzeł to wpisy w paczce, drzewko to widok → decisions.md, „Klasa i węzeł drzewka
  są wpisami w paczce”.
- [ ] MG zaznacza węzeł i odblokowuje go postaci; niespełnione wymaganie widać, ale nie blokuje.
- [ ] SRD: cechy klas i podklas do 5. poziomu.

### [ ] E8 · Zaklęcia

- [ ] Zaklęcia bez obrazka; filtry i sortowanie po poziomie i szkole, koncentracja i rytuał jako tagi.
- [ ] SRD: zaklęcia 0–3.
- ❓ Biblioteka rośnie w długi rząd płaskich zakładek (stany, zaklęcia, manewry, atuty). Kierunek:
  zakładki zostają, część grupuje się w rozwijaną listę (Stany nie na pierwszym poziomie). Zmienia
  „Zakładki według typu” w decisions.md.

### [ ] E9 · Ekwipunek, sakiewka, handel

- [ ] Złoto na entity. Cena i ilość należą do slotu (sklep, plecak), nie do wpisu → decisions.md,
  „Wartość przedmiotu jest we wpisie, cena na slocie”.
- [ ] Waluta kampanii to lista nominałów z mnożnikami (uniwersalna moneta albo własne monety); funty
  to samo dla wagi → decisions.md, „Jednostki to listy mnożników od jednej bazy”.
- [ ] Okno handlu.
- [ ] SRD: ekwipunek podstawowy, przedmioty magiczne pospolite do rzadkich.
- ❓ Skala wartości przed wyceną SRD: małe, czytelne liczby z miejscem w dół i w górę.
- ❓ Ekwipunek: entity w entity w drzewie świata czy slot — lista pozycji wewnątrz istoty.
- ❓ Stworzenie na sprzedaż (koń u handlarza): skąd wartość, z której liczy się cena.
- Notatki do ustalenia: okno handlu jak w cRPG — towar kupca z cenami (z wartości wpisu przez kurs
  waluty kampanii, sprzedaż za połowę), MG może zmienić cenę, „Kup” i „Sprzedaj” przenoszą przedmiot
  i złoto; brak złota widać, nie blokuje.

### [ ] E10 · Odpoczynek

- [ ] Przełom krótkiego i długiego odpoczynku z okienkiem: odnowienie PW, miejsc na zaklęcia
  i ładunków.
- ❓ Odnowienie „o świcie”. Propozycja: przełącznik w okienku długiego odpoczynku, bo zegar niczego
  nie uruchamia.

### [ ] E11 · Czas świata i podróże

- [ ] Okno zegara przesuwanego przez MG (szybkie przyciski i dowolna wartość).
- [ ] Podróż jako jedna akcja: drużyna do innego katalogu, zegar o czas wpisany przez MG.

## Po kamieniu milowym

- [ ] **Fabuła i lore** — zakładka dokumentów. Markdown z paczki, pisany poza aplikacją (np.
  w Obsidianie). Linki między stronami i do identyfikatorów; klik w identyfikator pokazuje kartę.
  Odhaczenia decyzji w fabule zapisują się w kampanii, więc jedna przygoda może iść dla kilku grup.
- [ ] **Ściągawki** — wybór wpisów (cechy klas, zaklęcia, przedmioty, stany) i wydruk A4 przez PDF.
  Każdy typ ma własny układ na białą kartkę. Zaznaczenia może podpowiedzieć postać (jej węzły,
  zaklęcia, ekwipunek); MG poprawia je przed drukiem.
- [ ] **Galeria wpisów** — zakładka treści na całą szerokość. Notatki do ustalenia:
  - filtry i sortowanie paskiem u góry; po lewej wąski spis treści (wyszukiwarka, grupy paczek,
    odrzucone wpisy); obok karty w 1–3 kolumnach stałej szerokości zależnie od okna, ułożone
    wierszami (karta do najkrótszej kolumny);
  - długa karta przycięta: dół gaśnie gradientem przezroczystości, przycisk „Rozwiń”;
  - klik w spisie przewija do karty i oznacza ją ramką akcentu (krótkie rozbłyśnięcie, potem stała
    ramka); klik w kartę zaznacza wiersz;
  - wymaga wirtualizacji kart o różnej wysokości (200+ stworzeń).
- [ ] **Formuły w danych paczki** — pola liczone, które dopisuje twórca paczki.

## Porządki w kodzie i dokumentach

- [ ] **Komentarze nieaktualne w treści:** `CampaignRowViewModel` (powód niedostępności, którego wiersz
  nie pokazuje), `AppShellView.axaml` (host rozgrzewki „także po wyborze systemu”), `PanelCatalog`
  (kolejność „panele powłoki, potem narzędzia”).
- [ ] **Strażnik granicy:** punkt 4 granicy (zdarzenia nie zapisują) i zakaz logiki per wpis nie mają
  strażnika w kodzie; pilnuje ich przegląd.
- [ ] **Powody odrzucenia wpisu paczki** są surowym angielskim tekstem parsera („The JSON value could
  not be converted to …”) — potrzebne polskie zdanie, które nazywa pole.

## Szlif — po kamieniu milowym

Wygląd, który działa, ale mógłby być lepszy. Nie blokuje kamienia milowego.

- [ ] Pełna nazwa przedmiotu po najechaniu znika przy zjechaniu na dopisaną część i po kliknięciu
  w tytuł — okienko musiałoby przyjmować wskaźnik i samo być tekstem do zaznaczenia.
- [ ] Tagi w nagłówku karty przy wielu wartościach zawijają się w drugą linię i wypychają nagłówek
  ponad obrazek (dlatego Miecz worpalny nie ma podtypu „żołnierska, do walki wręcz”).
- [ ] Tabela cech: modyfikator zero „+0” jak w podręczniku — do zmiany na „0”, gdyby zaczęło razić.
- [ ] Opis przedmiotu bez kreski i nagłówka „Opis”, z inicjałem pergaminu jak zasady na karcie stanu.
- [ ] Akapity w prozie: po twardym Enter nowa linia trochę niżej, jak w edytorze tekstu (także obok
  inicjału pergaminu).
- [ ] Suwak: uchwyt w spoczynku trochę za ciemny. Pomysł: pod myszą obwódka zamiast rozjaśnienia.
- [ ] Menu: skrót nie stoi w jednej linii ze strzałką podmenu, podmenu nachodzi na menu.
- [ ] Ikony kategorii na liście treści (przedmioty: broń, zbroja…; stworzenia: typ), małe i przygaszone
  jak ikony stanów; ta sama zamiast zastępczej w pustym polu obrazka, spoza listy — domyślna.
- [ ] Przełączanie zakładek bez efektu przenikania (fade) — zmiana ma być natychmiastowa. Usunąć też
  gradient po prawej stronie biblioteki kampanii.
- [ ] Widoczny wygląd fokusu klawiatury (kontrolki części żywej dostają go w 2.3a).
- [ ] Klik w tytuł okna podglądu rozwija katalogi do entity i zaznacza ją w drzewie.

## Po stronie autora

- [ ] Kampanie przenieść do `Dokumenty\DungeonApp\dnd5e\campaigns\` (katalogów `Packs` i `Campaigns`
  aplikacja nie czyta) i dopisać stworzeniom pole `group`.
- [ ] Wpisy we własnych paczkach bez obiektów `combat` i `item` pokazują się jako odrzucone —
  przepisać albo usunąć. Stworzenie: `"template": "dnd5e:creature"`, KP, PW i cechy w obiekcie
  `combat`, proza w sekcjach. Przedmiot: waga i wartość w obiekcie `item`, ładunki w `charges` (`max`,
  `recharge`). Wzór: wpisy paczki `dnd5e-srd`.

## Odłożone, z wyzwalaczem

Każda pozycja: co, potem — po myślniku — wyzwalacz, który ją uruchamia.

- ⏸ **„Zapisz jako nowy wpis”** z karty entity do własnej paczki MG; entity wskazuje potem nowy wpis —
  MG chce użyć entity z efektami i notatką w innej przygodzie.
- ⏸ **„Zmień wpis źródłowy”** entity (goblin okazuje się hobgoblinem; nazwa, notatka, bieżące PW
  i stany zostają) — pierwsza taka podmiana obchodzona przy stole usunięciem i dodaniem.
- ⏸ **Skrót z drzewa do edycji PW** — `-`, `+` albo `=` przy zaznaczonej entity otwiera pole zmiany
  bieżących PW w podglądzie — sięganie po mysz między drzewem a podglądem zaczyna spowalniać walkę.
- ⏸ **Cofnij / Ponów na listku** — pomyłka przy stole (zły cel, „-70” zamiast „-7”) zaczyna boleć
  mimo „Odrzuć” na karcie → spec katalogu, „Poza etapem 2”.
- ⏸ **Hotbar** — zegar świata (E11), pierwsze okno z widżetem → spec katalogu, „Poza etapem 2”.
- ⏸ **Edycja kilku zaznaczonych entity naraz** — MG zmienia to samo pole po kolei na kilku.
- ⏸ **Typ „Zdolność”** z kategorią (atut, manewr, inwokacja, metamagia, styl walki) — pierwsza
  zdolność spoza drzewek klas; do tego czasu wybory w klasie są węzłami.
- ⏸ **Odnośniki do wiedzy** (klik w zaklęcie albo stan w prozie otwiera kartę; na biurku jako
  pływające okno), słownik „Hasło” i zakładka „Zasady” — MG zbyt często szuka opisu wspomnianego
  w prozie.
- ⏸ **Edytor treści w aplikacji** (wpisy, przygoda, lore) — pisanie plików poza aplikacją zaczyna
  przeszkadzać w przygotowaniu sesji.
- ⏸ **Odległości między lokacjami** (propozycja czasu podróży) — wpisywanie czasu podróży zaczyna
  męczyć.
- ⏸ **Warianty zasad** — pierwszy wariant, który autor chce mieć w konkretnej kampanii.
- ⏸ **Dodanie paczki przeciągnięciem do okna** (kolizja nazwy daje odmowę z komunikatem, nigdy
  nadpisanie) — kopiowanie katalogów paczek w Eksploratorze zaczyna przeszkadzać.
- ⏸ **Wczytanie paczek od nowa** — przycisk w zakładce treści i F5; wybór zostaje, jeśli wpis o tym id
  nadal istnieje — restart po każdej poprawce paczki zaczyna przeszkadzać przy przygotowaniu sesji.
- ⏸ **Pomiar startu** — pierwsze zauważalne zacięcie przy starcie; wtedy najpierw liczby, potem
  poprawka.
