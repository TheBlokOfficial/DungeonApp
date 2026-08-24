# Architektura komponentów UI

> **Status:** obowiązujący standard implementacyjny warstwy Desktop  
> **Wersja:** 0.1  
> **Cel:** zachowanie czytelnego, testowalnego i hermetycznego drzewa interfejsu bez rozrostu monolitycznych plików XAML.

Każdy komponent musi respektować geometrię i polityki adaptacyjne z dokumentu [Kontrakt UI v1](05-kontrakt-ui-v1.md).

## 1. Decyzja

Warstwa `DungeonApp.Desktop` używa hierarchicznej kompozycji wyspecjalizowanych komponentów. Widoki najwyższego poziomu mają opisywać strukturę ekranu, a nie implementować szczegóły wszystkich jego części.

Docelowa rola `MainWindow` jest minimalna:

```text
MainWindow
  -> AppShell
      -> TopBar
      -> Sidebar bieżącego kontekstu
      -> WorkspaceHost / aktywny ekran
      -> StatusBar
```

Analogicznie ekran sesji składa się z jawnych paneli, ale nie zawiera we własnym pliku kompletnego markupu każdego panelu.

Komponent jest traktowany jak czarna skrzynka w zakresie:

- własnej geometrii wewnętrznej;
- prezentacji własnego stanu;
- obsługi lokalnych stanów wizualnych;
- reguł przepełnienia, ładowania, pustej treści i błędu;
- szczegółów użytych kontrolek Avalonia.

Nie jest czarną skrzynką w zakresie ukrytych zależności. Jego wejścia, wyjścia, wymagany ViewModel i kontrakt rozmiaru muszą być jawne.

## 2. Komponentyzacja nie oznacza maksymalnej liczby plików

Nie tworzymy osobnego `UserControl` dla każdego `TextBlock`, ikony, separatora ani prostego połączenia etykiety z wartością.

Element powinien stać się osobnym komponentem, jeżeli spełnia przynajmniej jedno z poniższych kryteriów:

- reprezentuje samodzielne pojęcie UX, takie jak sidebar, pasek statusu albo panel czasu;
- ma własny kontrakt danych lub interakcji;
- ma kilka stanów wizualnych wymagających wspólnego utrzymania;
- jest używany w wielu miejscach;
- posiada niezależne zasady geometrii i przepełnienia;
- jego wydzielenie pozwala rodzicowi opisywać kompozycję zamiast szczegółów;
- wymaga osobnego testowania zachowania albo przeglądu wizualnego.

Nie wydzielamy elementu wyłącznie dlatego, że jego markup zajmuje kilka linii. Nadmierna granulacja tworzy trudne do śledzenia warstwy przekazywania właściwości i ukrywa rzeczywistą strukturę ekranu równie skutecznie jak monolit.

## 3. Trzy mechanizmy budowy UI w Avalonia

### 3.1. `UserControl` — ekrany i fragmenty aplikacyjne

`UserControl` jest domyślnym wyborem dla widoków specyficznych dla DungeonApp:

- `AppShellView`;
- `GlobalSidebarView`;
- `CampaignSidebarView`;
- `CampaignLibraryView`;
- `CampaignCreatorView` i jego kroki;
- `SessionDashboardView`;
- `TimeAndEventsView`;
- większe panele pulpitu posiadające własny ViewModel.

Taki komponent może komponować istniejące kontrolki Avalonia i posiadać z góry ustalony layout. Jeżeli ma własny stan prezentacyjny lub scenariusze interakcji, otrzymuje dedykowany ViewModel.

### 3.2. Style i `DataTemplate` — powtarzalna prezentacja danych

Nie każdy powtarzalny wiersz wymaga własnej klasy kontrolki. Kolekcje elementów nawigacji, zdarzeń historii, kampanii albo bohaterów powinny być domyślnie renderowane przez `ItemsControl`, `ListBox` lub inny właściwy kontroler kolekcji wraz z typowanym `DataTemplate`.

Przykładowo sidebar nie powinien ręcznie deklarować kilkunastu osobnych instancji wyspecjalizowanego przycisku. Powinien otrzymać kolekcję `NavigationItemViewModel`, a jeden szablon określa ikonę, etykietę, stan aktywny i dostępność każdego wiersza.

Style służą do współdzielenia wyglądu istniejących kontrolek, gdy nie powstaje nowe zachowanie ani nowy kontrakt semantyczny.

### 3.3. `TemplatedControl` — generyczne elementy systemu projektowego

`TemplatedControl` stosujemy wtedy, gdy powstaje rzeczywiście generyczny, tematyzowalny element posiadający własne właściwości i stany, na przykład:

- rama panelu o formalnym kontrakcie nagłówka, treści i obszaru akcji;
- kontrolka wartości mechanicznej z etykietą, wartością i stanem semantycznym;
- specjalistyczny host ikony;
- element nawigacyjny, jeżeli możliwości wbudowanego `ListBoxItem` i jego szablonu okażą się niewystarczające;
- kontrolka dokowania w przyszłości.

