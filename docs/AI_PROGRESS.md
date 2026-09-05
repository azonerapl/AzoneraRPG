# AZONERA MMORPG — AI PROGRESS LOG

> Żywy plik stanu. Aktualizowany po każdej większej zmianie. Format: co zrobiono / co działa /
> co nie działa / co dalej. Statusy: ✅ DONE · 🟡 PARTIAL · ⛔ BLOCKED · ⬜ TODO.

---

## Aktualny stan: PHASE 0 (Audit & Hygiene) — domykanie

### Zrobione w tej sesji (2026-09-05)
- ✅ Fundament kodu (poprzednia sesja): 24 skrypty runtime + 2 editor, ~3000 linii, kompiluje się (0 błędów).
  Moduły: Core, Stats, Classes, Combat, Monsters, NPC, Items, Inventory, Equipment, Loot, Camera, Player,
  UI, Save, VFX, Debug. 14 ScriptableObjects. Scena `AzoneraStartingVillage` (proceduralna, placeholdery).
- ✅ Zweryfikowane w PLAY: ruch WSAD, kamera izo, HUD, brak błędów runtime.
- ✅ PHASE 0: audyt techniczny → `AZONERA_TECHNICAL_AUDIT.md`.
- ✅ PHASE 0: roadmapa → `AZONERA_ROADMAP.md`.
- ✅ PHASE 0: `AZONERA_ART_BIBLE.md` (standard wizualny + szablon promptów AI).
- ✅ PHASE 0: `.gitignore` (Unity), rebrand ProjectSettings (`Azonera Studio` / `Azonera`).
- ✅ PHASE 0: usunięto śmieci szablonu (SampleScene, TutorialInfo, Readme.asset).
- ✅ PHASE 0: struktura `Assets/Azonera/Art/{Characters,Monsters,Environment,Buildings,Weapons,Armor,Props,Textures,UI,VFX}`.

### Co działa
- Kompilacja czysta, scena SP grywalna (ruch/kamera/HUD).

### Co NIE działa / ryzyka
- 🟡 Pełna pętla walki (kill→loot→equip→level→save/load) — kod jest, **brak testu E2E i testów automatycznych**.
- ⛔ Grafika = wyłącznie prymitywy DEBUG (do zastąpienia realnymi assetami — patrz Art Bible).
- ⬜ Brak: sieci/MMO, Androida, audio, questów, spelli, scen Bootstrap/Menu/CharSelect, testów, prefabów.

### Decyzje architektoniczne (podjęte)
- Świat: modularny mesh kit + streaming (nie jeden Terrain).
- Postać: rig Humanoid + modularne sloty; logika oddzielona od „Visual".
- Sieć: autorytet serwera projektowany od początku.
- Dane: ScriptableObjects jako źródło prawdy.
- UI: TextMeshPro + design system dark-fantasy.

### NASTĘPNY KONKRETNY KROK
1. (domknięcie PHASE 0) git init + pierwszy commit „restore point".
2. PHASE 1: `TestArena.unity` + testy automatyczne (EditMode: Stats/Inventory/Loot/Save;
   PlayMode: pętla walki) → zweryfikować i domknąć pętlę single-player.

### Uwagi operacyjne
- Tryb produkcyjny: praca WYŁĄCZNIE w katalogu projektu, przez kod/CLI/edytor-skrypty. Bez sterowania pulpitem.
- Placeholdery-prymitywy = DEBUG, nigdy nie prezentowane jako finalna grafika.
