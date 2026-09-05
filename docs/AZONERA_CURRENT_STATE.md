# CURRENT STATE — AZONERA MMORPG (Existing Unity Project)

> Tryb: CONTINUE EXISTING PROJECT · PRESERVE → IMPROVE → REFACTOR ONLY WHEN NECESSARY
> Data: 2026-09-05 · Ustalono przez inspekcję plików (nie zgadywanie).
> Projekt (jedyny projekt Azonery w Unity): `C:\Users\Paweu\TopDownRPGTopDownRPG`
> (Drugi projekt Unity na dysku — „Setup Guide In-Editor Tutorial" — to samouczek URP, NIE Azonera.)
>
> ⚠️ Referencja wizualna („załączony screen") NIE została dostarczona w tej sesji. Zakładany kierunek:
> realistyczne, izometryczne dark-fantasy MMORPG w duchu klasyki (Tibia-like), oryginalna oprawa.
> Realna praca nad grafiką czeka na dostarczenie screena.

---

## Unity
- **Wersja:** 6000.6.0f1 (Unity 6)
- **Render Pipeline:** URP 17.6.0 · **Color Space: Linear** · dual tier: `PC_RPAsset` + `Mobile_RPAsset`
- **Packages (istotne):** Input System 1.20, AI Navigation 2.0 (NavMesh), Terrain, Timeline, UGUI, Test Framework, Visual Scripting
- **Build target:** PC-first (Standalone). Android: **niekonfigurowany** (przygotować później).

## EXISTING SYSTEMS
| System | Status | Uwaga |
|---|---|---|
| Statystyki (CharacterStats: HP/Mana/EXP/Level/obrażenia/modyfikatory) | **WORKING** | Wspólny gracz+potwory, eventowy |
| Profesje (ClassData SO: Knight/Sorcerer/Druid) | **WORKING** | Brakuje **Paladin** |
| Ruch gracza (Input System, WSAD) | **WORKING** | Zweryfikowany w PLAY |
| Kamera izometryczna (follow/zoom/obrót) | **WORKING** | Zweryfikowana w PLAY |
| Walka wręcz (MeleeAttacker, krytyki) | **PARTIAL** | Kod OK; brak testu E2E, brak floating dmg/hitFX |
| AI potworów (FSM Idle→Roam→Chase→Attack→Death) | **PARTIAL** | Ruch transformem (nie NavMesh); niezweryfikowany walką |
| Loot (LootTable + drop + auto-pickup) | **PARTIAL** | Kod OK, niezweryfikowany E2E |
| Plecak (Inventory: stack/waga) | **PARTIAL** | Brak UI drag&drop/use/equip |
| Ekwipunek (EquipmentController + przeliczanie statów) | **PARTIAL** | Brak UI, niezweryfikowany E2E |
| NPC + dialog | **PARTIAL** | Szkielet; brak sklepu/questów/usług |
| HUD (HP/Mana/EXP/klasa/udźwig) | **WORKING (placeholder-art)** | Legacy UI.Text — do migracji na TMP |
| Zapis/odczyt (SaveSystem JSON) | **PARTIAL** | Kod OK, niezweryfikowany E2E |
| Panel deweloperski (F1) | **WORKING** | EXP/heal/level/god/FPS |
| Generatory edytorowe (Data/Scene Builder) | **WORKING** | Idempotentne, do zachowania |

## EXISTING SCENES
- `Assets/Azonera/Scenes/AzoneraStartingVillage.unity` — **jedyna** scena gry (proceduralna wioska).
  W Build Settings jako jedyna, index 0. **Do ROZWINIĘCIA, nie zastępowania** (to przyszły „ROOM").
- (Usunięto śmieciową `SampleScene` z szablonu URP.)

## EXISTING PLAYER
- GameObject `Player` (tag Player): CapsuleCollider + Rigidbody (freeze rot X/Z) + CharacterStats(Knight) +
  Inventory + EquipmentController + MeleeAttacker + PlayerController + PlayerActions.
- Wizual: **PLACEHOLDER** (kapsuła + sześcian kierunku). Logika gotowa pod podmianę na model (węzeł „Visual").
- Brak Animatora / animacji.

## EXISTING WORLD
- Proceduralna wioska: teren (plane), kamienne ścieżki, mury z bramą, 4 domy, las, ogrodzenia,
  10 pochodni (point light + migotanie), wejście do lochu. **Wszystko z prymitywów = PLACEHOLDER.**
- Brak: Terrain właściwego, regionów, Z-poziomów (piętra/schody/podziemia), wnętrz budynków, biomów.

## EXISTING UI
- `HUD Canvas` (Screen Space Overlay) + EventSystem (InputSystemUIInputModule).
- HUD: paski HP/Mana/EXP (Image Filled), teksty klasa/poziom/udźwig — **legacy UnityEngine.UI.Text**.
- Panel dialogu (dół, ukryty). **Brak:** login, character select, inventory/equipment/skills/quests/map/chat UI.

## EXISTING GAMEPLAY
- Działa: ruch, kamera, HUD, inicjalizacja świata (GameManager), respawn, hotkeys zapisu.
- Zakodowane, niezweryfikowane E2E: walka→śmierć potwora→EXP→loot→pickup→plecak→equip→przeliczenie→save/load.
- **Brak:** spelle, skille, questy (poza dialogiem), sklep, depot, day/night, floating damage.

## EXISTING NETWORKING
- **BRAK.** Zero warstwy sieciowej. Architektura danych (CharacterStats/SO) jest jednak „server-authority friendly"
  (czyste dane + eventy) — nie wymaga przepisania, gdy dojdzie sieć.

## EXISTING DATABASE
- **BRAK** bazy/persystencji serwerowej. Lokalnie: zapis JSON w `persistentDataPath`.

## EXISTING ASSETS
- **0 modeli 3D, 0 tekstur, 0 animacji, 0 dźwięków.** 18 materiałów = jednolite kolory URP Lit (bez map PBR).
- 14 ScriptableObjects (3 klasy, 6 itemów, 4 potwory, 1 loot) — **dane realne, do zachowania**.
- Struktura pipeline'u `Assets/Azonera/Art/*` — utworzona, pusta (gotowa na import).

## EXISTING PREFABS
- **0 prefabów.** Encje (gracz/NPC/potwory) budowane proceduralnie w scenie przez generator.
  → Dług: brak prefabów utrudnia podmianę wizualiów; do wprowadzenia (Faza 3/7).

## EXISTING TOOLS
- `Azonera/Editor/AzoneraDataBuilder.cs` — generuje dane gry (idempotentnie). **WORKING.**
- `Azonera/Editor/AzoneraSceneBuilder.cs` — generuje scenę wioski. **WORKING.**
- Menu **Azonera** w edytorze. Brak edytora świata/map (Faza world editor — do zaprojektowania).

## CURRENT ERRORS
- **Kompilacja: 0 błędów.** (Log Editor.log: brak `error CS`.)
- Jedyny „error" w Console = licencyjny token Unity (`Access token is unavailable`) — środowiskowy, nie z kodu.

## CURRENT WARNINGS
- 0 warningów kompilacji (po sprzątnięciu `DEVELOPMENT_BUILD`→`DEBUG`).
- Runtime info (nie błąd): atlas cieni — rozwiązane wyłączeniem cieni pochodni.
- Uwaga: zmiany z Phase 0 (rebrand, usunięcie szablonu) Unity zaimportuje przy najbliższym focusie edytora.

## PLACEHOLDERS (DEBUG — NIE finalna grafika)
- Gracz, NPC, wszystkie potwory = kapsuły. Loot = obracający się sześcian.
- Budynki/drzewa/mury/pochodnie = prymitywy. Materiały = jednolite kolory (bez tekstur/normal/roughness/AO).
- HUD = legacy Text + proste prostokąty.

## TECHNICAL DEBT
- Brak prefabów (encje proceduralne) → utrudniona podmiana modeli.
- Ruch potworów transformem zamiast NavMeshAgent.
- HUD na legacy `UnityEngine.UI.Text` (docelowo TMP).
- Brak testów automatycznych (0 pokrycia) mimo obecnego Test Framework.
- GameManager rośnie jako punkt spinający — rozbić na serwisy, gdy urośnie.

## WHAT MUST BE PRESERVED (nie ruszać / nie przepisywać)
- Warstwa danych (ScriptableObjects Class/Item/Monster/Loot).
- `CharacterStats` i cały rozdział modułów (Combat≠Player, Inventory/Equipment osobno).
- Kamera izometryczna, ruch gracza, HUD-binding, GameManager, SaveSystem, generatory edytorowe.
- Konfiguracja: Linear + dual URP (PC/Mobile), scena `AzoneraStartingVillage` (rozwijać, nie kasować).

## WHAT MUST BE REBUILT (po udokumentowaniu i planie migracji — nie „delete & rebuild")
- Warstwa wizualna: prymitywy → prefaby z modelami/animacjami (bez zmiany logiki, przez węzeł „Visual").
- HUD: legacy Text → TextMeshPro + design system dark-fantasy.
- Ruch potworów: transform → NavMeshAgent + pooling.

## WHAT IS MISSING
- Grafika (modele/tekstury/animacje/prefaby), audio, VFX (floating dmg/hit/spell).
- Vocation **Paladin**; spelle/skille; questy data-driven; sklep/depot; day/night lighting.
- Sieć/MMO (client↔authoritative server↔DB), Z-poziomy/piętra, edytor świata.
- Sceny: Login/CharacterSelection/HUD-owe okna (inventory/equipment/skills/quests/map/chat).
- Konfiguracja Androida; testy automatyczne.

## PRIORITY
1. **P0** Zweryfikować i zablokować to, co „działa": testy + E2E istniejącej pętli SP (bez tego „PARTIAL" jest niepewny).
2. **P0** Referencja wizualna (screen) → `AZONERA_ART_BIBLE` dostrojony do docelowego stylu izometrycznego.
3. **P1** Rozwinąć istniejącą scenę w prawdziwy „ROOM" (spawn/NPC/sklep/depot/heal/tutorial/pierwsze potwory) — najpierw układ i systemy, potem realne assety.
4. **P1** Prefabizacja encji (gracz/NPC/potwór) + konwencja „Visual" pod podmianę modeli.
5. **P2** Pierwsze realne assety wg Art Bible (izometryczny kit + postać + 1–2 potwory).
6. **P2** Spelle/questy/sklep data-driven; floating damage/hit FX.
7. **P3** Fundament sieci (server-authority), Z-poziomy, edytor świata, Android.

---

# NEXT ACTION
**Jeden krok:** napisać **automatyczne testy (EditMode + PlayMode) i zweryfikować end-to-end istniejącą pętlę
single-player** (walka → śmierć potwora → EXP/level → loot → pickup → plecak → equip → przeliczenie statów →
save/load) na dedykowanej scenie `TestArena`. Cel: zamienić status „PARTIAL/niezweryfikowane" na twarde
„WORKING", zanim rozbudujemy świat, grafikę i warstwę MMO. To czysto kodowa, nie-destrukcyjna praca w projekcie,
zgodna z zasadą PRESERVE → IMPROVE.

(Równolegle BLOCKER dla warstwy wizualnej: proszę o dostarczenie referencyjnego screena — bez niego nie zaczynam realnej grafiki.)
