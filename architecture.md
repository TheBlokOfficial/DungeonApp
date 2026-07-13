# Architektura Projektu: DungeonApp

Ten dokument stanowi mapę drogową i spis treści architektury całego projektu DungeonApp. Aplikacja została zbudowana w oparciu o wzorzec **MVVM (Model-View-ViewModel)** przy użyciu frameworka **Avalonia UI** oraz zestawu narzędzi **CommunityToolkit.Mvvm**.

---

## 1. Struktura Katalogów

Poniżej znajduje się opis głównych domen aplikacji, odzwierciedlonych w strukturze folderów:

### 🗃️ `Models/` (Struktury Danych)
Czyste, pozbawione logiki UI klasy definiujące obiekty dziedzinowe.
* **Główne byty:** `Campaign`, `PlayerCharacter`, `Session`.
* **Moduł ContentPacks (`Models/ContentPacks/`):** Złożony system struktur do obsługi dynamicznie wczytywanych danych z plików JSON (DLC/Paczki systemowe). Zawiera szablony przeciwników, przedmiotów, system tagów oraz odznak (badges).

### ⚙️ `Services/` (Logika Biznesowa i System)
Mózg operacyjny aplikacji, zarządzany przez kontener wstrzykiwania zależności (Dependency Injection).
* `AppPaths`: Globalne ścieżki do folderów systemowych i zapisów.
* `NavigationService` / `INavigationService`: Odpowiada za zmianę ekranów i przełączanie aktywnych widoków w głównym oknie.
* `FileStorageService`: Uniwersalny zapis/odczyt plików i konfiguracji.
* `ContentRegistry`: Silnik wczytujący paczki JSON z katalogu `Packs/` i udostępniający je dla widoków (np. Rejestru Przedmiotów).
* `TemplateEvaluator` / `TranslationService`: Moduły do dynamicznego renderowania tekstów oraz ich tłumaczenia z plików systemowych.

### 📦 `Packs/` (Zewnętrzna Baza Danych)
Katalog zawierający pakiety zawartości w formacie JSON.
* **`core/`**: Podstawowa paczka dla systemu D&D 5e (tłumaczenia, przedmioty, wrogowie, kategorie rzadkości, tagi). System jest elastyczny i może ładować dodatkowe paczki.

### 🎨 `Styles/` (System Designu)
Globalne zasoby graficzne, zmienne systemowe i style Avalonia.
* `Colors.axaml`: Paleta barw, definicje kontrastów, style dark/light mode (jeśli wdrożone).
* `Metrics.axaml`: Globalne marginesy, zaokrąglenia, grubości ramek (utrzymujące stały design-system w aplikacji).
* `CommonStyles.axaml`: Uniwersalne definicje klas dla np. `<Button Classes="ghost">`.

---

## 2. Architektura UI (Views & ViewModels)

Zgodnie z koncepcją MVVM, do każdego `[Nazwa]View.axaml` przypisany jest logiczny kontroler `[Nazwa]ViewModel.cs`.

### 🏠 `Dashboard` (Menu Główne i Rejestry)
To tutaj trafia użytkownik po uruchomieniu aplikacji.
* Widoki globalne: `CampaignsTabView`, `CharactersTabView`, `SettingsTabView`.
* Widoki bazodanowe (wczytujące dane z `ContentRegistry`): `ItemsTabView`, `AdversariesTabView`.
* Rejestry współdzielą układ wizualny bazujący na kontrolce `RegistryLayoutControl`.

### 🗺️ `Campaigns` (Zarządzanie Kampanią)
Widoki obsługujące konkretną kampanię RPG.
* `CreateCampaignView`: Formularz tworzenia nowej przygody.
* **`CampaignDetailView` (Z-Index Shell)**: Zaawansowane główne okno kampanii. W tym miejscu stosowana jest architektura warstwowa. Okno zajmuje się zarządzaniem pływającym paskiem (HUD) portretów graczy oraz hostowaniem pod-zakładek z pomocą `<TransitioningContentControl>`.
* **Katalog `Tabs/`**: Pakiety modułów wstrzykiwanych do okna kampanii:
  * `CampaignDashboardView`: Bento Box – centrum dowodzenia kampanią, notatki systemowe, oś czasu.
  * `CampaignTrackerView`: Moduł do prowadzenia inicjatywy i walki (w budowie).
  * `CampaignNotesView`: Zaawansowany edytor notatek fabularnych (w budowie).
  * `CampaignStoryView`: Zarządzanie osią fabularną (w budowie).

### ⚔️ `Sessions` (Aktywne Rozgrywki)
Panel do prowadzenia pojedynczych, trwających sesji wewnątrz konkretnej kampanii. Zawiera własne moduły notatek i walki, wyizolowane na czas trwania konkretnego spotkania.

### 🎭 `Characters` (Bohaterowie)
Odseparowany moduł tworzenia kart postaci graczy, ich statystyk i ekwipunku (`CreateCharacterView`, `CharacterDetailView`).

### 🧱 `Controls` i `Components` (Wielokrotnego Użytku)
Modularne kontrolki wykorzystywane w wielu miejscach w projekcie, by redukować powtarzalność kodu:
* `AbilityScoresTable`: Ujednolicony panel do wyświetlania statystyk (Siła, Zręczność itp.).
* `BadgeControl`: System kolorowych znaczników/tagów.
* `RegistryLayoutControl`: Zunifikowany szkielet (tabela + filtry) dla list rejestrowych.

---

## 3. Konwencje i Workflow
- **Dependency Injection**: Aplikacja korzysta z biblioteki `Microsoft.Extensions.DependencyInjection`. Wszystkie usługi oraz ViewModele są rejestrowane i wstrzykiwane w `App.axaml.cs`.
- **Nawigacja**: Główne przejścia opierają się na przekazywaniu nowych ViewModeli do nadrzędnego `MainWindowViewModel`.
- **Podejście XAML**: Promujemy stabilność układu graficznego (Anti-Jumping UI) – rezygnujemy z ukrywania wierszy, które niszczą siatkę na rzecz nakładania elementów (Z-Index), używania `IsHitTestVisible` oraz `TransitioningContentControl` do płynnej animacji zakładek.
- **Data Binding**: Obsługiwany potężnym narzędziem Source Generators z poziomu `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).
