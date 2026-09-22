# DungeonApp — reguły wiążące

## Granica automatyzacji: pięć zakazów

Aplikacja prowadzi księgowość i nie egzekwuje reguł. Poniższe pięć własności
jest technicznym przełożeniem tej zasady. Każdą da się sprawdzić, czytając sam
diff — bez znajomości intencji autora i bez uruchamiania aplikacji.
Uzasadnienie, konsekwencje i test asystenta: `docs/architecture.md`, sekcja
"Granica automatyzacji".

Kierunki już raz rozważone i odrzucone, wraz z argumentami, są w
`docs/decisions.md`. Przeczytaj je, zanim zaproponujesz zmianę architektury —
nie po.

Nie są to preferencje. Zabranie którejkolwiek zamienia tę aplikację w silnik
cRPG — i to jest jedyny powód, dla którego wszystkie pięć jest tu wypisanych
osobno, zamiast jednego zdania o intencji.

1. **Wyrażenie się nie rozgałęzia.** Żaden węzeł, operator ani parametr
   wyrażenia nie wybiera wartości na podstawie warunku. `min` i `max` to
   arytmetyka i są dozwolone. Cokolwiek o kształcie „jeżeli / gdy / o ile",
   predykat, wartość strzeżona flagą — nie.

2. **Żaden typ nie niesie czasu.** Nigdzie nie ma pola oznaczającego czas
   trwania, rundę, turę, wygaśnięcie, początek ani koniec obowiązywania. Czas
   w treści jest tekstem na karcie, a nie polem, po którym cokolwiek liczy.

3. **Wkłady do pola sumują się bezwarunkowo.** Nie ma priorytetu, kolejności
   rozstrzygania, reguły „to się nie kumuluje" ani odrzucania duplikatów po
   źródle czy kategorii. Lista wkładów ma dokładnie jedną operację: sumę.

4. **Nic nie wybiera celów za Mistrza Gry.** Operacja zmienia wyłącznie to, co
   MG jawnie wskazał. Może to być kilka rzeczy naraz — jak przy przekazaniu
   czegoś między dwiema stronami — ale nigdy zbiór wyznaczony regułą: obszar
   działania, „wszyscy", „najbliższy". Wszystko, co operacja zmieni, MG widzi
   przed jej wywołaniem.

5. **Zmiana stanu nie wywołuje kolejnej zmiany stanu.** Nic nie subskrybuje
   zmiany po to, żeby zapisać. Zdarzenia powiadamiają widoki i nigdy nie
   mutują.

---

Jak prowadzić tę pracę — zielone światło per etap, styl raportowania, zakazy
obowiązujące subagentów, rozstrzygnięcia, które wracają — jest w
`docs/collaboration.md`. Przeczytaj go na początku sesji; tamte zakazy nie są
powtarzane w każdym poleceniu z osobna.
