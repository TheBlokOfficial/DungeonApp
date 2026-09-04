# Architektura

Ten dokument mówi **co wolno**: gdzie biegną szwy, co jest egzekwowane
kodem, a co wyłącznie umową. Opis stanu faktycznego — kto od kogo dziś
faktycznie zależy — żyje w `docs/code-map.md` i jest generowany osobno.

Rozjazd między tymi dwoma dokumentami jest sygnałem długu, nie błędem
zapisu.

## Dwie warstwy

`DungeonApp.Core` nie wie nic o interfejsie. `DungeonApp.Desktop` zna Core
i buduje na nim widoki. Zależność jest jednokierunkowa.

**Egzekwowane:** `CoreIndependenceTests.Core_does_not_reference_Avalonia`
sprawdza przez refleksję, że zestaw `DungeonApp.Core` nie referencjonuje
żadnego assembly zaczynającego się od `Avalonia`.

**Czego test nie łapie:** samego kierunku `Desktop -> Core`. Zabezpiecza go
dziś struktura projektów, nie test — Core nie ma referencji do Desktop,
więc odwrócenie zależności wymagałoby świadomej edycji `.csproj`. To
wystarczające, dopóki projektów są dwa.

## Dostęp do dysku

Granica jest o **rodzaj danych**, nie o użyte API:

- dane kampanii zapisuje wyłącznie `Core/Persistence`;
- `Core` poza `Persistence` nie dotyka dysku w ogóle;
- `Desktop` wolno zapisać własny stan lokalny — preferencje aplikacji i
  układ biurka — i nic ponad to.

Wcześniejsze brzmienie („`System.IO` nie wychodzi poza `Core/Persistence`")
było za szerokie i wzięte dosłownie kazałoby złamać szew mocniejszy: żeby
`Desktop` zapisywał układ biurka przez `Core`, rdzeń musiałby poznać
pojęcia interfejsu — szerokość panelu, pozycję na biurku, profil UI. Rdzeń
nie wie o warstwie UI i to jest ważniejsze.

**Egzekwowane:** nic. To konwencja — żaden test nie zatrzyma odczytu pliku
dopisanego w module ani w ViewModelu panelu.

**Stan faktyczny:** zgodny. W `Core` `System.IO` występuje wyłącznie w
`JsonCampaignJournalStore` i `JsonCampaignRepository`. W `Desktop` — w
`Settings/AppSettingsStore` i `Features/CampaignWorkspace/Layout/WorkspaceLayoutStore`,
oba mieszczą się w wyjątku na stan lokalny interfejsu.

## Kontrakt modułu

Moduł deklaruje się manifestem. `ICampaignModule` wymaga właściwości
`Manifest` typu `ModuleManifest`, który niesie: `ModuleId`, nazwę
wyświetlaną, wersję stanu (`StateVersion`) i listę wymaganych modułów
(`Requires`).

**Egzekwowane kompilatorem i runtime'em:** modułu bez manifestu nie da się
skompilować. `ModuleCatalog.Register` odrzuca rejestrację, w której
`manifest.Id` nie zgadza się z deklarowanym identyfikatorem, oraz
rejestrację duplikatu — w obu wypadkach rzuca `ArgumentException`.

Katalog modułów nie skanuje dysku ani assembly — rejestracje są wpisane
wprost w `ModuleCatalog`. Dodanie modułu to edycja tego pliku, świadoma i
widoczna w diffie. To celowe: nie chcemy magii ładowania.

## Zależności i kolejność ładowania

Moduł deklaruje swoje zależności w `Manifest.Requires`. Aktywacja odbywa
się w porządku topologicznym — zależność jest gotowa, zanim ruszy moduł,
który jej potrzebuje.

**Egzekwowane i przetestowane:** `CampaignModules.SortByDependency` sortuje
przez DFS z wykrywaniem cyklu i rzuca `ModuleActivationException` z kodem
`CircularDependency`. Moduł, którego zależność jest wyłączona, nie
aktywuje się wcale (`MissingDependency`). Pokrycie w `CampaignModulesTests`:
odmowa przy pętli zależności, aktywacja zależności przed modułem, który je
zadeklarował, dokładnie jedna aktywacja na moduł, odmowa przy zależności
wyłączonej.

To jedyny niezmiennik z pełnym pokryciem testowym. Traktuj go jako wzorzec
tego, jak powinny wyglądać pozostałe.

## Komunikacja między modułami

Moduły mają dwie legalne drogi do siebie i różnią się one kierunkiem:

- **Ogłoszenie zmiany** — wyłącznie przez `CampaignEvents`. Moduł, który
  coś zmienił, publikuje zdarzenie i nie wie, kto go słucha. Nigdy nie
  wywołuje cudzej metody, żeby powiadomić o fakcie.
- **Zapytanie o stan** — przez `CampaignModules.Get<TModule>()`, typowane i
  synchroniczne. Wolno wyłącznie modułowi, który zadeklarował tamten moduł
  w `Manifest.Requires`.

Rozróżnienie jest istotne: pytanie o cudzy stan to nie to samo co
ogłoszenie własnej zmiany. Pierwsze tworzy zależność jawną, zadeklarowaną i
uporządkowaną topologicznie. Drugie, robione wprost, tworzyłoby zależność
ukrytą i cykliczną.

**Egzekwowane pośrednio:** `Get<T>()` na module niezadeklarowanym w
`Requires` rzuci w runtime. Nie ma natomiast niczego, co zabroniłoby
modułowi sięgnąć po inny bez deklaracji, ani testu pilnującego, że
powiadomienia idą zdarzeniami.

Dziś z tej ścieżki korzysta `SchedulerModule`, sięgając po `ClockModule`.

## Stan modułu i zapis

Moduł nigdy nie wie, że istnieje JSON. Oddaje swój stan jako `object`
(`CaptureState()`), deklaruje jego typ (`StateType`) i przyjmuje go z
powrotem (`RestoreState()`). Wersję formatu niesie `Manifest.StateVersion`.

Serializacja żyje wyłącznie w `Core/Persistence` — `JsonCampaignRepository`
zamienia oddany obiekt na JSON i z powrotem, operując na `Type` podanym
przez moduł.

**Egzekwowane:** nic. To konwencja podparta kształtem interfejsu — moduł
zwraca `object`, więc technicznie mógłby serializować sam i nikt by tego
nie złapał. Stan faktyczny jest dziś czysty: w `Core/Modules` nie ma ani
`System.Text.Json`, ani atrybutów serializacji.

Konsekwencja praktyczna: zmiana kształtu stanu modułu to zmiana formatu
zapisu. Podniesienie `StateVersion` bez ścieżki migracji zepsuje istniejące
kampanie.

## Bilans egzekwowania

| Szew | Stan |
|---|---|
| Granica Core / Desktop | test częściowy (brak referencji do Avalonii) |
| Dostęp do dysku | konwencja |
| Kontrakt modułu | kompilator + walidacja przy rejestracji |
| Zależności i kolejność | test pełny |
| Komunikacja między modułami | częściowo runtime, reszta konwencja |
| Stan i zapis | konwencja |

Cztery z sześciu szwów nie mają dziś zabezpieczenia, które zatrzymałoby
naruszenie automatycznie. Dopóki tak jest, `docs/code-map.md` pełni rolę
zastępczą: pokazuje stan faktyczny, więc rozjazd z tym dokumentem widać
bez wchodzenia w kod.
