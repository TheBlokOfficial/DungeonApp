# DungeonApp — kierunki odrzucone

**Status: rejestr rozstrzygnięć, nie projekt.** Ten dokument nie mówi, jak aplikacja ma
działać — mówi, czego już próbowaliśmy i dlaczego tego nie robimy. Istnieje po to, żeby
odrzucony pomysł nie wracał co kilka miesięcy jako nowy.

Projekt docelowy: [architecture.md](architecture.md).

**Uwaga o słowniku.** Pozycje 1–22 zapisano w słowniku obowiązującym, gdy zapadały.
Dwa pojęcia zmieniły od tego czasu nazwę i postać: **„szablon"** to dziś **typ treści**
(rekord plus zaprojektowany widok, nie plik danych), a **„element karty"** to dziś
**kontrolka** komponowana przez projektanta, nie pozycja katalogu wybierana przez dane.
Argumenty pozostają w mocy w brzmieniu, w jakim je zapisano — zmiana nazwy nie unieważnia
żadnego z nich, a rozstrzygnięcia dotyczące samej tej zmiany są w grupie ostatniej.

## Treść i szablony

### 1. Generyczne prymitywy UI dla danych

**Co proponowano.** Żeby silnik udostępniał paczkom generyczne prymitywy interfejsu —
przycisk, pasek, etykietę — z których dane same składałyby wygląd karty.

**Dlaczego odrzucone.** Udostępnianie takich klocków, choćby najprostszych, przenosi
projektowanie interfejsu do warstwy danych i w praktyce oznacza napisanie silnika UI
cudzymi rękami. Zamiast tego dane wybierają i wypełniają gotowe kształty; nigdy nie
decydują o kształcie interfejsu.

**Czym to zastąpiono.** Skończone, zamknięte katalogi gotowych kształtów: katalog
elementów karty i katalog kontraktów prezentacji. Różnice między systemami wyraża się
parametrem istniejącego kształtu, nie nowym kształtem; nowy rodzaj elementu lub
kontraktu wymaga nowego wydania aplikacji. Jedynym legalnym stopniem swobody dla danych
jest zmienna liczba **jednorodnych** elementów (np. dowolnie długa lista cech) —
zmienny skład różnych typów obok siebie nadal nie jest dozwolony.

### 2. Jedna uniwersalna forma pośrednia

**Co proponowano.** Zamiast zamkniętych kontraktów prezentacji — żeby wpis opisywał
siebie jednym ustrukturyzowanym rekordem, a każdy konsument brał z niego to, co mu
pasuje.

**Dlaczego odrzucone.** Przenosi projektowanie interfejsu do danych tylnymi drzwiami —
o zawartości takiego rekordu i jego kolejności decydowałby autor szablonu. Rozstrzyga to
jedno pytanie: *czy autor szablonu może dodać do tego pole? Nie → kontrakt. Tak → forma
pośrednia, czyli przebrany silnik UI.* Trzy konkretne rzeczy zamieniłyby kontrakt w formę
pośrednią i dlatego są zakazane:
- pole o zawartości ustalanej przez szablon (np. „lista dodatkowych wartości”);
- jakikolwiek parametr wyglądu podany przez dane zamiast wybrany z zamkniętego słownika
  katalogu;
- konsument czytający pole, którego kontrakt nie deklaruje.

**Czym to zastąpiono.** Kontrakty prezentacji — nazwane, zamknięte zestawy pól, które
konsument (element karty albo narzędzie biurka) deklaruje jako to, co umie pokazać.
Szablon deklaruje wyłącznie, czym te pola wypełnia — wartością, ścieżką w danych albo
formułą.

### 3. Dziedziczenie szablonów

**Co proponowano.** Żeby szablon mógł dziedziczyć z innego szablonu, tworząc hierarchię
w rodzaju: diamentowy miecz → broń → narzędzie → przedmiot.

**Dlaczego odrzucone.**
- Skala jest inna niż w systemach, skąd ten wzorzec pochodzi: tam dziedziczy się, bo są
  tysiące definicji przedmiotów; tu tysiące rzeczy to wpisy, które już dzielą jeden
  szablon — pięćset broni wskazuje jeden szablon „broń” i nic się nie powtarza.
  Dziedziczenie deduplikowałoby wyłącznie warstwę szablonów, których w paczce systemowej
  jest niewiele.
- Szablon nie ma zachowania, więc nie ma czego dziedziczyć — zachowanie mieszka w
  zamkniętym katalogu warstwy silnika. Szablon to lista pól, slotów i wypełnień
  kontraktów; dziedziczenie samych deklaracji bez zachowania jest skracaniem tekstu, a do
  tego nie potrzeba hierarchii typów.

Różnica wobec systemów, skąd wzorzec pochodzi: tam zachowanie wykonuje silnik (program
musi rozstrzygnąć każdy przypadek, więc potrzebuje polimorfizmu), a przy stole wykonuje
je człowiek czytający podręcznik. Dlatego w tabeli broni miecz i topór różnią się
wyłącznie wartościami, a systemy stołowe od dekad wyrażają różnice tagami, nie
podtypami — bo tagi się składają, a podtypy nie.

