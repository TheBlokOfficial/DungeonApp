# Modele danych systemu i kampanii w aplikacjach TTRPG

Stan źródeł: **6 września 2026 r.** Kod Foundry `dnd5e` przywołano z wydania **5.3.3**; przykłady D&D 5e API pochodzą z endpointów **`/api/2014/`**. Odnośniki do pozostałych analizowanych plików kodu wskazują konkretne rewizje. **„Wniosek” i „Opcje i koszty” oznaczają analizę autora**, nie deklarację twórców aplikacji. Fragmenty JSON są oznaczonymi wyciągami albo przykładami składni, nie kompletnymi plikami importowymi.

## 1. Podział definicja / instancja

**Odpowiedź wprost.** Foundry po imporcie tworzy lokalny dokument, a przedmiot postaci jest dokumentem osadzonym w jej `Actor`; nie jest samym identyfikatorem wpisu katalogowego. Open5e i D&D 5e API opisują przede wszystkim katalog, więc ich rekordów nie należy utożsamiać z egzemplarzami w kampanii. W danych obsługiwanych przez DDB Importer występują jednocześnie identyfikatory definicji i rozwinięty obiekt `definition`, ale to nie dowodzi, że serwer D&D Beyond przechowuje osobną kopię definicji dla każdej postaci. [Foundry: import][f-compendium], [Owned Items][f-items], [Open5e: zakres API][o-docs], [DDB: typ ekwipunku][d-item].

### Foundry VTT / dnd5e

Występują trzy odrębne miejsca przechowywania: dokument w compendium, dokument świata oraz dokument osadzony w aktorze. Przykładowe postacie UUID to `Compendium.<pakiet>.<paczka>.Item.<id>`, `Item.<id>` i `Actor.<id>.Item.<id>`. Związek pochodzenia, np. `_stats.compendiumSource`, jest czymś innym niż własność dokumentu; kod systemu osobno posługuje się UUID źródła i tworzy lokalne kopie. [Dokumenty przedmiotów][f-items], [lokalizacja i kopiowanie aktorów][f-actor], [kopiowanie zawartości przedmiotów][f-itemdoc].

Import pojedynczego dokumentu tworzy nową kopię; późniejsze zmiany w świecie nie zmieniają katalogu. Dokumentacja wskazuje zachowanie oryginału do ponownego użycia jako korzyść tego rozdzielenia. **Wniosek o koszcie:** poprawka katalogowego miecza nie jest automatycznie poprawką wszystkich lokalnych mieczy; potrzebna jest polityka aktualizacji kopii, uwzględniająca zmiany MG. [Compendium Packs][f-compendium].

„Definicja” jest tutaj rolą dokumentu w katalogu, a nie osobnym, okrojonym typem bez pól stanu. Compendium może zawierać `Item` z `quantity`, `equipped` i `uses`, które po skopiowaniu stają się stanem konkretnego posiadacza. [PhysicalItemTemplate][f-physical], [EquippableItemTemplate][f-equip], [ActivitiesTemplate][f-activities].

### D&D Beyond

W modelu wejściowym DDB Importer `inventory[]` zawiera m.in. `id`, `definitionId`, `definitionTypeId`, `definition`, `quantity`, `equipped` i `isAttuned`. Rozwinięta definicja niesie np. `name`, `damage`, `properties`, `grantedModifiers`, `sources` i `isHomebrew`. To rozdzielenie identyfikacji egzemplarza i treści definicji jest widoczne na granicy integracji. [IDDBInventoryItem i IDDBItemDefinition][d-item].

**Granica potwierdzenia:** jest to publiczny kod konsumenta danych, częściowo otrzymywanych przez jego proxy, a nie oficjalny kontrakt API D&D Beyond. Autor importerowych typów uprzedza, że tworzył je na potrzeby własnego parsera i nie rekomenduje ich innym. Nie ustalono na tej podstawie, które rozwinięcia dodaje serwer DDB, które proxy ani jak wygląda fizyczny zapis bazy DDB. [Opis typów][d-types], [interfejs odpowiedzi proxy][d-character].

### Open5e, D&D 5e API i 5etools

Open5e V2 rozdziela nawet **dwa poziomy katalogowe**: `Item` może wskazywać `Weapon` albo `Armor`. Komentarz modelu `Weapon` wyraźnie określa go jako typ broni; egzemplarz rzeczy w katalogu odsyła do niego przez relację. Nie jest to jeszcze miecz należący do konkretnego bohatera. [ItemBase][o-item], [Weapon][o-weapon].

D&D 5e API używa odwołań postaci `{index, name, url}` do kategorii, typu obrażeń czy właściwości. 5etools rozróżnia np. `Longsword` ze źródła `PHB` i `Longsword` ze źródła `XPHB`, z odrębną edycją i `reprintedAs`. Nazwa użytkowa nie wystarcza więc do identyfikacji treści. [Rekord Longsword][a-sword], [dane bazowego ekwipunku 5etools][t-items].

### Roll20 — istotne doprecyzowanie

`sheet.json` w repozytorium kart jest **manifestem szablonu karty**, a nie JSON-em konkretnej postaci. Dane przechowują atrybuty karty i powtarzalne sekcje. Import compendium wypełnia pola określone przez `accept="Name"`, `accept="Damage"` itp.; zmiana wywołuje zdarzenia tak jak ręczne wpisanie danych. [Repozytorium kart Roll20][r-repo], [Compendium Integration][r-import].

### Opcje i koszty

| Podejście | Co daje | Koszt |
| --- | --- | --- |
| Odwołanie do aktualnej definicji | Jeden punkt poprawiania treści | Wynik odczytu może zmienić się po aktualizacji; konieczna obsługa brakującego źródła |
| Pełna kopia przy imporcie | Lokalna edycja i niezależność skopiowanych pól | Duplikacja, rozbieżne kopie, konieczność jawnego scalania aktualizacji |
| Definicja i zapis różnic | Mniej powielanych danych, widoczne odstępstwa | Rozwiązywanie zależności i konflikty różnic ze zmienioną bazą |
| Kopia oraz identyfikator pochodzenia | Samodzielny zapis z możliwością porównania ze źródłem | Sam identyfikator nie mówi, które pola zmienił MG; do scalania przydaje się również wersja lub poprzednia baza |

To opcje wynikające z opisanych mechanizmów. Nie ustalono publicznej wypowiedzi twórców DDB uzasadniającej wybór ich wewnętrznego modelu zapisu.

## 2. Kształt danych: przedmiot, zaklęcie, potwór

**Odpowiedź wprost.** Wszystkie trzy rodziny przekraczają model płaskiego formularza: przedmioty mają wielowariantowe użycie i relacje, zaklęcia alternatywne sposoby rzucenia, a potwory listy akcji z dalszymi zagnieżdżeniami. Część projektów strukturyzuje obliczenia, inne przede wszystkim prezentację tekstu. Sam fakt, że pole znajduje się w JSON-ie, nie oznacza, że jego znaczenie jest wykonywalne. [DamageData][f-damage], [SpellCastingOption][o-spell], [MonsterAction][a-monster-schema], [rekordy potworów 5etools][t-monsters].

### 2.1. Przedmiot

**Rzeczywisty rekord `Longsword`, D&D 5e API — wyciąg z odpowiedzi:**

```json
{
  "index": "longsword",
  "name": "Longsword",
  "weapon_category": "Martial",
  "weapon_range": "Melee",
  "cost": {"quantity": 15, "unit": "gp"},
  "weight": 3,
  "damage": {
    "damage_dice": "1d8",
    "damage_type": {
      "index": "slashing",
      "name": "Slashing",
      "url": "/api/2014/damage-types/slashing"
    }
  },
  "range": {"normal": 5},
  "properties": [{
    "index": "versatile",
    "name": "Versatile",
    "url": "/api/2014/weapon-properties/versatile"
  }],
  "two_handed_damage": {
    "damage_dice": "1d10",
    "damage_type": {
      "index": "slashing",
      "name": "Slashing",
      "url": "/api/2014/damage-types/slashing"
    }
  }
}
```

Źródło: [Longsword — odpowiedź API][a-sword]. Waga jest wartością katalogową; pokazany rekord nie opisuje właściciela ani dostrojenia.

