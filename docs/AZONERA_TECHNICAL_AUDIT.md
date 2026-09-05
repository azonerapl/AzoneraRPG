# AZONERA MMORPG — TECHNICAL AUDIT (PHASE 0)

> Wersja audytu: 1.0 · Data: 2026-09-05 · Autor: AI Lead Developer
> Projekt: `C:\Users\Paweu\TopDownRPGTopDownRPG` · Unity **6000.6.0f1** · URP 17.6 · Linear color space
> Tryb: PRODUCTION (Master Development Directive v1.0)

Ten dokument to punkt odniesienia całej produkcji. Stan ustalony przez inspekcję plików na dysku
(nie z pamięci). Klasyfikacja stanu: **DONE / PARTIAL / PLACEHOLDER / MISSING / BROKEN**.

---

## 0. Podsumowanie jednym zdaniem

Istnieje **czysto kompilujący się, modularny szkielet single-player RPG** (ruch, kamera izo, statystyki,
walka, AI, loot, ekwipunek, zapis, HUD) — ale **cała warstwa wizualna to prymitywy debugowe**, brak
jakiejkolwiek warstwy sieciowej MMO, brak pipeline'u assetów, brak konfiguracji Androida i brak kontroli wersji.
Fundament kodu: **solidny**. Fundament produkcji MMORPG i grafiki: **do zbudowania od zera**.

---

## 1. CO JUŻ ISTNIEJE

### Konfiguracja techniczna
- Unity **6000.6.0f1**, **URP 17.6.0**, **Color Space = Linear** (poprawny pod PBR).
- URP z dwoma tierami: `PC_RPAsset`/`PC_Renderer` oraz `Mobile_RPAsset`/`Mobile_Renderer` — baza pod PC+Android.
- Nowy **Input System 1.20** + `AI Navigation 2.0` (NavMesh) + `Terrain` + `Timeline` + `Test Framework`.

### Kod (Assets/Azonera/Scripts — 24 pliki runtime + 2 editor, ~3000 linii)
| Moduł | Pliki | Rola |
|---|---|---|
| Core | GameManager | Spięcie runtime (gracz↔kamera↔HUD), save hotkeys, respawn |
| Stats | CharacterStats, StatType | HP/Mana/EXP/Level, obrażenia, modyfikatory, eventy |
| Classes | ClassData, CharacterClass | 3 profesje (data-driven SO) |
| Combat | DamageTypes, MeleeAttacker | IDamageable, DamageInfo, atak wręcz z krytykami |
| Monsters | MonsterAI, MonsterData | FSM Idle→Roam→Chase→Attack→Death |
| NPC | NPCInteractable | Interakcja + dialog |
| Items | ItemData, ItemRegistry | Itemy (SO) + rejestr po ID (Resources) |
| Inventory | Inventory | Plecak: stackowanie, waga, sloty |
| Equipment | EquipmentController | Sloty + auto-przeliczanie statystyk |
| Loot | LootTable, ItemPickup | Tabele + drop w świecie + auto-podnoszenie |
| Camera | IsometricCameraController | Iso follow + zoom + obrót |
| Player | PlayerController, PlayerActions | Ruch (Input System) + klik-atak/interakcja |
| UI | HUDController, DialogueController, UIState | HUD + dialogi |
| Save | SaveSystem | JSON w persistentDataPath |
| VFX | TorchFlicker | Migotanie pochodni |
| Debug | DebugPanel | Panel dev (F1) |
| Editor | AzoneraDataBuilder, AzoneraSceneBuilder | Generatory danych i sceny |

### Dane gry (ScriptableObjects — 14 assetów)
- Klasy: Knight, Sorcerer, Druid.
- Itemy: RustedSword, LeatherArmor, IronRing, HealthPotion, ManaPotion, MonsterTooth.
- Potwory: MarshSnake, Wolf, Goblin, Skeleton.
- Loot: Loot_Common.

### Scena
- `Assets/Azonera/Scenes/AzoneraStartingVillage.unity` — generowana proceduralnie (Environment, Player,
  Main Camera, EventSystem, HUD Canvas, GameSystems, NPC, Monsters).

---

## 2. CO DZIAŁA (zweryfikowane w PLAY)

- **DONE** Kompilacja: 0 błędów, 0 warningów (po sprzątnięciu `DEVELOPMENT_BUILD`→`DEBUG`).
- **DONE** Uruchomienie sceny, brak wyjątków runtime.
- **DONE** Ruch gracza WSAD (Input System), kamera izometryczna podąża za graczem.
- **DONE** HUD wiąże się z CharacterStats (klasa, poziom, HP, mana, udźwig) i aktualizuje eventowo.
- **DONE** GameManager inicjalizuje świat; panel dev (F1) otwiera się.
- **DONE** Architektura modularna, event-driven, data-driven (SO), namespace'y per moduł.

## 3. CO JEST USZKODZONE / RYZYKA

- **BROKEN(brand)** `productName = "TopDownRPGTopDownRPG"`, `companyName = "DefaultCompany"` — nie „Azonera".
- **RISK** Ruch potworów przez `transform.position` (kinematyczny RB) — działa, ale docelowo pod NavMesh.
- **RISK** 0 pokrycia testami (Test Framework jest, ale brak testów) — wymagane wg dyrektywy §14.
- **UNVERIFIED** Pełna pętla walki (zabij→loot→equip→zmiana statów→level→save/load) skompilowana i wpięta,
  ale **nie przeszła end-to-end testu** (test GUI przy skali 0.31x był niepewny). Status: **PARTIAL**.

## 4. CO JEST PLACEHOLDEREM (❗ krytyczne wg dyrektywy §3–5, §12)