Koszty, których unika się przez odrzucenie: reguły kolejności rozstrzygania, semantyka
nadpisania kontra rozszerzenia, pytanie czy potomek może **usunąć** element rodzica
(gdyby tak, przestałoby być prawdą, że układ karty da się przeczytać z jednego
miejsca), problem diamentu, raportowanie błędów walidacji rozwiniętego szablonu w pliku
źródłowym, a przede wszystkim wersjonowanie: dziś wersja szablonu wiąże wpis i tą ścieżką
migruje, a przy dziedziczeniu podbicie szablonu bazowego po cichu zmieniałoby efektywny
kształt każdego potomka. Dziedziczenie i tak byłoby ograniczone do jednej paczki, bo
krawędź szablon→szablon między paczkami jest zakazana.

**Czym to zastąpiono.** Gdyby powtórzenia stały się realnym problemem — kompozycja, nie
dziedziczenie: fragment jako nazwany kawałek deklaracji, a szablon jako lista fragmentów
plus własne pola. Wyłącznie addytywnie, wyłącznie w obrębie paczki, bez nadpisywania —
zadeklarowanie tego samego pola dwa razy byłoby błędem wczytania, nie cichym scaleniem.

**Wyzwalacz powrotu.** Pierwsza paczka przekraczająca kilkanaście szablonów, albo taka,
w której to samo wypełnienie kontraktu powtarza się w większości z nich. Wcześniej to
spekulacja.

### 4. Osadzanie szablonu w szablonie

**Co proponowano.** Żeby jeden szablon dało się osadzić w drugim — np. szablon potwora
deklarowałby, że jego umiejętności mają kształt dany przez szablon umiejętności, i
wypisywał je jako listę.

**Dlaczego odrzucone.** Problem, który miał to rozwiązać, jest prawdziwy: akcje i cechy
szczególne potwora nie dają się sensownie zapisać jednym długim napisem z „\n\n” udającym
akapit. Rozwiązuje go jednak slot, bez jednego nowego mechanizmu i bez krawędzi
szablon→szablon: szablon potwora deklaruje slot „akcje”; wpis wypełnia go referencjami do
wpisów takich jak „bułat” czy „krótki łuk”; każdy z nich wskazuje własny szablon z własną
kartą; element listy pozycji renderuje je przez kontrakt prezentacji. Kluczowe jest to, że
**szablon potwora nie musi znać szablonu akcji** — który szablon te wpisy wskazują,
rozstrzyga się dopiero na poziomie wpisów.

**Czym to zastąpiono.** Mechanizm slotów: szablon deklaruje slot jako pole, którego
wartością jest lista referencji do wpisów; te wpisy mają własne szablony i własne karty i
są renderowane przez kontrakt prezentacji, jak każda inna dołączona pozycja (np.
ekwipunek).

### 5. Poziomy szablonów jako ratunek przed cyklem

**Co proponowano.** Żeby osadzanie szablonu w szablonie ratować przed cyklem przez
wprowadzenie poziomów: szablon poziomu 2 wolno osadzić w szablonie poziomu 1, ale nie
odwrotnie.

**Dlaczego odrzucone.** Poziomy łamią cykl, ale nie naprawiają czterech rzeczy:
- **Wersjonowanie łamie się tak samo jak przy dziedziczeniu.** Dziś wpis wiąże się z
  wersją szablonu i tą ścieżką migruje; przy osadzaniu podbicie szablonu osadzonego po
  cichu zmieniałoby efektywny kształt każdego wpisu związanego z szablonem-gospodarzem.
  Poziomy ograniczają głębokość, nie to.
- **Wartości przestają być płaskie.** Osadzony szablon powtarzalny wymaga, żeby wartości
  wpisu niosły listę obiektów, co rozbija zamkniętą, płaską hierarchię wartości i
  wprowadza zagnieżdżone drzewo (patrz kierunek „Zagnieżdżanie wartości w polu wpisu”
  niżej).
- **Poziom to nowa taksonomia, którą silnik musiałby znać.** Deklaracja „poziom: 2” jest
  daną mówiącą silnikowi o kategorii szablonu — system rodzajów pod inną nazwą, na osi,
  której nikt nie potrzebuje.
- **Znika wielokrotny użytek.** Przykładowa „Zwinna ucieczka” osadzona wewnątrz wpisu
  goblina nie byłaby wyszukiwalna w rejestrze, nie miałaby własnej karty i nie dałoby się
  jej dać hobgoblinowi. Jako osobny wpis ma wszystkie te trzy cechy, a atak bułatem
  powtarza się w połowie bestiariusza.

**Czym to zastąpiono.** To samo rozwiązanie slotowe, co przy osadzaniu (kierunek 4
powyżej). Czego jednak slot nie załatwia: opis (`description`) nadal jest jednym długim
tekstem w polu formularza — ten problem, długiego tekstu a nie liczby pól, zostaje
otwarty.

### 6. Rozdział na paczkę systemową i paczkę treści

**Co proponowano.** Podział paczek na dwa rodzaje — paczkę systemową (niosącą szablony) i
paczkę treści (niosącą wpisy). Ten podział istniał wcześniej w projekcie.

