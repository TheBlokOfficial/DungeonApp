# DungeonApp

Desktopowy panel Mistrza Gry do gry papierowej przy stole. C#/.NET 10, Avalonia 12. Jedna maszyna,
jeden użytkownik, bez sieci. Aplikacja prowadzi księgę kampanii — zasady stosuje MG. Terminy (księga,
przełom, entity, rama…) definiuje słownik w `docs/architecture.md`.

## Granica automatyzacji

Zasada produktu: **aplikacja liczy, pamięta i przenosi to, co przy stole się śledzi; zasad nie
rozstrzyga.** MG liczy atak i obrażenia tak jak gracze na papierze, a aplikacja nie ogranicza go
w zmienianiu świata. Czytaj według intencji; gdy litera i intencja się rozjeżdżają, powiedz to autorowi.

1. **Zasady i słownictwo są tekstem.** Opis zaklęcia, cechy, ataku, stanu czy przedmiotu czyta MG;
   wpis nie niesie logiki, a aplikacja nie rozstrzyga ataku, rzutu obronnego ani obrażeń. Strukturę
   ma tylko pole, które czyta księga albo przełom.
2. **Księga.** Śledzone wartości (PW, ładunki, miejsca na zaklęcia, złoto, ilości, liczniki stanów,
   kolejka, zegar) MG zmienia polem zmiany: `-12`, `+5`, `=30`. Rachunek księgowy z podręcznika
   (PW tymczasowe schodzą pierwsze) robi aplikacja i pokazuje rozpisanie.
3. **Przełomy.** Akcja MG, która zmienia wiele naraz według jawnej reguły (następna tura, odpoczynek,
   handel), działa od razu i pokazuje okienko boczne z każdą zmianą; odznaczenie pozycji ją przywraca.
   Lista jest zamknięta (`docs/decisions.md`); nowy przełom to decyzja autora i spełnia wszystko:
   wejście w księdze, jedna reguła dla każdej istoty bez decyzji, uruchomienie akcją MG, wynik do
   odznaczenia, a bez niego MG musiałby pamiętać. Wynik niepewny (rzut istoty) albo zależny od
   sytuacji przy stole (osłona, 0 PW, „chyba że ogień”) zostaje u MG.
4. **Stan zmienia się tylko po akcji MG.** Aplikacja nie ma poczucia czasu: żaden timer ani praca
   w tle nie zmienia stanu kampanii (timer interfejsu — animacja, odłożony zapis układu okien — wolno);
   zegar, rundę i turę przesuwa MG. Cele wskazuje MG (może kilka naraz), nigdy reguła („w obszarze”,
   „najbliższy”). Zdarzenia powiadamiają widoki, nigdy nie zapisują.
5. **Świat zmienia MG bez ograniczeń.** Każdą wartość istoty, także ze strukturą, MG zmienia na jej
   karcie; różnica od wpisu jest oznaczona i da się ją przywrócić. Entity działa też bez wpisu
   z paczki. Stan niezgodny z zasadami (PW ponad maksimum, ujemne złoto) widać, nie blokuje.
6. **Liczba ma pochodzenie tam, gdzie stoi.** Przy wartości widać ostatnią zmianę („30, było 42”),
   rzut w kalkulatorze ma rozpisanie, w walce zmiana od początku tury ma znacznik. Dziennika świata
   nie ma — te trzy miejsca pokrywają jego potrzeby (`docs/decisions.md`).

## Jak pracujemy

- **Autor** (właściciel produktu) **mówi, co ma powstać, i ogląda wynik w działającej aplikacji.**
  Pytaj go wyłącznie o decyzje produktowe; implementację i strukturę wybierasz sam i krótko raportujesz.
- **Jedna sesja asystenta to jeden wycinek** (funkcja widoczna w aplikacji), od planu do scalenia;
  następny wycinek to nowa sesja. Najpierw 5–10 linii planu: co autor zobaczy, co się zmieni w kodzie.
  Wygląd ekranu uzgadniasz z autorem na piśmie i na renderze obecnego stanu, zanim powstanie kod;
  szczegóły kontrolek wybierasz sam.
