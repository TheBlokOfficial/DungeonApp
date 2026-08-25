# Kierunek wizualny i stabilność UI

> **Status:** zatwierdzony kierunek wizualny; szczegółowe tokeny komponentów pozostają do dopracowania — wersja 0.2

Normatywne wymiary i progi adaptacyjne: [Kontrakt UI](contract.md).

## Tożsamość interfejsu

> **Nowoczesne narzędzie, które wygląda, jakby należało do świata klasycznego cRPG.**

Ma przypominać: zwarty pulpit pracy MG-a, żywy almanach świata, panel instrumentów pokazujący wiarygodny stan kampanii. Nie ma przypominać: generycznego dashboardu SaaS, mobilnej aplikacji z miękkimi kafelkami, ani dosłownej imitacji pergaminu/drewna/ornamentów.

## Referencje wizualne

Zrzuty w [`docs/ui/images`](images/) to nie makiety do odtworzenia — każdy reprezentuje inną warstwę języka wizualnego.

| Referencja | Co przejmujemy | Czego nie kopiujemy |
| --- | --- | --- |
| Neverwinter Nights 2 | ciężar paneli, ciepła ciemna paleta, wyraziste ramy | półprzezroczystość, ciasnota, mała typografia |
| Obsidian (nowoczesny) | hierarchia wiedzy, zakładki, boczna nawigacja, dzielona przestrzeń | wizualna anonimowość, chłodny generyczny dark mode |
| Obsidian (surowy) | zwarte panele, ciepłe grafity, bursztynowe zaznaczenia | nadmiar ramek, mikroskopijne kontrolki |
| Foundry VTT / D&D Beyond | gęstość danych, stałe kolumny, powtarzalne wiersze | wygląd arkusza administracyjnego |
| Statblock | prezentacja pojedynczego Entity, redakcyjna hierarchia | traktowanie widoku do czytania jako formularza edycyjnego |

## Zasady estetyczne

**Proporcja:** orientacyjnie 75–80% współczesnego narzędzia użytkowego, 20–25% atmosfery klasycznego RPG. Klimat nie może obniżać czytelności ani szybkości pracy.

Oldschoolowy charakter wynika z: ciepłej przygaszonej palety, mocnych ram głównych obszarów, ostrzejszej geometrii, regularnych linii podziału, typografii nagłówków, oszczędnej ikonografii, bursztynowo-miedzianych akcentów, kontrolowanej gęstości informacji. Nie wynika z: ciężkich ilustracji, fotorealizmu, imitacji drewna, ornamentów, rastrowego „brudu".

**Hierarchia ramek:** mocna rama = główny kontekst/samodzielne narzędzie; cieńsza linia = sekcje wewnętrzne; zmiana powierzchni = informacje drugiego poziomu; drobne wartości nie dostają własnych dekoracyjnych pudełek. Nadmiar równorzędnych obramowań to błąd hierarchii nawet gdy pojedyncze komponenty wyglądają poprawnie.

**Geometria:** narożniki ostre lub lekko zaokrąglone (`0–4px`; większe wymaga uzasadnienia). Panele jak części jednej konstrukcji, nie swobodnie unoszące się karty. Cienie rzadkie. Zwartość ≠ mikroskopijne cele interakcji — gęstość danych i wygodny obszar klikania to osobne cechy.

## Deterministyczna geometria

> **Geometria zmienia się wyłącznie w odpowiedzi na jawną akcję użytkownika lub jawną zmianę trybu widoku — nigdy przypadkowo wskutek zmiany danych.**

Zmiana HP, komunikat, ikona, dłuższa nazwa Entity, nowy wpis historii — nic z tego nie przesuwa sąsiednich paneli. Dopuszczalne wyzwalacze zmiany layoutu: warianty dla zakresów wielkości okna, przeciągnięcie separatora, zadokowanie/ukrycie panelu, przełączenie trybu przygotowania/sesji, przywrócenie zapisanego układu. Po zmianie geometria znów jest stabilna — nie stosujemy ciągłego webowego reflow.

**Kontrakt głównego panelu:**

```text
┌──────────────────────────────┐
│ nagłówek — stała wysokość    │
├──────────────────────────────┤
│ treść — ograniczony viewport │
│ przewijany wewnętrznie       │
├──────────────────────────────┤
│ akcje/status — stały obszar  │
└──────────────────────────────┘
```

**Klasy powierzchni:** szkielet aplikacji (nawigacja, kontekst, workspace, inspektor, separatory, status — bezwzględnie stabilny) · panele narzędziowe (listy, harmonogram, historia — ograniczona geometria, własne przewijanie) · powierzchnie dokumentowe (notatki, statblocki — długa treść wyłącznie w stabilnym viewporcie) · nakładki (tooltipy, menu, powiadomienia — ponad layoutem, poza jego przepływem).

**Zabezpieczenia przed przesunięciami:** zarezerwowany slot ikony niezależnie od stanu ładowania; loading/empty/error/ready = ta sama geometria kontenera; statusy i walidacja nie wypychają formularza; wiersze danych mają stałą/ograniczoną wysokość; wartości mechaniczne używają cyfr tablicowych i przewidzianej szerokości; długie etykiety mają politykę skracania/zawijania/przewijania; miejsce na scrollbar jest uwzględnione w geometrii; placeholdery odpowiadają docelowemu slotowi; animacje nie zmieniają rozmiaru głównych obszarów.

