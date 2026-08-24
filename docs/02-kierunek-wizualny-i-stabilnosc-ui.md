# Kierunek wizualny i stabilność UI

> **Status:** zatwierdzony kierunek wizualny; szczegółowe tokeny komponentów pozostają do dopracowania  
> **Wersja:** 0.2  
> **Cel:** utrzymanie spójnego języka wizualnego i deterministycznej geometrii interfejsu podczas kolejnych sesji implementacyjnych.

Konkretne wymiary, progi adaptacyjne i polityki wzrostu paneli definiuje dokument [Kontrakt UI v1](05-kontrakt-ui-v1.md).

## 1. Tożsamość interfejsu

DungeonApp ma łączyć oldschoolowy charakter klasycznych komputerowych RPG z ergonomią współczesnego narzędzia do pracy z wiedzą i gęstymi danymi mechanicznymi.

Najkrótsza definicja kierunku brzmi:

> **Nowoczesne narzędzie, które wygląda, jakby należało do świata klasycznego cRPG.**

Interfejs ma przypominać jednocześnie:

- zwarty, solidny pulpit pracy Mistrza Gry;
- żywy almanach świata;
- panel instrumentów pokazujący wiarygodny stan kampanii.

Nie ma przypominać generycznego dashboardu SaaS, mobilnej aplikacji z miękkimi kafelkami ani dosłownej imitacji pergaminu, drewna i średniowiecznych ornamentów.

## 2. Rola referencji wizualnych

Zrzuty w katalogu [`docs/images`](images/) nie są makietami do bezpośredniego odtworzenia. Każdy z nich reprezentuje inną warstwę docelowego języka wizualnego.

| Referencja | Element przejmowany przez DungeonApp | Elementy, których nie należy kopiować dosłownie |
| --- | --- | --- |
| Neverwinter Nights 2 | ciężar paneli, ciepła ciemna paleta, wyraziste ramy, surowy klimat klasycznego RPG | półprzezroczystość na szczegółowym tle, ciasnota, mała typografia, dekorowanie każdego elementu |
| Obsidian — nowoczesny układ | hierarchia wiedzy, zakładki, boczna nawigacja, dzielona przestrzeń robocza, czytelność dokumentów | wizualna anonimowość i chłodny, generyczny dark mode |
| Obsidian — wariant surowy | zwarte panele, ciepłe grafity, bursztynowe zaznaczenia, narzędziowy charakter | nadmiar równorzędnych ramek, mikroskopijne kontrolki i zagnieżdżone menu |
| Foundry VTT / D&D Beyond | gęstość danych, stałe kolumny, powtarzalne wiersze, szybkie porównywanie wartości | wygląd arkusza administracyjnego i słabą hierarchię wizualną |
| Statblock | prezentacja pojedynczego Entity, redakcyjna hierarchia, szybki dostęp do kluczowych parametrów | traktowanie widoku do czytania jako stałego formularza edycyjnego |

Oldschoolowa oprawa, nowoczesny układ, narzędziowa gęstość danych i redakcyjna karta Entity pełnią różne role. Nie należy uśredniać ich na poziomie każdego komponentu.

## 3. Zasady estetyczne

### 3.1. Atmosfera podporządkowana użyteczności

Orientacyjna proporcja to 75–80% współczesnego narzędzia użytkowego i 20–25% atmosfery klasycznego RPG. Klimat może być wyrazisty, ale nie może zmniejszać czytelności ani szybkości pracy przy stole.

Oldschoolowy charakter powinien wynikać przede wszystkim z:

- ciepłej, przygaszonej palety;
- mocnych ram głównych obszarów;
- ostrzejszej geometrii;
- regularnych linii podziału;
- typografii nagłówków;
- oszczędnej ikonografii;
- bursztynowych lub miedzianych akcentów;
- wysokiej, lecz kontrolowanej gęstości informacji.

Nie powinien wynikać z ciężkich ilustracji, fotorealistycznych materiałów, imitacji drewna, ornamentów w narożnikach ani rastrowego „brudu” pod tekstem.

### 3.2. Hierarchia ramek

Ramki nie mogą mieć wszędzie tej samej siły.

- Mocna rama wyznacza główny kontekst lub samodzielne narzędzie.
- Cieńsza linia oddziela sekcje wewnętrzne.
- Zmiana powierzchni grupuje informacje drugiego poziomu.
- Drobne wartości i pojedyncze etykiety nie otrzymują własnych dekoracyjnych pudełek bez wyraźnej potrzeby.