**Dlaczego odrzucone.** Mieszał dwie niezależne rzeczy: **jednostkę dystrybucji** (co
instaluje się i wersjonuje razem) z **warstwą zależności** (czy pozycja jest kształtem,
czy wartością). Autor własnego systemu wraz z bestiariuszem wydawał dwie paczki
wersjonowane osobno — a to jedna rzecz, i osobne wersjonowanie było o niej kłamstwem.
Rodzaj paczki był przy okazji jedyną rzeczą w tej warstwie, po której cokolwiek się
rozgałęziało.

**Czym to zastąpiono.** Jeden rodzaj paczki, bez deklarowanego rodzaju — obecność
katalogu szablonów lub katalogu wpisów na dysku **jest** samą deklaracją tego, co paczka
niesie; jedna paczka może nieść oba katalogi naraz. Jedyną dozwoloną krawędzią zależności
pozostaje wpis→szablon, nigdy paczka→paczka.

Koszt przyjęty świadomie: „jedna kampania to jedna gra” przestaje być własnością
wymuszalną przez policzenie paczek systemowych — wynika teraz z tego, co Mistrz Gry
faktycznie zainstalował, a nie z kształtu manifestu. Znika też kontrola pomyłki
autorskiej („paczka systemowa z wpisami”), przyjęta jako tania do oddania. Osobno, koszt
społeczny, nie techniczny: scalone paczki gorzej się podmienia — cudzy bestiariusz do
tego samego systemu jest nadal możliwy, ale przestaje być jedyną formą, w jakiej treść
się wydaje.

### 7. Zagnieżdżanie wartości w polu wpisu — odrzucone dwukrotnie

**Co proponowano.** Żeby wartości wpisu dało się grupować hierarchicznie — zagnieżdżone
drzewo zamiast płaskiej mapy pól — tak żeby powiązane wartości stały razem.

**Dlaczego odrzucone — pierwsze uzasadnienie** (przy okazji odrzucenia osadzania
szablonu w szablonie): osadzony, powtarzalny szablon wymagałby, żeby wartości wpisu
niosły listę obiektów, co rozbija zamkniętą, płaską hierarchię wartości i wprowadza
zagnieżdżone drzewo.

**Dlaczego odrzucone — drugie, mocniejsze uzasadnienie** (pomysł wrócił ponownie,
niezależnie od tematu osadzania szablonów): grupa, której szuka autor wpisu, **już
istnieje — w układzie karty, nie w wartościach** (sześć cech stoi razem w jednym
elemencie listy cech, KP i PZ w drugim). Deklaracja i przestrzeń adresowa wartości mają
zostać płaskie, bo:
- formuła czyta pole po nazwie i musi być walidowalna wobec ścieżek deklarowanych przez
  szablon, a zagnieżdżenie zamienia każdą referencję w ścieżkę z regułami rozstrzygania
  zakresu;
- identyfikator pola dopuszcza dziś wyłącznie litery i cyfry, więc drzewo wymagałoby
  wpuszczenia separatora do celowo wąskiego zbioru znaków;
- kontrakty prezentacji wypełnia się **nazwanym** polem, więc kontrakt zacząłby wiedzieć
  o strukturze wpisu;
- nakładka instancji jest rzadka i kluczowana polem, a rzadkość w drzewie przestaje być
  darmowa;
- grupowanie istniałoby w wartościach **obok** grupowania w karcie i mogłoby się z nim
  nie zgadzać, co wymagałoby nowej reguły rozstrzygania za zero korzyści.

**Czym to zastąpiono.** Płaska mapa wartości pozostaje jedyną formą. Grupowanie wizualne
żyje wyłącznie w układzie karty (w elementach karty), nigdy w danych.

### 8. Kosmetyczne grupowanie w pliku wpisu, spłaszczane przy wczytaniu

**Co proponowano.** Obejście pośrednie wobec zagnieżdżania: pozwolić autorowi grupować
wartości kosmetycznie w samym pliku wpisu na dysku, ale spłaszczać tę strukturę przy
wczytywaniu, tak żeby silnik i tak widział płaską mapę.

**Dlaczego odrzucone.** To dwa sposoby zapisania tego samego — kłamstwo loadera i koniec
porównywalności diffów. Dokument źródłowy nie podaje dodatkowego uzasadnienia poza tym.

## Nawigacja i interfejs

### 9. Kontekstowy sidebar

**Co proponowano.** Globalna szyna boczna nawigacji, której **zawartość** zmienia się po
wejściu w kampanię — wczesny szkic wypełniał ją nazwami w rodzaju „Bestiariusz”,
„Przedmioty”, „Lokacje”.

**Dlaczego odrzucone.** Trzy powody, każdy wystarczający osobno:
- **Pozycje pochodziłyby z danych** — czyli z rodzajów wpisów, których silnik z założenia
  nie zna. Praktycznie: literówka w nazwie pola odrzuca paczkę, a wraz z nią znikają
  pozycje nawigacji — nawigacja przestaje działać, bo autor pomylił klucz w pliku
  tekstowym. Do tego kampania deklaruje własną listę paczek, więc zbiór szablonów różni
  się między kampaniami — globalna szyna o zawartości zależnej od tego, co masz otwarte,
  nie jest nawigacją, tylko widokiem, który ją udaje.