- **Architekt** (główna sesja) rozmawia z autorem, czyta kod potrzebny do decyzji i zlecenia, przegląda
  wyniki i scala. Drobne, lokalne zmiany robi sam; fakt z kodu, którego nie zmienia, zbiera agentem
  Explore. Autorowi pokazuje wynik, który sam by przyjął.
- **Wykonawca** (agent `wykonawca` z `.claude/agents/`: Sonnet, w tle, worktree z HEAD architekta)
  dostaje zamkniętą część pracy. Zlecenie z adresami: cel, nazwa gałęzi, pliki i miejsca w nich, znane
  fakty (wzór, tokeny, pułapki), kryteria z nazwami renderów, czego nie robić, punkt zatrzymania;
  przed zleceniem commit. Opus (`model: "opus"`) tylko do zadania bez gotowego projektu albo szukania
  nieznanej przyczyny; pracę mechaniczną według reguły bierze agent `porzadki` (Opus). Niezależne
  obszary — równolegle.
- **Kolejna runda:** pracę wykonawcy przyjętą na renderach scalasz; drobne poprawki robisz sam
  w worktree wykonawcy (ścieżki bezwzględne i `git -C`, bez `cd` — sesja przeniosłaby się do worktree),
  dłuższą listę dostaje nowy wykonawca ze ścieżkami i hashami. Wykonawcy z dużym kontekstem nie
  wznawiaj — każdy jego krok wysyła cały dotychczasowy kontekst.
- **Uwagi autora** po obejrzeniu poprawiasz od razu, gdy to błąd; szlif (działa, ale mogłoby wyglądać
  lepiej) idzie jednym zdaniem do „Szlif” w `docs/roadmap.md`.
- **Dokumenty przy pracy:** roadmapę i rozstrzygnięcia aktualizujesz raz na wycinek, przy scaleniu,
  jednym commitem; `docs/architecture.md` — w gałęzi, która zmienia kod.
- **Git:** każdy commit przechodzi `tools/check.ps1`; gałąź → scalenie do `master` („Scalenie: …”) →
  `git push` (CI musi być zielone) → usunięcie worktree i gałęzi. Główny katalog stoi zawsze na
  `master`; każdy agent pracuje we własnej gałęzi i worktree. Commity po polsku, małe i logiczne.
- **Raport na koniec:** 3–6 zdań prostym językiem, co się zmieniło i dlaczego, plus 2–4 rzeczy do
  sprawdzenia w aplikacji (gdzie kliknąć, na co patrzeć). Bez nazw klas, chyba że autor zapyta.

## Budowanie i testy

`powershell -File tools/check.ps1` buduje i testuje, wypisując tylko błędy, ostrzeżenia i podsumowania
(`-Tests Core`, `-Filter <nazwa>`, `-NoTest`). Blokadę `bin/` aplikacji (`MSB3021`/`MSB3027`: podgląd
XAML w Riderze albo uruchomiona aplikacja autora) skrypt obchodzi sam — nie zabijaj procesów. Aplikacja:
`dotnet run --project src/DungeonApp.App`. Render bez ekranu: `powershell -File tools/render/render.ps1
-Only '<wzorzec>'` → `tools/render/out/` (bez `-Only` wszystkie karty i galeria, kilka minut; `zoom.ps1`
powiększa wycinek). Pliki czytaj i zmieniaj narzędziami Read/Edit/Write — PowerShell psuje polskie znaki.

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
słownictwa nie pozwala, żeby w Core i Desktop padła nazwa typu z systemu (także w komentarzach
i galerii kontrolek). Wiedza o D&D mieszka wyłącznie w `Content.Dnd5e`.

## Konwencje

- **Komentarz mówi, dlaczego kod jest taki. Nigdy nie mówi, skąd się wziął** (brief, porcja, krok,
  data, commit, „wcześniej było…”) — historia jest w gicie. Stare komentarze z takimi odwołaniami
  i z odsyłaczami do sekcji dokumentów, których już nie ma, poprawiaj w plikach, które i tak zmieniasz.
- Kod i komentarze po angielsku; teksty interfejsu, dokumenty i commity po polsku.
- Nie tłum ostrzeżeń (`#pragma`, `NoWarn`, `SuppressMessage`) — naprawiaj u źródła. Jedyny wyjątek:
  CS1591 w `Directory.Build.props` — komentarz dokumentacyjny nie jest obowiązkowy, ale każdy
  istniejący jest sprawdzany (zerwany `<see cref>` to ostrzeżenie, więc błąd builda).