Nadmiar równorzędnych obramowań jest traktowany jako błąd hierarchii, nawet jeśli pojedyncze komponenty wyglądają poprawnie.

### 3.3. Geometria

Preferowane są narożniki ostre albo tylko lekko zaokrąglone. Docelowy zakres promienia to orientacyjnie `0–4 px`; większe promienie wymagają świadomego uzasadnienia.

Panele mają wyglądać jak części jednej konstrukcji, a nie jak swobodnie unoszące się karty. Cienie powinny być rzadkie. Podstawowymi narzędziami hierarchii są powierzchnia, rama, linia podziału, odstęp i typografia.

Zwartość nie oznacza mikroskopijnych celów interakcji. Gęstość danych i wygodny obszar klikania są osobnymi cechami.

## 4. Deterministyczna geometria i zakaz przypadkowego layout shiftu

Najważniejsza zasada zachowania interfejsu brzmi:

> **Geometria może zmieniać się wyłącznie w odpowiedzi na jawną akcję użytkownika lub jawną zmianę trybu widoku — nigdy przypadkowo wskutek zmiany danych.**

Zmiana HP, pojawienie się komunikatu, załadowanie ikony, dłuższa nazwa Entity albo nowy wpis historii nie mogą przesuwać sąsiednich paneli ani zmieniać proporcji pulpitu.

Stabilność nie oznacza jednego sztywnego układu pikselowego dla wszystkich ekranów. Dopuszczalne są:

- jawne warianty układu dla określonych zakresów wielkości okna;
- przeciągnięcie separatora przez użytkownika;
- zadokowanie lub ukrycie panelu;
- przełączenie między trybem przygotowania i aktywnej sesji;
- przywrócenie zapisanego układu pulpitu.

Po każdej takiej zmianie geometria ponownie staje się stabilna. Nie stosujemy ciągłego, webowego reflow zależnego od długości bieżącej zawartości.

### 4.1. Kontrakt głównego panelu

Główny panel powinien składać się z przewidywalnych stref:

```text
┌──────────────────────────────┐
│ nagłówek — stała wysokość    │
├──────────────────────────────┤
│                              │
│ treść — ograniczony viewport │
│ przewijany wewnętrznie       │
│                              │
├──────────────────────────────┤
│ akcje/status — stały obszar  │
└──────────────────────────────┘
```

Zawartość dostosowuje się do przestrzeni wewnątrz panelu. Nie może samodzielnie powiększać panelu i wypychać sąsiednich obszarów.

### 4.2. Klasy powierzchni

1. **Szkielet aplikacji** — nawigacja, kontekst kampanii, workspace, inspektor, separatory i status. Jest bezwzględnie stabilny.
2. **Panele narzędziowe** — listy, harmonogram, historia, efekty i akcje. Mają ograniczoną geometrię i własne przewijanie.
3. **Powierzchnie dokumentowe** — notatki, opisy, statblocki i historia Entity. Mogą mieć naturalnie długą treść, ale wyłącznie wewnątrz stabilnego viewportu.
4. **Nakładki** — tooltipy, menu, podglądy i powiadomienia. Pojawiają się ponad układem i nie uczestniczą w jego przepływie.

### 4.3. Obowiązkowe zabezpieczenia przed przesunięciami

- Ikony zawsze mają zarezerwowany slot, także podczas ładowania i przy braku zasobu.
- Loading, empty state, błąd i poprawna zawartość zachowują tę samą geometrię kontenera.
- Statusy oraz walidacja nie mogą pojawiać się jako nieograniczone bloki wypychające formularz.
- Wiersze danych mają stałą albo jawnie ograniczoną wysokość.
- Wartości mechaniczne używają cyfr tablicowych i przewidzianej szerokości.
- Długie etykiety mają ustaloną politykę skracania, zawijania lub przewijania.
- Miejsce na pasek przewijania jest uwzględnione w geometrii.
- Treści asynchroniczne otrzymują placeholder dokładnie odpowiadający docelowemu slotowi.
- Animacje nie mogą zmieniać rozmiaru głównych obszarów roboczych.

## 5. Typografia

Docelowo interfejs powinien mieć dwa uzupełniające się głosy:

- krój bezszeryfowy zoptymalizowany dla ekranu — kontrolki, nawigacja, tabele, liczby i dłuższa praca narzędziowa;
- spokojny krój szeryfowy — nazwy Entity, nagłówki kart, rozdziały i redakcyjne powierzchnie dokumentowe.

Krój szeryfowy nie może być pseudośredniowieczny ani nadmiernie dekoracyjny. Ma przywoływać podręcznik RPG i drukowaną kartę, pozostając współczesnym oraz czytelnym.

Bazą bezszeryfową pozostaje **Inter**, który jest już używany w projekcie. Wspiera cyfry tablicowe i został zaprojektowany do pracy ekranowej. Krojem display jest **Alegreya**: obsługuje sygnet `D`, nazwy ekranów, inicjały kampanii, nagłówki kart i przyszłe powierzchnie redakcyjne. Jej humanistyczny rys przywraca lekko fantasy charakter mockupu bez użycia dekoracyjnego kroju pseudośredniowiecznego. Spectral pozostaje udokumentowaną alternatywą, ale nie jest częścią aktywnego systemu typograficznego.

Fonty muszą być dostarczane lokalnie z aplikacją. Interfejs nie może zależeć od połączenia z CDN ani od fontów zainstalowanych w systemie. Do repozytorium należy dołączyć odpowiednie pliki licencyjne i informację o pochodzeniu.

## 6. Ikonografia

Ikony są uzupełnieniem tekstu i punktami orientacyjnymi. Nie zastępują niejednoznacznych etykiet tylko po to, aby zmniejszyć liczbę napisów.

Preferowany kierunek:

- jeden podstawowy zestaw monochromatycznych ikon SVG;
- wspólny grid źródłowy i jednolita grubość linii;
- ostre lub neutralne zakończenia, bez miękkiego „mobilnego” charakteru;
- kolor nadawany przez tokeny interfejsu;
- stałe sloty, niezależne od rzeczywistego obrysu ikony;
- lokalne przechowywanie wyłącznie używanych ikon;
- własne symbole domenowe dopiero wtedy, gdy zestaw ogólny nie wyraża pojęcia TTRPG.

Podstawowym zestawem ikon jest **Lucide**: jego monochromatyczna, lekka geometria została zaakceptowana w makiecie layoutu i pasuje do narzędziowego charakteru aplikacji. Zestaw ma licencję ISC. Do aplikacji trafia kuratorowany, lokalny podzbiór SVG, a nie zależność pobierająca zasoby w czasie działania.

Własne ikony domenowe mogą powstać dla pojęć TTRPG, których Lucide nie opisuje. Muszą używać tego samego gridu, optycznej masy i sposobu prowadzenia linii. Nie należy zastępować całego zestawu inną biblioteką bez ponownego przeglądu spójności wizualnej.

Nie należy mieszać ikon z kilku bibliotek w podstawowym UI. Każdy wykorzystany lub zmodyfikowany zasób musi mieć zapisane źródło, wersję i licencję.

## 7. Tła, tekstury i grafiki

Domyślnym rozwiązaniem pozostają płaskie, precyzyjnie dobrane powierzchnie. Ciężkie rastrowe tła nie są częścią kierunku wizualnego.

Dopuszczalna jest bardzo subtelna faktura, jeżeli:

- nie obniża kontrastu tekstu;
- nie konkuruje z liniami siatki;
- nie utrudnia kompresji ani skalowania;
- nie tworzy widocznych powtórzeń;
- nie wpływa na geometrię i czas pojawienia się zawartości;
- jej brak nie zmienia czytelności ani znaczenia ekranu.

Preferowana jest faktura generowana proceduralnie lub bardzo mały, lokalny i bezszwowy zasób. Powinna być praktycznie niewidoczna podczas pracy, a zauważalna dopiero przy świadomym oglądaniu dużej pustej powierzchni.

Ilustracje i portrety należą do danych kampanii albo paczek zawartości, nie do podstawowego chrome aplikacji. Ich ładowanie zawsze odbywa się we wcześniej zarezerwowanym obszarze.

## 8. Jasne karty wewnątrz ciemnego pulpitu

Podstawowy shell jest ciemny i ciepły. Powierzchnie redakcyjne mogą w przyszłości otrzymać jeden z dwóch wariantów:

- ciemny statblock dopasowany do pulpitu;
- jasna, kremowa karta dokumentu osadzona wewnątrz ciemnej konstrukcji.