- **Szablon nie jest kategorią.** Szablon deklaruje kształt, nie rodzaj rzeczy — jeden
  szablon o kształcie statbloku może obsługiwać potwory, NPC-e i zwierzęta naraz, a trzy
  szablony mogą opisywać to, co DM uważa za jedno. Grupowanie po szablonie byłoby
  taksonomią silnika, nie człowieka.
- **Nie miałaby własnej treści.** Wszystko, co należy do jednej kampanii, jest z
  definicji oknem biurka, bo biurko jest warsztatem kampanii, a okna są jego jednostkami.
  Taka szyna mogłaby więc być wyłącznie drugim sposobem otwierania okien.

**Czym to zastąpiono.** Globalna szyna ma trzy stałe, skompilowane pozycje (Kampanie,
Rejestr, Ustawienia), z miejscem na co najwyżej dwie kolejne w przyszłości (zarządzanie
paczkami, autorstwo treści), gdy na to zasłużą. Przełącznik między powierzchniami
kampanii (Biurko, Świat, Fabuła, Kronika) należy do kampanii, nie do globalnej szyny.

### 10. Nawigacja o zawartości pochodzącej z danych

**Co proponowano.** Zasada ogólna, szersza niż sam kontekstowy sidebar: żeby zawartość
elementów nawigacji mogła pochodzić z paczek treści.

**Dlaczego odrzucone.** Odrzucona jest szyna nawigacji o zawartości z danych, a nie
grupowanie treści wewnątrz skompilowanego ekranu — granica jest wąska i konkretna.
Różnica jest sprawdzalna po skutku awarii: literówka w paczce ma psuć najwyżej etykietę
zakładki wewnątrz ekranu, a nie samą nawigację. Nie da się nią zgubić drogi powrotnej, bo
szyna stoi obok i nie zależy od treści.

**Czym to zastąpiono.** Grupowanie z danych jest legalne **wewnątrz** skompilowanego
ekranu — np. rejestr wolno pogrupować i ofiltrować po polu kategorii z kontraktu
prezentacji (zakładki „Potwory / Przedmioty / Zaklęcia / NPC”, nagłówki grup, licznik
przy każdej) — o ile układ samego ekranu nie pochodzi z paczki, a z danych pochodzi
wyłącznie zawartość jednego wymiaru.

### 11. Wywodzenie kategorii z szablonu

**Co proponowano.** Grupowanie lub kategoryzowanie wpisów (w nawigacji albo na liście) na
podstawie tego, jaki szablon dany wpis wykorzystuje.

**Dlaczego odrzucone.** Szablon deklaruje kształt, nie rodzaj rzeczy — jeden szablon
statbloku obsługuje potwory, NPC-e i zwierzęta naraz. Grupowanie po szablonie byłoby
taksonomią silnika, nie człowieka.

**Czym to zastąpiono.** Kategoria jest polem kontraktu prezentacji (`summary`),
wypełnianym **ścieżką do pola wpisu**, a nie stałą szablonu — wszędzie tam, gdzie wpisy
dzielące kształt mają się różnić grupą.

### 12. Rejestr jako miejsce wewnątrz kampanii

**Co proponowano.** Traktowanie rejestru — przeglądu zainstalowanej treści — jako miejsca
albo powierzchni wewnątrz otwartej kampanii, zamiast osobnej, globalnej sekcji aplikacji.

**Dlaczego odrzucone.** Wpisy to referencje, a kampania zawiera instancje — to różne
rzeczy. Wybór wpisu z rejestru jest w kampanii potrzebny (np. przy wprowadzaniu wpisu do
świata), ale jest **momentem, nie miejscem**: przywoływanym z narzędzia, przelotnym,
znikającym po wyborze. Błąd wczesnego szkicu polegał na zamienieniu czynności chwilowej w
stałe miejsce docelowe.

**Czym to zastąpiono.** Rejestr jest osobną, globalną pozycją szyny nawigacyjnej, poza
kampanią, tylko do odczytu. W kampanii wybór wpisu z rejestru jest przelotnym,
znikającym po użyciu wyborem wywoływanym z poziomu narzędzia, a nie stałym widokiem.

### 13. Tracker tur jako element karty

**Co proponowano.** Dokument źródłowy dzieli narzędzia na „narzędzia bytu” i „narzędzia
sceny”, umieszczając tracker tur w drugiej grupie — sugerując, że mógłby być
elementem/sekcją karty pojedynczego bytu.

**Dlaczego odrzucone.** Ten podział znika, bo tracker tur nie jest elementem karty —
kolejka tur dotyczy całej kampanii, a nie pojedynczego wpisu, więc nie może być sekcją
czyjejś karty. Rozstrzygnięcie wiąże się z tym, że warstwa scen w ogóle nie powstaje w tym
projekcie (patrz kierunek „Warstwa scen jako byt” w grupie „Model danych”) — nie ma więc
bytu, któremu tracker mógłby zostać przypisany jako sekcja.

**Czym to zastąpiono.** Tracker tur jest narzędziem biurka — własne okno, własny blok
danych — tak jak każde inne narzędzie.

## Logika i bezpieczeństwo

### 14. Poziom 3 logiki / skrypty Lua

