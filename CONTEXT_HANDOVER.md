# CONTEXT HANDOVER: System Konsoli, Komend i Animowanego Feedu
**Data utworzenia raportu:** Bieżąca sesja implementacyjna

## 1. Wstęp i Główne Założenia
Wdrożony system Konsoli stanowi teraz jedną z najbardziej zaawansowanych i dopracowanych osi komunikacyjnych w aplikacji *DungeonApp*. Naszym celem było stworzenie rozwiązania przypominającego nowoczesne systemy z silników gier (np. Unreal Engine) czy profesjonalnych VTT, które oferuje m.in. płynne animacje, inteligentne autouzupełnianie w stylu Minecrafta (Brigadier), pełną modularność oraz elegancki UX pozbawiony typowych wad interfejsów desktopowych.

Poniższy raport to tytaniczne podsumowanie wszystkich decyzji architektonicznych, naprawionych problemów oraz detali technicznych.

---

## 2. Architektura Silnika Komend (Autocomplete Engine)

### 2.1 Wzorzec Drzewa (CommandTree) i Walidacja (Brigadier)
Silnik został oparty na strukturze drzewiastej, gdzie każdy węzeł (`CommandNode`) definiuje poprawność danego argumentu.
- **Typy argumentów:** Zaimplementowano specjalistyczne parsery: `Literal` (stałe słowa), `String` (ciągi znaków), `Number`, `Enum` (wybór ze zdefiniowanej listy) oraz wybitnie elastyczny `TimeArgumentType` (obsługujący zapisy w stylu `10s`, `5m`, `2h`, `1d` z przeliczaniem na sekundy).
- **Zasada działania:** Podczas wpisywania każdego znaku, silnik iteruje po drzewie komend, walidując poszczególne fragmenty tekstu (tzw. "tokeny").
- **Ghost Text:** W locie podpowiada szary tekst (pierwszą pasującą komendę/argument), dając użytkownikowi wizualny znak, jakiej składni system się aktualnie spodziewa. Autouzupełnianie zatwierdzane jest klawiszem `Tab`.
- **Syntax Highlighting:** Wyodrębniono osobną klasę wspomagającą, która dynamicznie nadaje kolory (czerwony na błędy, zielony dla liczb, niebieski/żółty na literały) poszczególnym słowom wpisywanym w pole poleceń.

### 2.2 Automatyzacja Nadawców (OwnerModuleId)
Aby całkowicie odciążyć twórców nowych modułów od pamiętania o przedrostkach, zbudowano inteligentny system śledzenia własności komend:
1. Podczas inicjalizacji, np. `TimekeeperModule` (o ModuleId: `"Core.Timekeeper"`) wysyła `RegisterCommandEvent`.
2. Odbierając to, system na stałe nakleja na główny węzeł drzewa komendy "pieczątkę" właściciela (`OwnerModuleId`).
3. Gdy komenda zostaje wykonana poprawnie, system łapie wygenerowany przez nią rezultat, automatycznie wstrzykuje do niego ten zapamiętany identyfikator i przesyła dalej na magistralę.

### 2.3 Wymuszenie Informacji Zwrotnej (CommandResult)
- Wdrożono *Compiler-Driven Design*. Zamiast polegać na pustej akcji, interfejs komendy `.Executes(Func<CommandContext, CommandResult>)` wymusza na programiście zwrócenie konkretnego obiektu wyniku (Sukces, Błąd, Info, lub zadeklarowany Silent). Zapobiega to usterce zwanej "cichym wykonaniem", gdzie użytkownik po wpisaniu komendy nie otrzymywał żadnego wizualnego potwierdzenia.

---

## 3. Renderowanie UI: Fabryka i Inlines

### 3.1 Fabryka Widoków (ConsoleTemplateFactory)
Zamiast tradycyjnych (i często ograniczonych) szablonów `DataTemplate` w XAML, logi konsoli budowane są całkowicie programatycznie w klasie pomocniczej z użyciem C#. 
Daje to gigantyczną przewagę przy dynamicznej kreacji rzadkich elementów (takich jak `ProposalEvent` zawierający klikalne przyciski `[accept]` / `[reject]`, które bindują się wprost pod komendy w `ConsoleModule`).