**Gdzie kończą się proste pola:**

| Pole ze schematu D&D 5e API | Struktura i znaczenie |
| --- | --- |
| `cost` | Obiekt liczby monet i jednostki, nie sama cena |
| `damage`, `two_handed_damage` | Dwa profile obrażeń; profil zawiera formułę i odwołanie do typu obrażeń |
| `properties[]` | Lista referencji do osobnych definicji właściwości |
| `armor_class` | Obiekt `base`, `dex_bonus`, opcjonalne `max_bonus`; opis reguły AC pancerza |
| `contents[]` | Wpisy `item: APIReference` i `quantity`; katalogowa zawartość zestawu |
| `range`, `throw_range` | Osobne obiekty z zasięgiem normalnym i długim |
| Pola kategorii | Jeden model ma opcjonalne pola broni, pancerza, narzędzia, pojazdu itd.; zestaw nie jest jednorodny |

Źródło: [model Equipment i jego klasy pomocnicze][a-equipment-schema].

**Foundry dnd5e:** `WeaponData` składa szablony danych i własne pola, m.in. `type`, `damage`, `properties`, `range`, `mastery`, `magicalBonus`. Wspólny `system.activities` pozwala zapisać wiele sposobów użycia jednego przedmiotu. [WeaponData][f-weapon], [ActivitiesTemplate][f-activities].

Pojedyncza część obrażeń ma `number`, `denomination`, `bonus`, zbiór `types`, obiekt `custom {enabled, formula}` i `scaling {mode, number, formula}`. **Wariant jest jawny:** generator składa formułę z kości i bonusu albo bierze `custom.formula`; gdy kości nie ma, może pozostać sam bonus. Oprócz „liczba czy kostka” dochodzi więc „parametry czy własne wyrażenie”. [DamageData, getter `formula` i `_automaticFormula`][f-damage].

**Open5e V2:** `ItemBase.weapon`, `.armor` i `.category` są kluczami obcymi. `WeaponPropertyAssignment` jest osobnym rekordem łączącym broń z właściwością, z dodatkowym `detail`. **Relacja sama potrzebuje danych**, np. parametru właściwości; lista samych identyfikatorów nie zawsze wystarczy. [ItemBase][o-item], [WeaponPropertyAssignment][o-weapon].

### 2.2. Zaklęcie

**Rzeczywisty `Fireball`, D&D 5e API — wyciąg:**

```json
{
  "index": "fireball",
  "level": 3,
  "range": "150 feet",
  "components": ["V", "S", "M"],
  "ritual": false,
  "concentration": false,
  "casting_time": "1 action",
  "duration": "Instantaneous",
  "damage": {
    "damage_type": {
      "index": "fire",
      "name": "Fire",
      "url": "/api/2014/damage-types/fire"
    },
    "damage_at_slot_level": {
      "3": "8d6", "4": "9d6", "5": "10d6", "6": "11d6",
      "7": "12d6", "8": "13d6", "9": "14d6"
    }
  },
  "dc": {
    "dc_type": {
      "index": "dex",
      "name": "DEX",
      "url": "/api/2014/ability-scores/dex"
    },
    "dc_success": "half"
  },
  "area_of_effect": {"type": "sphere", "size": 20}
}
```

Źródło: [Fireball — odpowiedź API][a-fireball]. Pełny rekord zawiera też `name`, `desc[]`, `higher_level[]`, `material`, `school`, `classes[]`, `subclasses[]`, `url`, `updated_at`.

**Granice prostego modelu:** `damage_at_slot_level` jest mapą poziomu slotu na formułę; dla innych zaklęć schemat dopuszcza `damage_at_character_level` oraz `heal_at_slot_level`. `dc` opisuje rodzaj rzutu i skutek sukcesu, a nie stałe DC każdego rzucającego. `school`, `classes` i `subclasses` odsyłają do innych zasobów. `range`, `casting_time`, `duration` i część wyjątków w `desc` nadal są tekstem. [Model Spell][a-spell-schema].

**Open5e V2:** poza `level`, `school`, `desc`, `higher_level` są m.in. `target_type`, `range_text`, `range`, `range_unit`, `reaction_condition`, `verbal`, `somatic`, `material`, `material_specified`, `material_cost`, `material_consumed`, `target_count`, `saving_throw_ability`, `attack_roll`, `damage_roll`, `damage_types`, `shape_type`, `shape_size`, `concentration`, `classes`. [Model Spell][o-spell].

Najważniejszy dodatkowy byt to **`SpellCastingOption`**: `parent`, `type` oraz opcjonalne nadpisania `damage_roll`, `target_count`, `duration`, `range`, `concentration`, `shape_size`, `desc`. Dla opisanych w kodzie pól `null` oznacza **dziedziczenie wartości podstawowej**, a nie brak właściwości. `concentration: false` i `concentration: null` mają więc inne znaczenie. [SpellCastingOption][o-spell].

Jest też bezpośredni ślad ograniczenia schematu: `target_type` pozostaje pojedynczym polem tekstowym, ale komentarz autora zaznacza, że powinno być listą. To wskazane w samym kodzie miejsce, gdzie pojedyncza wartość jest zbyt wąska. [Pole `target_type`][o-spell].

**5etools — rzeczywisty wariant typu pola:** `Fireball.components.m` jest tekstem, natomiast `Revivify.components.m` jest obiektem z `text`, `cost: 30000` i `consume: true`. `range` zagnieżdża `distance`, a `time[]` i `duration[]` są listami; `Revivify.range.distance.type` ma wartość `touch`, bez liczbowego dystansu. Opis działania znajduje się w `entries`, skalowanie opisowe w `entriesHigherLevel`; znaczniki takie jak `{@damage 8d6}` wspierają renderowanie i rzuty, ale nie stanowią pełnej procedury rozstrzygnięcia zaklęcia. [Rekordy zaklęć PHB][t-spells].

**Foundry dnd5e 5.3.3:** `SpellData` zawiera `level`, `school`, `activation`, `duration`, `range`, `target`, `properties`, `materials {value, consumed, cost, supply}`, a także `method`, `prepared` i `sourceItem`. To ponownie połączenie treści zaklęcia i danych jego użycia przez postać. Nie należy przepisywać do tego wydania starszych przykładów `preparation.mode` / `preparation.prepared` bez uwzględnienia migracji. [SpellData][f-spell].

### 2.3. Potwór

**Rzeczywisty `Goblin`, D&D 5e API — wyciąg:**

```json
{
  "index": "goblin",
  "size": "Small",
  "type": "humanoid",
  "subtype": "goblinoid",
  "armor_class": [{
    "type": "armor",
    "value": 15,
    "armor": [
      {"index": "leather-armor", "name": "Leather Armor", "url": "/api/2014/equipment/leather-armor"},
      {"index": "shield", "name": "Shield", "url": "/api/2014/equipment/shield"}
    ]
  }],
  "hit_points": 7,
  "hit_dice": "2d6",
  "hit_points_roll": "2d6",
  "speed": {"walk": "30 ft."},
  "strength": 8,
  "dexterity": 14,
  "constitution": 10,
  "intelligence": 10,
  "wisdom": 8,
  "charisma": 8,
  "senses": {"darkvision": "60 ft.", "passive_perception": 9},
  "challenge_rating": 0.25,
  "xp": 50
}
```

Pełny rekord dodaje `alignment`, `proficiencies[]`, listy odporności, `languages`, `special_abilities[]`, `actions[]`, `legendary_actions[]`, `reactions[]`, `forms[]` i metadane. [Goblin — odpowiedź API][a-goblin].

**Miejsca złożoności w schemacie:**

| Obszar | Rzeczywisty model |
| --- | --- |
| AC | Lista wariantów oznaczonych `type`: `dex`, `natural`, `armor`, `spell`, `condition`; część zawiera kolejne referencje |
| Akcja | `name`, `desc`, `attack_bonus`, `damage[]`, `dc`, `usage`, `options`, `multiattack_type`, `actions`, `action_options` |
| Obrażenia akcji | `(Damage \| Choice)[]`; element może być obrażeniem albo wyborem |
| Multiattack | Odwołania `action_name`, liczba `count`, rodzaj `type`; `count` jest dopuszczone jako `number \| string` |
| Spellcasting | `ability`, `dc`, `modifier`, `components_required[]`, `slots`, `spells[]`; zaklęcie może mieć własne `usage` |
| Wybór | `choose`, `type`, `from`; zestaw opcji ma `option_set_type`, a opcja `option_type` i dalsze dane |