**Co proponowano.** Trzeci poziom logiki nad danymi — skrypt (Lua) — pozwalający autorowi
paczki pisać kod sterujący zachowaniem karty lub wpisu, wraz z osobnym elementem katalogu
„akcja skryptowa”.

**Dlaczego odrzucone.** Poziom drugi — jeden, współdzielony silnik deklaratywnych formuł,
bez pętli, bez gałęzi, bez efektów ubocznych, bezpieczny z definicji, wymagający tylko
walidacji przy wczytaniu paczki, nie piaskownicy — wystarcza jako domyślna i jedyna
ścieżka logiki. Wycofanie poziomu 3 usuwa nie tylko interpreter, ale całą towarzyszącą
infrastrukturę: piaskownicę, białą listę API hosta, limity czasu i liczby instrukcji,
izolację per paczka, przepływ jawnej zgody na instalację, kontener błędów skryptu oraz
sam element „akcja skryptowa” — wypada razem z poziomem 3. Po tej decyzji model
bezpieczeństwa redukuje się do walidacji przy wczytaniu i limitów rozmiarowych.

Zakaz gałęzi w formule pełni tu drugą rolę, ważniejszą niż bezpieczeństwo: uniemożliwia
warstwie szablonów napisanie własnego silnika reguł. Autor paczki nie może zapisać
„jeśli ciężki pancerz, to zeruj zręczność” — nie ma czym. Może napisać funkcję w rodzaju
`min(zręczność, pancerz.limit)`, ale to arytmetyka nad tym, co DM sam założył, deklarowana
przez konkretny przedmiot o sobie samym, a nie wiedza aplikacji o kategoriach przedmiotów.

Eksplodujące kości — w dokumencie źródłowym uzasadnienie dla wprowadzenia Lui — są tu
potraktowane jako własność notacji kości z twardym limitem eskalacji, a nie jako pętla
pisana przez autora paczki, co usuwa ostatni realny argument za poziomem 3.

**Czym to zastąpiono.** Dwa poziomy logiki: czyste dane (pole wskazuje ścieżkę w danych,
zero logiki) i deklaratywna formuła (jeden silnik formuł bez pętli i bez gałęzi, ten sam
dla obu katalogów treści).

**Wyzwalacz powrotu.** Jeśli okaże się, że poziom drugi nie pokrywa większości mechanik
docelowej klasy systemów („trad” — D&D, Pathfinder, większość OSR, Call of Cthulhu, Savage
Worlds), błędne jest założenie o zakresie produktu — wtedy trzeba wrócić do dyskusji o
zakresie aplikacji, a nie dopisywać skrypty.

### 15. Osobny „system efektów”

**Co proponowano.** Modelowanie efektów (magicznych, statusowych) jako odrębnego
mechanizmu w silniku, innego niż zwykłe wkłady przedmiotów do pola.

**Dlaczego odrzucone.** Efekt to zwykły wpis — ma szablon, kartę, może mieć prozę. Leży
na instancji jako pozycja slotu, dokładnie tak samo jak przedmiot w plecaku, a jego
szablon wypełnia kontrakt wkładu (nazwa pola plus wartość). Stąd wniosek, który usuwa całą
klasę projektowania: **pancerz i efekt są dla aplikacji tym samym** — oba to pozycje
deklarujące wkład do pola (np. KP). Formuła KP brzmi „baza plus suma wkładów do KP” i nie
wie, skąd te wkłady przyszły ani czym są.

**Czym to zastąpiono.** Mechanizm wkładów: jednolita lista pozycji wnoszących wartość do
pola, sumowana bezwarunkowo, bez rozróżniania źródła czy kategorii — nie ma „systemu
efektów”, są wkłady.

### 16. Efekty obejmujące wiele bytów, kaskady zmian i cofanie jako wymóg silnika

**Co proponowano.** Dokument źródłowy traktuje efekty oddziałujące na wiele bytów naraz —
rozsyłane przez aplikację — jako funkcjonalność świadomie odłożoną na później. Z tym samym
kierunkiem wiążą się: automatyczne kaskady kolejnych zmian stanu wywołane inną zmianą,
dialogi potwierdzeń przy naniesieniu obliczonego wyniku, oraz mechanizm cofania zmian
wprowadzonych przez regułę.

**Dlaczego odrzucone.** To nie jest brak funkcji, tylko granica produktu: u nas te rzeczy
są wykluczone, nie odłożone. Wynika to z zasady „aplikacja sumuje, DM decyduje o
składnikach” i z zakazów granicy automatyzacji — między innymi z zakazu celowania
(efekt jest tam, gdzie DM go położył, aplikacja nigdy nie rozsyła efektów) i zakazu
wyzwalaczy (nic nie dzieje się samo w odpowiedzi na zmianę stanu). Zasada „wynik jest
propozycją, nie zapisem” — dane kampanii są formularzem, DM dodaje, usuwa i poprawia
ręcznie, narzędzie tylko liczy i pokazuje wynik — usuwa z projektu wprost: kaskady
automatycznych zmian stanu, dialogi potwierdzeń, cofanie zmian wywołanych regułą, oraz
efekty obejmujące wiele instancji naraz jako wymaganie silnika.

