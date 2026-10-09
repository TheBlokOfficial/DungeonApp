# Słowniczek SRD 5.1 → paczka `dnd5e-srd`

Źródła słownictwa: polski wyciąg z zasad SRD 5.1 (Black Monk Games na podstawie materiałów Rebel, terminy
Podręcznika Gracza; oznaczony niżej „SRD-PL”) i 15 wpisów w `entries/`. SRD-PL wygrywa z wpisami (sekcja
„Rozstrzygnięte”). Termin z „?” nie był sprawdzony z polskim podręcznikiem. Lista stworzeń i partii: `tools/srd/stworzenia.md`.

## Stany

| EN | PL (SRD-PL) | id | rzeczownik (np. `conditionImmunities`) |
|---|---|---|---|
| Blinded | Oślepiony | `oslepiony` | oślepienie |
| Charmed | Zauroczony | `zauroczony` | zauroczenie |
| Deafened | Ogłuchły | `ogluchly` | ogłuchnięcie |
| Exhaustion | Wyczerpanie | `wyczerpanie` | wyczerpanie |
| Frightened | Przerażony | `przerazony` | przerażenie |
| Grappled | Pochwycony | `pochwycony` | pochwycenie |
| Incapacitated | Obezwładniony | `obezwladniony` | obezwładnienie |
| Invisible | Niewidzialny | `niewidzialny` | niewidzialność |
| Paralyzed | Sparaliżowany | `sparalizowany` | sparaliżowanie |
| Petrified | Skamieniały | `skamienialy` | skamienienie |
| Poisoned | Zatruty | `zatruty` | zatrucie |
| Prone | Powalony | `powalony` | powalenie |
| Restrained | Unieruchomiony | `unieruchomiony` | unieruchomienie |
| Stunned | Ogłuszony | `ogluszony` | ogłuszenie |
| Unconscious | Nieprzytomny | `nieprzytomny` | utrata przytomności |

Pary do pilnowania: **Deafened = Ogłuchły, Stunned = Ogłuszony** (nie odwrotnie); **Charmed = Zauroczony**
(nie „oczarowany”); **Frightened = Przerażony** (nie „przestraszony”); **Grappled = Pochwycony,
Restrained = Unieruchomiony**; **Incapacitated = Obezwładniony**. Wyczerpanie ma sześć poziomów
(„1 poziom wyczerpania”).

## Identyfikatory

- **id = slug polskiej nazwy wpisu**: małe litery ASCII, polskie znaki bez ogonków (ą→a, ć→c, ę→e, ł→l,
  ń→n, ó→o, ś→s, ź/ż→z), każdy ciąg spacji i znaków interpunkcyjnych → jeden `-`, bez `-` na brzegach.
  Plik wpisu to `entries/<id>.json`.
- Przykłady: Mikstura leczenia → `mikstura-leczenia`; Miecz krótki → `miecz-krotki`; Olbrzymi pająk wilczy →
  `olbrzymi-pajak-wilczy`; Sukkub/inkub → `sukkub-inkub`; Magiczny pocisk → `magiczny-pocisk`;
  Duchowa broń → `duchowa-bron`; Leczenie ran → `leczenie-ran`; stan Powalony → `powalony`.
- Nazwa w tekście bez dopisków liczbowych w id: „Lina konopna (15 m)” → `lina-konopna`.
- **Kolizja** (ten sam slug dla wpisów różnych typów): gołe id dostaje typ z pierwszeństwem
  stan > stworzenie > przedmiot > zaklęcie > cecha klasy; pozostałe dostają przyrostek `-stworzenie`,
  `-przedmiot`, `-czar`, `-cecha`. Znane kolizje: zaklęcie Tarcza → `tarcza-czar` (przedmiot `tarcza`);
  przyszła klasa Druid → `druid-klasa` (stworzenie `druid`). Dwa wpisy tego samego typu o tej samej
  nazwie — popraw nazwę, nie id.