Źródła: [Monster i klasy akcji][a-monster-schema], [Choice, OptionSet i warianty Option][a-choice]. **Wniosek:** reprezentacja akcji jako „nazwa + obrażenia” gubi wieloatak, alternatywy, dodatkowe składowe i odnowienie użycia.

W realnym `Adult Red Dragon` `Bite.damage[]` ma dwie składowe: `2d10+8` piercing i `2d6` fire. `Fire Breath.usage` ma `type: "recharge on roll"`, `dice: "1d6"`, `min_value: 5`. To dwa niezależne problemy: wieloskładnikowy skutek oraz reguła odzyskiwania możliwości użycia. [Adult Red Dragon][a-dragon].

**5etools:** goblin ma `type` jako obiekt z `tags`, a smok jako napis `"dragon"`; AC roju nietoperzy to `[12]`, a goblina lista obiektów `ac` i `from`. HP ma `average` oraz `formula`. `legendaryGroup {name, source}` odsyła do wspólnego opisu, a akcje są blokami `entries` ze znacznikami, np. `{@hit 4}` i `{@damage 1d6 + 2}`. To schemat silnie nastawiony na wierną prezentację statbloku. [Bestiariusz MM][t-monsters].

**Open5e V2:** `Creature` dziedziczy HP i AC z `Object`. Akcje są osobnymi `CreatureAction`, z `action_type`, `uses_type`, `uses_param`, `order_in_statblock`, `limited_to_form`, `legendary_action_cost`; ich `attacks` wskazują `CreatureActionAttack`. Atak rozdziela `damage_die_count`, `damage_die_type`, `damage_bonus`, `damage_type` oraz analogiczne pola `extra_damage_*`. To konkretna strukturyzacja, ale dwie grupy obrażeń nadal są mniej ogólne niż lista dowolnej długości. [Object][o-object], [CreatureAction i CreatureActionAttack][o-creature].

### Opcje i koszty

| Podejście | Zastosowanie | Koszt |
| --- | --- | --- |
| Pola liczbowe i opis wyjątków | Katalog oraz karta czytana przez MG | Wyjątki pozostają niewyszukiwalne lub niepoliczalne bez interpretacji |
| Struktury dla najczęstszych przypadków i dodatkowy tekst | Część filtrów i obliczeń bez modelowania wszystkiego | Dwie reprezentacje mogą sobie przeczyć; trzeba określić pierwszeństwo |
| Warianty oznaczone typem i listy składowych | Jawne rozróżnienie działania, wyboru, stałej i kości | Więcej walidatorów, formularzy i przypadków odczytu |
| Dane opisujące wykonanie wielu aktywności | Wielofunkcyjne przedmioty i zaklęcia | Trzeba utrzymywać semantykę wykonania, zużycie zasobów, zależności i migracje |

## 3. Wartości pochodne

**Odpowiedź wprost.** Foundry oddziela dane źródłowe od przygotowanego widoku i wylicza wiele statystyk podczas przygotowania dokumentów; niekoniecznie przy każdym wywołaniu gettera. Roll20 pozwala również zapisać wynik obliczeń do zwykłego atrybutu i aktualizować go zdarzeniami. D&D Beyond udostępnia integracjom dane wejściowe i modyfikatory, ale publiczne źródła nie pozwalają pewnie odtworzyć granicy między jego bazą danych, cache i obliczeniami klienta. [DataModel][f-datamodel], [CharacterData][f-character], [Sheet Workers][r-workers], [parser HP DDB][d-hp].

### Foundry: zapis i przygotowany wynik

| Wartość | Dane źródłowe i wynik w dnd5e 5.3.3 |
| --- | --- |
| HP bieżące i tymczasowe | `system.attributes.hp.value`, `.temp`; zmienny stan |
| Maksymalne HP bohatera | `.hp.max` jest nullable override; przy `null` system używa awansów `HitPoints`, modyfikatora cechy i `.hp.bonuses.level/overall` |
| Poziom i proficiency | Poziomy zapisane na przedmiotach typu `class`; suma trafia do `details.level`, z niej liczone `attributes.prof` |
| Umiejętności | Stopień biegłości, cecha i bonusy są wejściem; `skills.<id>.total` i `.passive` przygotowuje kod |
| AC | Wybór sposobu liczenia i jego parametry są wejściem; `attributes.ac.value` powstaje z pancerza, tarczy, cech i bonusów |
| Użycia przedmiotu | `uses.spent` jest stanem, `uses.max` źródłową formułą; `uses.value` jest wynikiem odejmowania i ograniczenia zakresu |

Źródła: [HP i proficiency bohatera][f-character], [umiejętności][f-creature], [HP i AC][f-attributes], [UsesField][f-uses]. Rola `hp.max` zależy od typu aktora: opis automatycznego liczenia z awansów dotyczy bohatera, nie każdego NPC.

`DataModel._source` przechowuje dane źródłowe. `toObject()` domyślnie eksportuje źródło, a `toObject(false)` dane przekształcone; `reset()` przywraca model ze źródła. To ważna różnica między „pole widzę w obiekcie w konsoli” i „pole zostało zapisane jako wynik”. [DataModel API][f-datamodel].

### Active Effects: deklaracje obsługiwane przez kod

Active Effect przechowuje zmiany wskazujące klucz pola, tryb i wartość. Tryby obejmują dodawanie, mnożenie, zastąpienie, minimum/maksimum realizowane przez Upgrade/Downgrade oraz Custom obsługiwany przez system lub moduł. Efekt zmienia przygotowany obraz danych bez zastępowania wartości bazowej. [Active Effects][f-effects].

Przykłady **konfiguracji według dokumentacji dnd5e**, zapisane w postaci tabeli zamiast pełnego dokumentu efektu:

| `key` | Tryb | `value` | Znaczenie |
| --- | --- | --- | --- |
| `system.abilities.str.value` | Upgrade | `19` | Podniesienie STR co najmniej do 19 |
| `system.bonuses.abilities.save` | Add | `1d4` | Dodatkowa kość przy rzucie obronnym |
| `system.bonuses.abilities.save` | Add | `@abilities.cha.mod` | Bonus zależny od modyfikatora CHA |

Formuły nie są dozwolone jednakowo na wszystkich ścieżkach. Dokumentacja zaznacza też, że bonus może wejść do rzutu, choć nie pojawi się w liczbie prezentowanej na karcie. **Wniosek:** istnieje mały język formuł i zależności, ale jego semantykę ogranicza kolejność przygotowania i kod konkretnego pola; nie jest to uniwersalny silnik wszystkich reguł D&D. [Active Effect Guide][f-effectguide].

### D&D Beyond: modyfikatory i kod ich interpretacji

Oficjalny przykład tworzenia magicznej broni używa konfiguracji **Bonus → Magic → Fixed Value: 2**. Ten sam poradnik rozróżnia dodatkowe kości obrażeń, bonus cechy i jej maksymalną wartość; warunki mogą jedynie pojawić się w notatce, bez automatycznego zastosowania. [Poradnik tworzenia magicznego przedmiotu][d-homebrew-guide].

Model konsumenta zawiera `type`, `subType`, `value`, `fixedValue`, `statId`, `restriction`, `requiresAttunement`, `componentId`, `componentTypeId`, `dice`/`die` i identyfikatory typów. `IDDBDamageDice` rozdziela `diceCount`, `diceValue`, `diceMultiplier`, `fixedValue`, `diceString`. Sam napis `restriction` nie jest dowodem istnienia formalnego języka predykatów. [Typy modifier i dice][d-base].

