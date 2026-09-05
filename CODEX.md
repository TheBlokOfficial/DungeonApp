`AGENTS.md` jest jedynym źródłem zasad projektu. Ten plik określa wyłącznie
sposób prowadzenia sesji przez Codex i ChatGPT.

## Rola agenta głównego

Agent główny odpowiada za zrozumienie polecenia, decyzje architektoniczne,
podział pracy, kontrolę raportów i końcową weryfikację.

Szeroką eksplorację codebase'u, analizę logów i inne zadania generujące dużo
materiału pomocniczego deleguj do subagenta. Celowaną weryfikację — jedno
wyszukanie, odczyt konkretnego pliku, `git status`, `git log` albo
uruchomienie testów — wykonuj sam.

Nie rób zwiadu na zapas. Rozpoznanie zlecaj agentowi, który zaraz potem
wykona pracę, bezpośrednio przed nią. Nie deleguj pracy, jeżeli zadanie jest
małe, sekwencyjne albo koordynacja kosztowałaby więcej niż wykonanie.

## Praca z subagentami

Deleguj wyłącznie konkretne i ograniczone zadania. Jeden subagent prowadzi
powierzony blok od diagnozy do raportu albo wdrożenia:

1. ustala stan faktyczny i źródło problemu;
2. proponuje rozwiązanie oraz wskazuje fragmenty kodu i pliki do zmiany;
3. czeka na decyzję agenta głównego przed wdrożeniem;
4. po akceptacji wdraża i wykonuje celowaną weryfikację.

Przy punktowej poprawce z jednoznaczną diagnozą analizę i wdrożenie wolno
połączyć w jednym zadaniu. Przy zmianie, dla której `AGENTS.md` wymaga
wcześniejszej propozycji, subagent nie rozpoczyna wdrożenia przed jej
zaakceptowaniem.

Nie uruchamiaj dwóch agentów zapisujących do tych samych plików. Prace
niezależne wolno prowadzić równolegle. Subagent nie deleguje swojej pracy
dalej, chyba że jego zlecenie mówi o tym wprost.

Wznawiaj istniejącego subagenta, gdy następne zadanie jest kontynuacją albo
dotyka tego samego obszaru. Nowego używaj dla obszaru niezależnego albo gdy
dotychczasowy kontekst przestał być użyteczny.

Raport ma być krótki, odpowiadać na pytania ze zlecenia i opisywać stan
faktyczny:

- co ustalono lub zmieniono;
- które pliki zostały dotknięte;
- jakie polecenia weryfikacyjne uruchomiono i z jakim wynikiem;
- co pozostało niewykonane lub niepewne.

Raport subagenta nie jest dowodem. Agent główny sprawdza rezultat na dysku
i sam wykonuje końcową weryfikację.

## Dobór modelu i poziomu rozumowania

Agent główny jest głównym architektem niezależnie od modelu wybranego dla
sesji. Model agenta głównego wybiera użytkownik w interfejsie; nie zmieniaj
go ani nie zakładaj, że jest to konkretny model.

Dobierając subagenta, jawnie wskaż jego model:

- **Terra** (`gpt-5.6-terra`) — eksploracja codebase'u, research, diagnoza,
  implementacja oraz zadania wymagające rozumienia przepływu sterowania.
- **Luna** (`gpt-5.6-luna`) — zadania zamknięte i maszynowo weryfikowalne:
  przemianowania, proste testy, transformacje i porządki.

Dla Terry używaj domyślnie średniego poziomu rozumowania, a wysokiego przy
analizie złożonej logiki i przypadków brzegowych. Dla Luny używaj niskiego
albo średniego poziomu.

Nie pozwalaj subagentowi niejawnie dziedziczyć modelu agenta głównego. Nie
uruchamiaj subagentów na Astrze ani Solu, chyba że użytkownik wyraźnie o to
poprosi.

## UI: prowadzenie mockupów

Wszystkie warianty jednej rundy mockupu zlecaj jednemu subagentowi w jednym
zadaniu. Jeżeli runda wróci wizualnie jednorodna, dodatkowy wariant wolno
zlecić nowemu agentowi z innym ograniczeniem strukturalnym.

## Commity

- Jeżeli `AGENTS.md` i polecenie użytkownika dopuszczają wykonanie commitu,
  robi go wyłącznie agent główny.
- Subagentom przypominaj w każdym zleceniu, że nie wolno im commitować ani
  pushować.
- W ramach zleconego commitowania commituj osobno każdy zamknięty blok pracy,
  zanim rozpoczniesz kolejny.
