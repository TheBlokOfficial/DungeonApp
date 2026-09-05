# DungeonApp

C#/.NET 10, Avalonia (desktop, bez warstwy web), projekt solo.

Dokumenty projektu:

- `docs/vision.md` — czym jest produkt, dla kogo, czego świadomie nie robi.
- `docs/architecture.md` — szwy, niezmienniki, kontrakt modułu. Co wolno.
- `docs/code-map.md` — graf zależności i przepływów. Jak jest naprawdę.

`docs/` opisuje zamiar, który bywa nieaktualny. Gdy dokument kłóci się z
bezpośrednim poleceniem — polecenie wygrywa; zanotuj wtedy, że dokument
wymaga aktualizacji.

## Weryfikacja

```bash
dotnet build DungeonApp.sln
dotnet test tests/DungeonApp.Core.Tests/DungeonApp.Core.Tests.csproj
dotnet test tests/DungeonApp.Desktop.Tests/DungeonApp.Desktop.Tests.csproj
```

Rozstrzyga wynik polecenia uruchomionego przez ciebie; cudzy raport o
sukcesie nie jest dowodem.

## Pilnowane szwy

Zanim napiszesz linijkę kodu dotykającą któregokolwiek z poniższych,
przeczytaj `docs/architecture.md`. Część z nich jest egzekwowana testem,
część wyłącznie konwencją — dokument mówi które i dlaczego.

1. Granica między rdzeniem a interfejsem — rdzeń nie wie o warstwie UI.
2. Dostęp do dysku: tylko warstwa zapisu, nigdzie indziej.
3. Sposób, w jaki część systemu deklaruje swoje istnienie i to, czego używa.
4. Sposób, w jaki części systemu porozumiewają się między sobą — zawsze
   pośrednio, nigdy przez bezpośrednie sięgnięcie do siebie nawzajem.
5. Nieznajomość formatu zapisu przez logikę domenową, jedyność drogi
   zapisu — nowy stan powstaje wyłącznie w jednym miejscu — oraz zgodność
   każdego zapisu z zadeklarowanym kształtem danych.

Nazwy dzisiejszych mechanizmów, które to realizują, celowo nie padają tutaj
— żyją w `docs/architecture.md`, bo mogą się zmienić. Powyższe własności
zmieniają się rzadziej niż kod, który je egzekwuje.

Zmiana dotykająca któregokolwiek jest decyzją architektoniczną:
przedstaw propozycję, zanim zaczniesz pisać kod.

## Język

- Interfejs aplikacji, komentarze w kodzie, komunikaty commitów — po polsku.
- Identyfikatory (klasy, metody, zmienne, nazwy plików) — po angielsku,
  standardowa konwencja dla C#/.NET.

## Commity

- Nie commituj i nie pushuj bez wyraźnej prośby użytkownika.
- Commituj zamknięty blok pracy, zanim zaczniesz kolejny.
- Nie mieszaj w jednym commicie niezwiązanych zmian.

## Dokumentacja po zmianach

Aktualizuj `docs/` jednym przejściem na końcu bloku pracy, nie po każdym
commicie.

`docs/code-map.md` weryfikuje się w całości, a poprawia punktowo.
Weryfikacja czyta mapę od początku do końca i każde jej twierdzenie
sprawdza wobec kodu — także te, których dzisiejsza zmiana nie dotyczy.
Poprawia nieprawdziwe, dopisuje brakujące, usuwa nieistniejące.

Samo dopisanie nowego kawałka nie wystarcza i jest najczęstszym sposobem, w
jaki mapa zaczyna kłamać: zmiana w jednym miejscu unieważnia zdania leżące
gdzie indziej, a agent patrzący na diff nie ma jak do nich trafić. Weryfikacja
całości je łapie, bo mapa jest listą kontrolną sama dla siebie — a kosztuje
proporcjonalnie do swojego rozmiaru, nie do rozmiaru projektu.

Regeneracja od zera, z przemieleniem codebase'u, jest trybem awaryjnym: na
sytuację, gdy mapa rozjechała się na tyle, że weryfikacja jej twierdzeń
przestaje mieć sens.

## UI: mockup przed wdrożeniem

Nowy widok albo nowa sekcja istniejącego widoku przechodzi najpierw przez
fazę mockupu. Wdrażanie od razu w docelowym widoku, podpiętym pod prawdziwy
ViewModel, wychodzi zwykle technicznie sprawne, ale wizualnie niechlujne.

1. Mockup powstaje jako izolowany `UserControl` w `design/mockups/`
   — korzysta z istniejących stylów, ikon i palety z `Themes/`, ale bez
   podpięcia pod ViewModel: statyczne dane, zero logiki, zero integracji.
   Nie czyta ViewModeli i nie rozważa, jak coś zostanie zaimplementowane —
   ale nie wolno mu być ślepym na funkcję. Zlecenie musi podać, co element
   robi i **w jakich stanach bywa**: pusto, w trakcie, błąd, dane dłuższe
   niż miejsce, lista na dwieście pozycji. Bez tego powstaje projekt
   wyłącznie przypadku szczęśliwego, a brakujące stany dorabia się później
   na oko w docelowym widoku — czyli tam, gdzie miało ich nie być.
   Dane statyczne mają być niewygodne, nie reprezentacyjne.
