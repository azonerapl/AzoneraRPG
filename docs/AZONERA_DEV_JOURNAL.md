# AZONERA RPG — DZIENNIK ROZWOJU (od początku)

> Jeden, szczegółowy dziennik całej pracy nad projektem. Prowadzony chronologicznie i aktualizowany
> na bieżąco po każdym istotnym kroku. Zasada: zapisuję CO zrobiono, DLACZEGO i JAK zweryfikowano.
> Powiązane dokumenty: `AZONERA_TECHNICAL_AUDIT.md`, `AZONERA_CURRENT_STATE.md`, `AZONERA_ROADMAP.md`,
> `AZONERA_ART_BIBLE.md`, `AZONERA_VISUAL_REFERENCE.md`, `AI_PROGRESS.md`, `TESTING.md`.

Silnik: **Unity 6000.6.0f1 (URP, Linear)** · Ścieżka: **C:\Projects\AzoneraRPG** · Git: gałąź `master` (bez remote).

---

## SESJA 1 (2026-09-05) — Utworzenie projektu i fundament RPG

1. Utworzono projekt Unity 6 (URP) — początkowo `C:\Users\Paweu\TopDownRPGTopDownRPG`.
2. Architektura folderów `Assets/Azonera/` (Scripts w 22 modułach, ScriptableObjects, Editor, Art, Prefabs...).
3. Fundament kodu (24 skrypty runtime), moduły:
   - **Stats**: `CharacterStats` (HP/Mana/EXP/Level, obrażenia Physical/Magic/True, modyfikatory, eventy), `StatType`.
   - **Classes**: `ClassData` (SO, data-driven), `CharacterClass` (Knight/Sorcerer/Druid).
   - **Combat**: `DamageTypes` (IDamageable, DamageInfo), `MeleeAttacker` (zasięg, cooldown, krytyki).
   - **Monsters**: `MonsterData` (SO), `MonsterAI` (FSM Idle→Roam→Chase→Attack→Death).
   - **Items**: `ItemData` (SO), `ItemRegistry` (Resources).
   - **Inventory**: `Inventory` (stack/waga/sloty). **Equipment**: `EquipmentController` (modyfikatory).
   - **Loot**: `LootTable` (SO), `ItemPickup` (auto-podnoszenie).
   - **Camera**: `IsometricCameraController` (follow/zoom/obrót). **Player**: `PlayerController` (Input System), `PlayerActions` (klik-atak/interakcja).
   - **UI**: `HUDController`, `DialogueController`, `UIState`. **NPC**: `NPCInteractable`.
   - **Save**: `SaveSystem` (JSON). **VFX**: `TorchFlicker`. **Debug**: `DebugPanel` (F1). **Core**: `GameManager`.
   - **Editor**: `AzoneraDataBuilder` (dane), `AzoneraSceneBuilder` (scena wioski).
4. Wygenerowano 14 ScriptableObjects (3 klasy, 6 itemów, 4 potwory, loot) + scenę `AzoneraStartingVillage`.
5. Weryfikacja: kompilacja 0 błędów; PLAY — ruch WSAD, kamera izo, HUD działają.

## SESJA 1 c.d. — Tryb produkcyjny, audyt, higiena (PHASE 0)
6. Audyt techniczny → `AZONERA_TECHNICAL_AUDIT.md`; roadmapa → `AZONERA_ROADMAP.md`; Art Bible; `AI_PROGRESS.md`.
7. Higiena: **git init** + `.gitignore` + `.gitattributes` (commit `2c1b1bc`). Rebrand ProjectSettings → „Azonera Studio / Azonera".
   Usunięto śmieci szablonu (SampleScene, TutorialInfo, Readme). Utworzono strukturę `Art/*`.
8. Referencje wizualne od właściciela (2 screeny) → analiza `AZONERA_VISUAL_REFERENCE.md` (commit `dcffda2`).
   Kierunek: **realistyczne izometryczne 3D dark fantasy + głębia systemów klasycznego MMORPG**, własna oprawa.

## SESJA 1 c.d. — Testy i weryfikacja pętli SP (PHASE 1 start)
9. Wprowadzono **assembly definitions**: `Azonera.Runtime`, `Azonera.Editor`, `Azonera.Tests.EditMode`, `Azonera.Tests.PlayMode`.
10. Napisano testy: EditMode (Stats/Inventory/Equipment/Loot/Save), PlayMode (pełna pętla walki). `EquipmentController` — leniwe staty.
11. Projekt przeniesiony na **C:\Projects\AzoneraRPG** (oddzielony od serwera OTS). `Library` do odbudowy.

## SESJA 2 (2026-09-05 wieczór) — Loch referencyjny + Classic HUD
12. Nowy kierunek z referencji #2: ciemny izometryczny loch + pełny HUD klasyka.
13. Dodano:
    - `Scripts/Player/PlayerSkills.cs` — witalność/skille Tibia-like (Soul/Stamina/Food/Speed/MagicLevel/bronie) do HUD.
    - `Scripts/UI/ClassicHUDController.cs` — HUD samo-okablowujący się (Val_*, Fill_*): portret, HP/Mana, panel Skills,
      doll ekwipunku, backpack, minimapa, battle list, chat z zakładkami, hotbary.
    - `Editor/AzoneraDungeonBuilder.cs` — scena `AzoneraTemple`: ciemny kamień, 8 palników (ogień+światło+flicker),
      ołtarz z zielonym blaskiem, czerwony dywan, skarb, kryształy-spawn, mgła + post-FX, gracz/kamera(iso 55°)/managery + Classic HUD.
