# AZONERA — TESTY

> Faza 1. Cel: zamienić „PARTIAL/niezweryfikowane" na twarde „WORKING" dla pętli single-player.

## Struktura
- **Assembly definitions** (wymagane, by testy widziały kod gry):
  - `Assets/Azonera/Scripts/Azonera.Runtime.asmdef` — cały runtime.
  - `Assets/Azonera/Editor/Azonera.Editor.asmdef` — generatory edytorowe.
  - `Assets/Azonera/Tests/EditMode/Azonera.Tests.EditMode.asmdef`
  - `Assets/Azonera/Tests/PlayMode/Azonera.Tests.PlayMode.asmdef`
- **EditMode** (logika, deterministyczne): `StatsTests`, `InventoryTests`, `EquipmentTests`, `LootTests`, `SaveTests`.
- **PlayMode** (integracja): `CombatLoopTests` — walka → śmierć → EXP → loot → pickup.

## Uruchomienie z GUI (edytor otwarty)
`Window → General → Test Runner` → zakładka EditMode / PlayMode → `Run All`.

## Uruchomienie z CLI (batchmode) — WYMAGA ZAMKNIĘTEGO EDYTORA (blokada Library)
```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe"
PROJ="C:/Users/Paweu/TopDownRPGTopDownRPG"

# EditMode
"$UNITY" -runTests -batchmode -projectPath "$PROJ" \
  -testPlatform EditMode \
  -testResults "$PROJ/docs/test-results-editmode.xml" \
  -logFile "$PROJ/docs/test-editmode.log"

# PlayMode
"$UNITY" -runTests -batchmode -projectPath "$PROJ" \
  -testPlatform PlayMode \
  -testResults "$PROJ/docs/test-results-playmode.xml" \
  -logFile "$PROJ/docs/test-playmode.log"
```
Kod wyjścia 0 = wszystkie testy zielone. Wyniki w plikach XML (NUnit) + logi.

## STATUS (2026-09-05)
- Testy **NAPISANE, jeszcze NIEZWERYFIKOWANE** (tryb „pisz teraz, weryfikacja później").
- Do zrobienia razem: uruchomić powyższe (zamknięty edytor) LUB Test Runner z GUI, zebrać wyniki, naprawić ewentualne błędy.
- Ryzyka do sprawdzenia przy pierwszym compile: poprawność nazw referencji w asmdef
  (`Unity.InputSystem`, `UnityEngine.UI`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`)
  oraz to, że wprowadzenie asmdef nie zrywa referencji w scenie (GUID skryptów bez zmian — powinno być OK).
