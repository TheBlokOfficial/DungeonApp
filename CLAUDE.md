# DungeonApp

Desktopowy panel Mistrza Gry do gry papierowej przy stole. C#/.NET 10, Avalonia 12. Jedna maszyna,
jeden użytkownik, bez sieci. Aplikacja prowadzi księgowość kampanii — decyzje podejmuje MG.

## Pięć zakazów

Zasada produktu: **aplikacja liczy, MG decyduje, co policzyć**. Złamanie któregokolwiek zakazu
zamienia aplikację w silnik cRPG. Czytaj je według intencji; gdy litera i intencja się rozjeżdżają,
powiedz to autorowi.

1. **Wyrażenie się nie rozgałęzia.** Żaden węzeł, operator ani parametr nie wybiera wartości na
   podstawie warunku. `min` i `max` to arytmetyka i są dozwolone.
2. **Żaden typ nie niesie czasu.** Nic nie odczytuje czasu trwania, rundy, tury ani wygaśnięcia.
   Czas może stać w polu rekordu jako napis na karcie („Koncentracja, do 1 minuty”), ale poza kartą
   nic go nie rozbiera, nie liczy, nie filtruje ani nie sortuje.
3. **Wkłady do pola sumują się bezwarunkowo.** Bez priorytetów, kolejności i reguł „to się nie
   kumuluje”.
4. **Nic nie wybiera celów za MG.** Operacja zmienia wyłącznie to, co MG jawnie wskazał (może to być
   kilka rzeczy naraz), nigdy zbiór wyznaczony regułą („wszyscy”, „w obszarze”, „najbliższy”).
5. **Zmiana stanu nie wywołuje kolejnej zmiany stanu.** Zdarzenia powiadamiają widoki, nigdy nie
   zapisują.

## Jak pracujemy

- **Autor mówi, co ma powstać, i ogląda wynik w działającej aplikacji.** Pytaj go wyłącznie o decyzje
  produktowe: co ma istnieć i jak ma działać dla MG. O implementację, strukturę i wygląd kontrolek
  decydujesz sam, wiedzą, i krótko to raportujesz.
- **Jednostka pracy to funkcja widoczna w aplikacji**, zrobiona w jednej sesji od kodu do commita.
  Nie dziel pracy na porcje tylko po to, żeby je dzielić.
- **Przed większą funkcją** daj w rozmowie 5–10 linii planu: co autor zobaczy i co się zmieni w kodzie.
  Nie zapisuj planów jako dokumentów w repozytorium.
- **Czytaj kod.** Kod jest źródłem prawdy; dokumenty są tylko tam, gdzie kod nie wystarcza.
- **Subagenci** tylko wtedy, gdy realnie oszczędzają: szerokie przeszukanie tylko do odczytu albo
  wydzielona praca mechaniczna. Nie są domyślnym trybem pracy.
- **Git:** praca w gałęzi → build bez ostrzeżeń i zielone testy → scalenie do `master` → `git push`.
  CI (`.github/workflows/ci.yml`) musi być zielone. Commity po polsku, małe i logiczne.
- **Raport na koniec:** 3–6 zdań prostym językiem, co się zmieniło i dlaczego, plus 2–4 rzeczy do
  sprawdzenia w aplikacji (gdzie kliknąć, na co patrzeć). Bez nazw klas, chyba że autor zapyta.
- **Uwagi autora po obejrzeniu** poprawia się w tej samej sesji, jeśli trwa. Drobna uwaga, której nie
  robisz od razu, idzie do `docs/roadmap.md`.

## Budowanie i testy

```bash
dotnet build DungeonApp.sln
dotnet test DungeonApp.sln
dotnet run --project src/DungeonApp.App
```

Błąd `MSB3021`/`MSB3027` na `bin/` projektu wykonywalnego to blokada pliku przez podgląd XAML w Riderze
albo uruchomioną aplikację autora, nie błąd kodu. Nie zabijaj procesów. Buduj wtedy i testuj projekty
testowe; one nie zależą od `DungeonApp.App`.

## Struktura

```text
src/DungeonApp.Core           domena bez Avalonii: Campaigns, State, Persistence, Systems, Entries
src/DungeonApp.Desktop        całe UI: Shell, Systems (kontrakt systemu), Entries (zakładki treści, karty),
                              Workspace (biurko), Features, Controls, Themes (motyw), Startup
src/DungeonApp.Content.Dnd5e  system D&D 5e: typy treści, karty, zakładki, narzędzia biurka, paczka SRD
src/DungeonApp.App            korzeń kompozycji; jedyne miejsce, które wymienia systemy z nazwy
tests/                        Core, Desktop, Desktop.RenderingTests (okno bez ekranu, xUnit v3),
                              Content.Dnd5e, Architecture; DungeonApp.Testing = wspólne pomocniki;
                              Fixtures/Packs = paczki wzorcowe
```