- **PLACEHOLDER** Gracz = kapsuła + sześcian. Niedozwolone jako finał.
- **PLACEHOLDER** Potwory = kapsuły kolorowe. NPC = kapsuła. Loot = obracający się sześcian.
- **PLACEHOLDER** Budynki = cube + obrócony cube (dach). Drzewa = cylinder + sfery. Mury/pochodnie = prymitywy.
- **PLACEHOLDER** Wszystkie 18 materiałów = jednolite kolory URP Lit (bez tekstur/normal/roughness/AO).
- **PLACEHOLDER** HUD = legacy `UnityEngine.UI.Text` + jednolite prostokąty (nie premium dark-fantasy UI).
- **PLACEHOLDER** Brak animacji — postacie się nie animują (system animatora przygotowany, ale pusty).

> Zgodnie z dyrektywą te prymitywy są **wyłącznie DEBUG/PROTOTYPE**. Nie są finalną grafiką.

## 5. CZEGO BRAKUJE (MISSING)

- **Grafika:** 0 modeli 3D, 0 tekstur, 0 animacji, 0 prefabów, 0 rigów, brak systemu modularnej postaci.
- **Art pipeline:** brak folderów Art/Characters/Monsters/…, brak wymagań assetów, brak promptów AI.
- **MMO/Networking:** brak jakiejkolwiek warstwy sieciowej (transport, autorytet serwera, sync, chat,
  party, guild, market, trade, persystencja kont/postaci na serwerze).
- **Gameplay:** brak spelli, skilli, questów (poza szkieletem dialogu), craftingu, ekonomii, PvP.
- **Sceny:** brak Bootstrap / MainMenu / CharacterSelection / TestArena.
- **Świat:** brak terenu (Terrain), biomów, regionów, lochów, wnętrz — jest jedna proceduralna wioska.
- **Audio:** brak AudioManager i jakichkolwiek dźwięków.
- **Android:** platforma nie skonfigurowana (touch controls, build target, tekstury/atlasy mobilne).
- **Testy:** brak testów EditMode/PlayMode.
- **Optymalizacja:** brak pooling/LOD/occlusion/batching audytu.
- **CI/Wersjonowanie:** brak repozytorium git.

## 6. CO NALEŻY PRZEBUDOWAĆ

- **UI** → z legacy Text na TextMeshPro + system skórek dark-fantasy (docelowo UI Toolkit lub uGUI+TMP).
- **Wizualia** → cała warstwa prymitywów zastąpiona prefabami z modelami (bez zmiany logiki — komponenty
  gotowe, wymieniamy tylko dziecko „Visual" i podpinamy Animator).
- **GameManager** → w miarę wzrostu rozbić na wyspecjalizowane serwisy (bootstrap, systemy, sesja).
- **Ruch potworów** → NavMeshAgent + pooling zamiast bezpośredniego transformu.

## 7. CO NALEŻY ZACHOWAĆ (mocne fundamenty — NIE przepisywać)

- Warstwa danych (ScriptableObjects: Class/Item/Monster/Loot) — czysta, skalowalna.
- `CharacterStats` (wspólny dla gracza i potworów, eventowy, gotowy pod autorytet serwera).
- Rozdział odpowiedzialności (Combat ≠ PlayerController, Inventory/Equipment osobno).
- Generatory edytorowe (idempotentne) — rozbudujemy je, nie kasujemy.
- Linear color space + dual URP tier (PC/Mobile).

## 8. PRIORYTETY (od najwyższego)

1. **P0 — Higiena:** git + .gitignore, rebrand (Azonera), usunięcie śmieci szablonu, `docs/AI_PROGRESS.md`.
2. **P0 — Art pipeline & standard:** struktura folderów Art/*, dokument wymagań assetów, spójny art direction,
   system modularnej postaci (equipment slots na kościach), konwencja prefabów „Visual".
3. **P1 — Zamknięcie pętli single-player:** zweryfikować end-to-end walka→loot→equip→level→save/load + testy.
4. **P1 — Sceny szkieletu:** Bootstrap → MainMenu → CharacterSelection → Game/TestArena + przepływ.
5. **P2 — Pierwsze realne assety** (modele/animacje) podmieniające placeholdery w kontrolowanym zakresie.
6. **P2 — Fundament sieci (MMO):** wybór stacku (Netcode for GameObjects / Mirror / Fish-Net), warstwa
   transportu, autorytet serwera dla ruchu i walki, sync encji.
7. **P3 — Systemy MMO:** chat, party, guild, market, trade, persystencja.
8. **P3 — Android:** input dotykowy, budżety wydajności, atlasy, testy na tierze Mobile.

## 9. PLAN PRODUKCJI

Pełny plan w `docs/AZONERA_ROADMAP.md` (fazy 0–13). Zasada: faza ukończona = **zweryfikowana**, nie „wygląda dobrze".

## 10. NASTĘPNY KONKRETNY KROK

**PHASE 0 domknięcie (bezpieczne, tylko pliki, bez ruszania sceny):**
1. Inicjalizacja git + `.gitignore` Unity + pierwszy commit „Phase 0-4 foundation (restore point)".
2. Rebrand w ProjectSettings: `companyName = Azonera Studio`, `productName = Azonera`.
3. Usunięcie pozostałości szablonu (`Assets/Scenes/SampleScene`, `TutorialInfo`, `Readme.asset`).
4. Utworzenie struktury `Assets/Azonera/Art/*` + `docs/AZONERA_ART_BIBLE.md` (standard wizualny + wymagania
   assetów + szablon promptów AI) + `docs/AI_PROGRESS.md`.

Po zatwierdzeniu tego kroku przechodzimy do **Faza 1 — domknięcie i test pętli single-player + testy automatyczne**.
