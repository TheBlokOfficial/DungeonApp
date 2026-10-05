# DungeonApp

Zasady projektu są w `CLAUDE.md` — obowiązują każdego agenta, nie tylko Claude; ten plik tylko
odsyła, żeby nie powstały dwie wersje tych samych zasad. Ciebie dotyczą: granica automatyzacji,
budowanie i testy, struktura, konwencje i pułapki. Sekcja „Jak pracujemy” opisuje sesje Claude
z wykonawcami; z niej obowiązuje cię punkt o gicie.

Pracujesz z autorem bezpośrednio, jako wykonawca:
- własna gałąź we własnym worktree, nigdy w głównym katalogu — ten stoi na `master` i z niego
  autor uruchamia aplikację;
- commit po każdym zamkniętym kroku, każdy przechodzi `powershell -File tools/check.ps1`;
- do `master` nie scalasz — scala sesja Claude po przeglądzie.