- Zanim postawisz znacznik do wpisu, którego jeszcze nie ma, sprawdź `stworzenia.md`, `entries/` i listę
  kolizji wyżej; id wpisu, który już istnieje, jest nienaruszalne.

## Znacznik `[[id|tekst]]`

- Stawiaj w prozie (`text`, `intro`, `description`) przy **każdej** wzmiance o stanie, zaklęciu,
  przedmiocie albo istocie, która ma (albo dostanie) własny wpis. `tekst` to słowo tak, jak stoi w zdaniu —
  odmienione; karta pokazuje sam `tekst`.
- Stan: „inaczej zostaje [[powalony|powalony]]”, „ataki przeciwko [[powalony|powalonemu]] celowi”,
  „cel jest [[pochwycony|pochwycony]] (ucieczka ST 13)”, „ma ułatwienie w rzutach obronnych przeciwko
  [[przerazony|przerażeniu]]”, „otrzymuje 1 poziom [[wyczerpanie|wyczerpania]]”.
- Zaklęcie: w liście czarów każde osobno — „[[swiatlo|światło]], [[swiety-plomien|święty płomień]]”;
  w prozie „rzuca [[magiczny-pocisk|magiczny pocisk]]”.
- Przedmiot / istota: „wypija [[mikstura-leczenia|miksturę leczenia]]”, „przyzywa [[wilk|wilka]]”.
- Bez znacznika: nazwa samego stworzenia we własnym wpisie, rodzaje obrażeń, cechy, umiejętności,
  ogólne słowa („trucizna” jako substancja, „niewidzialny” w znaczeniu potocznym — znacznik tylko
  przy stanie z zasad).
- Pola krótkie (`conditionImmunities`, `damageImmunities` itd.) — bez znaczników.

## Cechy, rzuty, testy

| EN | PL | Skrót na karcie | Skrót w `savingThrows` |
|---|---|---|---|
| Strength | Siła | SIŁ | Sił |
| Dexterity | Zręczność | ZRĘ | Zrę |
| Constitution | Kondycja | KON | Kon |
| Intelligence | Inteligencja | INT | Int |
| Wisdom | Mądrość | MDR | Mdr |
| Charisma | Charyzma | CHA | Cha |

| EN | PL |
|---|---|
| ability check | test cechy, np. „test Mądrości (Percepcja)” |
| saving throw | rzut obronny na Siłę/Zręczność/… — „rzut obronny na Kondycję o **ST 13**” |
| DC | ST (Stopień Trudności) |
| succeed / fail a save | odnieść sukces / ponieść porażkę w rzucie obronnym |
| advantage / disadvantage | ułatwienie / utrudnienie |
| attack roll | test ataku |
| proficiency bonus | premia z biegłości |
| hit points / Hit Dice | punkty wytrzymałości (PW) / Kości Wytrzymałości |
| Armor Class | Klasa Pancerza (KP) |
| natural armor | pancerz naturalny |
| critical hit | trafienie krytyczne |
| opportunity attack | atak okazyjny |
| short / long rest | krótki / długi odpoczynek |
| concentration | koncentracja |
| cantrip / spell slot / Nth level | sztuczka / komórka czarów / N. krąg |
| spellcasting ability | cecha bazowa (do rzucania czarów) |
| Dungeon Master | Mistrz Podziemi (MP) |

## Umiejętności (SRD-PL)

| EN | PL | EN | PL |
|---|---|---|---|
| Acrobatics | Akrobatyka | Medicine | Medycyna |
| Animal Handling | Opieka nad zwierzętami | Nature | Przyroda |
| Arcana | Wiedza tajemna | Perception | Percepcja |
| Athletics | Atletyka | Performance | Występy |
| Deception | Oszustwo | Persuasion | Perswazja |
| History | Historia | Religion | Religia |
| Insight | Intuicja | Sleight of Hand | Zwinne dłonie |
| Intimidation | Zastraszanie | Stealth | Skradanie się |
| Investigation | Śledztwo | Survival | Sztuka przetrwania |

