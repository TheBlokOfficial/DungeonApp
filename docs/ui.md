# UI

Ten dokument opisuje język wizualny interfejsu i mechanikę jego utrzymania.
Nie duplikuje `docs/architecture.md` (szwy Core/Desktop, kontrakt modułu) ani
`docs/vision.md` (decyzje produktowe) — czytaj je osobno, gdy potrzebny jest
tamten kontekst.

## 1. Język wizualny

**Kierunek:** nowoczesne narzędzie pracy, które ma wyglądać, jakby należało
do świata klasycznego cRPG — nie generyczny dashboard SaaS, nie mobilna
aplikacja z miękkimi kafelkami, nie dosłowna imitacja pergaminu/drewna.
Orientacyjna proporcja: przewaga współczesnego narzędzia użytkowego nad
atmosferą fantasy. Klimat nie może obniżać czytelności ani szybkości pracy —
to jest twardsze kryterium niż estetyka.

**Referencje** (nie makiety do odtworzenia, każda daje jedną warstwę):
Neverwinter Nights 2 (ciężar paneli, ciepła ciemna paleta, wyraziste ramy),
Obsidian w wariancie nowoczesnym (hierarchia wiedzy, boczna nawigacja) i
surowym (zwarte panele, bursztynowe zaznaczenia), Foundry VTT / D&D Beyond
(gęstość danych, stałe kolumny), prezentacja statbloku (redakcyjna
hierarchia jednego Entity, widok do czytania, nie formularz).

**Zasada poziomów głębi.** Trzy powierzchnie robocze, rozpoznawalne
peryferyjnie zanim cokolwiek się przeczyta: **rama** (topbar, sidebar,
statusbar — najjaśniejsza, zwarta konstrukcja), **zaplecze** (formularze,
listy, karty — lekko wpuszczone, płaska wewnętrzna krawędź, bez siatki),
**stół** (żywa sesja — wyraźnie wgłębiony, z siatką tła i cieniem `inset`).
Rozróżnienie niesie **ton i konstrukcja razem**, nie sam kolor — dlatego
role głębi (`Frame`/`Backstage`/`Desk`) są osobnymi tokenami nawet gdy dziś
mają równe wartości: mają móc ewoluować niezależnie. Granica biegnie po
**trybie pracy, nie po kontekście kampanii** — rejestry i edycja tej samej
kampanii są jasne, tylko żywa sesja jest ciemna; ten sam Entity (np.
drużyna) może mieć obie powierzchnie zależnie od celu ekranu.

**Dobór koloru.** Ciepła, przygaszona paleta grafitów z bursztynowo-miedzianym
akcentem; żadnej czystej czerni, jaskrawego neonu, chłodnego fioletu ani
dużych powierzchni nasyconego koloru. Hierarchia ramek niesie znaczenie:
mocna rama = główny kontekst, cieńsza linia = sekcje wewnętrzne, zmiana
powierzchni = informacja drugiego poziomu — nadmiar równorzędnych
obramowań jest błędem hierarchii nawet gdy pojedyncze komponenty wyglądają
poprawnie. Mocniejsza rama to inny token koloru, nie większa grubość linii.
Narożniki ostre lub lekko zaokrąglone — większy promień wymaga
uzasadnienia. `Pressed` nie ma osobnej reprezentacji wizualnej — wciśnięcie
zachowuje wygląd `hover`; `focus-visible`, `disabled`, `checked`/`selected`
pozostają odrębnymi, znaczącymi stanami.

**Dobór ikon.** Jeden podstawowy, kuratorowany zestaw monochromatycznych SVG
o wspólnym gridzie i grubości linii — ikony uzupełniają tekst, nie
zastępują niejednoznacznych etykiet. Własne ikony domenowe (pojęcia TTRPG
spoza zestawu bazowego) muszą trzymać ten sam grid i optyczną masę. Nie
mieszamy bibliotek ikon w podstawowym UI.

**Typografia — dwa głosy.** Bezszeryfowy dla kontrolek, nawigacji, tabel i
liczb (już w projekcie, cyfry tablicowe dla wartości mechanicznych).
Spokojny szeryfowy display dla nazw Entity, tytułów ekranów, sygnetu
aplikacji i nagłówków kart — ma przywoływać podręcznik RPG, zostając
czytelnym, nie pseudośredniowiecznym.

