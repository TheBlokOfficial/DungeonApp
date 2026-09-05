# Instrukcja na następną sesję — kroki 3 i 4

Plik tymczasowy. Kasujemy go, gdy blok 3 się domknie.

## Gdzie jesteśmy

Blok 3 (fundament danych + pionowa próbka) jest w połowie. Zrobione:

- **Krok 1** (`5e76642`) — fundament bloków danych w `src/DungeonApp.Core/DataBlocks/`.
- **Krok 2** (`fcefe5d`) — stary mechanizm modułów usunięty, zastąpiony rejestrem bloków
  danych; zapis przeszedł na `datablocks/<nazwa>.json`.

Zostało: **krok 3** (narzędzie), **krok 4** (okno), **krok 5** (dokumentacja).

Stan wyjściowy: build 0 błędów / 0 ostrzeżeń, Core 123 testy, Desktop 3 testy — wszystko
zielone. Rejestr bloków danych w `App.axaml.cs` jest **pusty** i to jest poprawny stan.

Aplikacja nie ma dziś ani jednego narzędzia ani okna. Po kroku 4 pierwszy raz od przebudowy
kliknięcie w interfejsie cokolwiek zmieni.

## Decyzje, pod które się pracuje

Pełny opis w `docs/vision.md`. Tu skrót tego, co bezpośrednio wiąże kroki 3–4:

1. **Słownik: narzędzie, dane, okno.** Słowo „moduł" nie istnieje.
2. **Narzędzie nie zależy od innego narzędzia.** Dwa kanały pośrednie: wspólne dane
   („jak jest") i magistrala zdarzeń („co się stało").
3. **Narzędzie jest bezstanowe** i deklaruje wyłącznie, których bloków danych używa.
4. **Zapis to przekształcenie, nie gotowa treść.** Narzędzie oddaje funkcję nad wartością;
   rdzeń stosuje ją do wartości bieżącej, sprawdza zgodność z kształtem i ogłasza zmianę.
5. **Okno czyta dane wprost**, nie przez narzędzie. Zgłasza narzędziu nazwany zamiar.
   Po ogłoszeniu zmiany odczytuje ponownie. Zdarzenie nigdy nie niesie wartości.
6. **Narzędzie żyje w rdzeniu**, nie w warstwie interfejsu — bo narzędziem jest to, co
   zostaje po zabraniu okna i co da się przetestować bez okna.
7. **Rdzeń nie wie nic o UI.** Dostęp do dysku wyłącznie w warstwie zapisu rdzenia.

## API, na którym się buduje

Wszystko w `namespace DungeonApp.Core.DataBlocks`:

```csharp
DataBlockId.Create(string)                                  // walidowany identyfikator
new DataBlockRegistry().Register(id, version, shape)        // zwraca rejestr (łańcuchowo)

// kształt
new PrimitiveShape(PrimitiveKind.Integer | Fractional | Text | Boolean)
new FieldShape(string name, DataBlockShape type)
new ObjectShape(IReadOnlyList<FieldShape> fields)

// wartości kampanii
object? CampaignDataBlocks.Read(DataBlockId)                // null = jeszcze nic tu nie ma
void    CampaignDataBlocks.Apply(DataBlockId, Func<object?, object> transform)
bool    CampaignDataBlocks.IsUnreadable(DataBlockId)
IReadOnlyCollection<UnreadableDataBlock> UnreadableBlocks

// zdarzenie
sealed record DataBlockChanged(DataBlockId Id) : ICampaignEvent
```

Uwagi, które oszczędzą czasu:

- Wartość bloku o kształcie `ObjectShape` to **słownik `string → object`**, nie typ CLR.
  Po zapisie wraca zamrożona (`ImmutableDictionary`).
- Liczby są **znormalizowane**: `Integer` → `long`, `Fractional` → `double`. Odczytując
  wartość liczbową, rzutuj na `long`, nie na `int`.
- `Read` na bloku nigdy niezapisanym zwraca `null` — narzędzie musi to obsłużyć.
- `Read`/`Apply` na bloku nieznanym rejestrowi albo nieczytelnym **rzucają**.

## Krok 3 — narzędzie licznika

Cel: bezstanowe narzędzie w rdzeniu, operujące na jednym bloku danych, plus rejestracja
w korzeniu kompozycji.

Powstaje:

1. **Blok danych próbki** — identyfikator `counter`, wersja 1, kształt: obiekt z jednym
   polem całkowitym (np. `count`). Umieść jego definicję tam, gdzie naturalnie należy do
   narzędzia, które go używa — nie w `App.axaml.cs`; w korzeniu kompozycji ma zostać samo
   wywołanie rejestracji.
2. **Kontrakt narzędzia** — minimalny interfejs niosący wyłącznie deklarację użycia
   (`IReadOnlyList<DataBlockId> Uses`). Nic więcej: bez aktywacji, bez cyklu życia, bez
   dostępu do innych narzędzi.
3. **Narzędzie licznika** — bezstanowe, wystawia dwie operacje zwracające **przekształcenia**
   (`Func<object?, object>`), nie wykonujące zapisu samodzielnie. Zwiększenie licznika
   zastosowane do `null` ma dać wartość 1 — blok niezapisany jest normalnym stanem
   wyjściowym, nie błędem.
