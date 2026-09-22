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
żadnego z nich, a rozstrzygnięcia dotyczące samej tej zmiany są w grupie „Skompilowane typy
treści".

Od 2026-09-22 **„zestaw"** to **system** — wybierany przy starcie aplikacji — a mechanizmy
dawnego silnika i powłoki dzielą się między **ramę** i **bibliotekę**. Pozycje do 32 włącznie
zapisano jeszcze w słowniku zestawów; rozstrzygnięcia samej przebudowy są w grupie ostatniej.

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

**Czym to zastąpiono.** To samo rozwiązanie slotowe, co przy osadzaniu (kierunek „Osadzanie
szablonu w szablonie" powyżej). Czego jednak slot nie załatwia: opis (`description`) nadal
jest jednym długim tekstem w polu formularza — ten problem, długiego tekstu a nie liczby
pól, zostaje otwarty.

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

### 7. Zagnieżdżanie wartości w polu wpisu — odrzucone trzykrotnie

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

**Dlaczego odrzucone — trzecie uzasadnienie** (pomysł wrócił po przeniesieniu typów treści
do kodu, z konkretną propozycją: `Monster` złożony z podrekordów `Metadata`, `CombatStats`,
`AbilityScores`, `UtilityTraits`, `ActionSet`, z prymitywami dopiero w liściach). Ta runda
jest warta zapisania dokładniej niż dwie poprzednie, bo **trzy z pięciu powyższych argumentów
padły razem z formatem danych** i nie wolno ich już przytaczać:

- argument o wąskim zbiorze znaków w identyfikatorze pola — **martwy**, typ nazwy pola został
  usunięty razem z szablonami danych;
- argument o kontraktach prezentacji, które zaczęłyby znać strukturę wpisu — **martwy**,
  kontrakt jest interfejsem C#, a rekord może go spełnić, nie odsłaniając swojego środka;
- argument o ścieżkach z regułami rozstrzygania zakresu — **osłabiony do zera**, dostęp do
  składowej w C# jest jednoznaczny, a kompilator sprawdza go darmo.

Rozstrzygnęły dwa, które przeżyły:

- **Zaproponowane grupy są układem karty przebranym za typ.** Trzy z pięciu pokrywały się
  z sekcjami karty potwora jeden do jednego (obrona i ruch, sześć cech, biegłości i zmysły),
  a dwie pozostałe **zlepiały to, co karta rozdziela** — `Metadata` scalała grupę cech z
  osobnym blokiem prozy, `ActionSet` dwa osobne bloki prozy. Rozjazd dwóch kopii tej samej
  decyzji objawił się **w samej propozycji**, przed czyjąkolwiek pomyłką. To jest piąty
  argument z drugiej rundy, potwierdzony konkretem, a nie osłabiony.
- **Plik wpisu jest płaski i ma taki zostać, więc zagnieżdżony rekord wymaga przekładu
  w obie strony.** Nie tylko przy czytaniu: nakładka instancji jest wyliczana różnicowaniem
  rekordu wobec wartości wpisu, więc przekład jest potrzebny też przy zapisie. Dopisanie pola
  przestaje być zmianą w jednym miejscu i staje się zmianą w trzech, z których dwa mogą się
  po cichu rozjechać — a objawem rozjazdu jest notatka Mistrza Gry, która nie zapisała się na
  dysk. Ten ręcznie pisany przekład wartości to dokładnie warstwa, której usunięcie było
  jednym z powodów przeniesienia typów do kodu.

**Trzy spójne wyjścia, żeby czwarta runda zaczęła się od rozwidlenia.** Półśrodek jest tu
gorszy od każdego z końców:

| Wariant | Przekład | Status |
|---|---|---|
| płasko wszędzie: rekord, plik, łatka | żaden | **przyjęte** |
| zagnieżdżone wszędzie, **włącznie z plikiem** | żaden | **nie zamknięte** — odmraża format pliku i porównywalność diffów treści; różne od pozycji „Kosmetyczne grupowanie w pliku wpisu", bo tam loader spłaszczał, a tu nikt nie spłaszcza |
| zagnieżdżony rekord nad płaskim plikiem | dwukierunkowy | odrzucone — to ten półśrodek |

**Czego ta runda NIE odrzuciła.** Dwa najniższe poziomy propozycji — `ArmorClass(Value, Source)`
i `HitPoints(Value, Formula)` — nie są grupami z karty, są pojęciami domenowymi („ile i z czego"
jest jedną myślą). **Odłożone, nie odrzucone**, bo nie ma dziś konsumenta, który by rozstrzygnął,
czy pomagają. **Wyzwalacz:** nakładka instancji — jedyny mechanizm realnie zależny od tego
kształtu. Rozstrzygać przy niej, na dowodach, nie przed nią.

**Czym to zastąpiono.** Płaska mapa wartości pozostaje jedyną formą. Grupowanie wizualne
żyje wyłącznie w układzie karty, nigdy w danych. Realna skarga, która tę rundę wywołała —
dwadzieścia jeden parametrów pozycyjnych konstruuje się licząc przecinki, a zamiana dwóch
sąsiednich pól tego samego typu przechodzi przez kompilator i przez deserializator — została
naprawiona osobno i taniej: **wymagane właściwości nazwane zamiast parametrów pozycyjnych**,
co przy okazji sprawia, że brak wymaganej wartości w pliku odrzuca wpis bez ani jednej linii
własnej walidacji.

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

**Uchylone 2026-09-22**, z propozycji autora. Żaden z trzech powodów już nie stoi:
- **Pozycje nie pochodzą z danych.** Zakładki deklaruje skompilowany system, więc literówka
  w pliku treści nie ma jak zepsuć paska, a zbiór zakładek zależy od wybranego systemu, nie od
  paczek kampanii.
- **Podział na zakładki nie jest taksonomią silnika.** Projektuje go człowiek — projektant
  systemu — w miejscu, któremu wolno wiedzieć, czym jest potwór.
- **Pasek ma własną treść.** Od przyjęcia powierzchni kampanii (świat, fabuła, kronika) biurko
  przestało być jedyną treścią kampanii; przełącznik tych powierzchni stał po prostu obok
  szyny, zamiast w niej.

Pierwsze dwa padły razem z przejściem na typy treści w kodzie, trzeci razem z przyjęciem
powierzchni — obie zmiany wcześniejsze niż ten powrót. Nie jest to więc wskrzeszenie
odrzuconego pomysłu, tylko pomysł, którego odrzucenie straciło podstawy. W mocy zostaje
pozycja „Nawigacja o zawartości pochodzącej z danych": zakładek nie wnosi paczka. Model, który
zastąpił szynę — [architecture.md](architecture.md), *Nawigacja: ekran wyboru systemu i pasek
boczny*.

### 10. Nawigacja o zawartości pochodzącej z danych

**Co proponowano.** Zasada ogólna, szersza niż sam kontekstowy sidebar: żeby zawartość
elementów nawigacji mogła pochodzić z paczek treści.

**Dlaczego odrzucone.** Odrzucona jest szyna nawigacji o zawartości z danych, a nie
grupowanie treści wewnątrz skompilowanego ekranu — granica jest wąska i konkretna. Jej
kryterium, sprawdzalne po skutku awarii, stoi w [architecture.md](architecture.md), *Zakładki
treści*.

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

**Potwierdzone 2026-09-22 w mocniejszej postaci:** rejestr przeszedł do zakładek treści
kategorii System, a zakładka tej kategorii nie widzi kampanii wcale —
[architecture.md](architecture.md), *Pasek boczny: trzy kategorie*.

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
warstwie treści napisanie własnego silnika reguł. Rozwinięcie tego argumentu wraz z przykładem
jest w [architecture.md](architecture.md), *Poziomy logiki i formuły*.

Eksplodujące kości — w dokumencie źródłowym uzasadnienie dla wprowadzenia Lui — są tu
potraktowane jako własność notacji kości z twardym limitem eskalacji, a nie jako pętla
pisana przez autora paczki, co usuwa ostatni realny argument za poziomem 3.

**Czym to zastąpiono.** Dwa poziomy logiki: czyste dane (pole wskazuje ścieżkę w danych,
zero logiki) i deklaratywna formuła (jeden silnik formuł bez pętli i bez gałęzi, ten sam
dla obu katalogów treści).

**Wyzwalacz powrotu.** Jeśli okaże się, że poziom drugi nie pokrywa większości mechanik
docelowej klasy systemów („trad” — zakres zdefiniowany w [architecture.md](architecture.md),
*Czym to jest*), błędne jest założenie o zakresie produktu — wtedy trzeba wrócić do dyskusji
o zakresie aplikacji, a nie dopisywać skrypty.

### 15. Osobny „system efektów”

**Co proponowano.** Modelowanie efektów (magicznych, statusowych) jako odrębnego
mechanizmu w silniku, innego niż zwykłe wkłady przedmiotów do pola.

**Dlaczego odrzucone.** Efekt to zwykły wpis leżący na instancji jako pozycja slotu, dokładnie
tak samo jak przedmiot w plecaku, i wypełniający kontrakt wkładu. Osobny mechanizm nie jest mu
do niczego potrzebny.

**Czym to zastąpiono.** Mechanizm wkładów, opisany wraz z wnioskiem, który usuwa całą klasę
projektowania, w [architecture.md](architecture.md), *Efekt jest wkładem, nie mechanizmem*.

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

**Częściowo uchylone 2026-09-22.** Operacja na kilku bytach **wskazanych przez MG** jest
dozwolona — obrażenia kuli ognia na czterech zaznaczonych goblinach, przekazanie przedmiotu
i złota między graczem a kupcem — a bezpośredni zapis po akcji MG przestaje być „rzadkim
wyjątkiem". Wykluczone pozostaje to, co było sednem tej pozycji: aplikacja wybierająca cele
sama (obszar, „wszyscy"), kaskady zmian, dialogi potwierdzeń, cofanie zmian wywołanych regułą.
Uzasadnienie — pozycja „Czwarty zakaz w brzmieniu «operacja zmienia to, na czym ją wywołano»".

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

### 20. Zarezerwowane pola `Ruleset` i `ContentPacks` w manifeście kampanii

**Co proponowano.** Trzymanie w manifeście kampanii dwóch pustych pól — `Ruleset`
(zawsze `null`) i `ContentPacks` (zawsze pusta lista) — zarezerwowanych na przyszłość,
żeby pierwszy system reguł i pierwsza paczka kampanii nie wymusiły przekształcenia
manifestu.

**Dlaczego odrzucone.** Oba pola zostały usunięte, nie odłożone: wożenie pola, dla
którego świadomie nie przewiduje się zastosowania **w tym samym wycinku pracy**, jest
gorsze niż jego brak. Pusty klucz w zapisanym pliku zaprasza przyszły build, żeby uznał
go za znaczący, a przez cztery sesje nie zbliżył się do niego żaden konsument.

Usunięcie nie podniosło wersji formatu i nie wymagało migracji, bo deserializacja
manifestu kampanii jest celowo pobłażliwa: w odróżnieniu od wczytywania paczek nie
wymusza ścisłego mapowania każdego klucza, więc starszy plik kampanii niosący `ruleset`
i `contentPacks` po prostu ma te klucze zignorowane. Pilnują tego dwa testy — jeden
wczytuje manifest w starym kształcie, drugi sprawdza, że nowy zapis tych kluczy już nie
niesie.

**Czym to zastąpiono.** Niczym — i to jest sedno. Gdy kampania faktycznie zacznie
deklarować swoje paczki, pole wraca **razem ze swoim konsumentem**, w kształcie listy
`packs` o pozycjach `{ id, major }` (`major`, bo wersja major zostawia referencję
nierozwiązaną, dopóki kampania świadomie nie zaktualizuje deklaracji — patrz
*Wersjonowanie* w `architecture.md`). Ten kształt jest tu zapisany właśnie po to, żeby go
wtedy nie wymyślać od nowa. Pojęcie systemu reguł nie wraca w ogóle: zestaw treści jest
skompilowany, a kampania nie wybiera go z pliku (patrz „Kompilator zna listę systemów RPG,
kampania wybiera jeden").

*Uwaga historyczna:* ta pozycja przez jedną sesję opisywała obie zmiany jako **już
wykonane**, łącznie z testem, który nie istniał — podczas gdy w kodzie stały oba pola
i test, który je zamrażał, nazywając je „kontraktem, nie dekoracją”. Rozjazd wyszedł
2026-09-13 przy sprawdzaniu przesłanek kolejki i wtedy decyzję wykonano naprawdę.
Wniosek ogólniejszy niż ta pozycja: **dokument odrzuconych kierunków opisuje
rozstrzygnięcia, nie stan repozytorium**, i czas przeszły w nim nie jest dowodem, że coś
jest w kodzie zrobione.

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
zakazywało tej wiedzy *wszędzie* — patrz „Karta składana z listy elementów podanej przez dane".

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

**Częściowo uchylone 2026-09-13.** Autor przyjął, że **kampania zaznacza przy zakładaniu, które
zestawy w niej działają**, a biurko pokazuje okna wyłącznie zaznaczonych. Co odróżnia to od
wariantu odrzuconego powyżej i dlaczego tamte argumenty nie trafiają:

* **Nie ma bytu „system" ani rodzaju zestawu.** Wszystko jest zestawem, bez wyróżnionego zestawu
  bazowego; zestaw może zadeklarować, że **wymaga** innego. „System" i „rozszerzenie" to odczyty
  z grafu zależności, nie zadeklarowane role. Taksonomia, której dotyczył drugi argument, nie
  wraca — nie powstaje pole z rodzajem, po którym dałoby się rozgałęzić.
* **Nic nie rozgałęzia się po nazwie.** Biurko pyta „czy zestaw tego okna jest na liście kampanii",
  nigdy „czy to jest D&D". Żadna linijka w silniku ani w powłoce nadal nie wymienia zestawu
  z nazwy, a pilnuje tego ten sam test co dotąd.
* **Homebrew nie wymaga forka**, bo lista zestawów nie jest zamknięta, a większość tego, co bywa
  rozszerzeniem, jest u nas **paczką** — wpisy są danymi i nie potrzebują zestawu w ogóle.
* Pierwszy argument dotyczył problemu („szablony nie powinny być plikami danych"), który został
  od tamtej pory rozwiązany inaczej, więc nie ma już czego rozstrzygać.

**Co zostaje otwarte:** co rozszerzeniu wolno zobaczyć u zestawu, od którego zależy. Zestawy nadal
nie mogą się nawzajem referencjonować — rozstrzygnie to pierwszy prawdziwy drugi zestaw, nie
rozmowa przed nim. Szczegóły i stan prac: `tasks.md`.

**Uchylone w całości 2026-09-22**, w postaci innej niż odrzucona: system wybiera się przy
starcie aplikacji, na ekranie wyboru, a kampania należy do jednego systemu. Model z 2026-09-13
— kampania zaznacza zestawy, zestaw może wymagać innego — nie powstał i nie powstanie;
zastąpiły go dodatki (pozycja „Rozszerzenia jako zestawy zależne od innego zestawu"). Pytanie
otwarte o to, co widzi rozszerzenie, znika razem z rozszerzeniami.

Dlaczego pozostałe argumenty nie trafiają:
- **taksonomia** — rama nie rozgałęzia się po systemie; obsługuje wybrany system przez jego
  zadeklarowane zakładki i nie pyta, który to jest. Pole, po którym dałoby się rozgałęzić,
  nie powstaje;
- **homebrew przez forka** — autorem wszystkich systemów jest autor repozytorium, a warianty
  zasad wewnątrz systemu dają dodatki.

**Cena przyjęta świadomie:** dwa pokrewne systemy (np. 5e i jego wariant) dzielą kod wyłącznie
przez bibliotekę; kod specyficzny dla jednego z nich — choćby karta potwora — powtarza się.

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

Ten sam argument trzyma od 2026-09-22 regułę, że o włączonych dodatkach system dowiaduje się
w jednym miejscu — pozycja „Dodatek jako przełącznik sprawdzany w logice".

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

**Pozostaje wariantem zapasowym**, gdyby cena przyjęta w „Karta składana z listy elementów
podanej przez dane" okazała się nie do przyjęcia.

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

**Co proponowano.** Nic — to zagrożenie, nie propozycja: po przeniesieniu typów treści do
kodu introspekcja stała się tania, więc zakaz musiał zostać zapisany, a nie dorozumiany.

**Dlaczego zakazane.** Zakaz i jego uzasadnienie stoją w [architecture.md](architecture.md),
*Kontrakty są interfejsami*. Pozycja jest tutaj wyłącznie po to, żeby temat dał się znaleźć
w rejestrze rozstrzygnięć — nie dlatego, że ktokolwiek to zaproponował.

**Czym to zastąpiono.** Narzędzie deklaruje interfejs i dostaje obiekty.

### 32. Utrzymanie warstwy bloków danych po odejściu jej jedynego konsumenta

**Co proponowano.** Zostawić rejestr bloków, hierarchię `DataBlockShape`, magazyn w kampanii
i jego połowę zapisu na dysk po usunięciu licznika — jako gotowe miejsce na stan przyszłych
narzędzi domenowych, w którym zapisze się zegar świata czy kolejka tur.

**Dlaczego odrzucone 2026-09-14.**
- **Pierwsze prawdziwe narzędzie biurka nie użyło bloku w ogóle.** Wyzwalacz z pytania otwartego
  „czy `DataBlockShape` zarabia na siebie" odpalił się odwrotnie, niż zakładano: nie przyniósł
  drugiego użytkownika mechanizmu, tylko dowód, że pierwszy prawdziwy konsument poszedł obok.
- **Mechanizm nie opisuje rzeczy, dla której go zaprojektowano.** Kształt umie pojedynczą wartość
  i płaski rekord o stałym zestawie pól. Kolejka tur, drużyna i skład potyczki są listami —
  każda z nich zażądałaby nowego rodzaju kształtu, zanim w ogóle by z niego skorzystała.
- **Ten sam problem rozwiązano w tym repozytorium drugi raz i lepiej.** Warstwa treści nie ma
  własnej hierarchii kształtów: jest rekord z `required` i deserializator jako jedyny walidator.
  Trzymanie słabszego wariantu obok lepszego oznacza, że następne narzędzie skopiuje ten bliższy
  ręki.
- **Sześć na sześć.** Wcześniejsze rzeczy utrzymywane bez konsumenta („Pytania otwarte"
  w `architecture.md`) umarły co do jednej. To siódmy przypadek tego samego.

**Rozważone i odrzucone w trakcie: zegar świata.** Jest realnym kontrargumentem, bo nie wiąże się
z żadnym wpisem — instancje go nie obsłużą — a usuwany kształt obsłużyłby go bez rozbudowy.
Rozstrzygnęło to, że zegar nie stoi w kolejce, oraz to, że wraca mu ułamek tego, co odchodzi.

**Czym to zastąpiono — i w jakim kształcie wróci.** Dziś: niczym; jedynym magazynem stanu kampanii
są instancje. Gdy pierwsze narzędzie zażąda stanu **niezwiązanego z żadnym wpisem** — zegar świata
jest najbliższym kandydatem — wraca **prostsza rzecz niż to, co usunięto**, i ten kształt jest tu
zapisany po to, żeby go wtedy nie wymyślać od nowa:

* **rekord z `required`, deserializator jako jedyny walidator** — wzorem `Monster` i `Gear`,
  bez ani jednej linijki ręcznej walidacji;
* **z mechanizmu zostaje wyłącznie to, co naprawdę było potrzebne:** identyfikator, numer wersji
  (żeby „ten build tego nie rozumie" nadal dawało się powiedzieć) i ten sam `AtomicWrite`;
* **nie wraca:** hierarchia kształtów, `Matches`, rejestr kształtów ani kontrakt `ITool`
  deklarujący używane bloki.

Wraca **razem ze swoim konsumentem**, nigdy przed nim — i nie przez przywrócenie usuniętego kodu
z historii, tylko jako to, czego ten konsument faktycznie potrzebuje.

**2026-09-22:** kształt wraca szerzej, niż zakładano — jako sposób, w jaki rama zapisuje każdy
model stanu zadeklarowany przez system ([architecture.md](architecture.md), *Gdzie mieszka
stan*). Pierwszym konsumentem są instancje, więc reguła „razem z konsumentem" jest spełniona.

---

## Rama, biblioteka, system

Rozstrzygnięcia z sesji 2026-09-22, która zdegradowała dawny silnik i powłokę do ramy, wydzieliła
wspólny kod do biblioteki i oddała systemowi wygląd i zawartość aplikacji. Model —
[architecture.md](architecture.md), *Rama, biblioteka, system*.

### 33. Rozszerzenia jako zestawy zależne od innego zestawu

**Co proponowano.** Model przyjęty 2026-09-13: wszystko jest zestawem, zestaw może wymagać
innego, a „system" i „rozszerzenie" czyta się z grafu zależności. Rozszerzenie dokłada treść do
fundamentu, nie zmieniając go.

**Dlaczego zastąpione 2026-09-22.**
- **Homebrew nie dokłada, tylko modyfikuje.** Argument autora, na przykładzie Cywilizacji VI:
  tryb barbarzyńskich klanów nie dodaje nowej cywilizacji, tylko zastępuje domyślną mechanikę.
  Warianty zasad przy stole działają tak samo. Model, w którym rozszerzenie nie wpływa na
  fundament, nie opisuje najczęstszego przypadku.
- **Graf zależności nigdy nie powstał**, a niósł pytanie, którego nie dało się zamknąć przed
  pierwszym prawdziwym drugim zestawem: co rozszerzeniu wolno zobaczyć u zestawu, od którego
  zależy.
- **Treść z dodatków wydawniczych jest paczką, nie zestawem** — bestiariusz czy nowe przedmioty
  działały bez tej maszynerii i nadal działają.

**Czym to zastąpiono.** Dodatki — warianty zasad wbudowane w system, włączane przy zakładaniu
kampanii, zdolne dokładać i zastępować. Reguły — [architecture.md](architecture.md), *Dodatki*.

### 34. Dodatek jako przełącznik sprawdzany w logice

**Co proponowano.** Wzorem konfiguracji modów: w środku logiki warunek „czy dodatek jest
włączony" — np. przy sprzedaży przedmiotu „jeśli sakiewki, szukaj najbliższej sakiewki gracza,
inaczej dopisz złoto do karty".

**Dlaczego odrzucone.**
- **To odrzucony wcześniej kształt, piętro niżej.** Moduły przełączające zachowanie po systemie
  (pozycja 25) odpadły, bo każdy nowy system wymagał edycji każdego modułu, a gałęzie rozjeżdżały
  się po cichu. Dodatki sprawdzane w logice robią to samo — z kombinacjami dodatków mnożącymi
  gałęzie.
- **Wartość strzeżona flagą** — wprost z pierwszego zakazu.
- **Nie ma tu czego przełączać w trakcie.** W grze komputerowej przełącznik siedzi w logice, bo
  logika sama wykonuje reguły w każdej turze. Tu dodatek zmienia wyłącznie kształt księgowości,
  a ten jest stały od otwarcia kampanii.
- **Przełącznik z przykładu był objawem, nie potrzebą.** Potrzebowała go aplikacja wybierająca
  sakiewkę sama. Gdy sakiewkę wskazuje MG, przełącznik znika: okno handlu pyta, dokąd trafia
  złoto, a dodatek rozstrzygnął przy otwarciu kampanii, co stoi na tej liście.

**Czym to zastąpiono.** System dowiaduje się o włączonych dodatkach w jednym miejscu, przy
składaniu kampanii; później nic o nie nie pyta.

### 35. Czwarty zakaz w brzmieniu „operacja zmienia to, na czym ją wywołano"

**Co proponowano.** Brzmienie obowiązujące do 2026-09-22: operacja zmienia to, na czym ją
wywołano; nic nie przechodzi po innych bytach, żeby coś na nich nanieść. W praktyce czytane jako
zakaz każdej operacji dotykającej więcej niż jednej rzeczy.

**Dlaczego odrzucone.** Autor, na przykładzie handlu: sprzedaż, w której MG sam usuwa przedmiot,
dopisuje złoto graczowi i przedmiot kupcowi, jest „strasznie męcząca, długa i monotonna" — a nic
w niej nie wymaga decyzji aplikacji. Asystent pomylił wtedy decyzję z jej zaksięgowaniem:
o transakcji zdecydował MG, klikając „sprzedaj", a przesunięcie przedmiotu i złota to zapis po
obu stronach tej decyzji. Sprawdzenie wobec pięciu zakazów wykazało, że handel łamał wyłącznie
literę czwartego — a litera ta zabraniałaby nawet przełożenia miecza z plecaka do skrzyni,
czego aplikacja musi umieć tak czy inaczej. Intencja zakazu była węższa: aplikacja nie wybiera
celów.

**Czym to zastąpiono.** „Nic nie wybiera celów za Mistrza Gry" — brzmienie w
[CLAUDE.md](../CLAUDE.md); uzasadnienie i granica, która nadal obowiązuje —
[architecture.md](architecture.md), *Księgowanie decyzji na kilku rzeczach naraz*.

### 36. Biblioteka wspólna jako warstwa pośrednia

**Co proponowano.** Asystent, 2026-09-22: wspólny kod — biurko, kontrolki kart, wpisy — jako
trzecia warstwa między ramą a systemem.

**Dlaczego odrzucone.** Autor: warstwa to coś, przez co wszystko przechodzi i co działa samo;
biblioteka nie robi nic, dopóki system jej nie użyje — jak każda biblioteka w programowaniu.
Nazwa „warstwa" sugerowałaby, że rama woła wspólny kod albo że system musi przez niego przejść.

**Czym to zastąpiono.** Biblioteka: rama o niej nie wie, sama niczego nie rejestruje, system
bierze z niej, co chce, albo nic — [architecture.md](architecture.md), *Biblioteka, nie warstwa*.

### 37. Ekran wyboru systemu odłożony do drugiego systemu

**Co proponowano.** Asystent, 2026-09-22: przy jednym systemie ekran wyboru pytałby przy każdym
starcie o jedną odpowiedź — kształt, który reguła „nic nie wchodzi bez konsumenta" dotąd
eliminowała. Zbudować go razem z drugim systemem.

**Dlaczego odrzucone.** Autor: ekran nie jest opcjonalnym mechanizmem czekającym na
konsumenta, tylko wejściem do aplikacji — miejscem, w którym system w ogóle zostaje wybrany.
Przy jednym systemie nadal pełni tę rolę. Reguła „nic bez konsumenta" dotyczy mechanizmów
utrzymywanych na zapas, nie punktu wejścia.

**Czym to zastąpiono.** Ekran wyboru istnieje od razu — [architecture.md](architecture.md),
*Nawigacja: ekran wyboru systemu i pasek boczny*.