**Licencjonowanie zasobów.** Każdy zasób (font, ikona) ma jawne źródło i
licencję pozwalającą na dystrybucję, jest przechowywany lokalnie i działa
bez sieci, do repo trafia tylko realnie użyty podzbiór z zapisaną wersją,
licencja leży obok w katalogu zasobów. Aktualizacja biblioteki ikon/fontu
jest świadomą zmianą wizualną, nigdy skutkiem ubocznym aktualizacji
zależności. Grafik z gier/narzędzi wskazanych jako referencje nie kopiujemy
— służą wyłącznie do opisania kierunku, nie do przenoszenia zasobów.

**Deterministyczna geometria.** Zasada nadrzędna nad estetyką: geometria
zmienia się wyłącznie w odpowiedzi na jawną akcję użytkownika lub jawną
zmianę trybu widoku, nigdy w reakcji na zmianę danych (nowa wartość HP,
dłuższa nazwa, kolejny wpis historii). Stany `loading`/`empty`/`ready`/`error`
mają tę samą geometrię kontenera.

### Materiał referencyjny

`docs/images/` — zrzuty ilustrujące warstwy języka wizualnego. To nie są
makiety do odtworzenia piksel w piksel, tylko odniesienie dla nastroju,
głębi i gęstości interfejsu.

## 2. Gdzie mieszkają liczby

Kod jest jedynym źródłem prawdy dla wartości — ten dokument wskazuje pliki i
kategorie kluczy, nigdy same liczby.

| Plik | Co trzyma |
| --- | --- |
| `src/DungeonApp.Desktop/Themes/Tokens.axaml` | paleta kolorów (tło, powierzchnie, role głębi `Frame`/`Backstage`/`Desk`, obramowania, tekst, akcent, semantyka sukces/ostrzeżenie/błąd, chrome formularzy i paneli), promień narożnika, referencje do dwóch rodzin fontów. Jawnie **nie** trzyma tokenów zależnych od profilu skali (patrz niżej) — dopisany komentarz w pliku wskazuje na `UiScaleProfiles.cs` jako jedynego pisarza tych wartości. |
| `src/DungeonApp.Desktop/Themes/Icons.axaml` | zestaw `DrawingImage` ikon SVG (obecnie ok. tuzina symboli nawigacji/akcji) plus dwa warianty pióra (`DungeonIconPen`, `DungeonIconPrimaryPen`) determinujące grubość i zaokrąglenie linii. |
| `src/DungeonApp.Desktop/Themes/UiScaleProfiles.cs` | jedyny pisarz wszystkich tokenów zależnych od profilu `Small`/`Medium`/`Large`: rozmiary fontów, wysokości topbara/sidebara/statusbara/nagłówka workspace'u, wysokości kontrolek i wierszy nawigacji/kampanii, rozmiar ikon akcji, minimalny rozmiar okna, geometria nagłówka i minimów panelu pulpitu, rozmiar karty w dolnym pasku. Ustawiane na `Application.Resources` przed utworzeniem pierwszego okna. |
| `src/DungeonApp.Desktop/Controls/Workspace/WorkspaceGridSettings.cs` | geometria blatu żywej sesji: rozmiar komórki siatki tła, tryby i krok snapowania (zwykły/precyzyjny), promień przyciągania, czas animacji domknięcia snapu, odstęp między panelami, margines krawędzi blatu. Czytane zarówno przez rendering siatki (`WorkspaceGridBackground`), jak i przez obliczenia geometrii panelu (`PanelGeometry`, `PanelWindow`) — jedno źródło, żeby siatka widoczna i snap nie rozjechały się. |

**Znane luki.** W `Tokens.axaml` nie ma osobnej, nazwanej skali odstępów
(marginesów/paddingów) — wartości odstępu, które nie są geometrią blatu,
występują dziś lokalnie w widokach (np. `ColumnSpacing`/`Margin` wprost w
`DungeonControls.axaml`), nie jako wspólne tokeny. Rozmiar ikon nie jest
zamkniętą, nazwaną skalą: `ActionIconSize` to jedna wartość na profil skali
(nie samodzielny wybór wariantu ikony), więc wyjątek dla „zamkniętej małej
skali rozmiarów ikon" z zasad projektu **nie ma dziś odpowiednika w
kodzie** — jeśli mockup potrzebuje więcej niż jednego rozmiaru ikony
naraz, to sygnał do zgłoszenia, nie do wpisania liczby na oko.

## 3. Reguły komponentowe

