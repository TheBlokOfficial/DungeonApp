# MockupRenderer

Headlessowe narzędzie deweloperskie: renderuje pojedynczy plik `.axaml`
(`UserControl`) do pliku PNG, bez uruchamiania całej aplikacji ani okna.
Służy do sprawdzania mockupów z `design/mockups/` na etapie projektowania UI
(patrz `CLAUDE.md`, sekcja „UI: mockup przed wdrożeniem”).

Narzędzie leży poza `DungeonApp.sln` — `dotnet build DungeonApp.sln` go nie
dotyczy. Buduje się i uruchamia samodzielnie, poleceniami poniżej.

## Wywołanie

```
dotnet run --project tools/MockupRenderer/MockupRenderer.csproj -- <wejście.axaml> <wyjście.png> [opcje]
```

Argumenty pozycyjne (wymagane, w tej kolejności):

1. ścieżka do pliku wejściowego `.axaml`,
2. ścieżka do pliku wyjściowego `.png`.

Opcje (wszystkie opcjonalne):

- `--width=N` — szerokość okna renderu w pikselach (domyślnie `1280`),
- `--height=N` — wysokość okna renderu w pikselach (domyślnie `800`),
- `--scale=Small|Medium|Large` — profil skalowania UI z `UiScaleProfiles`
  (domyślnie `Medium`),
- `--data=kontekst.json` — plik z atrapą kontekstu danych (patrz niżej).

Złe argumenty (brak pliku, zła flaga, niepoprawna liczba) kończą się czytelnym
komunikatem na `stderr` i kodem wyjścia różnym od zera — narzędzie nie rzuca
nieobsłużonym wyjątkiem.

Przykład:

```
dotnet run --project tools/MockupRenderer/MockupRenderer.csproj -- ^
  design/mockups/LoadingScreenMockupA.axaml ^
  design/mockups/LoadingScreenMockupA.png
```

## Atrapa kontekstu danych (`--data`)

Renderowana kontrolka bywa związana z właściwościami ViewModelu, którego
w trybie headless nie ma. Bez flagi `--data` `DataContext` pozostaje pusty —
dokładnie tak jak dotąd; kontrolki z samymi wartościami statycznymi (jak
`LoadingScreenMockupA.axaml`) renderują się bez żadnej atrapy.

Gdy kontrolka wymaga bindingów, wskaż `--data` na plik JSON z płaskim
obiektem — każde jego pole staje się właściwością obiektu podstawionego jako
`DataContext`. Wspierane są wartości tekstowe, liczbowe i logiczne; pola
zagnieżdżone (obiekty, tablice) są pomijane, bo atrapa nie modeluje hierarchii
prawdziwego ViewModelu.

Przykład — kontrolka z bindingami `{Binding TotalSteps}`,
`{Binding CompletedSteps}`, `{Binding StartupMessage}`:

```json
{
  "TotalSteps": 6,
  "CompletedSteps": 4,
  "StartupMessage": "Przygotowywanie panelu kroniki…"
}
```

```
dotnet run --project tools/MockupRenderer/MockupRenderer.csproj -- ^
  ScieżkaDoKontrolki.axaml wynik.png --data=kontekst.json
```

Uwaga dla autora renderowanego `.axaml`: nie ustawiaj `x:DataType` na typ,
który nie istnieje w tym projekcie (np. na prawdziwy ViewModel) — atrapa
podstawia obiekt wygenerowany w locie, więc bindingi muszą być klasyczne
(bez `x:DataType`), tak jak w mockupach z `design/mockups/`.
