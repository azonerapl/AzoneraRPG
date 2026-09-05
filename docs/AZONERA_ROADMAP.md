# AZONERA MMORPG — PRODUCTION ROADMAP

> Wersja: 1.0 · 2026-09-05 · Cel: **profesjonalne, realistyczne dark-fantasy MMORPG (PC + Android)**
> Zasada nadrzędna: faza jest ukończona dopiero, gdy jest **zweryfikowana** (kompilacja + testy + integracja).
> Każda faza: ANALYZE → PLAN → IMPLEMENT → TEST → FIX → VERIFY → DOCUMENT → NEXT.

Legenda statusu: ✅ DONE · 🟡 PARTIAL · ⛔ BLOCKED · ⬜ TODO

---

## PHASE 0 — PROJECT AUDIT & HYGIENE
- ✅ Audyt techniczny (`AZONERA_TECHNICAL_AUDIT.md`)
- ✅ Roadmapa (ten plik)
- ⬜ Git init + `.gitignore` Unity + commit „restore point"
- ⬜ Rebrand ProjectSettings (Azonera)
- ⬜ Usunięcie śmieci szablonu (SampleScene, TutorialInfo, Readme)
- ⬜ `AZONERA_ART_BIBLE.md` (standard wizualny + wymagania assetów + szablon promptów AI)
- ⬜ `AI_PROGRESS.md` (żywy status)
- ⬜ Struktura folderów `Art/*`
**Wyjście:** czysty, ownowersjonowany projekt „Azonera" z pipeline-ready strukturą. Zero śmieci, zero błędów.

