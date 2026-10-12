# Specyfikacja

## Ładowanie aplikacji

Pasek ładowania (loading bar) to gotowiec z galerii kontrolek, jego progress jest natychmiastowy. Chciałbym aby progress bar był prostokątny z ostrymi rogami, oraz dostał ease-in oraz ease-out animacje progresji.

Przy jednym konsumencie (DnD 5e) ekran wyboru systemu wogule się ma nie pojawiać, a aplikacja od razu przechodzi do docelowego widoku.

Aplikacja powinna pamiętać wielkość okna, po jego zamknięciu. Maximized/Windowed oraz size.

# Nawigacja i rama

- Usuwamy animacje Fade-in/out obszaru roboczego przy przełączaniu się między zakładkami na sidebarze. Sprawdzałem w internecie i taka konwencja jest słaba i nieintuicyjna. Robimy natychmiastowe przełączanie lub eksperymentujemy z innymi.

- W zakładke listy kampanii po prawej stronie znajduje się dekoracyjna cell-shade "kurtyna", złożona z kilku prostokątów o różnych przezroczystościach/odcieniach - do wycięcia całkowicie.

- Wejście w wybraną kampanie, nie zamienia nazwę zakładki na "Biurko" tylko na nazwę kampanii. Ikona SVG zostaje ta sama (otwarta książka) ale dostaje kolor akcentowy (złoty/żółty), taki jaki jest w tokenach.

- Wysokość topbara jak i obszaru z ikonką hamburgera i nazwą aplikacji oraz jej ikoną nie są tak samo wysokie. To znaczy ich dolne linie się nie pokrywają, a innymi słowy nie tworzą ładnego krzyża.

- Rozprawianie się z nadmiernym tooltipem pojawiającym się praktycznie wszędzie, nawet tam, gdzie jest to niepotrzebne. A miejsca, które najbardziej rażą, to na przykład tooltip przy najechaniu myszką na katalogi lub entity w katalogu świata, wpisy w zakładkach stworzeń, przedmiotów lub stanów, prawdopodobnie jakieś przyciski oraz inne elementy. Tooltip powinien być wyświetlany tam, gdzie jest faktycznie potrzebny, za długie nazwy oraz inne brzegowe przypadki.

    Dodatkowo działanie tooltipa powinno się również mechanicznie zmienić, ponieważ gdy najedziemy na jakiś element i tooltip się wyświetli, po najechaniu na kolejny aplikacja już nie czeka żadnego cooldownu, żeby wyświetlić drugi, robi to natychmiastowo, co jest błędne.

- Propozycja: Galeria Kontrolek również dostaje warmup.

# Biurko

## Katalog Świata

Naprawa katalogu świata skupiać się będzie nia wielu niezależnych poprawkach jakości:

- Katalog świata nie posiada płynnego grid-snappingu jak okienka, (jest natychmiastowy) - do naprawy
- Wszystkie foldery i pliki znajdujące się w katalogu świata mają owalne, lekko zaokrąglone rogi oraz przestrzeń pomiędzy dwoma kolejnymi katalogami lub plikami, ich margines wynosi zero, przez co dwa owalne rogi zaokrąglone ze sobą się stykają, co wygląda nieestetycznie.

    Rozwiązaniem może być usunięcie rogów lub zwiększenie marginesu. Proponuję tę pierwszą.
- Zmiana nazwy katalogów czy entity nie zaznacza pojawionego textfielda, I dopóki on się nie zaznaczy, nie jesteśmy w stanie anulować zmiany nazwy ani przez kliknięcie poza katalog, ani przez zaznaczenie innego entity czy katalogu, praktycznie nic.
- Przyciski chevronu lub plusika na katalogu lub pliku mają własny hover efekt oraz tło. Wygląda to nieestetycznie i daje wrażenie pudełka w pudełku. Proponuję zamiast tego zrobić jaśniejszy kolor ikonki lub bielszy z takim samym hitboxem, ale już bez tła, to znaczy staje się przezroczyste.
    
    Plusik moim zdaniem możemy kompletnie usunąć. On się pojawia tylko wtedy, kiedy najeżdżamy na konkretny plik lub katalog. Jest to niepotrzebne. Menu kontekstowe po naciśnięciu PPM jest wystarczające.
- Text field przy zmienianiu nazwy katalogu lub pliku jest źle zrobiony lub ma pewne niedociągnięcia. Mianowicie, i-beam kursor lub zaznaczenie może wchodzić lub nawet wychodzić poza ramkę text fieldu, co jest błędem. Również tekst zaczyna się dosłownie dotykając lub wchodząc na ramkę bez żadnego realnego odstępu. Sam text field jest prostokątem z w miarę dużymi zaokrągleniami, co niestety tworzy efekt pudełka w pudełku. Proponuję również usunąć zaokrąglenia.
- Raz zaznaczonego pliku lub katalogu nie da się odznaczyć. Jedynym sposobem jest zaznaczanie innego pliku lub katalogu. Kliknięcie poza kontener z katalogiem świata nie odznacza niczego. Normalnie nie byłoby to problematyczne, gdyby po uruchomieniu kampanii od razu coś było zaznaczone. Natomiast tak nie jest.

    Dodatkowo, odznaczanie entity powinno również w okienku podglądu wracać do empty zawartości, która nie wyświetla podglądu żadnego entity.

- Znak numeracji entity wraz z cyferką powinien się znajdować w lepszym miejscu. Obecnie znajduje się on zaraz po tytule, po nazwie przedmiotu czy stworzenia, z lekkim marginesem, co sprawia, że sama numeracja, porównując kilka wpisy, kilka entity w drzewie, numeracja występuje na różnych szerokościach. Ona powinna być w jednej kolumnie na jednej szerokości, nie zaraz po tytule entity, który ma dynamiczną szerokość.