4. **Rejestracja w `App.axaml.cs`** — wpisana wprost, widoczna w diffie: rejestracja bloku
   w rejestrze plus sprawdzenie, że każdy blok zadeklarowany przez narzędzie jest znany
   rejestrowi. Bez katalogu narzędzi — jedno narzędzie go nie uzasadnia; katalog powstanie
   przy drugim.

Testy: przekształcenie zwiększające zastosowane do `null` daje 1; zwiększenie i zmniejszenie
wracają do wartości wyjściowej; deklaracja użycia wskazuje dokładnie blok licznika;
przekształcenie przepuszczone przez `Apply` faktycznie zmienia wartość i ogłasza zmianę.

Po tym kroku nic jeszcze nie wywołuje narzędzia z interfejsu — build i testy mają być zielone.

## Krok 4 — okno na blacie

Cel: pierwsze okno od czasu przebudowy, domykające pętlę.

Zanim cokolwiek napiszesz, **przeczytaj istniejący mechanizm blatu**:
`src/DungeonApp.Desktop/Features/CampaignWorkspace/` (zwłaszcza `Panels/PanelCatalog.cs`,
`Panels/WorkspacePanelDescriptor.cs`, `Deck/PanelDeckView.axaml.cs`,
`CampaignWorkspaceView.axaml`). Mechanizm jest gotowy i **nie wymaga zmian** — okno wpina
się w istniejący kontrakt. `PanelCatalog.For` zwraca dziś pustą listę; to jedyne miejsce,
w którym okno ma się zgłosić.

Powstaje:

1. **ViewModel okna** — czyta wartość bloku wprost z kampanii (nie przez narzędzie),
   trzyma narzędzie licznika, wystawia dwie komendy. Komenda nie zmienia wartości sama:
   bierze przekształcenie z narzędzia i oddaje je do jedynego wejścia zapisu.
2. **Subskrypcja `DataBlockChanged`** — po ogłoszeniu zmiany okno **odczytuje blok ponownie**.
   Nie bierze wartości ze zdarzenia; zdarzenie jej nie niesie.
3. **Widok** — dwa przyciski i wyświetlona wartość. Wyłącznie style i tokeny z `Themes/`,
   zero magicznych liczb, odstępy ze skali `DungeonSpacing*`/`DungeonPadding*`. Brak
   potrzebnej wartości w skali jest zgłoszeniem, nie powodem do wpisania liczby.
4. **Zgłoszenie w `PanelCatalog.For`** i szablon widoku w `CampaignWorkspaceView.axaml`.

Stan pusty ma być obsłużony: blok nigdy niezapisany daje `null`, a okno musi wtedy pokazać
sensowną wartość początkową, nie wywrócić się i nie pokazać pustki.

Weryfikacja końcowa: uruchom aplikację, otwórz kampanię, kliknij oba przyciski, zamknij
i otwórz aplikację ponownie — wartość ma przetrwać. To jest pierwszy raz, gdy pętla
okno → narzędzie → dane → okno działa w całości, i jedyny sposób, żeby to sprawdzić.

## Krok 5 — dokumentacja

Jedno przejście na końcu bloku, nie po każdym commicie:

- `docs/architecture.md` — sekcje opisujące kontrakt modułu, zależności i kolejność
  ładowania, komunikację między modułami oraz stan i zapis **opisują mechanizm, którego już
  nie ma**. Do przepisania pod bloki danych. Tabela „Bilans egzekwowania" też.
- `docs/code-map.md` — weryfikacja w całości, twierdzenie po twierdzeniu, nie łatanie diffu.
- Do dopisania w `docs/vision.md`, jeśli jeszcze tam nie trafiło: **blok nieczytelny** —
  czego nie umiemy odczytać, to zachowujemy i zgłaszamy; plik i wpis w manifeście
  przeżywają kolejne zapisy nietknięte; zapis do takiego bloku jest odrzucany.

## Otwarte, świadomie odłożone

- **Ścieżki migracji** — rejestr zna wersję, ale migracji nie ma. Dopisać przy pierwszym
  realnym podniesieniu wersji bloku.
- **Reprezentacja wartości jako worek pól** zamiast typu CLR — zgodne z zasadą „rdzeń zna
  budowę, nie znaczenie", ale narzędzia płacą za to wygodą pisania. Wrócić, gdy zaboli
  przy pierwszym prawdziwym narzędziu; przy liczniku nie zaboli.
- **Katalog narzędzi** — przy drugim narzędziu.
- **Zagnieżdżanie i referencje w kształcie** — hierarchia `DataBlockShape` jest na to
  gotowa (`FieldShape.Type` jest kształtem, nie enumem). Dopisać nowy wariant, gdy pojawi
  się treść, która tego wymaga.
- **System paczek treści** — odłożony, patrz `docs/vision.md`.

## Dyscyplina

- Weryfikacja rozstrzyga i uruchamia się ją samemu:
  ```
  dotnet build DungeonApp.sln
  dotnet test tests/DungeonApp.Core.Tests/DungeonApp.Core.Tests.csproj
  dotnet test tests/DungeonApp.Desktop.Tests/DungeonApp.Desktop.Tests.csproj
  ```
- Nie commitować bez wyraźnej prośby użytkownika. Osobny commit po kroku 3, osobny po 4,
  osobny po dokumentacji.
- Krok 4 dotyka interfejsu — obowiązują reguły pracy nad UI z `AGENTS.md`.
