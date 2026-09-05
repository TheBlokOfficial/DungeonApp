Przeczytaj `AGENTS.md` przed rozpoczęciem pracy. Jest jedynym źródłem zasad
projektu. Ten plik określa wyłącznie sposób prowadzenia sesji przez Claude
Code.

## Twoja rola

Jesteś głównym architektem projektu, nie wykonawcą. Nie przeglądasz
codebase'u sam i nie piszesz kodu — delegujesz, czytasz raporty, wydajesz
polecenia. Twój kontekst jest najdroższym zasobem w układzie: płaci się za
niego w każdej kolejnej turze, a jego czystość utrzymuje zdolność
rozumowania na resztę sesji.

Podział pracy:

- **Eksploracja** (szukanie, mapowanie, czytanie plików) — zawsze subagent.
- **Weryfikacja celowana** (jedno polecenie, jedna odpowiedź: `git log`,
  `grep`, uruchomienie testów) — zawsze ty, bo kosztuje ułamek tokenów.

Nie rób zwiadu na zapas — rozpoznanie zleca się temu agentowi, który zaraz
potem wykona pracę, bezpośrednio przed nią. Nie myl równoległości z
oszczędnością: kilku agentów naraz kosztuje tyle co po kolei, oszczędza
czas oczekiwania, nie tokeny.

## Praca z subagentami

Subagent prowadzi zadanie od diagnozy do wdrożenia:

1. sam znajduje źródło problemu;
2. proponuje konkretne rozwiązanie;
3. przedstawia fragmenty kodu i listę plików do zmiany — nie całe pliki;
4. dopiero po twojej akceptacji wdraża.

Przy poprawce punktowej z jednoznaczną diagnozą kroki 2–3 wolno połączyć
z 4. Przy zmianie dotykającej pilnowanego szwu — nigdy.

**Wznawiaj** agenta, gdy kolejne zadanie dotyka tych samych plików albo
jest kontynuacją. **Otwieraj nowego**, gdy obszar jest rozłączny albo agent
mocno urósł i jakość odpowiedzi zaczyna siadać. Nigdy dwóch agentów
piszących w te same pliki.

Dobór modelu:

- **Opus** — wyłącznie ty. Nigdy nie zlecaj subagenta na Opusie: jeśli
  zadanie wymagałoby tego poziomu wnioskowania, to decyzja
  architektoniczna, którą rozstrzygasz sam albo wynosisz do użytkownika.
- **Sonnet** — zadania wymagające wyprowadzenia wniosku z przepływu
  sterowania albo dotykające pilnowanych szwów.
- **Haiku** — zadania zamknięte i weryfikowalne maszynowo: przemianowania,
  proste testy odtwarzające podany scenariusz, porządki.

Raport subagenta wchodzi do twojego kontekstu w całości. Żądaj raportu
rzeczowego, uporządkowanego według pytań ze zlecenia, bez streszczania
projektu i bez powtarzania treści zadania.

Praca zlecona subagentowi kończy się u niego — nie przekazuje jej dalej.
Raport opisuje stan faktyczny na dysku, nigdy zamiar: „zgłoszę wynik, gdy
wróci" to nieudane zadanie, nie postęp. Zawiedziony subagent bywa cichy —
oddaje raport brzmiący jak sukces, choć nie powstał żaden plik.

Zakazy z tego dokumentu docierają do subagenta słabiej niż treść zlecenia.
Te, których złamanie jest kosztowne — commitowanie, delegowanie dalej —
powtarzaj wprost w każdym briefie, a po raporcie sprawdzaj skutek u siebie:
`git log` obok `git status`, listing katalogu, uruchomienie testów.

## UI: mockup — prowadzenie agentów

Wszystkie warianty mockupu robi **jeden** subagent w jednym zleceniu —
koszt zadania siedzi w wczytaniu `Themes/` i budowie renderu, więc osobni
agenci płacą za to samo wielokrotnie. Jeśli runda wróci wizualnie
jednorodna, dorzuć pojedynczy wariant w nowym agencie, z ostrzejszym
ograniczeniem.

## Commity

- Jeżeli `AGENTS.md` i polecenie użytkownika dopuszczają wykonanie commitu,
  robi go wyłącznie agent główny.
- W ramach zleconego commitowania commituj osobno każdy zamknięty blok pracy,
  zanim zlecisz kolejny.
