# ADR-0001: Utrzymanie czterech projektów warstwowych

> **Status:** zaakceptowany  
> **Data:** 2026-08-24

## Kontekst

Należało rozstrzygnąć, czy podział na `DungeonApp.Domain`, `DungeonApp.Application`, `DungeonApp.Infrastructure` i `DungeonApp.Desktop` jest w obecnej skali użyteczną granicą, czy zbędnym narzutem. Domena kampanii, port repozytorium, lokalna trwałość JSON i frontend Avalonia mają różne powody do zmiany i różne wymagania testowe.

## Decyzja

Utrzymujemy cztery projekty produkcyjne:

```text
Desktop -> Application -> Domain
Desktop -> Infrastructure -> Application
Infrastructure -> Domain
```

- `Domain` zawiera model świata, reguły, moduły, komendy i fakty domenowe.
- `Application` zawiera scenariusze użycia, modele odczytu oraz porty wymagane przez aplikację.
- `Infrastructure` implementuje trwałość, serializację, filesystem i inne adaptery techniczne.
- `Desktop` zawiera Avalonia, MVVM, nawigację, composition root i zasoby wizualne.

MVVM jest wzorcem organizacji `Desktop`, a nie dodatkową warstwą domenową.

## Ograniczenia decyzji

Podział na projekty nie uzasadnia dodawania zbędnej ceremonii. Bez konkretnej potrzeby nie wprowadzamy:

- MediatR ani własnego frameworka komend;
- generycznych repozytoriów;
- osobnego projektu dla każdego modułu;
- automatycznych mapperów ukrywających kontrakty;
- rozbudowanego kontenera DI;
- abstrakcji tworzonych wyłącznie na potrzeby hipotetycznej wymienności.

Moduły funkcjonalne mogą pozostawać w jednym assembly domenowym. Modułowość nie oznacza jeszcze dynamicznych pluginów binarnych.

## Konsekwencje

### Pozytywne

- Reguły świata są testowane bez Avalonia i filesystemu.
- UI nie podejmuje decyzji domenowych.
- Format zapisu może ewoluować bez uzależniania domeny od JSON.
- Composition root ma jednoznaczne miejsce w `Desktop`.
- Granice zależności są widoczne już na poziomie plików projektu.

### Negatywne

- Nawet mała funkcja może wymagać zmian w kilku projektach.
- Modele odczytu i mapowanie tworzą dodatkowy kod.
- Należy pilnować, aby `Application` nie stało się zbiorem mechanicznych wrapperów bez wartości.

## Sygnały do ponownego rozpatrzenia

- Projekty pozostają prawie puste przez wiele pionowych wycinków.
- Większość zmian zawsze dotyka wszystkich warstw bez realnego rozdzielenia odpowiedzialności.
- Pojawia się potrzeba osobnego procesu, SDK modułów albo dystrybucji rulesetów w kodzie.

Do tego czasu cztery projekty są obowiązującą strukturą solucji.