## Obrażenia (SRD-PL)

| EN | PL („… obrażeń X”) | EN | PL |
|---|---|---|---|
| acid | od kwasu | necrotic | nekrotycznych |
| bludgeoning | obuchowych | piercing | kłutych |
| cold | od zimna | poison | od trucizn |
| fire | od ognia | psychic | psychicznych |
| force | od mocy | radiant | od światłości |
| lightning | od elektryczności | slashing | ciętych |
| thunder | od dźwięku | | |

W polach odporności: „od ognia, od zimna”; „obuchowe, kłute i cięte od niemagicznych ataków”; „…od
niemagicznych ataków bronią, która nie jest posrebrzana” ?; „…nie jest adamantytowa” ?.

| EN pole statbloku | PL (etykieta karty) | pole JSON |
|---|---|---|
| Damage Vulnerabilities | Podatność na obrażenia | `damageVulnerabilities` |
| Damage Resistances | Odporność na obrażenia | `damageResistances` |
| Damage Immunities | Niewrażliwość na obrażenia | `damageImmunities` |
| Condition Immunities | Niewrażliwość na stany | `conditionImmunities` (rzeczowniki z tabeli stanów) |

## Typy, rozmiary, charaktery

| Rozmiar EN | `size` | | Typ EN | `type` | `group` |
|---|---|---|---|---|---|
| Tiny | Malutki | | aberration | aberracja ? | Aberracje |
| Small | Mały | | beast | bestia | Zwierzęta |
| Medium | Średni | | celestial | niebianin | Niebianie |
| Large | Duży | | construct | konstrukt | Konstrukty |
| Huge | Wielki | | dragon | smok | Smoki |
| Gargantuan | Ogromny | | elemental | żywiołak | Żywiołaki |
| | | | fey | fey ? | Fey |
| | | | fiend | czart ? | Czarty |
| | | | giant | gigant | Giganci |
| | | | humanoid | humanoid (podtyp) | Ludzie / Humanoidy / Likantropy |
| | | | monstrosity | monstrum ? | Monstra |
| | | | ooze | szlam | Szlamy |
| | | | plant | roślina | Rośliny |
| | | | undead | nieumarły | Nieumarli |
| | | | swarm of Tiny beasts | rój Malutkich bestii | Zwierzęta |

Uwaga: **Huge = Wielki, Gargantuan = Ogromny** (SRD-PL). Przymiotnik „giant” w nazwie zwierzęcia to
„olbrzymi” (Olbrzymi pająk), żeby nie mieszać go z rozmiarem.

Podtypy: `humanoid (dowolna rasa)`, `humanoid (goblinoid)`, `humanoid (kobold)`, `humanoid (ork)`,
`humanoid (elf)`, `humanoid (krasnolud)`, `humanoid (gnom)`, `humanoid (gnoll)`, `humanoid (człowiek)`,
`humanoid (człowiek, zmiennokształtny)` (likantropy), `czart (diabeł)`, `czart (demon)`,
`czart (zmiennokształtny)`, `monstrum (zmiennokształtny)`; inne — nazwa rasy w liczbie pojedynczej.

**Grupa** (`group`, zamknięty zestaw 16 nazw, w liczbie mnogiej): Aberracje, Czarty, Fey, Giganci,
Humanoidy, Konstrukty, Likantropy, Ludzie, Monstra, Niebianie, Nieumarli, Rośliny, Smoki, Szlamy,
Zwierzęta, Żywiołaki. Grupa idzie za typem; humanoid dzieli się na **Ludzie** (statbloki „dowolna rasa”
i ludzie, np. Bandyta, Kapłan, Weteran półsmok), **Humanoidy** (konkretne ludy: Goblin, Ork, Drow…)
i **Likantropy**. Roje → Zwierzęta. Grupę każdego stworzenia podaje `stworzenia.md`.