**Czym to zastąpiono.** Bezpośredni zapis do pola w następstwie wykonanej operacji jest
rzadkim wyjątkiem, zadeklarowanym jawnie w kontrakcie akcji, i tam, gdzie występuje, **nie
pyta o potwierdzenie** — samo wywołanie akcji przez DM-a jest już intencją.

## Model danych

### 17. Warstwa scen jako byt

**Co proponowano.** Pośredni byt między kampanią a instancjami — „scena”, pojęcie z
dokumentu źródłowego, który mówi o „sesji” i „scenie” i nie zna pojęcia kampanii.

**Dlaczego odrzucone.** Kampania jest tym wszystkim naraz — sesją, światem i zapisem gry;
utworzenie kampanii jest utworzeniem sesji i utworzeniem nowego stanu świata. Nie ma
potrzeby bytu pośredniego: kampania zawiera swój świat wprost — instancje oraz stan
narzędzi biurka. Wchodzi się do kampanii, w niej aktualizuje się stan świata, i to jest
cały model.

**Czym to zastąpiono.** Kampania jest wprost właścicielem zbioru instancji; narzędzia
biurka trzymają wyłącznie referencje do nich, a cykl życia instancji — powołanie,
usunięcie — jest sprawą świata (kampanii), nie okna ani sceny.

### 18. Nakładka jako miejsce na warianty rzeczy

**Co proponowano.** Wykorzystanie nakładki instancji do modelowania wariantu rzeczy — np.
zapisanie „miecza +1” jako zwykłego miecza z nadpisanym (skorygowanym) polem w nakładce,
zamiast jako osobnego wpisu.

**Dlaczego odrzucone.** To reguła projektowa, nie blokada techniczna: miecz +1 to osobny
wpis, nie miecz z nadpisanym polem. Różnica jest ostra i sprawdzalna — korekta w nakładce
jest lokalna, nie da się jej użyć ponownie i nie ma jej w rejestrze; wariant jest treścią
i należy do paczki. Bez tego zakazu kampania po cichu staje się miejscem autorstwa
treści, co łamie zasadę, że kampania treści nie zawiera. Kryterium rozstrzygające: **czy
chcesz tego użyć ponownie**. Goblin łucznik, który ma być pod ręką w każdej przyszłej
potyczce i wyszukiwalny w rejestrze, jest wpisem; ten jeden goblin, któremu DM dał dziś
procę, jest instancją.

**Czym to zastąpiono.** Nakładce wolno nieść wyłącznie: stan zmienny (ilość, zużycie,
bieżącą wartość zasobu, przełączniki), dołączone wpisy w zadeklarowanych slotach,
tożsamość egzemplarza (np. „Goblin 2”) i ręczną korektę pola. Obie ścieżki — wariant jako
osobny wpis w paczce, i ubieranie instancji nakładką — istnieją równolegle i się nie
wykluczają: kilka wariantów w paczce daje szybki punkt startu, a ubieranie instancji
zostaje dostępne zawsze.

### 19. Wpisy lokalne dla kampanii

**Co proponowano.** Kuszące obejście problemu „DM ubiera goblina i nie ma jak zapisać
wyniku jako czegoś wielokrotnego użytku” — pozwolić kampanii przechowywać własne, lokalne
wpisy, należące tylko do niej, nie do żadnej paczki.

**Dlaczego odrzucone.** Brzmi tanio, ale sprawia, że kampania zaczyna zawierać treść —
czyli powoduje dokładnie tę erozję granicy między treścią a stanem świata, której pilnuje
rozróżnienie wpis / instancja / nakładka. Ta potrzeba ma zostać policzona jako argument za
porządnym rozwiązaniem autorstwa treści w aplikacji, a nie przemycona bocznymi drzwiami.
Pytanie, jak i gdzie autorsko pisze się treść dla aplikacji, pozostaje otwarte.

### 20. Pole `Ruleset` w manifeście kampanii

**Co proponowano.** Zarezerwowane pole `Ruleset` w manifeście kampanii.

**Dlaczego odrzucone.** Pole zostało usunięte, nie odłożone. Wcześniejsza decyzja
brzmiała „nie ożywiamy go i nie wymyślamy dla niego znaczenia”; wożenie pola, dla którego
świadomie nie przewiduje się zastosowania, jest gorsze niż jego brak.

Usunięcie tego pola — tak jak przekształcenie dawnej listy `ContentPacks` w jedną listę
`packs` o kształcie `{ id, major }` — nie podniosło wersji formatu i nie wymagało
migracji, bo deserializacja manifestu kampanii jest celowo pobłażliwa: w odróżnieniu od
wczytywania paczek nie wymusza ścisłego mapowania każdego klucza, więc starszy plik
kampanii niosący `ruleset` po prostu ma ten klucz zignorowany. Pilnuje tego test
wczytujący manifest w starym kształcie.

### 21. Rozgałęzianie po rodzaju wpisu w silniku i powłoce

**Co proponowano.** Reprezentowanie rodzaju wpisu jako typu, enuma albo rozgałęzienia
(`switch`/dispatch) **wewnątrz `Core` lub `Desktop`**.

**Dlaczego odrzucone.** Silnik i powłoka nie mają się stać aplikacją jednego systemu RPG.
Gdy rdzeń zna klasę pancerza, wymiana systemu przestaje być napisaniem czegoś obok i staje
się przepisaniem rdzenia. Kryterium jest weryfikowalne tak samo jak istniejący zakaz
odwołań do biblioteki interfejsu wewnątrz silnika.