Granic pilnują testy: Core nie referencuje Avalonii; Core i Desktop nie referencują `Content.*`; skan
słownictwa nie pozwala, żeby w Core i Desktop padła nazwa typu z systemu (także w galerii kontrolek).
Wiedza o D&D mieszka wyłącznie w `Content.Dnd5e`.

## Konwencje

- **Komentarz mówi, dlaczego kod jest taki. Nigdy nie mówi, skąd się wziął** (brief, porcja, krok,
  data, commit, „wcześniej było…”) — historia jest w gicie. Stare komentarze z takimi odwołaniami
  i z odsyłaczami do sekcji dokumentów, których już nie ma, poprawiaj w plikach, które i tak zmieniasz.
- Kod i komentarze po angielsku; teksty interfejsu, dokumenty i commity po polsku.
- Nie tłum ostrzeżeń (`#pragma`, `NoWarn`, `SuppressMessage`) — naprawiaj u źródła.
- Wygląd składaj z kontrolek i tokenów motywu (`Themes/`, podgląd w zakładce „Galeria”). Nowa kontrolka
  motywu powstaje dopiero wtedy, gdy potrzebuje jej ekran. Kolor ma jedno znaczenie, zapisane przy
  tokenie; widok sięga po kolor ze względu na znaczenie, nie barwę.
- Testy: zmiana zachowania logiki → test; nowy lub przepisany widok → test budujący go w oknie bez
  ekranu (`RenderingTests`, wzór `ContentTabViewBuildTests`); zapis kampanii i format paczek — zawsze.
  Wyglądu nie dowodzi się testami — sprawdza go autor.
- Nie kasuj danych, których nie rozumiesz (nieznane pliki w katalogu kampanii czy paczki zostają).

## Pułapki, których nie widać w kodzie

1. Błąd w motywie kontrolki kompiluje się i wywraca aplikację przy pierwszym użyciu kontrolki. Pilnuje
   tego `ControlThemesBuildTests`; nowy plik motywu dopisz do tego testu.
2. Motywu Fluent nie ma. Wbudowana kontrolka bez szablonu w `Themes/DungeonControls.axaml` jest
   niewidoczna. Dopisz jej motyw i typ do `BuiltInControlThemesTests`.
3. Zasób złego typu w XAML-u widoku (liczba tam, gdzie ma być `GridLength`) kompiluje się i wywraca
   widok przy utworzeniu — stąd test budujący widok.
4. Każdy `Button` ma stałą wysokość z motywu; przycisk jako wiersz albo karta ustawia `Height` jawnie
   (albo `NaN`).
5. Każde nowe przejście (animację) w motywie dopisz do `Themes/ReducedMotion.axaml`.
6. Przygaszenie wyłączonej kontrolki: selektor `^:disabled:not(:disabled :disabled)`, nie
   `[IsEnabled=False]`.
7. Pasek przewijania jest nakładką; treść musi kończyć się przed nim (`DungeonScrollBarZone`). Przewija
   jedna warstwa — lista nie stoi w drugim `ScrollViewer`.
8. Okno potwierdzenia i powiadomienie szukają `WindowOverlay` przez okno elementu, który je wywołał.
   Wywołane z okienka wysuwanego rzucą wyjątkiem.
9. Lista gubi zaznaczenie, gdy model widoku podmienia obiekty wierszy — zachowaj obiekty albo przywróć
   zaznaczenie po kluczu.
10. Testy bez ekranu nie dekodują obrazów i nie mierzą okienek wysuwanych jak Windows. To sprawdza
    tylko aplikacja.
11. Serializacja jest ścisła w treści (deserializator jest walidatorem wpisu), a pobłażliwa
    w manifeście kampanii (nowe pole nie podnosi wersji formatu). Tak jest celowo.
12. System ma dwa identyfikatory o tym samym brzmieniu: `SystemId` (tożsamość dla ramy, manifest
    kampanii) i `ContentSetId` (przestrzeń typów treści, pole `template` we wpisie).

## Dokumenty

| Plik | Co zawiera | Limit |
|---|---|---|
| `CLAUDE.md` | zakazy, sposób pracy, pułapki | 150 linii |
| `docs/architecture.md` | jak aplikacja jest zbudowana dziś i dokąd zmierza | 400 linii |
| `docs/decisions.md` | rozstrzygnięcia, zwłaszcza odrzucone kierunki; czytaj przed zmianą architektury | wpis ≤ 8 linii |
| `docs/roadmap.md` | co dalej | 150 linii |
| `docs/archive/` | dokumenty sprzed przebudowy obiegu pracy | nie czytaj przy zwykłej pracy |

Dokument opisuje stan, nie historię: bez dat, „wcześniej”, „autor powiedział”. Aktualizuje się go
w tym samym commicie co kod. Przekroczony limit znaczy: przytnij, zamiast dopisywać. Wiedzy
o projekcie nie zapisuje się w pamięci asystenta — tylko w tych plikach.
