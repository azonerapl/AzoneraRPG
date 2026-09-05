# AZONERA RPG — HANDOVER (przejęcie projektu)

> Wszystko, czego potrzebuje kolejny developer, by kontynuować pracę. Czytaj też: `AZONERA_DEV_JOURNAL.md`
> (pełna historia), `AZONERA_CURRENT_STATE.md`, `AZONERA_ROADMAP.md`, `AZONERA_ART_BIBLE.md`,
> `AZONERA_VISUAL_REFERENCE.md`, `TESTING.md`, `AI_PROGRESS.md`.

## 1. Tożsamość i wymagania
- **Silnik:** Unity **6000.6.0f1** (URP, Color Space **Linear**). Zainstaluj dokładnie tę wersję przez Unity Hub.
- **Platforma:** PC-first (Standalone). Android przewidziany później (URP ma tier Mobile).
- **Repo (GitHub, PRYWATNE):** **origin → https://github.com/azonerapl/AzoneraRPG.git**, gałąź **`main`**.
  Dostęp: poproś właściciela (`azonerapl`) o zaproszenie jako **collaborator** (GitHub → Settings → Collaborators),
  wtedy `git clone` zadziała.
- To **osobny projekt**, NIE ma nic wspólnego z serwerem Tibia OTS „Azonera.pl" (mimo nazwy marki).

## 2. Jak zdobyć i otworzyć projekt
```bash
git clone https://github.com/azonerapl/AzoneraRPG.git   # gałąź main; repo prywatne (wymaga dostępu collaborator)
```
- W **Unity Hub → Add → wskaż folder projektu**. Pierwsze otwarcie odbuduje `Library/` (kilka minut) — to normalne
  (`Library/`, `Temp/`, `Logs/`, `obj/`, `*.csproj`, `*.sln` są w `.gitignore`).
- Scena startowa: **`Assets/Azonera/Scenes/AzoneraTemple.unity`** (loch + Classic HUD + animowany rycerz + potwory/NPC).

## 3. Struktura repo
```
Assets/Azonera/
  Scripts/            kod runtime (asmdef Azonera.Runtime), moduły:
    Core, Stats, Classes, Progression, Skills, Combat, Monsters, NPC, Items,
    Inventory, Equipment, Loot, Camera, Player, UI, Save, VFX, Debug
  Editor/             generatory + narzędzia (asmdef Azonera.Editor)
  Tests/EditMode|PlayMode   testy (asmdef Azonera.Tests.*)
  ScriptableObjects/  dane gry (klasy, potwory, loot); Resources/Items (itemy)
  Scenes/             AzoneraTemple (główna), AzoneraStartingVillage
  Materials/          materiały (w tym Materials/Dungeon)
  Art/                docelowe assety (Characters/Monsters/Environment/...) — na razie puste
docs/                 dokumentacja + referencje (docs/reference/*.png)
```
**Assembly definitions:** `Azonera.Runtime`, `Azonera.Editor`, `Azonera.Tests.EditMode`, `Azonera.Tests.PlayMode`.
Nazwy assembly-refs w asmdef: `Unity.InputSystem`, `UnityEngine.UI`, `Unity.RenderPipelines.Core.Runtime`,
`Unity.RenderPipelines.Universal.Runtime`.

## 4. Generatory scen (menu „Azonera" w edytorze)
- **★ Zbuduj Loch Referencyjny (Temple + Classic HUD)** → buduje `AzoneraTemple` (loch, HUD, gracz, kamera, potwory, NPC).
- **★ Zbuduj Vertical Slice (wszystko)** / **Zbuduj scenę wioski** → wioska (`AzoneraStartingVillage`).
- **1. Utwórz dane gry** → generuje ScriptableObjects (klasy/itemy/potwory/loot).
Sceny są **generowane z kodu** (idempotentnie) — po zmianie generatora uruchom menu ponownie, by odświeżyć scenę.