2. Renderuje mockup do PNG **narzędziem projektu z `tools/`**, nigdy
   harnessem budowanym na miejscu — składnia w jego `README.md`. Na tym
   **kończy zadanie**. Nie ogląda własnego
   renderu, nie klika po podglądzie, nie porównuje wariantów.
   Kadr renderu dobiera się do powierzchni, jaką element docelowo zajmuje —
   element oceniany na pustym tle wygląda lepiej, niż będzie wyglądał
   naprawdę, bo z niczym nie konkuruje o uwagę:
   - wypełnia całe okno — render sam w sobie, jest własnym kontekstem;
   - żyje wewnątrz obszaru roboczego — render na **statycznej scenie**:
     atrapie sidebara, górnego paska i tła workspace'u, do której wstawia
     się wariant;
   - mały element — dwa rendery, izolowany i na scenie.
   Scena jest martwym `UserControl`-em w `design/mockups/`, budowanym raz i
   używanym przez kolejne mockupy. Nigdy nie jest to żywy shell: podpięcie
   pod prawdziwe ViewModele to ta integracja, której faza mockupu unika.
   Render ma rozmiar okna z chwili, w której element jest widziany, nigdy
   dobrany płótnem — inaczej ocenia się obrazek, nie ekran. Dla elementu
   obszaru roboczego to okno zmaksymalizowane na typowym monitorze, bo tak
   wygląda sesja; rozmiar domyślny z `MainWindow.axaml` to rozmiar pierwszego
   uruchomienia, nie ten, w którym się pracuje. Dla elementu widocznego
   wyłącznie na starcie — odwrotnie.
   Wariant wybrany przez użytkownika warto skontrolować drugim renderem w
   przeciwnym rozmiarze: układ dobry w dużym oknie potrafi rozjechać się w
   małym i odwrotnie. Robi się to po odsiewie, nie dla każdego wariantu.
3. Obraz trafia do użytkownika, bez krytyki wizualnej ze strony tworzącego.
   Ocenę robi użytkownik. Dopiero po jego akceptacji następuje wdrożenie do
   docelowego widoku jako osobne zadanie — port decyzji (układ, odstępy,
   hierarchia), nie kopiowanie pliku mockupu żywcem.
4. Przy większych elementach proś od razu o kilka wariantów. Wybór z kilku
   albo złożenie ich najlepszych części daje lepszy efekt niż ocena
   pojedynczej propozycji.
   Reżim zlecenia:
   - Każdy wariant dostaje własny **hint strukturalny** — ograniczenie
     układu, wykonalne bez oceny estetycznej („logo wyrównane do lewej na
     szerokości sidebara, pasek pełnej szerokości przy dolnej krawędzi").
     Hinty nastrojowe („więcej swobody", „nowocześniej") są zakazane:
     wykonawca nie ma gustu, więc odda wariant pierwszy plus ozdobniki.
   - Wszystkie pliki powstają, zanim którykolwiek wariant zostanie
     wyrenderowany — inaczej wariant B iteruje na bazie A i warianty się
     zlewają.
   - Żaden wariant nie jest wybierany jako faworyt ani komentowany.
5. `design/mockups/` jest jednorazowe — pliki, które przegrały, kasujesz.

## UI: robota mechaniczna, nie artystyczna

Polecenie „zrób ładniej" każe łączyć decyzję projektową, implementacyjną i
integracyjną w jednym kroku — stąd przeciętne UI. Trzymaj się reguł, które
nie wymagają gustu, tylko wykonania:

- **Zero magicznych liczb.** Marginesy, odstępy, rozmiary ikon — wyłącznie
  ze skali w `Themes/`. Brak potrzebnej wartości w skali to sygnał do
  zgłoszenia, nie do wpisania liczby na oko.
- **Wyrównanie przez kontener, nie przez fudge'owanie.** Pozycjonowanie
  układem `Grid`/`StackPanel`, przez `HorizontalAlignment` /
  `VerticalAlignment` i wiersze o jednolitym odstępie — nie przez ręczne
  dobieranie marginesu, aż wyjdzie.
- **Ikony tylko z istniejącego zasobu SVG**, w rozmiarze branym z klucza w
  `Themes/`, nigdy wpisanym wprost. Brakującej ikony nie rysujemy ad hoc —
  to zgłoszenie.
- **Kontrolka wbudowana bez własnego stylu w `Themes/` przynosi paletę i
  zaokrąglenia Avalonii.** Przechodzi przy tym przez zakaz magicznych liczb,
  bo żadnej liczby nie wpisano — a mimo to wnosi kolor spoza projektu. Brak
  stylu to zgłoszenie, tak samo jak brakująca ikona; kolor dobrany lokalnie
  w widoku, żeby zakryć problem, jest gorszy niż samo zgłoszenie.
- **Jedna faza na raz.** Najpierw struktura i hierarchia, potem wyrównanie
  i odstępy względem skali, na końcu przegląd na renderze.
- **Zawsze kończ renderem, nigdy własną oceną.** Render PNG to punkt
  kontrolny „czy się w ogóle wyrenderowało", nie okazja do krytyki.

## Kiedy pytać

- Przed przebudową architektoniczną — najpierw propozycja kierunku, potem kod.
- Gdy zadanie wymaga wyboru produktowego, nie technicznego.
- Gdy poprawka wymagałaby złamania jednej z zasad wyżej.

Przy punktowej poprawce z gotową diagnozą — wdrażaj od razu, bez pytania.