Do tego możemy na razie usunąć tekst, który mówi o ilości punktów wytrzymałości danego stworzenia np 13/13. Wprowadzimy je prawdopodobnie jak zajdzie taka potrzeba.

## Okienka

- Okna oraz ich przyciski w pasku tytułowym, powinny być lepiej kategoryzowane i klasyfikowane. Na przykład okienko podglądu przedmiotu nie powinno być możliwe do zmaksymalizowania. Klasyfikacja powinna być prosta i trywialna. Na przykład: jeżeli okienko jest w stanie wyświetlić zawartości swojej zawartości na pełnym ekranie w przyzwoity sposób, to znaczy jest w stanie wyświetlić w taki sposób treść, żeby wypełnić w dobrym sposób cały ekran, to klasyfikuje się do tego, aby dostać ikonkę maksymalizacji.

    Okno trwałe, takie na przykład jak przyszłe śledzenie inicjatywy czy zegar lub inne okno tego typu, nie powinno być możliwe do zamknięcia, a jedynie zminimalizowania.

- Przyciski w pasku tytułowym okien mają hover background jako owalny kwadrat, co jest problematyczne. Przez to przyciski nie wypełniają całego dostępnego możliwego obszaru, jaki zajmują, a to wszystko właśnie przez te zaokrąglenia. Jeżeli przycisk znajduje się maksymalnie po prawej, a jak wiemy okno jest ma zaokrąglenia na rogach, to pierwszy przycisk od prawej powinien wypełniać cały dostępny obszar, jak byśmy wylali na niego farbę, czyli zrobili filler. Dwa obok siebie przyciski mają pionową ścianę, którą wypełniają po swojej stronie.

    *Nie umiałem tego lepiej wytłumaczyć. Podobnie zrobione jest to na listku w biurku, tylko oczywiście bez separatorów między przyciskami

- Manipulacja rozmiarem okienek na biurku również powinna być inteligentniej regulowana. To znaczy, na przykład ja mogę rozszerzać podgląd karty przedmiotu, mimo że jedyne co się pojawia to pusta przestrzeń. Takie kategorie okienek powinny być ręcznie regulowane. Karty przedmiotów, stworów czy w przyszłości ewentualnych zaklęć lub postaci czy graczy, które występują w formie wpisu, nie powinny mieć możliwości zmieniania szerokości, a jedynie wysokości. Co za tym idzie, maksymalizacja przy takich przypadkach również powinna być wyłączona.

### Naprawa/zmiana działania pinezki

Obecny system pineski przy podglądzie przedmiotu lub potwora polega w taki sposób, że kliknięcie pineski tworzy obok kolejne okno entity trwałe. To nie powinno tak działać. Kiedy klikamy pineskę, ikonka pineski zmienia swoją ikonkę na pineskę przypiętą i kliknięcie w kolejną przedmiot w katalogu świata tworzy nowe okno, ponieważ pierwsze zostało przypięte. Przy naciskaniu pineski żadne nowe okno się nie tworzy.

## Listek

Obecny kolor listka, dużo ciemniejszy niż reszta interfejsu, nie pasuje i zbyt bardzo odstaje. Mianowicie w żadnym miejscu w aplikacji nie występuje tak ciemny kolor, co wskazywałoby na to, że listek stoi ponad ramą, co jest nieprawdą. Powinniśmy zdegradować jego kolor do koloru ramy i tła okienek. To jest chyba ten sam kolor. Dodatkowo margines jest za mały, on powinien być nieco większy.

## Paleta komend/poleceń

- [Eksperymentalne] Obecny wygląd palety poleceń jest bardzo spójny ze stylem naszej aplikacji, natomiast chciałbym wprowadzić pewien redesign. Eksperymentalnie chciałbym wprowadzić design stylu glossy czy liquid glass znanego z Apple. Czyli jest to półprzezroczysty materiał, który w dużym skrócie rozmazuje interfejs znajdujący się za nim. Praca iteracyjna i designerska.
    Do tego dochodziłaby prawdopodobnie jakaś animacja wejścia tej palety, prawdopodobnie subtelny fade in oraz wjazd z góry na dół o dosłownie kilkanaście, kilkadziesiąt pikseli z animacją ease out. Jeżeli to nie będzie pasować już na papierze, no to możesz to (animacje) od razu odrzucić, żeby nie marnować czasu.
- Niektóre wpisy stworzeń czy przedmiotów mają problem z wyświetlaniem swojej zawartości właśnie w tej palecie poleceń. Mianowicie, jeżeli jakieś stworzenie ma za dużo tagów lub są za długie, to tagi, które są aligned do prawej strony i tak są tak długie, że wchodzą na tytuł, który jest aligned do lewej strony. Trzeba to jakoś rozwikłać. Jestem otwarty na propozycje, a nawet decyzję możesz podjąć w tej kwestii beze mnie.

# Przedmioty i potwory

- W spisie przedmiotów i potworów dzielą jeden problem. I've been kursor w miejscu tytułu pojawia się również w obszarze, w którym tytuł już się dawno skończył. Jest to błąd i powinien zostać rozwiązany.
- [Eksperymentalne] Sekcje takie jak opis, akcje, rzucanie czarów, akcje dodatkowe, akcje legendarne Dostaną zaraz po swoim tytule chevron, który działa dokładnie jak lista rozwijana. Po najechaniu na niego chevron zwiększa jasność lub staje się bardziej bielszy, tak aby podkreślić, że wjechaliśmy kursorem na obszar interakcji. Natomiast sam obszar obejmuje zarówno chevron, tytuł, jak i kolorowy pasek.