Jasny wariant może nadać Entity charakter fizycznej karty lub strony podręcznika, ale nie powinien być stosowany do każdego panelu. Decyzja pozostaje otwarta do porównania obu wariantów na tej samej treści.

## 9. Zatwierdzona paleta bazowa

Poniższe wartości pochodzą z zaakceptowanej makiety i stanowią bazę dla tokenów produkcyjnych. Dopuszczalne są niewielkie korekty kontrastu podczas testów dostępności, ale nie zmiana temperatury ani ogólnego charakteru palety.

```text
Background          #121313
Surface             #181918
Surface header      #1E1E1C
Surface active      #2B251D
Border              #4A443C
Border strong       #5A5146
Text primary        #F1E7D7
Text regular        #BBB4AA
Text muted          #8E877E
Accent              #B47C36
Accent bright       #D19A52
Accent action       #8B5727
Success             #84A98B
Warning             #CF9B55
Danger              #C76767
```

Paleta ma być ciepła i stonowana. Czysta czerń, jaskrawy neon, chłodny fiolet typowy dla aplikacji produktywnościowych oraz duże powierzchnie nasyconego koloru nie są domyślnym kierunkiem.

## 10. Zasady pozyskiwania i utrzymania zasobów

Każdy zasób trafiający do aplikacji musi spełnić wszystkie poniższe warunki:

1. Ma jawne źródło i licencję pozwalającą na dystrybucję z aplikacją.
2. Jest przechowywany lokalnie i działa bez sieci.
3. Do repozytorium trafia tylko potrzebny podzbiór wraz z informacją o wersji.
4. Licencja i wymagane noty zostają zachowane w katalogu zasobów lub zbiorczym pliku informacji o komponentach zewnętrznych.
5. Zasób nie może być jedynym nośnikiem znaczenia domenowego.
6. Brak albo błąd zasobu nie może zmieniać geometrii widoku.
7. Aktualizacja biblioteki ikon lub fontu jest świadomą zmianą wizualną, a nie automatycznym skutkiem aktualizacji zależności.

Nie kopiujemy grafik ani ikon z gier i komercyjnych narzędzi wskazanych jako referencje. Służą one wyłącznie do opisania kierunku.

## 11. Decyzje obowiązujące

- Interfejs używa stabilnego, gridowego pulpitu.
- Zawartość nie może samodzielnie zmieniać geometrii głównych kontenerów.
- Styl jest konstrukcyjny: opiera się na powierzchniach, ramach, liniach, odstępach i typografii.
- Oldschoolowy charakter jest oprawą nowoczesnego narzędzia, a nie dosłowną dekoracją fantasy.
- Podstawowy shell jest ciemny, ciepły, zwarty i ma wyraziste obramowania.
- Ciężkie grafiki i rastrowe tła nie należą do podstawowego chrome aplikacji.
- Zasoby są lokalne, wersjonowane i mają udokumentowane licencje.
- Ikony pochodzą z kuratorowanego podzbioru Lucide, są monochromatyczne i osadzone w stałych slotach.
- Widok Entity ma charakter czytelnej karty/statblocku; edycja nie powinna stale dominować nad odczytem.
- Bazowa paleta jest zgodna z zaakceptowaną makietą: ciepłe grafity, kremowa typografia i bursztynowo-miedziany akcent.
- Koncept znaku aplikacji to szeryfowa litera `D` zamknięta w cienkiej kwadratowej ramie. Znak może być używany w chrome aplikacji; finalna ikona pliku wykonywalnego wymaga osobnego opracowania i sprawdzenia w małych rozmiarach.
- Ramki, sidebar, pasek górny i pasek statusu tworzą jedną zwartą konstrukcję zamiast zestawu unoszących się kart.

## 12. Decyzje otwarte

- Ostateczne tokeny kolorów, odstępów, obramowań i wymiarów.
- Ciemny kontra jasny statblock wewnątrz ciemnego shellu.
- Czy subtelna faktura daje dostrzegalną korzyść bez pogorszenia czytelności.
- Zakres przyszłego dokowania i zestaw dozwolonych konfiguracji pulpitu.
- Finalna geometria znaku `D` oraz warianty ikony aplikacji dla małych rozmiarów.

Decyzje te powinny zostać podjęte przez porównanie tych samych realistycznych treści, a nie przez ocenę izolowanych próbek kolorów, fontów lub ikon.