Wygląd `TemplatedControl` jest określany przez `ControlTheme`, a zachowanie przez jawne `StyledProperty`, `DirectProperty`, komendy, zdarzenia i pseudoklasy.

Nie tworzymy własnej kontrolki rysowanej przez `Render`, jeżeli ten sam efekt można osiągnąć kompozycją, stylem lub szablonem istniejących kontrolek.

## 4. Kontrakt komponentu

Każdy istotny komponent powinien mieć możliwie mały i jawny interfejs.

### Wejścia

- typowany ViewModel przekazany przez `DataContext` albo jawne właściwości kontrolki;
- zawartość przekazana przez `Content`, `ItemsSource` lub właściwości semantyczne;
- tokeny stylu dziedziczone z motywu aplikacji;
- jawnie opisany wariant rozmiaru lub tryb, jeżeli komponent go obsługuje.

### Wyjścia

- komendy ViewModelu dla operacji użytkownika;
- zdarzenia kontrolki wyłącznie dla generycznych zachowań UI;
- jawna zmiana zaznaczenia, fokusowania lub dokowania;
- brak bezpośredniego zapisu do repozytorium i brak wywoływania domeny z code-behind.

### Gwarancje

- udokumentowany rozmiar minimalny, preferowany i sposób przepełnienia;
- stabilna geometria pomiędzy stanami `loading`, `empty`, `ready` i `error`;
- brak zależności od nazw elementów znajdujących się w rodzicu;
- brak wyszukiwania usług przez globalny service locator;
- brak założeń o konkretnym rodzicu poza formalnym kontraktem kontrolki;
- obsługa klawiatury i widoczny fokus;
- typowane, kompilowane bindingi.

## 5. Granice ViewModeli

ViewModel odpowiada za stan prezentacyjny i orkiestrację przypadków użycia potrzebnych danemu ekranowi. Nie zna konkretnych elementów XAML.

- `ShellViewModel` zna bieżący kontekst i aktywny ekran, ale nie implementuje zachowania wszystkich ekranów.
- ViewModel sidebara zna dostępne pozycje, zaznaczenie oraz komendy nawigacji, ale nie zna wewnętrznego layoutu przycisku.
- ViewModel ekranu pobiera modele odczytu z `Application` i udostępnia komendy użytkownika.
- ViewModel panelu jest wydzielany tylko wtedy, gdy panel ma własny stan lub zachowanie. Panel czysto prezentacyjny może otrzymać gotowy model od rodzica.

ViewModel nie może zawierać reguł kampanii, serializacji, ścieżek filesystemu ani zależności od kontrolek Avalonia.

## 6. Code-behind

Code-behind nie jest całkowicie zakazany. Może obsługiwać zachowania należące wyłącznie do widoku:

- zarządzanie fokusem;
- interakcje wskaźnika i przeciąganie;
- pomiar albo reakcję na jawny wariant rozmiaru;
- integrację z mechanizmami okna;
- animacje i zachowania niedające korzyści po przeniesieniu do ViewModelu.

Code-behind nie wywołuje przypadków użycia, nie interpretuje reguł kampanii i nie staje się drugim ViewModelem.

## 7. Bindingi i `DataContext`

W projekcie są globalnie włączone compiled bindings. Każdy `Window`, `UserControl` i `DataTemplate` powinien deklarować poprawne `x:DataType`. Błąd nazwy właściwości powinien być wykrywany podczas kompilacji.

Komponent generyczny nie ustawia w konstruktorze `DataContext = this`. Takie zachowanie przerywa dziedziczenie kontekstu przekazanego przez rodzica i prowadzi do trudnych do wykrycia błędów bindingu.

Wewnątrz `TemplatedControl` właściwości własne są odczytywane przez `TemplateBinding` albo jawne powiązanie z templated parent. Wewnątrz aplikacyjnego `UserControl` bindingi odnoszą się do zadeklarowanego ViewModelu.

## 8. Style nie są całkowicie hermetyczne

Hermetyzacja struktury nie oznacza duplikowania kolorów, odstępów i typografii wewnątrz każdego komponentu.

Warstwa stylów ma dwa poziomy:

1. **Wspólny system projektowy** — kolory, typografia, odstępy, grubości ramek, wysokości wierszy, fokus i stany semantyczne.
2. **Motyw lub styl komponentu** — struktura i stany charakterystyczne dla konkretnej kontrolki.

Komponent korzysta ze wspólnych tokenów, ale nie polega na przypadkowych selektorach potomków z innego widoku. Rodzic nie powinien sięgać selektorem głęboko do wnętrza dziecka, aby naprawić jego layout.

Zabronione jest rozproszenie surowych wartości kolorów i wymiarów po plikach widoków. Wyjątek wymaga uzasadnienia jako wartość rzeczywiście lokalna, a nie pominięty token.

