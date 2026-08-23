# Wizja i fundamenty projektu

> **Status:** żywy dokument fazy zerowej  
> **Wersja:** 0.1  
> **Cel:** zachowanie wspólnego kierunku projektu przed podjęciem szczegółowych decyzji technicznych.

## 1. Jednozdaniowa definicja produktu

Tworzymy desktopową aplikację dla Mistrza Gry, która utrzymuje spójny stan kampanii, automatyzuje księgowość i konsekwencje reguł, pozostawiając grę przy stole tradycyjną: opartą na rozmowie, papierowych kartach postaci i fizycznych kościach.

## 2. Problem, który rozwiązujemy

Tradycyjne TTRPG potrafi wymagać dużej ilości ręcznych obliczeń oraz pamiętania o stanie świata: zasobach, ekwipunku, obrażeniach, efektach, czasie, NPC-ach, dostępności towarów i konsekwencjach wcześniejszych decyzji. Taka księgowość odbiera tempo oraz uwagę, które powinny być przeznaczone na przygodę i narrację.

Statyczne notatki są przydatne dla lore'u, ale nie stanowią aktywnego modelu świata. Nie potrafią samodzielnie stosować reguł, zmieniać stanu po upływie czasu ani wyjaśniać skutków zdarzeń.

Projekt ma przenieść na komputer właśnie tę część pracy: pamięć, liczenie, śledzenie stanu i symulowanie zdefiniowanych konsekwencji. Nie ma zastępować wyobraźni, rozmowy ani odpowiedzialności MG-a za prowadzenie sesji.

## 3. Granica produktu

### Produkt jest

- prywatnym panelem pracy MG-a;
- źródłem prawdy o zapisanym stanie kampanii;
- narzędziem do prowadzenia aktywnej sesji i długotrwałego świata;
- silnikiem wspierającym deterministyczne, wyjaśnialne stosowanie reguł;
- aplikacją desktopową działającą lokalnie i dyskretnie przy stole.

### Produkt nie jest

- VTT ani stołem online dla graczy;
- aplikacją, która wymaga od każdego gracza tabletu, konta lub własnej kopii programu;
- grą komputerową, silnikiem 3D czy symulatorem fizyki;
- wyłącznie bazą tekstowych notatek;
- arbitrem narracji zastępującym MG-a.

Gracze nadal posługują się papierowymi kartami, ołówkiem i gumką. Rzuty mogą pozostać fizyczne. Aplikacja śledzi ich skutki wtedy, gdy MG je wprowadzi lub potwierdzi.

## 4. Zasady projektowe

1. **Mechanika wspiera narrację.** Automatyzacja usuwa uciążliwe liczenie, aby przy stole zostały decyzje, wyobraźnia i tempo.
2. **Stan świata ma przyczynę.** Zmiana powinna wynikać z działania, upływu czasu, rzutu lub jawnej decyzji MG-a.
3. **System jest wyjaśnialny.** MG ma móc odtworzyć, co, kiedy i na jakiej podstawie zmieniło stan kampanii.
4. **MG pozostaje właścicielem świata.** Ręczne korekty i decyzje fabularne są dozwolone, ale powinny być świadomymi, zapisanymi zdarzeniami zamiast ukrytym omijaniem reguł.
5. **Dokładność nie może utrudniać sesji.** Interfejs musi być szybki, czytelny i odporny na typowe pomyłki przy stole.
6. **Brak sztucznego skalowania jako domyślnej zasady.** Świat reaguje na stan i reguły; przygotowanie oraz ryzyko graczy powinny mieć realne konsekwencje.

## 5. Autonomiczny, ale nie przesymulowany świat

Autonomiczność nie oznacza konieczności ciągłego symulowania wszystkiego. Oznacza, że istotne zmiany są konsekwentne:

```text
stan świata + upływ czasu + działania + wynik rzutu + reguły
    -> nowy stan świata + historia wyjaśniająca zmianę
```

Na przykład podróż może przesunąć zegar kampanii, wygasić efekt czasowy, uruchomić wcześniej zaplanowane zdarzenie albo zmienić dostępność zasobu. Zakres takich symulacji ma być kontrolowany przez kampanię i ruleset; nie zakładamy modelowania całego świata w jednakowej szczegółowości.

## 6. Core, ruleset i zawartość

Nie zamykamy produktu w ramach jednego istniejącego systemu, takiego jak D&D 5e. Nie oznacza to jednak budowy od pierwszego dnia bezkształtnego edytora dowolnych zasad.

Przyjmujemy trzy poziomy odpowiedzialności:

| Poziom | Odpowiedzialność | Przykłady |
| --- | --- | --- |
| **Core aplikacji** | stabilne mechanizmy niezależne od konkretnej mechaniki | kampania, czas, tożsamość encji, historia zdarzeń, zapisy, assety, wyszukiwanie, ładowanie modułów |
| **Ruleset** | znaczenie statystyk oraz konkretne reguły działania świata | walka, odporności, magia, progresja, obliczanie obrażeń, zasady ekwipunku |
| **Paczka zawartości** | dane używane przez konkretną kampanię lub przygodę | definicje bestii, przedmiotów, zaklęć, NPC-ów, kupców, łupów, lokacji |

### 6.1. Uniwersalne abstrakcje