**Zakres zakazu — uwaga, zmieniony.** Zakaz dotyczy wyłącznie `Core` i `Desktop`. **Nie
dotyczy zestawu treści**, który istnieje właśnie po to, żeby wiedzieć, czym jest potwór,
i którego widoki dispatchują po typie treści zupełnie legalnie. Wcześniejsze brzmienie
zakazywało tej wiedzy *wszędzie* — patrz pozycja 23.

**Czym to zastąpiono.** Wiedza nie jest zakazana, tylko umiejscowiona: mieszka w zestawie
i nigdzie indziej. Granicy pilnuje skan słownictwa treści po źródłach `Core` i `Desktop`,
ze słownikiem wyprowadzanym z zestawu.

### 22. Migracja formatu budowana z wyprzedzeniem

**Co proponowano.** Zbudowanie mechanizmu migracji formatu (wersji katalogu, szablonu
albo paczki) z wyprzedzeniem, zanim jakakolwiek wersja faktycznie zostanie podniesiona.

**Dlaczego odrzucone.** Migracji nie budujemy teraz — spójnie z decyzją, którą projekt
już podjął dla bloków danych: migracja ma sens dopiero, gdy jakaś wersja faktycznie
została podniesiona.

**Wyzwalacz powrotu.** Gdy jakaś wersja — katalogu, szablonu albo paczki — faktycznie
zostanie podniesiona.

---

## Skompilowane typy treści

Rozstrzygnięcia z sesji, która cofnęła projekt do fazy koncepcyjnej po stwierdzeniu, że
warstwa szablonów nie ma własności uzasadniających jej postać danych.

### 23. Karta składana z listy elementów podanej przez dane

**Co proponowano.** Szablon deklaruje w pliku, z jakich elementów katalogu składa się
karta i w jakiej kolejności; silnik renderuje je po kolei. To był obowiązujący projekt,
częściowo zbudowany — nie cudza propozycja.

**Dlaczego porzucone.**
- **Statblok jest zaprojektowanym układem, nie stosem sekcji.** Linia nad cechami, sześć
  kolumn, oddzielenie akcji — tego nie da się wyprodukować z sekwencji elementów.
- **Format zaczął przeciekać układem.** Parametry `compact` i `selfDescribing` broniono
  jako „stwierdzeń o treści, nie o układzie", a istniały wyłącznie po to, żeby wymusić
  konkretny układ w rendererze, który układu nie znał. Każdy kolejny układ wymagałby
  kolejnej takiej flagi.
- **Projektowanie karty należało do pliku danych, nie do projektanta.** Dało się
  projektować pojedyncze elementy; kompozycja pochodziła z pliku.
- **Powód, dla którego mechanizm istniał, dawał się osiągnąć inaczej.** Miał wymuszać, że
  silnik nie zna rodzajów wpisów — cel został utrzymany przez umiejscowienie tej wiedzy
  w osobnym projekcie, zamiast zakazywania jej wszędzie.

**Czym to zastąpiono.** Typ treści to para: rekord plus zaprojektowany widok. Elementy
katalogu przeżyły jako kontrolki wielokrotnego użytku.

**Cena przyjęta świadomie.** Pięć zakazów przechodzi z „strukturalnie niemożliwe" na
„zabronione testem i przeglądem". Łagodzi to fakt, że skompilowany typ treści jest
wartością liczoną raz przy składaniu aplikacji, a nie kodem wykonywanym w trakcie gry.

### 24. Kompilator zna listę systemów RPG, kampania wybiera jeden

**Co proponowano.** Aplikacja ma wbudowane systemy (D&D 5e, Pathfinder 2e); tworzenie
kampanii polega na wyborze z listy, a okna i modele danych są przypisane do systemu.

**Dlaczego odrzucone.**
- **Nie rozwiązuje postawionego problemu.** Problem brzmiał „szablony nie powinny być
  plikami danych"; ten wariant odpowiada „szablony należą do systemu". To prostopadłe
  zdania — skompilowane typy treści są możliwe bez bytu „system".
- **Przywraca taksonomię, którą projekt metodycznie usuwał** — rodzaj paczki i pole
  `Ruleset` zniknęły wcześniej właśnie dlatego, że po nich się rozgałęziano.
- **Homebrew staje się możliwy wyłącznie przez fork.** Nowe okno, zakładka czy logika
  wymagają duplikacji cudzego systemu.

### 25. Moduły deklarujące wsparcie systemów, z rozgałęzieniem po systemie w środku

**Co proponowano.** Moduły odłączone od instancji systemu; jeden moduł wspiera kilka
systemów i przełącza zachowanie instrukcją `switch` po wybranym systemie.

**Dlaczego odrzucone.**
- **To jest zakazane rozgałęzienie, uogólnione.** Gorsze niż rozgałęzienie po rodzaju
  wpisu, bo rozgałęzia zachowanie modułu hurtem, a nie punktowo.
- **Skaluje się mnożąco.** Dodanie systemu wymaga edycji każdego modułu; N modułów razy
  M systemów daje N×M gałęzi, z których każda jest miejscem cichego rozjechania się.