| Charakter EN | `alignment` |
|---|---|
| lawful good / neutral good / chaotic good | praworządny dobry / neutralny dobry / chaotyczny dobry |
| lawful neutral / neutral / chaotic neutral | praworządny neutralny / neutralny / chaotyczny neutralny |
| lawful evil / neutral evil / chaotic evil | praworządny zły / neutralny zły / chaotyczny zły |
| unaligned | bez charakteru |
| any alignment | dowolny charakter |
| any non-good / non-lawful / chaotic / evil | dowolny charakter poza dobrym / poza praworządnym / dowolny chaotyczny / dowolny zły |

## Zmysły, prędkości, języki

| EN | PL |
|---|---|
| blindsight 30 ft. (blind beyond this radius) | ślepowidzenie 9 m (poza tym promieniem ślepy) |
| darkvision 60 ft. | widzenie w ciemności 18 m |
| tremorsense 60 ft. | wyczuwanie drgań 18 m ? |
| truesight 120 ft. | prawdziwe widzenie 36 m |
| passive Perception 13 | pasywna Percepcja 13 |

`speed`: „9 m”; kilka rodzajów po przecinku: „9 m, latanie 18 m (unoszenie się)”, „pływanie 12 m”,
„wspinanie się 9 m”, „kopanie 3 m” ? (walk/fly/swim/climb/burrow; „Otwarte”).

| EN | PL | EN | PL |
|---|---|---|---|
| Common | wspólny | Abyssal | otchłanny |
| Dwarvish | krasnoludzki | Celestial | niebiański |
| Elvish | elfi | Deep Speech | głębinowy |
| Giant | gigantów ? | Draconic | smoczy |
| Gnomish | gnomi | Infernal | piekielny |
| Goblin | gobliński | Primordial | pierwotny |
| Halfling | niziołczy | Aquan / Auran / Ignan / Terran | wodny / powietrzny / płomienny / ziemny |
| Orc | orczy | Sylvan | leśny |
| | | Undercommon | podwspólny |

„telepathy 120 ft.” → „telepatia 36 m”; „understands Common but can't speak” → „rozumie wspólny, ale nie
mówi”; „any one language (usually Common)” → „dowolny jeden (zwykle wspólny)”; „—” → pole pominięte.
Język spoza tabeli: przymiotnik od nazwy ludu („gnollowy”, „sahuagiński”).

## Akcje i ataki

| EN | PL |
|---|---|
| Melee Weapon Attack | Atak bronią w zwarciu |
| Ranged Weapon Attack | Atak bronią dystansową |
| Melee or Ranged Weapon Attack | Atak bronią w zwarciu lub dystansową |
| Melee / Ranged Spell Attack | Atak czarem w zwarciu / Dystansowy atak czarem |
| +4 to hit, reach 5 ft., one target | **+4 do trafienia**, zasięg 1,5 m, jeden cel |
| range 80/320 ft., one creature | zasięg 24/96 m, jedno stworzenie |
| Hit: 7 (2d4 + 2) piercing damage | Trafienie: **7 (2k4+2)** obrażeń kłutych |
| … plus 3 (1d6) poison damage | … plus **3 (1k6)** obrażeń od trucizn |
| must succeed on a DC 11 Strength saving throw or be knocked prone | musi odnieść sukces w rzucie obronnym na Siłę o **ST 11**, inaczej zostaje [[powalony\|powalony]] |
| taking 10 (3d6) fire damage on a failed save, or half as much damage on a successful one | przy porażce otrzymuje **10 (3k6)** obrażeń od ognia, przy sukcesie połowę |
| the target is grappled (escape DC 13) | cel jest [[pochwycony\|pochwycony]] (ucieczka ST 13) |
| Multiattack | Wielokrotny atak |
| Bonus Actions / Reactions | Akcje dodatkowe / Reakcje |
| Legendary Actions | Akcje legendarne — `intro` jak w `jednorozec.json` |
| Costs 2 Actions | `note`: „kosztuje 2 akcje” |
| (3/Day), (1/Day each) | `note`: „3 na dzień”; nagłówek grupy czarów „Raz na dzień każdy” |
| (Recharge 5–6) | `note`: „odnawia się przy 5–6” („Otwarte”) |
| (Recharges after a Short or Long Rest) | `note`: „odnawia się po krótkim lub długim odpoczynku” |
| Breath Weapon | Zionięcie |
| cone / line / sphere / cube / radius | stożek / linia / kula / sześcian / promień |
| Spellcasting / Innate Spellcasting | Rzucanie czarów / Wrodzone rzucanie czarów |
| Magic Resistance / Pack Tactics / Keen Smell | Odporność na magię / Taktyka stada / Wyostrzony węch |

