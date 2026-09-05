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

### ZMIANA TRYBU (2026-09-05, po dyrektywie użytkownika)
- Tryb doprecyzowany: **CONTINUE EXISTING PROJECT** (nie budowa od zera) → PRESERVE → IMPROVE → refactor tylko gdy konieczne.
- Potwierdzono: jedyny projekt Azonery w Unity = `TopDownRPGTopDownRPG` (drugi projekt = tutorial URP).
- Dodano `docs/AZONERA_CURRENT_STATE.md` w wymaganym formacie (audyt „przejęcia" istniejącego projektu).
- Vocations docelowe wg dyrektywy: Knight/Paladin/Sorcerer/Druid — brakuje **Paladin**.
- ✅ Referencja wizualna DOSTARCZONA (2 screeny) → analiza w `AZONERA_VISUAL_REFERENCE.md`, kopie w `docs/reference/`.
  Cel potwierdzony: realistyczne izometryczne 3D (Ref#1) + głębia systemów klasycznego MMORPG (Ref#2), oryginalna oprawa.
  Kamera izometryczna = właściwa tech → ZACHOWUJEMY. Do dodania: roof-hiding, floating dmg, gęste wnętrza, Paladin, spelle/depot.
- Art Bible zaktualizowany do „Realistic Isometric 3D Dark Fantasy MMORPG".

### NASTĘPNY KONKRETNY KROK
1. (domknięcie PHASE 0) git init + pierwszy commit „restore point" — ✅ zrobione (2c1b1bc).
2. PHASE 1: `TestArena.unity` + testy automatyczne (EditMode: Stats/Inventory/Loot/Save;
   PlayMode: pętla walki) → zweryfikować i domknąć pętlę single-player.
   Uwaga wykonawcza: uruchomienie testów w batchmode wymaga zamknięcia otwartego edytora (blokada Library),
   albo uruchomienia Test Runner z GUI przez użytkownika.

### Uwagi operacyjne
- Tryb produkcyjny: praca WYŁĄCZNIE w katalogu projektu, przez kod/CLI/edytor-skrypty. Bez sterowania pulpitem.
- Placeholdery-prymitywy = DEBUG, nigdy nie prezentowane jako finalna grafika.

---

## Sesja 2026-09-05 (wieczór) — projekt w C:\Projects\AzoneraRPG + kierunek „loch referencyjny"

Referencja #2 (4 panele): ciemny izometryczny loch/świątynia + PEŁNY HUD klasycznego MMORPG
(Skills: Exp/Level/HP/Mana/Soul/Capacity/Speed/Food/Stamina/Magic Level + skille broni; doll ekwipunku;
backpack; minimapa; battle list; chat z zakładkami; hotbary).

### Zrobione (kod, tryb „pisz teraz, weryfikacja później")
- `Scripts/Player/PlayerSkills.cs` — witalność/skille Tibia-like zasilające HUD (Soul/Stamina/Food/Speed/MagicLevel/broń).
- `Scripts/UI/ClassicHUDController.cs` — HUD samo-okablowujący się po nazwach (Val_*, Fill_*), reaguje na eventy statów.
- `Editor/AzoneraDungeonBuilder.cs` — generator sceny `AzoneraTemple.unity`: ciemny kamienny loch (podłoga/ściany/kolumny),
  8 palników (ogień + ciepłe światło + flicker), centralny ołtarz z zielonym blaskiem, czerwony dywan, skarb, kryształy-spawn,
  mgła i post-FX (bloom/vignette/kontrast) + PEŁNY Classic HUD + gracz/kamera(iso pitch55)/managery.
- Środowisko lochu = GREYBOX (prymitywy, klimat/kompozycja) — jawnie DEBUG do czasu realnych assetów. HUD = docelowy.

### Weryfikacja
- Uruchomiono Unity **batchmode** (`-executeMethod AzoneraDungeonBuilder.BuildDungeon`): odbudowa Library + kompilacja
  całości (w tym asmdefy/testy z poprzedniej tury — pierwsza realna kompilacja) + budowa sceny. Log: `docs/build-dungeon.log`.
- Status: W TOKU / do potwierdzenia z logu (kod wyjścia + brak `error CS`).

### Następny krok po weryfikacji
- Jeśli zielono: PLAY na `AzoneraTemple` → screenshot; potem realne assety (timber/stone kit, postać) wg Art Bible.
- Jeśli błędy: diagnoza z `build-dungeon.log`, fix, ponowny batchmode.

## Sesja 2026-09-06 — przejęcie projektu + Visual Overhaul (SESJA 4)

Pełny opis: `AZONERA_DEV_JOURNAL.md` → SESJA 4.

### Co działa (nowe)
- ✅ Liczby obrażeń/leczenia + VFX trafień/śmierci/awansu (pulowane, event-driven).
- ✅ System celowania (Tab/Esc/klik) + **działająca Battle List** + nameplate'y nad potworami.
- ✅ Spawnery z respawnem — lokacja przestała się „zużywać".
- ✅ Kamera MMORPG: skokowy obrót 45°, poziomy zoomu, roof-hiding zasłaniającej geometrii.
- ✅ Materiały PBR faktycznie podpięte w lochu (4 zestawy CC0: bruk, cegła, drewno, blacha).

### Co NIE działa / nadal placeholder
- ⛔ **Modele/sprite'y/animacje**: brak narzędzia do generowania grafiki na tym koncie.
  Potwory/NPC = kapsuły `Visual_DEBUG`, gracz = proceduralny rycerz z brył, VFX = cząstki `_PLACEHOLDER`.
  Architektura pod podmianę jest gotowa (`MonsterData.ModelPrefab`, węzeł `Visual`), assety muszą przyjść z zewnątrz.
- 🟡 HUD: backpack, paper doll, hotbary, minimapa, chat, spellbook, quest log — **nadal kosmetyczne**.
- ⬜ Brak: spelli/cooldownów, questów, sklepu/ekonomii, status effectów, Tierów 1–3, profesji
  Paladin/Mage/Monk (enum jest, brak assetów ClassData), sieci, audio.

### NASTĘPNY KONKRETNY KROK
Funkcjonalny ekwipunek: siatka backpacka + paper doll podpięte do `Inventory`/`EquipmentController`
(ikony, klik = użyj/załóż, waga vs `Capacity`). To zamienia największy „martwy" fragment HUD-u w system.

---

### WERYFIKACJA (batchmode, 2026-09-05 23:09) — ZIELONO
- Kompilacja: **0 błędów** (cały projekt + asmdefy + testy).
- Scena `AzoneraTemple` zbudowana; Library odbudowane.
- **Testy EditMode: 22/22 PASS** (Stats/Inventory/Equipment/Loot/Save).
- **Testy PlayMode: 1/1 PASS** — pętla walka→śmierć→EXP→loot→pickup zweryfikowana E2E.
- Wniosek: pętla single-player = **WORKING** (nie już PARTIAL). Fundament twardo potwierdzony.
