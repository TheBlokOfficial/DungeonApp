# Specyfikacja - Katalog Świata

Katalog świata będzie przechowywał i zarządzał długotrwałą treścią kampanii/świata.
Obiektami treści będą Entities, uosabiane przez Potwory, Obiekty (np. skrzynie), NPC, Postacie Graczy, oraz wszystko co może mieć mechaniczną funkcje w grze. Opisy pokoi, czy budynków oraz wszeloraka wiedza bez funkcji mechanicznej się do tego nie zalicza.

## Podział zakładek

Istotnym do rozwiązania problemem przed implementacją katalogu świata jest podział zakładek, obecnie jedynie biurko.
Jest to problem przy katalogu zakładek, gdyż nie wiemy w jakiej formie i gdzie przedstawić ten katalog.

TTRPG oraz D&D naturalnie opiera się na trzech głównych filarach zabawy: Eksploracja, Interacja Społeczena oraz Walka.
Czy dzielimy przestrzeń roboczą DM'a, według tych trzech zakładek? (Osobiście bym odradzał). Czy biurko staje się głównym HUB'em gry?

Katalog Świata będzie na pewno używany przez wszystkie tryby na raz: Walka - śledzenie inicjatywy, Eksploracja - skrzynie oraz inne obiekty interakcyjne, Interakcja Społeczna - NPC z ekwipunkami lub sklepami.
To znaczy że Katalog Świata będzie potrzebny być dostępny wszędzie.

Jeżeli Biurko staje się HUB-em i jedynym miejscem gry, co rozwiązuje wielość występowania Katalogu Świata, to również mamy problem, ponieważ nie wiemy czy Initiative Tracker nie będzie wymagał indywidualnej pełnowymiarowej zakładki: cały panel + szczegółowy podgląd uczestników walki.

Koncepcja jeszcze do rozwiązania.

## Wygląd Katalogu Świata i działanie (na biurku)

Proponuje w formie embedded biurka, nie okienka jak inne narzędzia. Katalog istnieje jako panel bez własnego tła (background), pojedyńcze foldery oraz ich obiekty nie również go nie mają, chyba że siatka (Grid) biurka będzie utrudniał czytanie liter.

Rozmiar czcionki plików będzie mniejszy niż wiersz listy rozwijanej - kompaktowy styl. Foldery da się zwijać i rozwijać animowanym chevronem.

Foldery da się dodawać i usuwać; Pliki i foldery reagują na PPM i dostają menu kontekstowe (usuń; zmień nazwę; etc).

Rozwiązanie dodawania nowych obiektów z listy (dziesiątki potworów, NPC i obiektów). Propozycja (nie rozstrzygnięta) - Aktywacja akcji dodania wyświetla na środku ekranu w górnej części pływające okno kontekstowe z wyszukiwaniem (podobne do Ctrl+K na Macu). Dodatkowo rozstrzygnąć będzie trzeba jak przerzucać obiekty z katalogu na kolejke inicjatywy.

Pływające okno kontekstowe możemy dodać jako osobną funkcję/element interfejsu, który może służyć do wielu rzeczy. Wtedy trzeba by zasygnalizować że to pływające okno wyświetliło się w celu wyznczenia obiektu do katalogu (filtrowanie tylko valid wyszukiwań).

Minimalizm ikonkowy i zaznaczanie. Minimalistyczna ilość ikon przy folderach i plikach - ewentalny "+" na dodanie, bez koszy czy innych ikonek funkcyjnych - Od tego będzie służyć zaznaczenie (wielorazowe z SHIFT), menu kontekstowe PPM oraz przyciski takie jak DEL do usuwania.

Foldery mają przed swoją nazwą ikonke katalogu SVG, pliki muszą jakoś sygnalizować swoją kategorię poza swoją nazwą (stworzenie czy obiekt?) wedle funkcji którą pełnią dla mechanik - np stworzenie może uczestniczyć w walce.
