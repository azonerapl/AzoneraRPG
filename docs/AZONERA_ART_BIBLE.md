# AZONERA — ART BIBLE (Standard wizualny)

> Wersja: 1.0 · 2026-09-05 · Obowiązuje CAŁY projekt. Każdy asset musi być zgodny z tym dokumentem.
> Zasada: **wszystko wygląda, jakby pochodziło z JEDNEGO świata.** Zero mieszania stylów.

---

## 1. Kierunek artystyczny
**REALISTIC ISOMETRIC 3D DARK FANTASY MMORPG** — realistyczne izometryczne 3D w jakości nowoczesnego RPG,
z głębią systemów klasycznego MMORPG. Potwierdzony referencjami użytkownika — pełna analiza:
**`AZONERA_VISUAL_REFERENCE.md`** (obowiązkowa lektura przed każdym assetem). Materiał: `docs/reference/*.png`.
Charakterystyka: architektura szachulcowa (biały tynk + ciemne belki, deski, kamień), gęste realistyczne wnętrza,
ciepłe światło pochodni vs. chłodne światło dzienne, **roof-hiding** wnętrz, realistyczne proporcje postaci,
world-space nameplaty (imię + portret + HP/mana + ikona profesji). NIE kopiujemy referencji 1:1 — odtwarzamy
CHARAKTER oryginalną oprawą. NIE cartoon, NIE toy-like, NIE prymitywy jako finał.

## 2. Paleta i światło
- Bazowa kolorystyka środowiska: desaturowane zielenie, grafit/szarość kamienia, brąz drewna, patyna.
- Akcenty: ciepłe pomarańcze/złoto (ogień, złoto), głęboka czerwień (HP/krew), chłodny błękit (mana/księżyc).
- Światło: fizycznie wiarygodne, wysoki kontrast, głębokie miękkie cienie, atmosferyczna mgła, delikatny bloom.
- Unikać: jaskrawych, nasyconych, „plastikowych" kolorów; płaskiego oświetlenia.

## 3. Standard PBR (wszystkie materiały)
Każdy materiał = URP **Lit** z pełnym zestawem map:
- **Base Color (Albedo)** — bez wypalonego światła/cieni.
- **Normal** — detal powierzchni.
- **Metallic** + **Smoothness/Roughness** — fizycznie poprawne (metal ~1 metallic; kamień/drewno ~0).
- **Ambient Occlusion**.
- Opcjonalnie: Height/Parallax, Emission (świecące elementy).
Rozdzielczość tekstur: postacie/bronie 2K, środowisko 1–2K, drobne propsy 512–1K. Kompresja per platforma.

## 4. Skala i proporcje (1 unit = 1 metr)
- Człowiek dorosły: ~1.8 m. Realistyczna anatomia i proporcje (żadnych „fasolek"/„kulek z nogami").
- Drzwi ~2.1 m, kondygnacja ~3 m, drzewo dojrzałe 6–12 m. Spójna skala między assetami.
- Pivoty: postacie/potwory u stóp (y=0); budynki u podstawy; propsy u podstawy; bronie w punkcie chwytu.

## 5. Postacie (modularny system)
- Rig **humanoidalny** (Unity Humanoid) — współdzielone animacje.
- Modularne sloty wymienne bez zmiany logiki: `Head, Hair, Face, Body, Armor, Helmet, Gloves, Boots, Weapon_R, Weapon_L/Shield, Cape`.
- Budżet trójkątów (orientacyjnie): gracz/hero 30–60k, zwykły NPC 15–30k, potwór 20–50k (z LOD-ami).
- Animacje minimalne: `Idle, Walk, Run, Attack (1-2), Hit, Death`. Docelowo: cast, block, dodge, emotes.

## 6. Środowisko / budynki
- Modularny mesh kit (ściany, narożniki, dachy, podłogi, filary) — nie monolityczne bryły.
- Realistyczne materiały: kamień, cegła, drewno, dachówka, metal, tynk. Zniszczenia, mech, brud = wiarygodność.
- Roślinność: LOD + billboard w dali; wiatr (shader). Teren: warstwy (trawa/ziemia/skała/ścieżka) z blendem.

## 7. UI
- **TextMeshPro** (nie legacy Text). Styl premium dark-fantasy: ciemny grafit, metaliczne ramki, złote akcenty,
  HP czerwień, mana błękit, pozytywne zielenie. Ikonografia spójna. Zero „webowego" wyglądu, zero grubych obwódek.

## 8. VFX
- Subtelne, „premium", fizycznie wiarygodne (iskry, dym, magia z odpowiednim światłem/emisją). Bez przesady.

## 9. Konwencja prefabów (podmiana placeholdera → model)
Każda animowana encja = prefab z:
```
Entity (logika: CharacterStats, AI/Player, Collider, RB)
 └─ Visual (model + Animator)   ← TYLKO ten węzeł wymieniamy przy podmianie grafiki
```
Logika NIGDY nie zależy od konkretnego modelu. Placeholder-primitywy istnieją wyłącznie jako **DEBUG**
i muszą być oznaczone (nazwa `*_DEBUG`/warstwa) — nigdy nie prezentujemy ich jako finalnej grafiki.

## 10. Struktura folderów assetów
```
Assets/Azonera/Art/{Characters,Monsters,Environment,Buildings,Weapons,Armor,Props,Textures,UI,VFX}
Assets/Azonera/{Models,Materials,Animations,Prefabs,Audio}
```

---

## 11. Szablon ZAMÓWIENIA/PROMPTU assetu AI
Dla każdego brakującego assetu tworzymy wpis wg szablonu (nie generujemy „byle czego"):

```
ASSET: <nazwa>            TYP: <character|monster|weapon|armor|building|prop|texture|vfx|ui>
PRZEZNACZENIE: <gdzie i jak użyty w grze>
STYL: Realistic dark fantasy, spójny z Azonera Art Bible
PROPORCJE/SKALA: <np. człowiek 1.8 m; miecz 1.1 m>
MATERIAŁ/PBR: <albedo+normal+roughness+metallic+AO; opis powierzchni>
KOLORYSTYKA: <paleta wg §2>
OŚWIETLENIE: neutralne (mapy bez wypalonego światła)
PERSPEKTYWA/ORIENTACJA: <np. T-pose, front, pivot u stóp>
POZIOM SZCZEGÓŁOWOŚCI / BUDŻET: <triangles / rozdzielczość>
FORMAT: <FBX/GLB + tekstury PNG; rig Humanoid jeśli postać>
ANIMACJE: <lista jeśli dotyczy>
PRZEZROCZYSTOŚĆ: <tak/nie>
TODO-ID: <numer w AI_PROGRESS.md>
```

Wszystkie zamówienia trzymamy w `docs/AZONERA_ASSET_REQUESTS.md` (tworzony w Fazie 7 / w miarę potrzeb).
