# Współpraca — Azonera RPG

Dziękujemy za pracę nad Azonerą. Poniżej zasady, które utrzymują projekt spójny i stabilny.

## Zanim zaczniesz
- Unity **6000.6.0f1** (dokładnie ta wersja, przez Unity Hub).
- Przeczytaj [`docs/AZONERA_HANDOVER.md`](docs/AZONERA_HANDOVER.md) (setup, architektura, generatory, testy).
- Repo prywatne — pracuj po uzyskaniu dostępu (collaborator).

## Zasady kodu
- **Data‑driven**: dane gry w ScriptableObjects (klasy, itemy, potwory, loot, spelle…). Balans = edycja assetu, nie kodu.
- **Event‑driven**: powiadamiaj eventami zamiast pollingu w `Update`, gdzie to sensowne.
- **Rozdział warstw**: DATA / GAMEPLAY / PRESENTATION. **UI nie jest źródłem prawdy** — HUD tylko czyta i reaguje.
- **MMO‑ready**: nie pisz logiki „tylko lokalnej", której nie da się później zsynchronizować z serwerem.
- **Bez atrap jako finał**: prymitywy w scenie = GREYBOX/DEBUG (nazwy `*_DEBUG`). Węzeł „Visual"/„KnightVisual"
  jest gotowy pod podmianę na docelowy model — nie zaszywaj logiki w konkretny wygląd.
- **Moduły**: nie twórz „boga" `GameManager`. Rozbijaj duże systemy. Trzymaj kod w odpowiednim module `Scripts/<Moduł>`.
- **Assembly definitions**: runtime → `Azonera.Runtime`, edytor → `Azonera.Editor`, testy → `Azonera.Tests.*`.
  Nowy kod runtime umieszczaj pod `Assets/Azonera/Scripts` (obejmuje go `Azonera.Runtime`).

## Zanim zacommitujesz — WERYFIKUJ
1. Kompilacja Unity bez błędów (Console czysta).
2. Uruchom testy: `Window → General → Test Runner → Run All` (lub batchmode, [`docs/TESTING.md`](docs/TESTING.md)).
3. Sprawdź brak regresji (przetestuj powiązane systemy — zmiana itemu/potwora może dotknąć wielu miejsc).
4. **Nie deklaruj „działa" bez testu.** Nie zostawiaj `TODO: implement later`, jeśli możesz to zrobić teraz poprawnie.

## Git
```bash
git pull --rebase
# ...praca...
git add -A
git commit -m "typ(zakres): opis"
git push
```
- Konwencja commitów: `feat(...)`, `fix(...)`, `test(...)`, `docs(...)`, `refactor(...)`, `chore(...)`.
- **Nie** rób `git reset --hard`, `push --force`, masowego kasowania bez potrzeby i uzgodnienia.
- Gałąź główna: **`main`**. Większe zmiany rób na gałęzi i scalaj po weryfikacji.

## Dziennik
Po każdym istotnym kroku dopisz wpis do [`docs/AZONERA_DEV_JOURNAL.md`](docs/AZONERA_DEV_JOURNAL.md):
**co** zrobiono, **dlaczego**, **jak zweryfikowano**. Aktualizuj też `docs/AI_PROGRESS.md` / `docs/AZONERA_CURRENT_STATE.md`
przy zmianie stanu systemu.

## Definition of Done
System jest gotowy, gdy: kod istnieje, jest podłączony, **gameplay go realnie używa**, UI reaguje na dane,
jest przetestowany, kompiluje się bez błędów, batchmode przechodzi, brak ukrytych błędów w logach — i da się go dalej rozwijać.