## 9. Nawigacja i kompozycja ekranów

`MainWindow` hostuje `AppShellView`. Shell wybiera sidebar właściwy dla kontekstu oraz aktywny ekran workspace’u.

Orientacyjna kompozycja:

```xml
<Window>
    <shell:AppShellView />
</Window>
```

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

Przykład pokazuje oczekiwany poziom deklaratywności, a nie finalne nazwy właściwości ani mechanizm routingu. Rodzic odpowiada za rozmieszczenie swoich bezpośrednich dzieci. Dziecko odpowiada za własne wnętrze.

Widoki nie tworzą innych ViewModeli w code-behind. Kompozycja zależności odbywa się w composition root aplikacji, a nawigacja operuje na jawnych deskryptorach lub ViewModelach ekranów.

## 10. Proponowana organizacja katalogów Desktop

Preferowana jest organizacja feature-first z osobnym obszarem współdzielonego shellu i systemu projektowego:

```text
DungeonApp.Desktop/
  Shell/
    AppShellView.axaml
    AppShellViewModel.cs
    TopBar/
    Sidebars/
    StatusBar/
    Navigation/

  Features/
    CampaignLibrary/
    CampaignCreator/
    CampaignWorkspace/
      SessionDashboard/
      TimeAndEvents/

  Controls/
    PanelFrame/
    MechanicalValue/
    IconHost/

  Themes/
    Tokens.axaml
    Typography.axaml
    BuiltInControls.axaml
    DungeonControls.axaml

  Assets/
    Fonts/
    Icons/
    Licenses/
```

Widok, jego code-behind i ViewModel powinny znajdować się blisko siebie w katalogu funkcji. Nie utrzymujemy jednego rosnącego katalogu wszystkich widoków oraz osobnego, równoległego katalogu wszystkich ViewModeli.

## 11. Reguły zależności

- `Shell` może zależeć od kontraktów nawigacji i współdzielonych kontrolek.
- Funkcja może zależeć od `Application`, kontraktów nawigacji i współdzielonych kontrolek Desktop.
- Współdzielona kontrolka nie zależy od konkretnej funkcji ani od domenowego typu kampanii.
- Komponent prezentacyjny nie zależy od `Infrastructure`.
- `Desktop` tworzy implementacje `Infrastructure` wyłącznie w composition root; widoki i ViewModele nie odwołują się do nich bezpośrednio.
- Funkcje nie mogą stylować wnętrza innych funkcji przez globalne selektory.

## 12. Testowanie

- Reguły nawigacji i stan ekranów testujemy przez ViewModele bez uruchamiania Avalonia.
- Generyczne kontrolki z własnym zachowaniem otrzymują testy kontrolki albo celowane testy integracyjne.
- Kluczowe ekrany otrzymują przegląd wizualny w realistycznych stanach danych.
- Testy stabilności obejmują najkrótsze i najdłuższe oczekiwane etykiety, wartości jedno- i wielocyfrowe, brak ikon, loading, błąd oraz przepełnienie listy.
- Widoki powinny kompilować się z compiled bindings; ostrzeżenia bindingów nie mogą być normalnym sposobem wykrywania literówek w runtime.

## 13. Antywzorce

- Jeden wielki `MainWindow.axaml` zawierający wszystkie ekrany i stany.
- Jeden `MainWindowViewModel` orkiestrujący całą aplikację.
- `UserControl` dla każdego drobnego elementu wizualnego.
- Ręczne powtarzanie markupów elementów kolekcji zamiast `ItemsControl` i `DataTemplate`.
- Ustawianie `DataContext = this` w kontrolce generycznej.
- Bezpośrednie odwołania dziecka do elementów wizualnych rodzica.
- Ukryte pobieranie usług z globalnego kontenera.
- Reguły domenowe i wywołania repozytorium w code-behind.
- Globalne style ingerujące w prywatną strukturę konkretnego widoku.
- Duplikowanie tokenów wizualnych w wielu plikach.
- Wydzielanie komponentu bez jasnej odpowiedzialności tylko w celu skrócenia pliku XAML.

## 14. Kryterium przeglądu komponentu

Przed zaakceptowaniem nowego komponentu należy odpowiedzieć:

1. Jakie jedno pojęcie UX reprezentuje?
2. Jaki jest jego jawny kontrakt wejścia i wyjścia?
3. Kto jest właścicielem jego stanu?
4. Jak zachowuje geometrię w stanach loading, empty, ready i error?
5. Dlaczego jest `UserControl`, `TemplatedControl`, szablonem danych albo jedynie stylem?
6. Czy rodzic może zrozumieć kompozycję bez znajomości wnętrza dziecka?
7. Czy dziecko można zmienić bez modyfikowania niespokrewnionych ekranów?

Jeżeli odpowiedzi nie są jasne, granica komponentu wymaga ponownego wyznaczenia.
