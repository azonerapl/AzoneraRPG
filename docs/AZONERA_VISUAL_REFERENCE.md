# AZONERA — VISUAL & SYSTEMS REFERENCE

> Kotwica art-directionu i zakresu systemów. Materiał źródłowy: 2 screeny dostarczone przez użytkownika
> (`docs/reference/azonera_ref_01_isometric_interior.png`, `docs/reference/azonera_ref_02_mmorpg_systems.png`).
>
> ⚠️ ZASADA PRAWNA/PROJEKTOWA: to REFERENCJE CHARAKTERU, nie do klonowania. **Nie kopiujemy** assetów, modeli,
> tekstur, map, nazw, spelli ani UI 1:1. Odtwarzamy *wrażenie i jakość*, z **oryginalną** oprawą Azonery.

---

## A. Ref #1 — CEL GRAFICZNY (realistyczne izometryczne 3D dark fantasy)

**Kamera / prezentacja**
- Wysoki widok izometryczny/top-down, pitch ~55–62°, yaw ~45°. Obiekty czytelne z góry.
- **Roof-hiding**: dach/górne piętro znika, gdy gracz jest w środku budynku (widok wnętrza). Mechanika obowiązkowa.
- Lekka perspektywa (nie płaskie orto). → Nasz `IsometricCameraController` (pitch 52°) = właściwa baza, tuning +.

**Architektura (timber-frame / mur pruski)**
- Białe tynkowane panele obramowane ciemnymi belkami drewnianymi; deski podłogowe; kamienne akcenty.
- Modularny kit: ściany, narożniki, belki, panele, podłogi, framugi, dach (osobny, znikający).

**Wnętrza — gęstość rekwizytów (bardzo ważne)**
- Meble/props: łóżko z pościelą, skrzynie, beczki, półki, stół, alembik/destylarnia (mosiądz), moździerz,
  obrazy na ścianie, czaszka, książki, buteleczki/mikstury (kolorowe, lekko emisyjne), narzędzia alchemiczne.
- Pochodnie ścienne z ciepłym, migoczącym światłem. Wnętrze „żyje", nie jest puste.

**Materiały (PBR)**
- Drewno (deski/belki), tynk, kamień/cobblestone, mosiądz/metal, tkanina (pościel), szkło (mikstury).
- Albedo bez wypalonego światła + normal + roughness + metallic + AO. Realistyczne, nie plastikowe.

**Oświetlenie**
- Ciepłe pomarańczowe point-lighty pochodni (interior glow) vs. chłodniejsze światło dzienne na zewnątrz.
- Miękkie cienie, delikatny bloom na emisji (mikstury, ogień). Kontrast wnętrze/zewnętrze.

**Środowisko zewnętrzne**
- Soczysta, naturalna zieleń trawy, kwiaty, krzewy, drzewa, cobblestone ścieżki, drewniane tabliczki/szyldy.

**Postacie**
- Realistyczne proporcje humanoidalne (~1.8 m), zbroje płytowe/skórzane, widoczne włosy, czytelna sylwetka z góry.

**In-world nameplate / world-space UI (do odtworzenia, oryginalnie)**
- Nad postacią: pływające **imię**, mała **ikona-portret** w okręgu, **pasek HP (zielony)** + **mana (niebieski)**,
  **ikona profesji** (tarcza), **ikony statusu/questa** (książka). NPC: **dymek dialogowy** z ikoną.

## B. Ref #2 — CEL SYSTEMOWY (głębia klasycznego MMORPG, „Tibia-like")

To NIE jest cel graficzny (to klasyczny klient 2D) — to **wzorzec ZAKRESU systemów i gęstości HUD**:
- **Ekwipunek (paper doll):** ~10 slotów (hełm, amulet, plecak, zbroja, ręce L/P, nogi, buty, pierścień, amunicja).
- **Backpack** siatkowy + wiele plecaków/kontenerów, stackowanie, liczniki.
- **Paski spelli / hotkeys** (dolne) + runy/mikstury.
- **Battle list** (lista celów), **minimapa**, **VIP/party list**, **analityka** (hunt/loot/XP/damage).
- **Floating damage** (liczby kolorowe), **VFX zaklęć** (błyskawice, holy, obszarowe), efekty trafień.
- **Vocations:** Knight, Paladin, Sorcerer, Druid. Spelle rzucane komendami — u nas **oryginalne nazwy Azonery**.
- Śmierć/zwłoki/loot, depot, ekonomia, PvP, party — docelowy zakres MMORPG.

## C. Synteza — czym jest Azonera wizualnie i systemowo
**Realistyczne izometryczne 3D (jakość Ref#1) + głębia systemów klasycznego MMORPG (zakres Ref#2), oryginalna oprawa.**

## D. Konsekwencje dla istniejącego projektu (PRESERVE → IMPROVE)
- **ZACHOWAĆ:** kamerę izometryczną (dostroić kąt/zoom + dodać roof-hiding), całą logikę (staty/combat/inv/equip/save),
  dane SO, generatory edytorowe.
- **ULEPSZYĆ:** materiały → PBR; HUD → TextMeshPro + world-space nameplaty wg A; encje → prefaby z modelami.
- **DODAĆ:** roof-hiding, floating damage/hit FX, gęste wnętrza (prop kit), vocation Paladin, spelle/runy, depot.
- **Placeholdery-prymitywy pozostają wyłącznie DEBUG**, aż wejdą realne assety wg tego dokumentu i Art Bible.

## E. Kolejność assetów (Faza 7, po zatwierdzeniu)
1. Modularny **timber-frame building kit** + podłogi/dach (roof-hiding).
2. **Interior prop kit** (łóżko, skrzynia, beczka, półka, stół, alembik, mikstury, pochodnia, obrazy).
3. **Postać gracza** (rig Humanoid + modularne sloty) — 1 kompletny set na start.
4. **Potwory** — 1–2 z oryginalnym designem (odpowiedniki szczura/węża/wilka/humanoida).
5. **Nameplate world-space UI** + HUD TMP.
Każdy asset zamawiany wg szablonu promptu z `AZONERA_ART_BIBLE.md` §11 i wpisywany do `AZONERA_ASSET_REQUESTS.md`.