Core może wprowadzać pojęcia takie jak `Entity` jako trwałą tożsamość obiektu w świecie, jego cykl życia, powiązania, zdarzenia i możliwość posiadania stanu. Taką encją może być postać, potwór, bestia, NPC albo inny obiekt wskazany przez ruleset.

`Entity` jest na tym etapie **pojęciem architektonicznym**, a nie przesądzoną klasą bazową, od której musi dziedziczyć cały model. Ostateczny kształt typów zostanie wybrany po pierwszych konkretnych przypadkach użycia. Nie należy zamieniać core'u w nieczytelną, uniwersalną tabelę atrybutów.

Analogicznie core może znać ogólny fakt istnienia definicji i instancji przedmiotu, ale nie określa na stałe, czym jest miecz, jego obrażenia albo zasady rzadkości.

### 6.2. Rulesety

Ruleset definiuje konkretne pojęcia i ich zachowanie. To on może powiedzieć, że w danej kampanii istnieją punkty życia, klasa pancerza, określone typy obrażeń, poziomy rzadkości albo czary. Powinien móc używać normalnych, czytelnych i typowanych modeli C#, zamiast ograniczać się do luźnych pól danych.

Pierwszy ruleset zostanie świadomie ograniczony i posłuży do weryfikacji core'u. Nie projektujemy uniwersalnej abstrakcji bez przetestowania jej na realnej mechanice.

### 6.3. Paczki zawartości

Paczka zawartości dostarcza dane kompatybilne z rulesetem. Przykładowo „drewniany miecz” jest definicją danych w paczce, a nie elementem zaszytym w core aplikacji. Kampania może tworzyć własne instancje tej definicji, które zmieniają stan: są posiadane, zużyte, uszkodzone, sprzedane lub zaczarowane.

Format paczek, sposób ich walidacji oraz decyzja, które rozszerzenia wymagają kodu, a które wystarczą jako dane, pozostają otwarte.

## 7. Model odpowiedzialności technicznych

Docelowy podział aplikacji:

```text
Frontend (Avalonia)
    -> Application
        -> Domain

Infrastructure
    -> implementuje porty wymagane przez Application/Domain
```

- **Frontend**: widoki, stan UI, nawigacja, bindingi i komendy użytkownika.
- **Application**: scenariusze użycia, orkiestracja operacji, kontrola transakcji i dostępu do danych.
- **Domain**: model świata, reguły, rozstrzyganie działań i zmiana stanu.
- **Infrastructure**: filesystem, serializacja, repozytoria, assety oraz szczegóły trwałości danych.

Warstwy `Application` i `Domain` nie zależą od Avalonia. Interfejs użytkownika wywołuje scenariusze użycia, a nie wykonuje bezpośrednio decyzji domenowych. Infrastruktura implementuje interfejsy potrzebne aplikacji, zamiast narzucać jej konkretne rozwiązanie zapisu.

MVVM jest techniką organizacji wyłącznie w warstwie Avalonia. ViewModele nie są miejscem dla reguł kampanii ani mechaniki świata.

## 8. Konsekwencje dla doświadczenia przy stole

Priorytetami interfejsu podczas sesji są:

- szybkie odczytywanie aktualnego stanu bez przekopywania się przez notatki;
- możliwie mało kliknięć oraz dobre skróty klawiszowe;
- bezpieczne wprowadzanie i korygowanie wyników;
- historia zmian i możliwość odzyskania się po błędzie;
- lokalne działanie bez konieczności połączenia z siecią;
- brak presji, aby gracze korzystali z aplikacji.

Pierwszy pionowy wycinek powinien sprawdzić ekran aktywnej sesji oraz przepływ: zmiana czasu, wykonanie działania, zastosowanie konsekwencji i zapisanie wyjaśnialnej historii.

## 9. Decyzje podjęte obecnie

- Aplikacja jest desktopowa i przeznaczona przede wszystkim dla MG-a.
- Preferowany stack: **C# + Avalonia**.
- Logika świata jest oddzielona od frontendu i możliwa do testowania bez Avalonia.
- Core nie zawiera na stałe konkretnych przedmiotów, stworzeń ani szczegółowych mechanik jednego systemu.
- Konkretne reguły i zawartość są rozdzielone od core'u.

## 10. Decyzje otwarte

- Jaki będzie pierwszy ruleset: prototypowy czy od razu zalążek autorskiego systemu?
- Jak daleko ma sięgać symulacja czasu i zdarzeń poza aktywną sesją?
- Jaki jest minimalny, użyteczny pionowy wycinek aktywnej sesji?
- Jaki będzie format save'ów, paczek zawartości i wersjonowania danych?
- Które elementy rulesetu są deklaratywnymi danymi, a które rozszerzeniami w kodzie?
- Czy i kiedy pojawi się eksport, podgląd lub synchronizacja danych dla graczy?

## 11. Kryterium oceny przyszłych decyzji

Przed zaakceptowaniem istotnej funkcji lub rozwiązania zadajemy pytanie:

> Czy pomaga ono MG-owi utrzymać szybką, przejrzystą i wiarygodnie konsekwentną sesję, bez odbierania graczom tradycyjnego charakteru gry?

Jeżeli odpowiedź nie jest wyraźnym „tak”, funkcję należy odłożyć, zawęzić albo ponownie uzasadnić.