- **Nazwa systemu jest atrapą zdolności.** Tracker tur nie potrzebuje „D&D 5e" — potrzebuje
  czegoś, co umie podać nazwę i klucz sortowania.

**Czym to zastąpiono.** Kontrakty jako interfejsy plus reguła umiejscowienia: narzędzie
czytające pole po nazwie mieszka w zestawie, który je deklaruje; narzędzie czytające
interfejs jest neutralne. Wybór jest wyborem katalogu, w którym leży plik, nie gałęzią.

### 26. Jeden system bez żadnej rozłączności

**Co proponowano.** Aplikacja szyta pod jeden system: brak wyboru, wszystkie moduły,
okna i rejestry budowane w jednym liniowym toku, bez szwów.

**Dlaczego odrzucone w tej postaci.** Odrzucona jest **linearność, nie jednostkowość**.
Decyzja „budujemy pod jeden system" jest słuszna i została przyjęta. Zrezygnowanie
z rozdziału sprawia natomiast, że silnik zaczyna znać system — i wtedy zmiana systemu
faktycznie staje się przepisaniem aplikacji, czyli materializuje się ryzyko, którego ten
wariant miał tylko nie naprawiać.

**Czym to zastąpiono.** Zakres z tego wariantu (jeden zestaw treści dzisiaj), szew
z wariantu poprzedniego (kontrakty), granica utrzymana testem. Liczba mnoga zestawów jest
zdolnością, nie planem.

### 27. Ładowanie zestawów treści w czasie wykonania

**Co proponowano.** Zestawy jako wtyczki ładowane z katalogu przy starcie, zamiast
referencji projektu.

**Dlaczego odrzucone.** Przywraca cały model bezpieczeństwa, który wycofanie skryptów
usunęło — piaskownicę, jawną zgodę na instalację, izolację, kolejność ładowania,
wersjonowanie binarne — i kupuje za to zero, bo instalującym jest autor repozytorium.

### 28. Szablony jako plik danych wbudowany w aplikację

**Co proponowano.** Wariant pośredni: szablony zostają plikami danych, ale przenoszą się
do wnętrza aplikacji jako zasób wbudowany, więc ich zmiana wymaga przebudowy.

**Dlaczego niewybrane.** Zachowuje granicę danych i kosztuje niewiele, ale nie daje
kontroli typów, podpowiadania w edytorze, dobrych komunikatów błędu ani sprawdzenia
szablonu wobec narzędzia, które go konsumuje — a przede wszystkim nie rozwiązuje problemu,
że homebrew to głównie nowa logika i nowe okna, nie nowe pliki tekstowe. Broniłby granicy
przed autorem zewnętrznym, którym jest autor repozytorium.

**Pozostaje wariantem zapasowym**, gdyby cena z pozycji 23 okazała się nie do przyjęcia.

### 29. Materializacja wpisu w instancji

**Co proponowano.** Instancja kopiuje wartości wpisu w chwili powołania, zamiast trzymać
łącze i rzadką nakładkę.

**Dlaczego odrzucone.** Poprawka w treści przestałaby docierać do istniejących kampanii.
Aktualizacja wpisu ma działać jak patchnote balansujący grę, a nie jak zdarzenie psujące
zapisane kampanie.

**Czym to zastąpiono.** Nakładka jako rzadka łatka nad wartościami wpisu, scalana przy
odczycie i wyliczana przez różnicowanie przy zapisie — bez nazw pól w kodzie.

### 30. Odrzucanie całej paczki za jeden wadliwy wpis

**Co proponowano.** Reguła obowiązująca wcześniej: jeden błędny plik unieważnia całą
paczkę, żeby odrzucona paczka była tak dobra jak niezainstalowana.

**Dlaczego uchylone.** Trzymał ją jeden argument — rejestr nie może zawierać wpisu
rozwiązanego wobec szablonu z nieobecnej paczki. Gdy typy treści przestały pochodzić
z paczek, wadliwy plik przestał zagrażać jakiemukolwiek innemu. Zostawało samo ukrywanie
dwustu poprawnych wpisów za jedną literówką — wprost przeciw zasadzie, że wadliwa treść
zostaje widoczna i oznaczona, nigdy nie znika po cichu.

**Czym to zastąpiono.** Trzy zakresy odrzucenia: wadliwy manifest odrzuca paczkę, wadliwy
plik pozycji oznacza tę pozycję, kolizja id oznacza obie kolidujące.

### 31. Introspekcja typu treści przez narzędzie

**Co proponowano.** Nic — to zagrożenie, nie propozycja. Skompilowany typ leży w tym samym
procesie, więc „znajdź wszystkie typy mające pole `initiative`" jest jedną linijką.

**Dlaczego zakazane.** Narzędzie odkrywające swój kształt w czasie wykonania przez
chodzenie po cudzych deklaracjach jest dokładnie tą wiotkością, przed którą broni cała ta
architektura. Gdy typy były plikami danych, introspekcja wymagała parsowania i pokusa
praktycznie nie istniała; po zmianie zakaz musi być zapisany, a nie dorozumiany.

**Czym to zastąpiono.** Narzędzie deklaruje interfejs i dostaje obiekty.
