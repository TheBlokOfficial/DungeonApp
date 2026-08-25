# ADR-0002: Lokalny zapis kampanii v1

> **Status:** zaakceptowany jako kierunek v1; migracja obecnego PoC pozostaje do implementacji
> **Data:** 2026-08-24

## Kontekst

Obecny PoC zapisuje każdy agregat jako pojedynczy plik JSON w `%LocalAppData%/DungeonApp/campaigns`. Rozwiązanie potwierdziło poprawność adapterów i odtwarzania modułów, ale przed kolejnymi funkcjami potrzebny jest jawny kontrakt lokalizacji, wersjonowania, assetów i odzyskiwania danych.

Projekt jest lokalny, bez sieci — nie wymaga współbieżnego zapisu ani serwera bazodanowego.

## Decyzja

### Biblioteka użytkownika

Domyślna biblioteka kampanii w katalogu dokumentów użytkownika:

```text
Documents/DungeonApp/Campaigns/
```

Lokalizacja będzie konfigurowalna. Ustawienia aplikacji zostają w katalogu danych aplikacji właściwym dla platformy.

### Jednostka kampanii

Każda kampania ma własny katalog identyfikowany stabilnym `CampaignId`, niezależnym od edytowalnej nazwy:

```text
Campaigns/<campaign-id>/
  campaign.json
  assets/
  backups/
```

### Dokument v1

`campaign.json` jest pojedynczym, samowystarczalnym dokumentem stanu i historii:

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

`formatVersion` jest niezależny od wersji aplikacji. Prywatne stany modułów i payloady zdarzeń mają własne identyfikatory i wersje adapterów.

### Bezpieczny zapis

- Nowy dokument zapisywany do pliku tymczasowego w tym samym katalogu, zastępuje plik docelowy dopiero po poprawnej serializacji i walidacji.
- Poprzednia poprawna wersja może zostać zachowana jako rotacyjna kopia w `backups/`.
- Awaria zapisu nie może usunąć ostatniej poprawnej wersji.
- Pliki tymczasowe po awarii są rozpoznawane i sprzątane albo oferowane do odzyskania.

### Assety

Duże zasoby (ilustracje, portrety, mapy) w `assets/`; dokument odwołuje się do nich przez ścieżki względne lub stabilne identyfikatory. Brak assetu nie unieważnia stanu kampanii.

### Migracje

- Czytnik rozpoznaje `formatVersion` przed odtworzeniem domeny.
- Migracja jest jawna, testowalna, wykonywana na kopii; oryginał zachowany do potwierdzenia poprawnego otwarcia wersji zmigrowanej.
- Nieznana nowsza wersja formatu jest odrzucana z czytelnym komunikatem, bez częściowego odczytu.

## Dlaczego JSON

Kampania jest ładowana jako jeden agregat, zapis wykonuje jeden lokalny proces, format jest czytelny diagnostycznie, adaptery już wersjonują moduły i zdarzenia, nie ma potwierdzonej potrzeby złożonych zapytań po danych niezaładowanych do pamięci.

Snapshot i historia nie są rozdzielone na osobne pliki — utrudniłoby to atomowe zatwierdzenie jednej operacji.

## Konsekwencje

**Pozytywne:** kampania jest widocznym, przenośnym dokumentem użytkownika; katalog daje naturalne miejsce na assety i backupy; pojedynczy JSON ma prostą semantykę zatwierdzenia; `formatVersion` daje jawny punkt wejścia dla migracji.

**Negatywne:** każdy zapis przepisuje pełny dokument; rosnąca historia zwiększa koszt odczytu/zapisu; ręczna edycja JSON może złamać spójność; katalog kampanii jest mniej wygodny do przesłania niż pojedynczy plik (eksport będzie wymagał paczki archiwalnej).

## Sygnały do przejścia na SQLite albo inny magazyn

- Historia powoduje mierzalnie nieakceptowalny czas zapisu lub otwarcia.
- Potrzebne są zapytania po dużej liczbie encji bez ładowania całej kampanii.
- Pojawia się współbieżny zapis, indeksowanie w tle albo transakcja obejmująca wiele niezależnych agregatów.
- Migracje pojedynczego dokumentu stają się ryzykowne ze względu na jego wielkość.

Zmiana magazynu nie może zmienić portów aplikacji ani wprowadzić zależności trwałości do domeny.