Kostki: `k` zamiast `d`, w nawiasie bez spacji: `2k8+2`. Pogrubienie `**…**` tylko na premii do trafienia,
wartości obrażeń z kośćmi i ST — tak jak w istniejących wpisach.

Broń w atakach — nazwy z tabel SRD-PL: Buława, Drąg, Lekki młot, Maczuga, Oszczep, Pałka, Toporek, Sierp,
Sztylet, Włócznia, Krótki łuk, Kusza lekka, Proca, Strzałka, Bicz, Glewia, Halabarda, Kiścień, Lanca, Miecz
długi, Miecz dwuręczny, Miecz krótki, Młot bojowy, Młot dwuręczny, Morgensztern, Nadziak, Pika, Rapier,
Sejmitar, Topór bojowy, Topór dwuręczny, Trójząb, Długi łuk, Dmuchawka, Kusza ciężka, Kusza ręczna, Sieć.
Pancerze (`acSource`): przeszywanica, zbroja skórzana, ćwiekowana skóra, zbroja futrzana, koszulka kolcza,
zbroja łuskowa, napierśnik, zbroja półpłytowa, zbroja pierścieniowa, kolczuga, zbroja płytkowa (splint),
zbroja płytowa; „z tarczą” dopisuj po przecinku.

## Wyzwanie i PD

| Wyzwanie | 0 | 1/8 | 1/4 | 1/2 | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|---|---|---|
| PD (`xp`) | 10 (Żaba, Konik morski: 0) | 25 | 50 | 100 | 200 | 450 | 700 | 1100 | 1800 |

`challenge` to tekst „0”, „1/8”, „1/4”, „1/2”, „1”…; `xp` liczba. Wyjątki źródła — pkt 15.

## Przeliczenia

1 stopa = 0,3 m (5 stóp = 1,5 m), zawsze metry z przecinkiem dziesiętnym:

| stopy | 5 | 10 | 15 | 20 | 25 | 30 | 40 | 50 | 60 | 80 | 90 | 100 | 120 | 150 | 300 | 320 | 600 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| m | 1,5 | 3 | 4,5 | 6 | 7,5 | 9 | 12 | 15 | 18 | 24 | 27 | 30 | 36 | 45 | 90 | 96 | 180 |

Zasięgi dystansowe parą: 20/60 → 6/18, 25/100 → 7,5/30, 30/120 → 9/36, 80/320 → 24/96, 100/400 → 30/120,
150/600 → 45/180. 1 mila = 1,5 km. Funty: 1 lb = 0,5 kg (`item.weight` w kg: 3 lb → 1,5; 1/2 lb → 0,25).
Monety: sm, ss, se, sz, sp (miedź, srebro, elektrum, złoto, platyna).