- Błędu akcji MG nie łap, żeby go ukryć — przepuść go do handlera wątku interfejsu (log
  i powiadomienie). Błąd złapany i niepokazany MG zapisuj przez `AppLog.Error`; pusty `catch` bez wpisu
  ukrywa awarię przed autorem. Mechanizm: `docs/architecture.md`, „Błędy”.
- Wygląd składaj z kontrolek i tokenów motywu (`Themes/`, podgląd w zakładce „Galeria”). Nowa kontrolka
  motywu powstaje dopiero wtedy, gdy potrzebuje jej ekran. Kolor ma jedno znaczenie, zapisane przy
  tokenie; widok sięga po kolor ze względu na znaczenie, nie barwę.
- Kontrolka w dwóch formach (tekst do czytania i pole w trybie edycji) zajmuje w obu dokładnie to samo
  miejsce: przełączenie formy nie przesuwa żadnej kontrolki, sekcji ani okna. Każda taka kontrolka ma
  test bez ekranu, który mierzy obie formy.
- Testy: zmiana zachowania logiki → test; nowy lub przepisany widok → test budujący go w oknie bez
  ekranu (`RenderingTests`, wzór `ContentTabViewBuildTests`); zapis kampanii i format paczek — zawsze.
  Wyglądu nie dowodzi się testami — sprawdza go autor.
- Nie kasuj danych, których nie rozumiesz (nieznane pliki w katalogu kampanii czy paczki zostają).

## Pułapki, których nie widać w kodzie

1. Błąd w motywie kontrolki kompiluje się i wywraca aplikację przy pierwszym użyciu kontrolki. Pilnuje
   tego `ControlThemesBuildTests`. Motyw kontrolki to plik w `Themes/Controls/` wpisany do spisu
   `Themes/DungeonControls.axaml`; test odrzuca plik, którego nie ma w spisie.
2. Domyślnego motywu Avalonii (Fluent) nie ma (`docs/decisions.md`). Wbudowana kontrolka bez motywu
   w `Themes/Controls/` jest niewidoczna. Dopisz jej plik motywu i typ do `BuiltInControlThemesTests`.
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

| Plik | Co zawiera | Zalecana długość |
|---|---|---|
| `CLAUDE.md` | granica automatyzacji, sposób pracy, pułapki | ok. 150 linii |
| `docs/architecture.md` | słownik; jak aplikacja jest zbudowana i dokąd zmierza | ok. 400 linii |
| `docs/decisions.md` | rozstrzygnięcia z powodem, zwłaszcza odrzucone kierunki | wpis ok. 8 linii |
| `docs/roadmap.md` | co dalej: to, czego nie zbudowano | ok. 150 linii |
| `docs/spec/` | uzgodniona koncepcja etapu przed budową | — |
| `docs/archive/` | nieobowiązujące; tylko przez odsyłacz z decisions.md albo roadmapy | — |

Kod jest źródłem prawdy; dokument jest tam, gdzie kod nie wystarcza. Wiedza o projekcie mieszka tylko
w tych plikach, nie w pamięci asystenta. Długość jest wskazówką, nie granicą: przekroczenie to sygnał
do przeglądu — usuń powtórzenia, historię i treść, która należy do innego dokumentu. Nie skracaj
kosztem jasności, powodu ani informacji; dokument dłuższy i czytelny jest lepszy niż ścisły.
- Stan, nie historia: bez dat, słów „dziś”, „na razie”, „dawny”, „stary”, „wcześniej” i bez tego,
  kto co powiedział lub przyjął. Rola autora w procesie (decyduje, ogląda) to nie historia.
- Rzecz ma jedno miejsce: mechanizm w architekturze, powód w rozstrzygnięciach, plan w roadmapie;
  gdzie indziej tylko odsyłacz. Zmieniając regułę, szukaj jej słów we wszystkich dokumentach.
- Pojęcie ma jedną nazwę ze słownika; nowy termin wchodzi razem z wpisem w słowniku.
- „X nie ma” podaje powód albo odsyła do rozstrzygnięcia. Punkt to jedna myśl, do ok. 60 słów.
