# Szlif: drobne poprawki

Lista zadań uzgodniona z autorem; odhaczana przy scaleniu. Sesja wznawia od pierwszej
nieodhaczonej pozycji.

## Karty

- [x] Karta stanu: zasady w sekcji z nagłówkiem „Opis”, jak opis na karcie przedmiotu, bez inicjału
  zwoju. Kontrolka inicjału zostaje w Galerii.
- [x] Proza: twarda nowa linia (Enter w tekście wpisu) zaczyna następną linię trochę niżej niż zwykłe
  zawinięcie — ok. 1/3 wysokości linii, wyraźnie mniej niż pusta linia. Także pod inicjałem.
- [x] Miecz worpalny dostaje podtyp z podręcznika („dowolny miecz zadający obrażenia cięte”). Rząd
  tagów stoi w jednej linii: pierwszy niemieszczący się tag jest ucięty „…” i pełny po najechaniu.
- [x] Pełny tytuł po najechaniu zostaje, dopóki wskaźnik jest nad którąkolwiek jego częścią; da się go
  zaznaczyć w całości, kliknięcie go nie zamyka.

## Ikony

- [x] Ikony stanów wymienione na jeden spójny zestaw o poziomie szczegółu czaszki (Lucide `skull`):
  wypełnione sylwetki z game-icons.net (CC BY 3.0).
- [x] Ikony kategorii na liście treści: typ stworzenia, kategoria przedmiotu; małe i przygaszone jak
  ikony stanów. Ta sama ikona w pustym polu obrazka i w drzewie katalogu świata; kategoria spoza
  listy — ikona domyślna typu.

## Katalog świata

- [x] Klik w tytuł okna podglądu albo przypiętej karty rozwija katalogi do entity i zaznacza ją
  w drzewie.
- [x] Paleta dodaje do drzewa tylko istoty, bez przedmiotów → decisions.md, „Przedmiot nie leży luzem
  w drzewie świata”.

## Motyw

- [x] Lista rozwijana chipa jest szeroka jak najszerszy element całej listy, nie tylko wyrenderowany —
  nie zmienia szerokości przy przewijaniu.
- [x] Widoczny fokus klawiatury: jedna obwódka (token `DungeonFocusRing*`) we wszystkich kontrolkach,
  które przyjmują fokus.
- [x] Menu: skrót i strzałka podmenu kończą się na jednej krawędzi, podmenu nie nachodzi na menu.
- [ ] Szkło palety: krycie zabarwienia wybrane przez autora z renderów całego okna 1920×1080
  (`render.ps1 -Only 'szklo*'`: 55, 70 — obecne, 80, 90 %).

## Porządki

- [x] Powody odrzucenia paczki i wpisu po polsku; błąd JSON nazywa pole.
- [x] Nieaktualne komentarze: `CampaignRowViewModel`, `PanelCatalog`, `AppShellView.axaml`.