**Kiedy wydzielić komponent.** Element staje się `UserControl`/
`TemplatedControl`, gdy spełnia co najmniej jedno: reprezentuje samodzielne
pojęcie UX; ma własny kontrakt danych/interakcji; ma kilka stanów
wizualnych wymagających wspólnego utrzymania; jest używany w wielu
miejscach; ma niezależne zasady geometrii/przepełnienia; wydzielenie
pozwala rodzicowi opisywać kompozycję zamiast szczegółów. Nie wydzielamy
elementu tylko dlatego, że jego markup zajmuje kilka linii XAML.

**Trzy mechanizmy w Avalonia:** `UserControl` — domyślny wybór dla
ekranów/fragmentów specyficznych dla DungeonApp; style + `DataTemplate` —
powtarzalna prezentacja kolekcji przez `ItemsControl`/`ListBox`, nie ręcznie
powielany markup; `TemplatedControl` — generyczny, tematyzowalny element z
własnymi stanami, wygląd przez `ControlTheme`, zachowanie przez
`StyledProperty`/komendy/pseudoklasy. Własnej kontrolki rysowanej przez
`Render` nie tworzymy, jeśli efekt jest osiągalny kompozycją/stylem.

**Kontrakt wejść/wyjść.** Wejścia: typowany ViewModel przez `DataContext`
lub jawne właściwości, zawartość przez `Content`/`ItemsSource`, tokeny
stylu z motywu, jawny wariant rozmiaru/tryb. Wyjścia: komendy ViewModelu,
zdarzenia kontrolki wyłącznie dla generycznych zachowań UI — nigdy
bezpośredni zapis do repozytorium ani wywołanie domeny z code-behind.
Gwarancje: udokumentowany rozmiar min/preferowany i sposób przepełnienia,
stabilna geometria między `loading`/`empty`/`ready`/`error`, brak
zależności od nazw elementów rodzica, typowane i kompilowane bindingi.

**Granica ViewModel / code-behind.** ViewModel odpowiada za stan
prezentacyjny i orkiestrację use case'ów danego ekranu — nie zna
konkretnych elementów XAML, nie zawiera reguł kampanii, serializacji ani
ścieżek filesystemu. Code-behind jest dozwolony wyłącznie dla zachowań
należących do widoku: fokus, interakcje wskaźnika/przeciąganie, reakcja na
wariant rozmiaru, integracja z mechanizmami okna, animacje, koordynacja
pierwszej klatki i rozgrzewania drzewa wizualnego. Nigdy nie wywołuje use
case'ów ani nie interpretuje reguł kampanii.

**Organizacja katalogów** (stan faktyczny w `src/DungeonApp.Desktop/`):
`Shell/` (topbar, sidebary, statusbar), `Features/` (`CampaignLibrary/`,
`CampaignWorkspace/` — widok razem z ViewModelem, nie osobne równoległe
drzewa), `Controls/Workspace/` (geometria i kontrolki blatu żywej sesji),
`Themes/` (tokeny, kontrolki, ikony, profile skali), `Assets/`, `Settings/`,
`ViewModels/`.

## 4. ZAMIERZONE — nie istnieje dziś w kodzie

- **Breakpointy workspace'u `Compact`/`Standard`/`Wide`** i przypisane im
  kompozycje dashboardu — brak w kodzie logiki reagującej na szerokość
  workspace'u; blat żywej sesji ma jedną swobodną geometrię paneli
  niezależnie od rozmiaru okna.
- **Strategie wzrostu paneli `FixedMetric`/`Bounded`/`FluidData`/
  `Document`** jako formalny, nazwany kontrakt — brak typu/atrybutu
  niosącego tę deklarację w kodzie.
- **Docelowa architektura informacji workspace'u kampanii** (grupy
  `Sesja`/`Świat`/`Wiedza`/`Kampania`, wariant kompaktowy sidebara) —
  sidebar globalny ma dziś tylko grupy biblioteki/systemu; wariant
  kompaktowy został świadomie usunięty i wróci przy realnej potrzebie.
- **Paczki zawartości i baza wiedzy** (Entity jako kanoniczna tożsamość
  łącząca statblock, notatki, relacje, backlinki) — moduły i ekrany tego
  obszaru jeszcze nie istnieją.
- **Jasny/ciemny wariant powierzchni redakcyjnej** (statblock dopasowany
  do pulpitu vs. kremowy) — decyzja otwarta, wymaga porównania na tej
  samej treści przed implementacją.
- **Nazwana skala odstępów (spacing)** jako token w `Tokens.axaml` — dziś
  odstępy poza geometrią blatu żyją lokalnie w widokach.
- **Zamknięta, nazwana skala rozmiarów ikon** — dziś istnieje jeden
  rozmiar ikony akcji na profil skali, nie wybieralna skala.