## PHASE 1 — TECHNICAL FOUNDATION
- ✅ Modularna architektura (namespace'y, SO, event-driven) — istnieje, utrzymać
- 🟡 Domknięcie pętli SP: walka→loot→equip→zmiana statów→level→save/load (kod jest; **brak testu E2E**)
- ⬜ Testy automatyczne (EditMode: Stats/Inventory/Loot/Save; PlayMode: pętla walki na TestArena)
- ⬜ `TestArena.unity` (scena do szybkich testów mechanik)
- ⬜ Rozbicie GameManager na serwisy (Bootstrap/Session/SystemsRegistry) gdy urośnie
**Wyjście:** zielone testy pętli SP na TestArenie, brak regresji.

## PHASE 2 — WORLD FOUNDATION
- ⬜ Standard świata: Terrain vs mesh modularny (decyzja: **modularny mesh kit** pod realizm + wydajność)
- ⬜ Grid/streaming regionów (pod duży świat MMO), NavMesh bake
- ⬜ Pierwszy region „Azonera — Wioska Startowa" **z realnymi assetami** (zastępuje proceduralne prymitywy)
- ⬜ Oświetlenie: directional + volumetric fog + baked/mixed GI + reflection probes
**Wyjście:** jeden dopracowany, realistyczny region z prawidłowym oświetleniem i NavMesh.

## PHASE 3 — PLAYER SYSTEM
- 🟡 Ruch/kamera/staty — istnieje (placeholder wizualny)
- ⬜ **Modularna postać 3D** (rig humanoidalny, sloty: head/hair/body/armor/helmet/gloves/boots/weapon/cape)
- ⬜ Animator (idle/walk/run/attack/hit/death) + blend + root motion decyzja
- ⬜ Podmiana placeholdera na model bez zmiany logiki (komponent „Visual" + Animator)
**Wyjście:** grywalna, animowana, realistyczna postać z wymiennym ekwipunkiem wizualnym.

## PHASE 4 — COMBAT
- 🟡 Melee + krytyki + FSM AI — istnieje
- ⬜ Typy obrażeń/rezystencje/tarcze, hit-reactions, telegraphy, i-frames
- ⬜ Skille/spelle jako SO (cooldowny, koszt many, VFX, projectile/AoE)
- ⬜ Balans przez dane, nie kod
**Wyjście:** czytelna, satysfakcjonująca walka na realnych modelach z VFX.

## PHASE 5 — NPC + QUESTS
- 🟡 NPC + dialog — szkielet
- ⬜ System questów (SO: cele, warunki, nagrody, stany), dziennik questów
- ⬜ Sklep/handel NPC, gossip, questgivery, lore hooks
**Wyjście:** łańcuch questów startowych działający end-to-end.

## PHASE 6 — MMORPG SYSTEMS (NETWORKING)
- ⬜ Wybór stacku sieciowego (rekomendacja: **Unity Netcode for GameObjects** lub **Fish-Net**; ocena Mirror)
- ⬜ Transport, autorytet serwera: ruch, walka, loot, inventory (anti-cheat by design)
- ⬜ Sync encji, interest management (AoI), tick rate, reconciliation
- ⬜ Konta/postacie/persystencja (serwer + DB), chat, party, guild, market, trade, PvP
**Wyjście:** 2+ klientów w jednym świecie, autorytatywny serwer ruchu i walki.

## PHASE 7 — ART PIPELINE
- ⬜ `AZONERA_ART_BIBLE.md` egzekwowany: jeden spójny styl (realistic dark fantasy, skala, oświetlenie)
- ⬜ Import pipeline (modele/tekstury/materiały PBR: albedo/normal/roughness/metallic/AO)
- ⬜ Biblioteka prefabów (environment kit, postacie, potwory, bronie, zbroje), atlasy, LOD-y
- ⬜ Dla brakujących assetów: **dokładne prompty AI** + wymagania (format, skala, orientacja, PBR)
**Wyjście:** spójny, skalowalny katalog assetów; zero mieszania stylów.

## PHASE 8 — UI / UX
- ⬜ Migracja HUD z legacy Text → **TextMeshPro**; skórka premium dark-fantasy (grafit/złoto/HP-czerw/mana-niebieski)
- ⬜ Ekrany: MainMenu, CharacterSelection/Creation, Settings, Inventory/Character/Quests/Map
- ⬜ Skalowanie UI PC↔Android, input dotykowy
**Wyjście:** profesjonalny, spójny UI na obu platformach.

## PHASE 9 — AUDIO / VFX / POLISH
- ⬜ AudioManager (music/ambient/combat/ui/footsteps/spells), miksery, pule
- ⬜ VFX: hit/crit/heal/spell/level-up/teleport/pickup (subtelne, premium), post-processing tuning
**Wyjście:** oprawa audio-wizualna „cinematic".

## PHASE 10 — PC OPTIMIZATION
- ⬜ Object pooling, batching/SRP batcher, occlusion culling, LOD, shadow/light budżety, profiler 60+ FPS

## PHASE 11 — ANDROID OPTIMIZATION
- ⬜ Tier Mobile URP, touch controls, atlasy/kompresja tekstur, budżet draw calls/RAM/GPU, testy na urządzeniu

## PHASE 12 — QA
- ⬜ Pełne testy regresyjne, playtesty, telemetria/analytics, naprawa błędów, stabilizacja

## PHASE 13 — RELEASE BUILD
- ⬜ Buildy PC + Android, podpisywanie, wersjonowanie, dystrybucja

---

## Decyzje architektoniczne (wstępne, do rewizji per faza)
- **Świat:** modularny mesh kit + streaming regionów (nie jeden wielki Terrain) — realizm + skalowalność MMO.
- **Postać:** rig humanoidalny + modularne sloty na kościach; logika oddzielona od wizualiów (swap „Visual").
- **Sieć:** autorytet serwera od początku w projektowaniu (żadnej logiki „tylko lokalnej", której nie da się zsync).
- **Dane:** ScriptableObjects jako źródło prawdy balansu; docelowo eksport/serwerowy katalog.
- **UI:** TextMeshPro + spójny design system; UI Toolkit rozważany dla narzędzi edytorskich.

## NASTĘPNY KONKRETNY KROK
Domknięcie **PHASE 0** (git, rebrand, sprzątanie, Art Bible + struktura Art, AI_PROGRESS) — wyłącznie operacje
plikowe/konfiguracyjne w katalogu projektu, bez modyfikacji sceny. Potem **PHASE 1**: testy + domknięcie pętli SP.