Importer liczy HP z `baseHitPoints`, premii za CON i poziomy, modyfikatorów `hit-points-per-level` oraz override; do HP bieżących uwzględnia `bonusHitPoints` i `removedHitPoints`, osobno zachowując `temporaryHitPoints`. Obliczenia są zapisane w TypeScript. To potwierdza możliwość odtworzenia wartości z danych wejściowych, **nie** to, że DDB nie przechowuje gotowego wyniku w swoim cache. [Implementacja `_generateHitPoints`][d-hp].

### Roll20 i punkt odniesienia PF2e

Sheet Worker reaguje na `change:<attribute>`, odczytuje `getAttrs` i zapisuje `setAttrs`. Poniższy **własny przykład składni API** zapisuje wynik; nie jest wyciągiem z konkretnej karty D&D:

```javascript
on("change:strength sheet:opened", () => {
  getAttrs(["strength"], values => {
    setAttrs({strength_mod: Math.floor((Number(values.strength) - 10) / 2)});
  });
});
```

To kod wykonujący obliczenie i utrzymujący wynik, a nie deklaratywny opis reguły w rekordzie przedmiotu. Dokumentacja ostrzega przed kaskadami asynchronicznych odczytów i zapisów; automatycznie obliczane pola stanowią osobny mechanizm. [Sheet Worker Scripts][r-workers].

W Foundry PF2e Rule Elements mają znacznie bogatszą semantykę. Wyciąg z przykładu dokumentacyjnego:

```json
{
  "key": "FlatModifier",
  "selector": "acrobatics",
  "type": "circumstance",
  "value": 2,
  "predicate": ["action:tumble-through"],
  "slug": "daredevils-boots-tumble-through"
}
```

Obsługiwane są również logiczne predykaty `and`, `or`, `not`, porównania typu `gte`, wartości `@item.badge.value` i różne rodzaje Rule Elements. **Wniosek:** to język domenowy interpretowany przez system, z kontekstem rzutu, typami bonusów i warunkami, a nie tylko lista „dodaj N do pola”. Nie badano historii pozwalającej stwierdzić, w którym wydaniu przekroczył tę granicę. [Rule Elements PF2e][p-rules].

### Opcje i koszty

| Podejście | Korzyść | Koszt |
| --- | --- | --- |
| Liczenie z wejść przy przygotowaniu | Efekty można odwrócić bez odgadywania dawnej wartości | Kolejność zależności, czas przygotowania, ryzyko cykli |
| Zapis wyniku i aktualizacja zdarzeniowa | Tani odczyt, jak w Sheet Workers | Nieaktualny wynik po pominiętym zdarzeniu lub zmianie kodu; potrzebna odbudowa |
| Kod systemu i kilka rodzajów modifier | Ograniczony zakres, łatwiejsza kontrola semantyki | Nowa rodzina wyjątku może wymagać zmiany programu |
| Bogaty język reguł w treści | Autor treści zapisuje więcej zachowań bez nowego kodu | Utrzymanie interpretera, diagnostyki, kontekstu, zgodności i migracji reguł |
| Jawny override | MG może ustalić ostateczną wartość | Trzeba rozróżnić override od cache; bez tego przeliczenie może skasować decyzję MG |

## 4. Referencja, która się nie rozwiązuje

**Odpowiedź wprost.** Nie ma jednej odpowiedzi nawet dla Foundry: lokalna kopia przechowuje własne dane, ale może zawierać dalsze odwołania do zaklęcia, aktora lub pliku. Dlatego zachowanie skopiowanych pól nie gwarantuje działania każdej aktywności i każdego widoku. Dla D&D Beyond nie potwierdzono ogólnego kontraktu zachowania postaci po usunięciu prywatnej definicji. [Lokalny import][f-compendium], [CastActivity][f-castdata], [naprawy brakujących odwołań][f-release531].

### Potwierdzone zachowanie i granice

| Przypadek | Co można stwierdzić |
| --- | --- |
| Foundry: niedostępne compendium po imporcie | Dane skopiowanego dokumentu są lokalne. **Wniosek z modelu:** sam brak wpisu źródłowego nie wymaga wyczyszczenia tych danych; nie jest to gwarancja działania wszystkich modułów |
| Foundry: przedmiot z Cast Activity | `spell.uuid` jest referencją do `Item`; walidator toleruje nierozwiązane UUID. `cachedSpell` szuka lokalnego zaklęcia osadzonego w aktorze, oznaczonego `flags.dnd5e.cachedFor`. Użycie aktywności tworzy taką kopię, jeśli jej brakuje. Jeżeli nie istnieją ani kopia, ani źródło, `getCachedSpellData()` nie odtworzy treści; dokładnego objawu UI nie sprawdzono |
| Foundry: brak referencji w widoku | W 5.3.0 brak odnajdywanej podklasy potrafił zablokować renderowanie strony klasy w dzienniku; poprawiono to w 5.3.1. Nie był to dowód utraty danych bohatera |
| Foundry: baza niepowiązanego tokena | Token przechowuje deltę wobec World Actor. API `TokenDocument.actor` dopuszcza `null`; nie ma podstaw, by deltę traktować jako samowystarczalny pełny zapis aktora |
| Roll20: skopiowane atrybuty i przycisk compendium | Importowane wartości są wpisane do karty; przycisk odsyłający do compendium jest osobnym mechanizmem. **Wniosek:** zachowanie wartości i możliwość otwarcia źródła trzeba rozpatrywać oddzielnie |
| Open5e V2: usunięcie `Document` w bazie katalogu | `FromDocument.document` ma `on_delete=models.CASCADE`. To zachowanie bazy katalogu, nie polityka usuwania ekwipunku w zewnętrznej aplikacji |

