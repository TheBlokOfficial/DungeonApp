# Architektura komponentów UI

> **Status:** obowiązujący standard implementacyjny warstwy Desktop — wersja 0.1

Każdy komponent musi respektować geometrię i polityki adaptacyjne z [Kontraktu UI](contract.md).

## Decyzja

`DungeonApp.Desktop` używa hierarchicznej kompozycji wyspecjalizowanych komponentów. Widoki najwyższego poziomu opisują strukturę ekranu, nie implementują szczegółów wszystkich jego części.

```text
MainWindow
  -> AppShell
      -> TopBar
      -> Sidebar bieżącego kontekstu
      -> WorkspaceHost / aktywny ekran
      -> StatusBar
```

Komponent jest czarną skrzynką w zakresie: własnej geometrii wewnętrznej, prezentacji własnego stanu, lokalnych stanów wizualnych, reguł przepełnienia/ładowania/pustej treści/błędu, szczegółów kontrolek Avalonia. Nie jest czarną skrzynką w zakresie ukrytych zależności — wejścia, wyjścia, wymagany ViewModel i kontrakt rozmiaru muszą być jawne.

## Kiedy wydzielić komponent

Nie tworzymy osobnego `UserControl` dla każdego `TextBlock`, ikony czy separatora. Element staje się komponentem, jeżeli spełnia ≥1 kryterium: reprezentuje samodzielne pojęcie UX (sidebar, statusbar, panel czasu); ma własny kontrakt danych/interakcji; ma kilka stanów wizualnych wymagających wspólnego utrzymania; jest używany w wielu miejscach; ma niezależne zasady geometrii/przepełnienia; wydzielenie pozwala rodzicowi opisywać kompozycję zamiast szczegółów; wymaga osobnego testowania/przeglądu.

Nie wydzielamy elementu tylko dlatego, że jego markup zajmuje kilka linii — nadmierna granulacja ukrywa strukturę ekranu równie skutecznie jak monolit.

## Trzy mechanizmy budowy UI w Avalonia

**`UserControl`** — domyślny wybór dla ekranów/fragmentów specyficznych dla DungeonApp (`AppShellView`, `GlobalSidebarView`, `CampaignLibraryView`, kroki kreatora, panele pulpitu z własnym ViewModelem).

**Style i `DataTemplate`** — powtarzalna prezentacja danych. Kolekcje (nawigacja, historia, kampanie, bohaterowie) renderowane przez `ItemsControl`/`ListBox` z typowanym `DataTemplate`, nie ręcznie powielanym markupem. Style współdzielą wygląd istniejących kontrolek, gdy nie powstaje nowy kontrakt semantyczny.

**`TemplatedControl`** — generyczny, tematyzowalny element z własnymi właściwościami i stanami: rama panelu, kontrolka wartości mechanicznej, host ikony, element nawigacyjny (gdy `ListBoxItem` nie wystarcza), przyszła kontrolka dokowania. Wygląd przez `ControlTheme`, zachowanie przez `StyledProperty`/`DirectProperty`/komendy/zdarzenia/pseudoklasy. Własnej kontrolki rysowanej przez `Render` nie tworzymy, jeśli efekt osiągalny kompozycją/stylem/szablonem.

## Kontrakt komponentu

**Wejścia:** typowany ViewModel przez `DataContext` lub jawne właściwości; zawartość przez `Content`/`ItemsSource`; tokeny stylu z motywu; jawny wariant rozmiaru/tryb.

**Wyjścia:** komendy ViewModelu; zdarzenia kontrolki wyłącznie dla generycznych zachowań UI; jawna zmiana zaznaczenia/fokusu/dokowania; brak bezpośredniego zapisu do repozytorium czy wywołania domeny z code-behind.

**Gwarancje:** udokumentowany rozmiar min/preferowany i sposób przepełnienia; stabilna geometria między `loading`/`empty`/`ready`/`error`; brak zależności od nazw elementów rodzica; brak service locatora; brak założeń o konkretnym rodzicu poza formalnym kontraktem; obsługa klawiatury i widoczny fokus; typowane, kompilowane bindingi.

## Granice ViewModeli

ViewModel odpowiada za stan prezentacyjny i orkiestrację use case'ów danego ekranu — nie zna konkretnych elementów XAML. `ShellViewModel` zna bieżący kontekst i aktywny ekran, nie implementuje zachowania wszystkich ekranów. ViewModel panelu jest wydzielany tylko gdy panel ma własny stan/zachowanie — czysto prezentacyjny panel może dostać gotowy model od rodzica. ViewModel nie zawiera reguł kampanii, serializacji, ścieżek filesystemu ani zależności od kontrolek Avalonia.

## Code-behind

Dozwolony dla zachowań należących wyłącznie do widoku: zarządzanie fokusem, interakcje wskaźnika/przeciąganie, reakcja na wariant rozmiaru, integracja z mechanizmami okna, animacje. Nie wywołuje use case'ów, nie interpretuje reguł kampanii, nie staje się drugim ViewModelem.

Wyjątkiem należącym nadal do widoku jest koordynacja pierwszej klatki i rozgrzewania drzewa wizualnego. `AppShellView` może zgłosić ViewModelowi moment `Loaded`, osadzić techniczny warmup host i zakończyć bramkę gotowości po layout/renderze; odczyt repozytoriów oraz przygotowanie danych pozostają w ViewModelu/usłudze przygotowującej.

