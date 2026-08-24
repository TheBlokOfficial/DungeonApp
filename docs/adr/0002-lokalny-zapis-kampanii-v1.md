# ADR-0002: Lokalny zapis kampanii v1

> **Status:** zaakceptowany jako kierunek v1; migracja obecnego PoC pozostaje do implementacji  
> **Data:** 2026-08-24

## Kontekst

Kampania jest wartościową zawartością użytkownika i źródłem prawdy o świecie. Obecny PoC zapisuje każdy agregat jako pojedynczy plik JSON w `%LocalAppData%/DungeonApp/campaigns`. Rozwiązanie potwierdziło poprawność adapterów i odtwarzania modułów, ale przed kolejnymi funkcjami potrzebny jest jawny kontrakt lokalizacji, wersjonowania, assetów i odzyskiwania danych.

Projekt jest lokalny i działa bez sieci. Nie wymaga współbieżnego zapisu przez wielu użytkowników ani serwera bazodanowego.

## Decyzja

### Biblioteka użytkownika

Domyślna biblioteka kampanii jest przechowywana w katalogu dokumentów użytkownika, w ścieżce odpowiadającej platformie:

```text
Documents/
  DungeonApp/
    Campaigns/
```

Lokalizacja biblioteki będzie konfigurowalna. Ustawienia samej aplikacji pozostają w katalogu danych aplikacji właściwym dla platformy.

### Jednostka kampanii

Każda kampania otrzymuje własny katalog identyfikowany stabilnym `CampaignId`:

```text
Campaigns/
  <campaign-id>/
    campaign.json
    assets/
    backups/
```

Nazwa katalogu nie zależy od edytowalnej nazwy kampanii.

### Dokument v1

`campaign.json` pozostaje pojedynczym, samowystarczalnym dokumentem stanu i historii:

```json
{
  "formatVersion": 1,
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Nazwa kampanii",
  "ruleset": null,
  "modules": [],
  "history": []
}
```

Dokument zawiera wersję formatu niezależną od wersji aplikacji. Prywatne stany modułów i payloady zdarzeń zachowują własne identyfikatory oraz wersje adapterów.

### Bezpieczny zapis

- Nowy dokument jest zapisywany do pliku tymczasowego w tym samym katalogu.
- Po poprawnym zakończeniu serializacji i walidacji zastępuje plik docelowy operacją filesystemu.
- Poprzednia poprawna wersja może zostać zachowana jako ograniczona rotacyjna kopia w `backups/`.
- Awaria zapisu nie może usuwać ostatniej poprawnej wersji.
- Pliki tymczasowe pozostawione po awarii są rozpoznawane i sprzątane albo oferowane do odzyskania.

### Assety

Ilustracje, portrety, mapy i inne duże zasoby kampanii są przechowywane w `assets/`, a dokument kampanii odwołuje się do nich przez ścieżki względne lub stabilne identyfikatory. Brak assetu nie unieważnia podstawowego stanu kampanii.

### Migracje

- Czytnik rozpoznaje `formatVersion` przed odtworzeniem domeny.
- Migracja jest jawna, testowalna i wykonywana na kopii.
- Oryginalny dokument jest zachowany do czasu potwierdzenia poprawnego otwarcia wersji zmigrowanej.
- Nieznana nowsza wersja formatu jest odrzucana z czytelnym komunikatem zamiast częściowego odczytu.

## Dlaczego JSON

W wersji v1 JSON pozostaje wystarczający, ponieważ:

- kampania jest ładowana jako jeden agregat;
- zapis wykonuje jeden lokalny proces;
- format jest czytelny diagnostycznie;
- istniejące adaptery wersjonują moduły i zdarzenia;
- nie ma jeszcze potwierdzonej potrzeby złożonych zapytań po danych niezaładowanych do pamięci.

Nie rozdzielamy teraz snapshotu i historii na osobne pliki, ponieważ utrudniłoby to atomowe zatwierdzenie jednej operacji.

## Konsekwencje

### Pozytywne

- Kampania jest widocznym dokumentem użytkownika, łatwym do skopiowania i archiwizacji.
- Katalog daje naturalne miejsce na assety i backupy.
- Pojedynczy JSON zachowuje prostą semantykę zatwierdzenia.
- `formatVersion` daje jawny punkt wejścia dla migracji.

### Negatywne

- Każdy zapis przepisuje pełny dokument.
- Rosnąca historia zwiększa koszt odczytu i zapisu.
- Ręczna edycja JSON może złamać spójność dokumentu.
- Katalog kampanii jest mniej wygodny do przesłania niż pojedynczy plik; eksport będzie wymagał paczki archiwalnej.

## Sygnały do przejścia na SQLite albo inny magazyn

- Historia powoduje mierzalnie nieakceptowalny czas zapisu lub otwarcia.
- Potrzebne są zapytania po dużej liczbie encji bez ładowania całej kampanii.
- Pojawia się współbieżny zapis, indeksowanie w tle albo transakcja obejmująca wiele niezależnych agregatów.
- Migracje pojedynczego dokumentu stają się ryzykowne ze względu na jego wielkość.

Zmiana magazynu nie może zmienić portów aplikacji ani wprowadzić zależności trwałości do domeny.