Źródła w kolejności wierszy: [import][f-compendium], [CastActivityData][f-castdata] i [tworzenie lokalnego zaklęcia][f-castdoc], [issue #6869][f-issue6869] i [5.3.1][f-release531], [TokenDocument][f-token] i [ActorDelta][f-delta], [integracja Roll20][r-import], [FromDocument][o-document].

**D&D Beyond — trzy różne operacje:** usunięcie z kolekcji, usunięcie prywatnej kreacji i usunięcie publicznej publikacji nie są tym samym. Oficjalna pomoc dokumentuje usuwanie z kolekcji, a osobno stwierdza, że opublikowanego homebrew użytkownik nie może po prostu usunąć. Nie publikuje w tych materiałach kompletnej tabeli skutków dla istniejących postaci. [Kolekcja][d-collection], [usuwanie opublikowanego homebrew][d-delete].

Nie potwierdzono, że każdy prywatny homebrew po usunięciu zachowuje się identycznie, ani że każda postać zawsze się otworzy. Nie wolno tego wywnioskować wyłącznie z obecności `definition` w danych odczytywanych przez importer. Dla 5etools oraz Fantasy Grounds również nie ustalono kompletnego zachowania wszystkich konsumentów po usunięciu zależnego źródła.

### Opcje i koszty

| Polityka | Co pozostaje dostępne | Koszt |
| --- | --- | --- |
| Ścisła referencja | Tylko dane, których źródło nadal istnieje | Zależność dostępności karty od paczki; wymagany czytelny stan błędu |
| Zachowana kopia | Skopiowana treść i parametry | Możliwe stare dane; zewnętrzne grafiki, skrypty i dalsze referencje nadal mogą zniknąć |
| Referencja z awaryjnym snapshotem | Ostatnia zachowana treść przy utracie źródła | Reguła wyboru między źródłem a snapshotem, oznaczenie wersji i rozbieżności |
| Zachowanie nierozwiązanego ID i częściowe działanie | Pozostała karta oraz informacja o brakującym elemencie | Każdy widok i kalkulator musi tolerować brak zależności |
| Blokada usunięcia lub trwałe archiwum definicji | Referencje pozostają rozwiązywalne | Koszt utrzymania starych wersji i ograniczona możliwość sprzątania |

## 5. Homebrew

**Odpowiedź wprost.** Foundry pozwala tworzyć pełne dokumenty świata i umieszczać je we własnych compendiach; D&D Beyond ma kreacje oraz kolekcję powiązane z kontem, z udostępnianiem w kampanii. W 5etools homebrew jest paczką danych z metadanymi źródeł i lokalnym zapisem użytkownika. Kolizja wyświetlanej nazwy, tożsamość rekordu i świadome nadpisanie istniejącego wpisu to trzy różne operacje. [Tworzenie przedmiotów Foundry][f-items], [pakowanie treści][f-packaging], [kolekcja DDB][d-collection], [udostępnianie DDB][d-sharing], [BrewDoc][t-brewmodels].

### Foundry: dokumenty świata i własne paczki

MG może utworzyć samodzielny `Item` albo edytować dokument należący do aktora; katalog nie jest obowiązkowym etapem. Aby treść była wielokrotnego użytku między światami, można umieścić compendium w module. Zasięg określa więc miejsce dokumentu i paczka, a nie odrębna globalna tabela „homebrew”. [Items][f-items], [Content Packaging Guide][f-packaging].

Przy zwykłym imporcie jednakowe nazwy nie oznaczają tego samego ID. Z kolei eksport folderu oferuje **Merge By Name** i **Keep Document IDs**, czyli jawne mechanizmy dopasowywania i zastępowania. Dokumentacja ostrzega też, że aktualizacja systemu lub modułu może usunąć lokalne zmiany w dostarczanym przez niego compendium. [Compendium Packs][f-compendium].

Pochodzenie opisuje `system.source`: `book`, `page`, `custom`, `license`, `revision`, `rules`. Metadane te nie zmieniają domowego miecza w inny typ dokumentu. **Wniosek:** kopia oficjalnego wpisu może pozostać tym samym typem danych, lecz uzyskać lokalną tożsamość i treść. [SourceField][f-source].

### D&D Beyond: własna definicja i kolekcja konta

Homebrew może powstać od zera lub na podstawie istniejącego szablonu; poradnik pokazuje tworzenie pełnego magicznego przedmiotu z modyfikatorami i zaklęciami. To coś więcej niż zmiana etykiety jednego miecza na karcie. Kolekcja decyduje o dostępnej własnej treści; udostępnienie członkom kampanii nie wymaga publicznego opublikowania kreacji. [Tworzenie magicznego przedmiotu][d-homebrew-guide], [kolekcja][d-collection], [udostępnianie i publikowanie][d-sharing].

W danych integracji rozróżnienie wspierają ID definicji oraz `isHomebrew`, `isLegacy`, `version`. **Nie potwierdzono**, że utworzenie homebrew o nazwie oficjalnego przedmiotu zastępuje jego definicję na wszystkich kartach. Nie ustalono również kompletnej polityki automatycznego przenoszenia istniejących egzemplarzy na nową wersję homebrew. [Typ definicji przedmiotu][d-item].

Rzeczywisty błąd wystąpił za to po stronie **DDB Importer**: wydanie 6.5.14 naprawia stosowanie dodatkowych przekształceń oficjalnej treści do homebrew o tej samej nazwie. To dowód problemu dopasowania w integracji, nie wewnętrznej kolizji kluczy w DDB. [Changelog 6.5.14][d-importer-changelog].

### 5etools i Open5e: tożsamość źródła obok nazwy

Paczka 5etools ma `body._meta.sources`, a jej lokalny nagłówek m.in. `docIdLocal`, `checksum`, `url`, `filename`, `isLocal`, `isEditable`. Edytowalne wpisy są wyszukiwane i zapisywane po `uniqueId`; operacja duplikacji nadaje nowe ID. Zapis przechodzi przez `StorageUtil` i klucze `HOMEBREW_2_STORAGE*`, a nie przez identyfikator kampanii. [BrewDoc i nagłówek][t-brewmodels], [edycja homebrew][t-brewimpl], [odczyt i zapis paczek][t-brewbase].

Nazwy źródeł odróżniają warianty katalogowe, np. `PHB` i `XPHB`. Sam `BrewDoc.mergeObjects` łączy tablice rekordów, bez uniwersalnej reguły „ostatnia taka sama nazwa wygrywa”. Nie ustalono jednej polityki rozstrzygania duplikatów dla wszystkich ekranów i importerów 5etools. [Dane przedmiotów][t-items], [łączenie BrewDoc][t-brewmodels].

Open5e V2 ma osobne `key` i `name`, a treść wskazuje `Document`, powiązany z wydawcą i systemem gry. Jest to model wieloźródłowego katalogu; z samego schematu nie wynika usługa przechowująca prywatny homebrew konta lub kampanii. [Modele abstrakcyjne][o-abstracts], [Document][o-document-model].

### Opcje i koszty

| Podejście | Co daje | Koszt |
| --- | --- | --- |
| Własna treść tylko w kampanii | Prosty zakres i niezależność sesji | Kopiowanie między kampaniami, powielanie poprawek |
| Wspólny katalog konta lub instalacji | Ponowne użycie jednej definicji | Aktualizacje mogą oddziaływać na wiele kampanii |
| Paczki z własną przestrzenią identyfikatorów | Współistnienie tej samej nazwy z różnych źródeł | Potrzebna identyfikacja wydawcy, paczki i wydania oraz obsługa zależności |
| Kopia oficjalnej definicji pod nowym ID | Lokalna swoboda bez zmiany oryginału | Brak automatycznego dziedziczenia przyszłych poprawek |
| Warstwa nadpisań oficjalnego wpisu | Jednoznaczne wskazanie, co zmienił MG | Reguła pierwszeństwa i konflikty po zmianie definicji bazowej |

## 6. Ekwipunek jako instancja

**Odpowiedź wprost.** W Foundry stan egzemplarza jest częścią lokalnego `Item`, a w danych DDB integracja rozpoznaje osobny rekord `inventory[]` z ilością, dostrojeniem, użyciami i pojemnikiem. Stos zwykle reprezentuje jeden rekord z ilością, więc jego wspólne pola nie opisują różnic między poszczególnymi sztukami. Pojemnik wymaga osobnej relacji oraz reguł przenoszenia i usuwania zawartości. [PhysicalItemTemplate][f-physical], [UsesField][f-uses], [ContainerData][f-container], [IDDBInventoryItem][d-item].

### Konkretne pola

| Cecha egzemplarza | Foundry dnd5e 5.3.3 | Dane obsługiwane przez DDB Importer |
| --- | --- | --- |
| Tożsamość i treść | Osadzony `Item._id`, własne `name` i `system` | `id`, `entityTypeId`, ID definicji i rozwinięte `definition` |
| Liczba sztuk | `system.quantity` | `quantity`; definicja dodatkowo `stackable`, `bundleSize` |
| Użycia / ładunki | `system.uses.spent`, `.max`, `.recovery[]`; także zasoby aktywności | `chargesUsed`, `limitedUse {maxUses, numberUsed, resetType, resetTypeDescription}` |
| Dostrojenie | `system.attunement`: wymaganie; `system.attuned`: stan | `definition.canAttune` i `isAttuned` |
| Założenie / używanie | `system.equipped` | `equipped` |
| Pojemnik nadrzędny | `system.container`: ID innego `Item` | `containerEntityId`, `containerEntityTypeId`, `containerDefinitionKey` |
| Własna nazwa | Zwykłe `Item.name` kopii | W pokazanym typie inventory nie ma osobnego `name`; nie ustalono tu kompletnego zapisu dostosowań nazwy |
| Zużycie fizyczne | We wspólnych szablonach physical/equippable nie ma uniwersalnego pola trwałości każdej rzeczy | W badanym interfejsie inventory brak uniwersalnego pola trwałości; nie jest to dowód braku takiej funkcji w całym produkcie |

Źródła: [Item][f-items], [pola fizyczne][f-physical], [wyposażenie i dostrojenie][f-equip], [użycia][f-uses], [aktywności][f-activities], [definicja i egzemplarz DDB][d-item]. `equipped: false` nie wyraża osobno „w plecaku”, „w ręce” i „porzucony”: położenie wymaga dodatkowej informacji.

### Stosy: ilość i kryterium zgodności

W Foundry `_onDropStackConsumables` automatycznie łączy upuszczony **consumable** tylko przy obecności źródłowego ID; szuka zgodnego źródła, nazwy i pojemnika, po czym zwiększa `system.quantity`. Nie porównuje w tej funkcji wszystkich pól `uses`, efektów czy opisu. Nie jest to więc ogólny algorytm porównujący komplet danych dwóch dowolnych rzeczy. [Kod karty aktora][f-actor-sheet].

**Wniosek:** jeden stos ma wspólną nazwę, stan i profil działania; dwie sztuki z różnymi zmianami MG wymagają rozdzielenia, jeśli te różnice mają być zachowane. W DDB `stackable` i `bundleSize` należą do definicji, a `quantity` do inventory; są różnymi pojęciami. Importer przy ustalaniu wagi jednostkowej uwzględnia `bundleSize`, co pokazuje koszt niejawnego utożsamienia „paczka” ze „sztuka”. [Typy DDB][d-item], [przeliczanie wagi w parserze][d-item-parser].

### Pojemniki: drzewo zbudowane z odwołań

W Foundry przedmioty w plecaku nadal należą do `actor.items`; plecak nie ma własnej zagnieżdżonej kolekcji dokumentów dzieci. Getter `contents` wyszukuje rekordy, których `system.container` wskazuje jego ID. `ContainerData` przechowuje `capacity.count`, `capacity.volume {value, units}` i `capacity.weight {value, units}`, a ilość samego pojemnika wymusza na `1`. [ContainerData][f-container].

Karta blokuje wkładanie pojemnika do niego samego lub jego potomka. Usunięcie zawartości przy usunięciu pojemnika zależy od opcji `deleteContents`; kopiowanie całego drzewa wymaga przemapowania ID. To dodatkowe zachowania ponad samo pole `parentId`. [Obsługa upuszczania na pojemnik][f-actor-sheet], [usuwanie zawartości][f-container], [kopiowanie drzewa przedmiotów][f-itemdoc].

### Opcje i koszty

| Model | Korzyść | Koszt |
| --- | --- | --- |
| Jeden rekord na sztukę | Każda rzecz ma własne zmiany i historię | Dużo rekordów i trudniejsza obsługa setek identycznych monet lub strzał |
| Jeden rekord na stos | Prosta ilość i zbiorowe operacje | Konieczność zdefiniowania równości; indywidualny stan wymaga rozdzielenia stosu |
| Wspólny stos z wyjątkami sztuk | Kompaktowy zapis plus odstępstwa | Złożone scalanie, podział i adresowanie konkretnej sztuki |
| Zagnieżdżone dokumenty pojemników | Własność i zawartość w jednej strukturze | Przenoszenie zmienia ścieżkę; trzeba ustalić tożsamość i skutki usunięcia rodzica |
| Płaska kolekcja z ID pojemnika | Stabilna tożsamość podczas przenoszenia | Sprawdzanie cykli, brakujących rodziców, agregacji wagi i usuwania |

## 7. Potwór i NPC w trakcie starcia

**Odpowiedź wprost.** W Foundry bieżące HP niezależnego goblina może żyć w delcie tokena, a HP trwałego NPC w dokumencie świata; sam wpis inicjatywy nie jest właścicielem HP. Fantasy Grounds rozdziela przygotowane spotkanie od konkretnych uczestników Combat Trackera, a Roll20 pozwala trzymać HP w atrybucie postaci albo lokalnym pasku tokena. Statblok katalogowy i wymyślony NPC nie muszą być odrębnymi typami schematu: różnić może je tożsamość, pochodzenie i czas życia. [TokenDocument][f-token], [ActorDelta][f-delta], [BaseCombatant][f-combatant], [przygotowanie spotkań FG][fg-encounters], [obiekty Roll20][r-api], [NPCData][f-npc].

### Foundry: goblin numer trzy

`TokenDocument` jest dzieckiem `Scene`, wskazuje bazowego aktora przez `actorId`, a `actorLink` rozstrzyga sposób powiązania. Dla `actorLink: true` tokeny odsyłają do tego samego World Actor. Dla `false` każdy ma `ActorDelta`, z której i bazy system składa Synthetic Actor; ten ostatni jest obiektem roboczym, nie osobnym wpisem w katalogu aktorów świata. [TokenDocument API][f-token], [model ActorDelta][f-delta].

| Sytuacja | Gdzie jest stan i co współdzieli |
| --- | --- |
| Trzy niezależne tokeny jednego goblina | Każdy ma własną deltę; zmienione HP odczytuje się jako `token.actor.system.attributes.hp.value` i zapisuje przez aktualizację aktora tokena |
| Trzy tokeny powiązane z jednym NPC | Aktualizują `system.attributes.hp.value` tego samego World Actor, więc nie są trzema niezależnymi zasobami HP |
| Trwały NPC bez udziału w bieżącej walce | Może istnieć jako World Actor i zachować HP oraz ekwipunek poza listą inicjatywy |
| Uczestnik walki | Core `Combatant` przechowuje m.in. `actorId`, `sceneId`, `tokenId`, `initiative`, `defeated`; jego podstawowy schemat nie ma osobnego pola HP |

Źródła: [TokenDocument][f-token], [przekład aktualizacji na ActorDelta][f-delta], [pola HP][f-attributes], [schemat Combatant w core V13][f-combatant]. **Wniosek z własności dokumentów:** czas życia stanu tokena wiąże się ze sceną i tokenem, nie automatycznie z wpisem Combat. Nie przeprowadzono testu wszystkich opcji usuwania walki ani zachowań modułów.

W `dnd5e` potwór używa `Actor.type: "npc"`. `NPCData` ma HP z `formula`, `resources.legact {max, spent}`, `resources.legres {max, spent}`, `resources.lair {value, initiative, inside}`, `traits.important` i `source`. MG może użyć tego samego rodzaju dokumentu dla własnego burmistrza lub skopiowanego goblina; znacznik `important` nie jest testem „autorstwa MG”. [NPCData][f-npc], [SourceField][f-source].

### Fantasy Grounds: szablon spotkania i Combat Tracker

Przygotowane Encounter określa typy i liczby NPC oraz ewentualne pozycje; dodanie go do Combat Trackera tworzy uczestników walki. Tracker rozróżnia **HP całkowite, Tmp i Wounds**, więc utracone HP są prezentowane jako rany. Poszczególni NPC mogą mieć numery w nazwach, własne obrażenia i efekty; MG może ich edytować, duplikować lub usuwać z trackera. [Preparing Encounters][fg-encounters], [5E Combat Tracker][fg-combat].

To osobny stan operacyjny, nie sama liczba „3 gobliny” w szablonie spotkania. Dokumentacja opisuje jawne kasowanie uczestników, a nie zasadę „znikają po ostatniej turze”; **nie ustalono tu dokładnych ścieżek XML ani gwarancji archiwizacji stanu po usunięciu wpisu**. Własny NPC może powstać od pustej karty lub kopii istniejącego NPC, więc autorstwo samo w sobie nie wymaga osobnego typu. [Combat Tracker][fg-combat], [5E NPCs and Encounters][fg-npc].

### Roll20 i otwarte katalogi

Roll20 `graphic` ma `represents`, `bar1_value`, `bar1_max`, `bar1_link`. Powiązanie paska z atrybutem postaci synchronizuje jego stan; pusty link pozwala trzymać odrębną wartość na tokenie. Atrybut postaci ma `current` i `max`, a token należy do strony przez `_pageid`. **Wniosek:** powiązanie HP decyduje, czy wiele tokenów współdzieli jeden zasób. [API Objects: Graphic i Attribute][r-api].

Natomiast `Goblin.hit_points: 7` w D&D 5e API oraz `hp.average` / `hp.formula` w 5etools opisują statblok. Nie ma w tych rekordach identyfikatora „goblin numer trzy z wtorkowej sesji” ani jego bieżących ran. Nie należy wyprowadzać modelu trwałości kampanii z samego katalogu. [Goblin API][a-goblin], [Bestiariusz 5etools][t-monsters].

### Opcje i koszty

| Miejsce stanu NPC | Korzyść | Koszt |
| --- | --- | --- |
| Stały dokument kampanii | NPC zachowuje tożsamość i stan między spotkaniami | Sprzątanie wielu jednorazowych przeciwników; rozróżnienie szablonu i osoby |
| Instancja należąca do spotkania | Prosty zakres i zbiorowe usuwanie | Trzeba jawnie zachować lub przenieść ocalałego NPC |
| Instancja należąca do sceny / tokena | Jeden stan niezależny od aktualnej listy inicjatywy | Trwałość zależy od bytu przestrzennego; przenoszenie wymaga określonej semantyki |
| Baza i delta | Wspólne dane statbloku z odrębnymi ranami | Zależność od bazy i dokładna obsługa różnic kolekcji, efektów oraz usunięć |
| Osobna tożsamość NPC i udział w starciu | Ta sama osoba może brać udział w kolejnych starciach | Dwa byty i reguła, które pola należą do osoby, a które do danego udziału |

## 8. Czego żałują: udokumentowane problemy i migracje

**Odpowiedź wprost.** Najmocniejsze dowody dotyczą granic abstrakcji: zbyt grubych różnic tokena, niedziałających odwołań, dopasowywania po nazwie i obliczeń ponawianych przy zmianie HP. Historia kodu pokazuje również, że wersja formatu, wersja reguł i rewizja treści muszą być rozpatrywane oddzielnie. Poniżej „ból” oznacza opisany problem lub widoczną migrację; tylko tam, gdzie twórcy sami oceniają wcześniejszą decyzję, przypisuję im taką ocenę. [Zmiana ActorDelta][f-delta], [wydanie 5.3.1][f-release531], [DDB Importer changelog][d-importer-changelog], [migracje dnd5e][f-migration], [DDB changelog 2024][d-changelog].

### Przypadki, które rzeczywiście pękły

| Projekt / wersja | Udokumentowany problem lub zmiana | Wniosek o koszcie modelu |
| --- | --- | --- |
| Foundry core, V10 → V11 | Stare `actorData` wymagało emulacji zdarzeń aktora i przedmiotów. Zmiana jednego przedmiotu kopiowała całą kolekcję do tokena; autorzy opisują rozrost zapisu. `ActorDelta` wprowadziło różnice poszczególnych dokumentów osadzonych | Mała delta pól nie wystarcza, jeżeli kolekcje są traktowane jako niepodzielna wartość; obejścia starego zachowania komplikują migrację |
| dnd5e 5.3.0 → 5.3.1, #6869 | Brak odnajdywanej podklasy blokował renderowanie strony klasy w dzienniku | Uszkodzona zależność opcjonalnej treści może unieruchomić cały widok, choć reszta dokumentu jest poprawna |
| dnd5e 5.3.1, #6867 | Poprawiono błąd renderowania karty NPC, gdy gear odsyłał do przedmiotu osadzonego w tym samym aktorze | Ten sam typ treści może być adresowany w różnych zakresach; kod konsumenta musi je rozumieć |
| dnd5e 5.3.1, #6901 | Efekt `Aid` powodował nieprawidłowe zwiększanie tymczasowego maksimum HP przy zmianach HP | Obliczenie wartości zależnej od efektu powinno dawać ten sam wynik dla tych samych wejść; objaw nie dowodzi sam w sobie, że wadliwy cache zapisano w bazie |
| DDB Importer 6.5.14 | Homebrew o nazwie oficjalnej rzeczy dostawał nieodpowiednie przekształcenia importera | Nazwa użyta jako klucz do reguły przekształcenia może pomylić różne definicje |
| DDB Importer 6.2.7–6.2.8 | Naprawy przycisków migracji compendiów oraz brakującego folderu homebrew przy migracji przedmiotów i zaklęć | Zmiana formatu obejmuje także organizację danych i narzędzia przejścia, nie tylko schemat rekordu |

Źródła: [uzasadnienie ActorDelta][f-delta], [issue #6869][f-issue6869], [wydanie 5.3.1 z #6867 i #6901][f-release531], [zgłoszenie błędnego HP][f-issue6901], [changelog importera][d-importer-changelog]. Ostatnia kolumna jest analizą konsekwencji, nie cytatem z issue.

### Migracje, które pokazują zbyt wąski dawny kształt danych

W 5etools migrator homebrew wykonuje m.in.:

```text
monster.size: "M"            → monster.size: ["M"]
variant                     → magicvariant
trap.tier / level / threat  → trap.rating: [{tier, level, threat}]
```

Komentarze datują te przekształcenia odpowiednio na 22 marca 2022, 9 lipca 2022 i 13 listopada 2024. To konkretne przykłady przejścia ze skalaru do listy, zmiany nazwy kategorii i zgrupowania pól w powtarzalną strukturę. Osobno wersjonowany jest zapis lokalnego homebrew (`_VERSION = 2`); nowe klucze magazynu są celowo inne od starych, aby użytkownik mógł odzyskać wcześniejsze dane. [BrewDocContentMigrator][t-migrator], [wersja magazynu homebrew][t-brewimpl].

W dnd5e migracja dostrojenia zamienia stary kod liczbowy na dwa pola: `attunement` opisujące wymaganie i `attuned` opisujące faktyczny stan. Dla wartości `2` powstaje `attunement: "required"` oraz `attuned: true`; dla `1` wymaganie pozostaje, ale stan nie jest aktywny. Pojemniki przechodzą od `capacity {type, value}` do osobnych parametrów liczby rzeczy i wagi. **Wniosek:** dawny enum lub pojedyncza para typ/wartość potrafią ukrywać niezależne osie. [Migracja EquippableItemTemplate][f-equip], [migracja ContainerData][f-container].

Open5e utrzymuje V1 jako API przestarzałe i kieruje nowe integracje do V2. W kodzie V1 `Monster` ma m.in. `actions_json` i `speed_json` jako pola tekstowe parsowane przez `json.loads`; V2 modeluje akcje i ich ataki jako relacje. To potwierdzona zmiana kształtu, **nie dowód**, że autorzy oficjalnie nazwali cały model V1 błędem. [Dokumentacja wersji API][o-docs], [Monster V1][o-v1monster], [Creature V2][o-creature].

### Wersja formatu, reguł i treści

| Mechanizm dnd5e | Do czego odnosi się wersja |
| --- | --- |
| `_stats.systemVersion` | Wersja systemu zapisana w metadanych dokumentu, używana do wyboru migracji |
| Ustawienie świata `systemMigrationVersion` | Wersja zapisywana po przebiegu migracji świata |
| `system.source.rules` | Wariant reguł treści, np. `2014` albo `2024` |
| `system.source.revision` | Osobne pole rewizji źródłowej treści; domyślna wartość `1` |

Migrator dnd5e przechodzi przez aktorów, przedmioty, sceny oraz wybrane compendia. Kod ustawia starszym dokumentom `source.rules: "2014"`, a dla danych sprzed 5.3.0 utrwala zmianę struktury `advancement`. **Wniosek:** aktualny program nie oznacza automatycznie aktualnego formatu każdej kopii ani przepisania jej zasad na edycję 2024. [Kod migracji][f-migration], [SourceField][f-source].

### Aktualizacja treści może naruszyć oczekiwania użytkownika

W komunikatach dotyczących przejścia D&D Beyond na zasady 2024 twórcy przyznali, że źle ocenili wpływ planowanej aktualizacji na istniejące postacie. Skorygowali plan zastępowania treści: zachowano możliwość korzystania z wcześniejszych opcji, zaklęć i magicznych przedmiotów bez odtwarzania ich jako homebrew. Starsze komunikaty są dziś oznaczone jako przestarzałe; dokumentacja docelowego zachowania pokazuje równoległy wybór zaklęć 2014 i 2024. To dowód rewizji decyzji produktowej, nie opis fizycznej migracji bazy DDB. [Changelog: aktualizacja zasad 2024 oraz wcześniejsze komunikaty][d-changelog].

### Rozjazd opisu i danych, a nie tylko cache

W pobranym rekordzie `Adult Red Dragon` opis `Fire Breath` mówi o połowie obrażeń po udanym rzucie, lecz struktura tej akcji zawiera `dc.success_type: "none"`. To zaobserwowana sprzeczność między dwiema reprezentacjami tej samej reguły; nie odnaleziono tu zgłoszenia wyjaśniającego jej pochodzenie. **Wniosek:** automatyczny konsument nie może zakładać, że ustrukturyzowane pole i opis zawsze są zgodne. [Rekord Adult Red Dragon, stan z dnia analizy][a-dragon].

Nie znaleziono w wykorzystanych źródłach jednoznacznego przypadku dowodzącego **trwałego zapisania nieaktualnej wartości pochodnej** jako ogólnej wady DDB lub Foundry. Roll20 umożliwia przechowywanie wyników przez `setAttrs`, a błąd `Aid` pokazuje nieprawidłowe przeliczanie; to dwie różne kategorie dowodu. Ryzyko starego wyniku po pominięciu zdarzenia jest wnioskiem z mechanizmu, nie przedstawioną jako fakt awarią wszystkich kart. [Sheet Workers][r-workers], [issue #6901][f-issue6901].

### Opcje i koszty

| Polityka zmiany | Korzyść | Koszt |
| --- | --- | --- |
| Migracja zapisu do nowego formatu | Jeden docelowy schemat w działającej aplikacji | Migrator musi objąć kopie i homebrew; cofnięcie programu może wymagać starego zapisu |
| Adapter starego formatu podczas odczytu | Można otwierać dawne dane bez natychmiastowego nadpisania | Program długo utrzymuje wiele semantyk i przypadków walidacji |
| Wersje treści obok siebie | Kampania może zachować konkretną edycję zasad | Więcej rekordów, filtrów oraz jawnych wyborów wersji w referencjach |
| Jawna aktualizacja lokalnej kopii | Użytkownik kontroluje moment zmiany | Porównanie zmian, konflikty homebrew i obsługa częściowego przyjęcia poprawek |
| Wyliczanie z danych źródłowych | Brak trwałego cache wymagającego osobnej migracji | Koszt obliczeń i konieczność poprawnej kolejności zależności |
| Wynik zapisany z możliwością odbudowy | Szybki odczyt i świadoma kontrola świeżości | Wersja reguły, unieważnianie wyniku i odróżnienie go od override MG |

[a-choice]: https://github.com/5e-bits/5e-srd-api/blob/7a9b6e4c597edecbc51243ba922cff7cd2e095a5/src/models/common/choice.ts
[a-dragon]: https://www.dnd5eapi.co/api/2014/monsters/adult-red-dragon
[a-equipment-schema]: https://github.com/5e-bits/5e-srd-api/blob/7a9b6e4c597edecbc51243ba922cff7cd2e095a5/src/models/2014/equipment.ts
[a-fireball]: https://www.dnd5eapi.co/api/2014/spells/fireball
[a-goblin]: https://www.dnd5eapi.co/api/2014/monsters/goblin
[a-monster-schema]: https://github.com/5e-bits/5e-srd-api/blob/7a9b6e4c597edecbc51243ba922cff7cd2e095a5/src/models/2014/monster.ts
[a-spell-schema]: https://github.com/5e-bits/5e-srd-api/blob/7a9b6e4c597edecbc51243ba922cff7cd2e095a5/src/models/2014/spell.ts
[a-sword]: https://www.dnd5eapi.co/api/2014/equipment/longsword
[d-base]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/src/types/ddb-base.d.ts
[d-changelog]: https://www.dndbeyond.com/changelog
[d-character]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/src/types/ddb-character-source.d.ts
[d-collection]: https://dndbeyond-support.wizards.com/hc/en-us/articles/7747238519700-Homebrew-Creation-and-Collection-Basics
[d-delete]: https://dndbeyond-support.wizards.com/hc/en-us/articles/14175427969428-Deleting-Published-Homebrew-Content
[d-homebrew-guide]: https://www.dndbeyond.com/posts/1103-how-to-create-a-homebrew-magic-item-using-d-d
[d-hp]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/src/parser/character/hp.ts
[d-importer-changelog]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/CHANGELOG.md
[d-item]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/src/types/ddb-item-source.d.ts
[d-item-parser]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/src/parser/item/DDBItem.ts
[d-sharing]: https://dndbeyond-support.wizards.com/hc/en-us/articles/7747210455828-Sharing-and-Publishing-Homebrew-Content
[d-types]: https://github.com/MrPrimate/ddb-importer/blob/0816333dce5e9491181a620e1e405f6d4fc54a49/src/types/README.md
[f-activities]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/item/templates/activities.mjs
[f-actor]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/documents/actor/actor.mjs
[f-actor-sheet]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/applications/actor/api/base-actor-sheet.mjs
[f-attributes]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/actor/templates/attributes.mjs
[f-castdata]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/activity/cast-data.mjs
[f-castdoc]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/documents/activity/cast.mjs
[f-character]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/actor/character.mjs
[f-combatant]: https://foundryvtt.com/api/v13/classes/foundry.documents.BaseCombatant.html
[f-compendium]: https://foundryvtt.com/article/compendium/
[f-container]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/item/container.mjs
[f-creature]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/actor/templates/creature.mjs
[f-damage]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/shared/damage-field.mjs
[f-datamodel]: https://foundryvtt.com/api/v13/classes/foundry.abstract.DataModel.html
[f-delta]: https://foundryvtt.com/article/v11-actor-delta/
[f-effectguide]: https://github.com/foundryvtt/dnd5e/wiki/Active-Effect-Guide
[f-effects]: https://foundryvtt.com/article/active-effects/
[f-equip]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/item/templates/equippable-item.mjs
[f-issue6869]: https://github.com/foundryvtt/dnd5e/issues/6869
[f-issue6901]: https://github.com/foundryvtt/dnd5e/issues/6901
[f-itemdoc]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/documents/item.mjs
[f-items]: https://foundryvtt.com/article/items/
[f-migration]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/migration.mjs
[f-npc]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/actor/npc.mjs
[f-packaging]: https://foundryvtt.com/article/packaging-guide/
[f-physical]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/item/templates/physical-item.mjs
[f-release531]: https://github.com/foundryvtt/dnd5e/releases/tag/release-5.3.1
[f-source]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/shared/source-field.mjs
[f-spell]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/item/spell.mjs
[f-token]: https://foundryvtt.com/api/classes/foundry.documents.TokenDocument.html
[f-uses]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/shared/uses-field.mjs
[f-weapon]: https://github.com/foundryvtt/dnd5e/blob/release-5.3.3/module/data/item/weapon.mjs
[fg-combat]: https://fantasygroundsunity.atlassian.net/wiki/spaces/FGCP/pages/996641984/5E+Combat+Tracker
[fg-encounters]: https://fantasygroundsunity.atlassian.net/wiki/spaces/FGCP/pages/996640999/Preparing+Encounters+and+Random+Encounters
[fg-npc]: https://fantasygroundsunity.atlassian.net/wiki/spaces/FGCP/pages/996641934/5E+NPCs+and+Encounters
[o-abstracts]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/abstracts.py
[o-creature]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/creature.py
[o-docs]: https://open5e.com/api-docs
[o-document]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/abstracts.py
[o-document-model]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/document.py
[o-item]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/item.py
[o-object]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/object.py
[o-spell]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/spell.py
[o-v1monster]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api/models/monster.py
[o-weapon]: https://github.com/open5e/open5e-api/blob/4b314adb19b52ae6caf705f6620311d90ed10a74/api_v2/models/weapon.py
[p-rules]: https://github.com/foundryvtt/pf2e/wiki/Quickstart-guide-for-rule-elements
[r-api]: https://help.roll20.net/hc/en-us/articles/360037772793-API-Objects
[r-import]: https://help.roll20.net/hc/en-us/articles/360043790493-Compendium-Integration
[r-repo]: https://github.com/Roll20/roll20-character-sheets
[r-workers]: https://help.roll20.net/hc/en-us/articles/360037773513-Sheet-Worker-Scripts
[t-brewbase]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/js/utils-brew/utils-brew-base.js
[t-brewimpl]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/js/utils-brew/utils-brew-impl-brew.js
[t-brewmodels]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/js/utils-brew/utils-brew-models.js
[t-items]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/data/items-base.json
[t-migrator]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/js/utils-brew/utils-brew-content-migrator.js
[t-monsters]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/data/bestiary/bestiary-mm.json
[t-spells]: https://github.com/5etools-mirror-3/5etools-src/blob/302acbe2a868b830f9f2d827048bc16ec40d437d/data/spells/spells-phb.json