Rzadkość przedmiotu (skala aplikacji: `docs/decisions.md`, „Rzadkość według skali gier”): niemagiczne
i pospolite → Pospolity, niezwykłe → Niepospolity, rzadkie → Rzadki, bardzo rzadkie → Epicki,
legendarne i artefakty → Legendarny.

## Pola wpisu stworzenia

Szablon: `{"id", "name", "template": "dnd5e:creature", "templateVersion": 1, "values": {…}}`; wzór
`wilk.json` (bez czarów), `kaplan.json` (czary), `jednorozec.json` (czary wrodzone, akcje legendarne).

| pole | wymagane | zawartość |
|---|---|---|
| `size` | tak | rozmiar z tabeli, wielką literą („Średni”) |
| `type` | tak | typ małą literą, podtyp w nawiasie |
| `group` | nie, ale zawsze wypełniaj | grupa z zamkniętego zestawu |
| `alignment` | tak | charakter małymi literami |
| `combat` | tak | `ac`, `acSource`?, `hp`, `hpDice`, `str`, `dex`, `con`, `int`, `wis`, `cha` (liczby) |
| `speed` | tak | „9 m”, kolejne rodzaje po przecinku |
| `savingThrows`, `skills` | nie | „Kon +4, Mdr +2”; „Percepcja +3, Skradanie się +4” |
| `damageVulnerabilities`, `damageResistances`, `damageImmunities`, `conditionImmunities` | nie | małymi literami, po przecinku |
| `senses` | tak | zawsze z bierną Percepcją na końcu |
| `languages` | nie | pomiń przy „—” |
| `challenge`, `xp` | tak / nie (wypełniaj) | „1/4” / 50 |
| `specialAbilities`, `bonusActions`, `reactions` | nie | sekcja `{"entries": [{"name", "note"?, "text"}]}` |
| `actions` | tak, min. 1 pozycja | jak wyżej; „Wielokrotny atak” pierwszy |
| `spellcasting` | nie | `intro` (cecha, ST, premia) + pozycje: nazwa grupy („Sztuczki”, „Bez ograniczeń”), `note` (liczba komórek), `text` = lista czarów ze znacznikami |
| `legendaryActions` | nie | `intro` z liczbą akcji, `note` z kosztem |
| `description` | nie | SRD nie ma opisu; najwyżej 1–2 własne zdania o wyglądzie i zwyczajach, bez zasad |

Sekcja bez `intro` i bez pozycji jest odrzucana; pusta sekcja — pomiń klucz. Likantrop: jeden wpis, liczby
postaci zwierzęcej i hybrydy w tekście zdolności „Zmiennokształtność”.

## Rozstrzygnięte

Słownictwo z polskiego podręcznika (SRD-PL) wygrywa z istniejącymi wpisami; wpisy są poprawione.
Punkty wytrzymałości (PW), zauroczenie, przerażenie, „od trucizn”, niepodatność, pasywna Percepcja,
test ataku, krąg zaklęcia i cecha bazowa, Mikstura leczenia (`mikstura-leczenia`), Buława, Miecz krótki,
Kusza ciężka, „półtoraręczna”, języki niebiański i leśny. Wyjątki: skróty cech na karcie zostają
SIŁ/ZRĘ/KON/INT/MDR/CHA; wzór statbloku „Atak bronią w zwarciu” zostaje; kolczuga 27,5 kg (1 lb =
0,5 kg). Znaczniki `[[id|tekst]]` w prozie i w listach czarów, nie w krótkich polach cech. PD z tabeli
wyzwań, nie z 5e-database. Chory olbrzymi szczur to zdolność wpisu `olbrzymi-szczur`.

## Otwarte

- Nazwy stworzeń z „?” w `stworzenia.md` i typy aberracja, fey, czart, monstrum — nieporównane
  z Księgą Potworów. Tłumaczy się je przy pierwszym użyciu; MG poprawia to, co zgrzyta.
- Brzmienie Recharge („odnawia się przy 5–6”), ruch „kopanie” i „unoszenie się”.