14. **Weryfikacja batchmode** (`-executeMethod AzoneraDungeonBuilder.BuildDungeon`): Library odbudowane, **0 błędów**,
    scena zbudowana. Commit `4b90a0c`.
15. **Testy batchmode**: **EditMode 22/22 PASS, PlayMode 1/1 PASS** → pętla SP potwierdzona jako WORKING. Commit `6159b9c`.

## SESJA 3 (2026-09-05) — PHASE 1 właściwa: Player/Vocation/Skills (data-driven, wpływ na gameplay)
Cel: prawdziwe systemy MMORPG (nie atrapy), zgodnie z Master Directive. Edytor otwarty na życzenie właściciela
(uwaga: otwarty edytor blokuje `Library`, więc testy batchmode uruchamiam przy zamkniętym edytorze; kompilację
weryfikuję z logu edytora, bo Unity auto-kompiluje przez directory-monitoring).

Dodane/zmienione:
- **`Scripts/Progression/ExperienceTable.cs`** — kanoniczna krzywa EXP (sześcienna, styl klasyka; 1→2 = 100 EXP).
  `CharacterStats.RequiredXpForLevel` deleguje tutaj.
- **`Scripts/Skills/SkillType.cs`** (+`Skill`) — typy skilli (Fist/Club/Sword/Axe/Distance/Shielding/Magic/Fishing) + dane skilla.
- **`Scripts/Skills/SkillSet.cs`** — prawdziwy trening/awans: próg = constant·1.1^(level-10)·trudność, eventy `OnSkillAdvanced/Changed`.
- **`Classes/CharacterClass.cs`** — enum rozszerzony (append) o **Paladin/Mage/Monk**; `ClassData` +pola: MaxSoulPoints,
  StartMagicLevel, DisplaySpeed, regen HP/Mana (amount+interwał), MeleeSkillDifficulty, MagicSkillDifficulty.
- **`Combat/MeleeAttacker.cs`** — integracja skilli: trafienie **trenuje** broń (AddTries) i broniącego (Shielding),
  a **poziom skilla zwiększa obrażenia** (WeaponSkillFactor). Leniwe pobranie SkillSet (odporne na kolejność Awake/runtime).
- **`UI/ClassicHUDController.cs`** — czyta skille/Magic Level z realnego `SkillSet` (fallback do PlayerSkills), subskrybuje eventy.
- **`Player/PlayerController.cs`** — `EnsureSkills()`: gwarantuje graczowi `SkillSet` w runtime i ustawia trudność wg profesji
  (działa też w już zbudowanej scenie `AzoneraTemple`, bez regeneracji).
- **`Editor/AzoneraDungeonBuilder.cs`** — dodano do generatora **NPC (Guide Alwin)** i **6 potworów testowych**
  (2× Marsh Snake, 2× Goblin, Wolf, Skeleton) → temple staje się w pełni testowalny (walka, loot, trening skilli).

Weryfikacja kompilacji (z logu otwartego edytora): wszystkie pliki Fazy 1 skompilowały się czysto; jedyne błędy
(przejściowe) dotyczyły 2 metod generatora dodanych chwilę później — po dodaniu definicji kod na dysku jest kompletny.
Świeży zielony compile potwierdzę po odzyskaniu focusu przez Unity (lub batchmode przy zamkniętym edytorze).

### Jak przetestować temple + postać (dla właściciela)
1. W otwartym Unity kliknij w okno edytora (wymusza rekompilację → Console ma być zielona).
2. Menu **Azonera → ★ Zbuduj Loch Referencyjny (Temple + Classic HUD)** — regeneruje `AzoneraTemple` z potworami/NPC/skillami.
3. Otwórz scenę `AzoneraTemple` i naciśnij **PLAY**. Sterowanie: **WSAD** ruch, **kółko** zoom, **Q/E** obrót,
   **LPM** atak/rozmowa, **F1** panel dev, **F5/F9** zapis/wczyt. Atakuj potwory — rośnie skill (HUD) i EXP.

### Następne kroki (po potwierdzeniu temple)
- Dokończyć Fazę 1: testy EditMode dla ExperienceTable i SkillSet; przełączyć witalność HUD w pełni na modele danych.
- Faza 2+: Items (Definition vs Instance), Combat DamageCalculator, TargetSystem+BattleList, Spelle, Status Effects,
  NPC/Dialog/Shop/Economy, Questy/Taski, świat/spawny/minimapa, Party/Guild/PvP, Save PlayerSaveData, architektura sieci.

---
_Ostatnia aktualizacja: SESJA 3, PHASE 1 — KOMPILACJA ZIELONA (0 błędów, potwierdzone z logu otwartego edytora, świeży CompileScripts bez error CS). Testy batchmode do uruchomienia przy zamkniętym edytorze._