Batchmode (bez GUI, wymaga ZAMKNIĘTEGO edytora — blokada `Library`):
```bash
UNITY="/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe"
"$UNITY" -batchmode -projectPath "C:/Projects/AzoneraRPG" \
  -executeMethod Azonera.EditorTools.AzoneraDungeonBuilder.BuildDungeon -logFile build.log -quit
```

## 5. Testy
- **GUI:** `Window → General → Test Runner` → EditMode/PlayMode → Run All.
- **CLI (edytor zamknięty):** patrz `TESTING.md`. Ostatni wynik: **EditMode 22/22, PlayMode 1/1** (+ nowe testy EXP/Skill).
- Zasada: **nie commituj bez zielonej kompilacji i testów.**

## 6. Stan obecny (co działa)
- Pętla single-player (walka→śmierć→EXP→loot→pickup, staty/inventory/equipment/save) — **WORKING**, pokryta testami.
- **Faza 1**: `ExperienceTable`, system skilli (`SkillSet` — trening/awans, wpływ na obrażenia), profesje
  (`ClassData` + enum Knight/Sorcerer/Druid/Paladin/Mage/Monk), integracja walka↔skille, HUD czyta skille.
- **Classic HUD** (portret, HP/Mana, panel Skills, doll ekwipunku, backpack, minimapa, battle list, chat, hotbary).
- **Postać gracza**: artykułowany rycerz (rig ze stawami: biodra/kolana/barki/łokcie/kręgosłup) z warstwowym pancerzem
  i **proceduralną animacją chodu/idle** (`CharacterVisualFactory` + `KnightRig` + `KnightAnimator` +
  `PlayerCharacterVisual` dodający go w runtime).
- Kamera izometryczna, ruch WSAD, GameManager (F5 zapis / F9 wczyt), panel dev (F1).

## 7. Konwencje / architektura
- **Data-driven** (ScriptableObjects źródłem prawdy), **event-driven** (eventy zamiast pollingu).
- Rozdział **DATA / GAMEPLAY / PRESENTATION**; UI NIE jest źródłem prawdy (HUD tylko czyta).
- Prymitywy w scenie = **GREYBOX/DEBUG** (nazwy `*_DEBUG`), nigdy jako finalna grafika. Węzeł „Visual"/„KnightVisual"
  gotowy pod podmianę na docelowy model/render bez zmiany logiki.
- Architektura **MMO-ready**: nie pisz logiki „tylko lokalnej", której nie da się później zsynchronizować z serwerem.

## 8. Znane blokery
- **Generacja grafiki AI**: workspace usługi graficznej = plan `free`, `0` kredytów, `unlim.available:false` →
  foto-real render/sprite/model **zablokowany**. Po doładowaniu kredytów: wygeneruj postać/assety i podmień węzły „Visual".
- **Batchmode vs otwarty edytor**: batchmode blokuje się o `Library`, gdy edytor jest otwarty. Testy/generację CLI
  uruchamiaj przy zamkniętym edytorze; przy otwartym — Unity auto-kompiluje (directory monitoring / focus), a kompilację
  widać w Console lub logu edytora.

## 9. Następne kroki (skrót roadmapy — pełna w `AZONERA_ROADMAP.md`)
- Dokończyć Fazę 1 (witalność HUD w pełni z modeli danych; animacja ataku pod `MeleeAttacker`).
- **Faza 2**: Items **Definition vs Instance**, `DamageCalculator`, **TargetSystem + działająca Battle List**, spelle/runy,
  status effects; potem NPC/dialog/shop/economy, questy/taski, świat/spawny/minimapa, party/guild/PvP,
  `PlayerSaveData`, warstwa sieci (authoritative server).

## 10. Git — jak współpracować
```bash
git pull --rebase        # pobierz zmiany
# ...pracuj...
git add -A && git commit -m "typ(zakres): opis"
git push                 # wypchnij na origin (GitHub)
```
- Commituj po zielonej kompilacji + testach. Nie rób `git reset --hard`/`push --force` bez potrzeby.
- Prowadź `docs/AZONERA_DEV_JOURNAL.md` (co/dlaczego/jak zweryfikowano) po każdym istotnym kroku.