### 3.2 Zaznaczalność i Inlines
Początkowo prefixy i właściwe komunikaty były umieszczane w oddzielnych kolumnach systemowego `Grid`. Generowało to "efekt blokowy" i uniemożliwiało płynne kopiowanie tekstu myszką.
- **Rozwiązanie:** Zastąpiono `Grid` pojedynczą kontrolką `SelectableTextBlock` należącą do Avalonii (wsparcie dla tekstu zaznaczalnego). Poszczególne kolorowe tagi (np. `[INFO]`, `[Timekeeper]`) są wpychane do kontrolki jako wstawki typu `Run`. Efekt to wizualnie trójkolorowy log, który można skopiować jednym prostym przeciągnięciem kursora!

---

## 4. Animacje i "Pancerny" Layout (AnimatedFeedList)

### 4.1 Efekt Wypychania (Minecraft Chat-Style)
Konsola nie jest nudną listą dodającą itemy na końcu kontrolki `ListBox`. To dedykowana kontrolka oparta o obszar `Canvas`.
- Każdy dodawany log renderowany jest najpierw poza ekranem dla pobrania jego wymiarów (DesiredSize).
- Następnie, za sprawą dedykowanych tranzycji (`DoubleTransition` z krzywą `CubicEaseOut`), wszystkie obecne w konsoli logi są płynnie przesuwane do góry dokładnie o wymiar nowego wpisu, podczas gdy on sam "wyjeżdża" od dołu, przechodząc z `Opacity 0` do `Opacity 1`.

### 4.2 Złoty Kompromis (Brak Multi-log Select)
Ze względu na architekturę fizycznie odseparowanych elementów na obszarze Canvas, niemożliwe jest natywne przeciągnięcie zaznaczenia myszką z jednego logu do drugiego. 
- Po konsultacji podjęliśmy świadomą decyzję (Opcja A), stawiając rewelacyjne UX animacji ponad możliwość kopiowania wielu logów naraz, co jest nieznacznym "podatkiem" za innowacyjność tego interfejsu.

### 4.3 Pływający pasek wpisywania i TextWrapping
W ramach szlifowania UI, rozwiązano problem nieestetycznego chowania się tekstu poza krawędzie:
1. Trójwarstwowa struktura wprowadzania tekstu (Ghost, Składnia, Rzeczywisty kursor) została opakowana wspólnym wertykalnym kontenerem `ScrollViewer` z parametrem `MaxHeight="120"`.
2. Zastosowano `TextWrapping="Wrap"` i wertykalne równanie do góry. Skutkuje to tym, że przy bardzo długich komendach pole delikatnie zwiększa swoją wysokość o kolejne linijki (niczym w najlepszych komunikatorach biznesowych).
3. **Genialna synergia DeltaY:** Na obiekcie `AnimatedFeedList` podpięto nasłuchiwanie na `BoundsProperty`. Gdy wpisujesz nową linię tekstu i pasek konsoli puchnie w górę (zmniejszając wysokość przydzieloną na Canvas), silnik natychmiast kalkuluje ubytki przestrzeni (`deltaY`) i błyskawicznie przesuwa wygenerowane wcześniej animowane logi do góry, zapobiegając ich przysłanianiu przez podnoszący się pasek wpisywania.

---

## 5. Implementacja Modułu Czasu (TimekeeperModule)
Kompletne przepisanie interakcji czasowych z pominięciem starych mechanizmów:
- Usunięto niepożądaną komendę `set` chroniąc strukturę chronologiczną kampanii.
- Utworzono intuicyjne polecenie `/time add <wartość_czasowa>` (np. `/time add 4d`), które perfekcyjnie mapuje się z `TimeArgumentType` zamieniając wejście na bezwzględne sekundy, a następnie odpalając silnikową metodę `AdvanceTime(minutes)`.
- Metoda `AdvanceTime` w ułamku sekundy kaskadowo przelicza minuty, godziny i dni według autorskiego kalendarza Fantasy, zapisuje je trwale na dysku i wymusza odświeżenie UI zegarka widocznego w pasku bocznym gry.
- Dodano `/time query` — bezinwazyjną komendę diagnostyczną dla MG.

## Podsumowanie i Wizja
Mechanizm Konsoli stanowi od teraz kompletny, "strzeloodporny" fundament. Użytkownicy otrzymują wbudowany system edukacji w czasie rzeczywistym (Ghost Text), błyskawiczną interakcję oraz satysfakcjonujący feedback wizualny, podczas gdy przyszli twórcy modułów zostali wyzwoleni od konieczności obsługi złożonego boilerplate'u i walidacji tekstu – wszystkim tym zajmuje się Magistrala!