## Typografia

Dwa głosy: bezszeryfowy dla kontrolek/nawigacji/tabel/liczb, spokojny szeryfowy dla nazw Entity, nagłówków kart i rozdziałów (ma przywoływać podręcznik RPG, pozostając czytelny — nie pseudośredniowieczny).

- **Inter** — baza bezszeryfowa, cyfry tablicowe, już używana w projekcie.
- **Alegreya** — krój display: sygnet `D`, nazwy ekranów, inicjały kampanii, nagłówki kart, powierzchnie redakcyjne.
- Spectral pozostaje udokumentowaną alternatywą, ale nie jest częścią aktywnego systemu.

Fonty dostarczane lokalnie z aplikacją, bez zależności od CDN czy fontów systemowych. Pliki licencyjne i informacja o pochodzeniu w repozytorium.

## Ikonografia

Ikony uzupełniają tekst, nie zastępują niejednoznacznych etykiet. Kierunek: jeden podstawowy zestaw monochromatycznych SVG, wspólny grid i grubość linii, ostre/neutralne zakończenia, kolor z tokenów, stałe sloty, lokalne przechowywanie tylko używanych ikon.

Podstawowy zestaw: **Lucide** (licencja ISC) — kuratorowany, lokalny podzbiór SVG, bez zależności runtime. Własne ikony domenowe dla pojęć TTRPG, których Lucide nie opisuje, muszą używać tego samego gridu i optycznej masy. Nie mieszamy bibliotek ikon w podstawowym UI; każdy zasób ma zapisane źródło, wersję i licencję.

## Tła i tekstury

Domyślnie płaskie, precyzyjnie dobrane powierzchnie — bez ciężkich rastrowych teł. Subtelna faktura dopuszczalna tylko jeśli: nie obniża kontrastu, nie konkuruje z liniami siatki, nie utrudnia kompresji/skalowania, nie tworzy widocznych powtórzeń, nie wpływa na geometrię/czas ładowania, jej brak nie zmienia czytelności. Ilustracje i portrety należą do danych kampanii/paczek zawartości, nie do chrome aplikacji.

## Jasne karty w ciemnym pulpicie

Shell jest ciemny i ciepły. Powierzchnie redakcyjne mogą docelowo mieć wariant ciemny (dopasowany do pulpitu) lub jasny/kremowy (charakter fizycznej karty) — decyzja otwarta, wymaga porównania na tej samej treści.

## Zatwierdzona paleta bazowa

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

Niewielkie korekty kontrastu są dopuszczalne (dostępność); temperatura i charakter palety — nie. Nie: czysta czerń, jaskrawy neon, chłodny fiolet, duże powierzchnie nasyconego koloru.

## Zasady pozyskiwania zasobów

Każdy zasób: ma jawne źródło i licencję pozwalającą na dystrybucję; przechowywany lokalnie, działa bez sieci; do repo trafia tylko potrzebny podzbiór z wersją; licencja zachowana w katalogu zasobów; nie jest jedynym nośnikiem znaczenia domenowego; jego brak/błąd nie zmienia geometrii widoku; aktualizacja biblioteki ikon/fontu jest świadomą zmianą wizualną, nie skutkiem ubocznym aktualizacji zależności. Nie kopiujemy grafik z gier/narzędzi wskazanych jako referencje — służą wyłącznie do opisania kierunku.

## Decyzje obowiązujące

- Stabilny, gridowy pulpit; zawartość nie zmienia geometrii głównych kontenerów samodzielnie.
- Styl konstrukcyjny: powierzchnie, ramy, linie, odstępy, typografia.
- Oldschoolowy charakter jako oprawa nowoczesnego narzędzia, nie dosłowna dekoracja fantasy.
- Shell ciemny, ciepły, zwarty, wyraziste obramowania.
- Zasoby lokalne, wersjonowane, udokumentowane licencje.
- Ikony: kuratorowany podzbiór Lucide, monochromatyczne, stałe sloty.
- Widok Entity jako czytelna karta/statblock — edycja nie dominuje nad odczytem.
- Znak aplikacji: szeryfowa litera `D` w cienkiej kwadratowej ramie (ikona pliku wykonywalnego wymaga osobnego opracowania).
- Ramki, sidebar, topbar, statusbar tworzą jedną zwartą konstrukcję, nie zestaw unoszących się kart.

## Decyzje otwarte

- Ostateczne tokeny kolorów, odstępów, obramowań, wymiarów.
- Ciemny kontra jasny statblock.
- Czy subtelna faktura daje korzyść bez pogorszenia czytelności.
- Zakres przyszłego dokowania i konfiguracji pulpitu.
- Finalna geometria znaku `D` i warianty ikony aplikacji w małych rozmiarach.

Decyzje podejmujemy przez porównanie tych samych realistycznych treści, nie przez izolowane próbki kolorów/fontów/ikon.