## Bindingi i `DataContext`

Compiled bindings włączone globalnie — każdy `Window`/`UserControl`/`DataTemplate` deklaruje `x:DataType`. Komponent generyczny nie ustawia `DataContext = this` w konstruktorze (przerywa dziedziczenie kontekstu od rodzica). W `TemplatedControl` właściwości własne przez `TemplateBinding`; w aplikacyjnym `UserControl` bindingi odnoszą się do zadeklarowanego ViewModelu.

## Style i system projektowy

Dwa poziomy: wspólny system projektowy (kolory, typografia, odstępy, grubości ramek, wysokości wierszy, fokus, stany semantyczne) i motyw/styl komponentu (struktura i stany konkretnej kontrolki). Komponent korzysta ze wspólnych tokenów, nie z przypadkowych selektorów potomków innego widoku. Rozproszenie surowych wartości kolorów/wymiarów po plikach widoków jest zabronione bez uzasadnienia jako wartość rzeczywiście lokalna.

## Nawigacja i kompozycja

`MainWindow` hostuje `AppShellView`, który wybiera sidebar właściwy dla kontekstu i aktywny ekran workspace'u:

```xml
<UserControl x:Class="AppShellView">
    <Grid>
        <shell:TopBarView />
        <ContentControl Content="{Binding CurrentSidebar}" />
        <ContentControl Content="{Binding CurrentWorkspace}" />
        <shell:StatusBarView />
    </Grid>
</UserControl>
```

Rodzic rozmieszcza bezpośrednie dzieci; dziecko odpowiada za własne wnętrze. Widoki nie tworzą innych ViewModeli w code-behind — kompozycja zależności w composition root, nawigacja operuje na jawnych deskryptorach/ViewModelach ekranów.

## Organizacja katalogów Desktop

Feature-first, z osobnym obszarem shellu i systemu projektowego:

```text
DungeonApp.Desktop/
  Shell/
    AppShellView.axaml, AppShellViewModel.cs
    TopBar/  Sidebars/  StatusBar/  Navigation/
  Features/
    CampaignLibrary/
    CampaignCreator/
    CampaignWorkspace/
      SessionDashboard/
      TimeAndEvents/
  Controls/
    PanelFrame/  MechanicalValue/  IconHost/
  Themes/
    Tokens.axaml  Typography.axaml  BuiltInControls.axaml  DungeonControls.axaml
  Assets/
    Fonts/  Icons/  Licenses/
```

Widok, code-behind i ViewModel razem w katalogu funkcji — nie jeden rosnący katalog widoków i osobny równoległy katalog ViewModeli.

## Reguły zależności

- `Shell` może zależeć od kontraktów nawigacji i współdzielonych kontrolek.
- Funkcja może zależeć od `Application`, kontraktów nawigacji, współdzielonych kontrolek Desktop.
- Współdzielona kontrolka nie zależy od konkretnej funkcji ani od domenowego typu kampanii.
- Komponent prezentacyjny nie zależy od `Infrastructure`.
- `Desktop` tworzy implementacje `Infrastructure` wyłącznie w composition root.
- Funkcje nie stylują wnętrza innych funkcji przez globalne selektory.

## Testowanie

Reguły nawigacji i stan ekranów testujemy przez ViewModele bez Avalonia. Generyczne kontrolki z własnym zachowaniem dostają testy kontrolki/integracyjne. Kluczowe ekrany — przegląd wizualny w realistycznych stanach danych, w tym: najkrótsze/najdłuższe etykiety, wartości jedno- i wielocyfrowe, brak ikon, loading, błąd, przepełnienie listy. Widoki kompilują się z compiled bindings — ostrzeżenia bindingów nie są sposobem wykrywania literówek w runtime.

## Antywzorce

Jeden wielki `MainWindow.axaml` ze wszystkimi ekranami · jeden `MainWindowViewModel` orkiestrujący całość · `UserControl` dla każdego drobnego elementu · ręczne powtarzanie markupu kolekcji zamiast `ItemsControl`+`DataTemplate` · `DataContext = this` w kontrolce generycznej · odwołania dziecka do elementów wizualnych rodzica · ukryte pobieranie usług z globalnego kontenera · reguły domenowe/wywołania repozytorium w code-behind · globalne style ingerujące w prywatną strukturę widoku · duplikowanie tokenów wizualnych · wydzielanie komponentu bez jasnej odpowiedzialności tylko po to, by skrócić plik XAML.

## Kryterium przeglądu komponentu

1. Jakie jedno pojęcie UX reprezentuje?
2. Jaki jest jego jawny kontrakt wejścia i wyjścia?
3. Kto jest właścicielem jego stanu?
4. Jak zachowuje geometrię w stanach loading/empty/ready/error?
5. Dlaczego jest `UserControl`, `TemplatedControl`, szablonem danych albo stylem?
6. Czy rodzic rozumie kompozycję bez znajomości wnętrza dziecka?
7. Czy dziecko można zmienić bez modyfikowania niespokrewnionych ekranów?

Jeżeli odpowiedzi nie są jasne, granica komponentu wymaga ponownego wyznaczenia.
