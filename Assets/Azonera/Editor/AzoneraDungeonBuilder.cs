#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Azonera.Stats;
using Azonera.Classes;
using Azonera.Player;
using Azonera.CameraSystem;
using Azonera.Inventory;
using Azonera.Equipment;
using Azonera.Combat;
using Azonera.Monsters;
using Azonera.NPC;
using Azonera.UI;
using Azonera.Core;
using Azonera.DevTools;
using Azonera.VFX;
using Azonera.World;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Buduje mroczną scenę lochu/świątyni (klimat z referencji) + pełny HUD klasycznego MMORPG.
    /// Samodzielny generator (własne helpery), nie modyfikuje generatora wioski. Idempotentny.
    /// Prymitywy środowiska = GREYBOX (klimat/kompozycja) do czasu realnych assetów. HUD = docelowy.
    /// </summary>
    public static class AzoneraDungeonBuilder
    {
        private const string MatFolder = "Assets/Azonera/Materials/Dungeon";
        private const string SceneFolder = "Assets/Azonera/Scenes";
        private const string ScenePath = SceneFolder + "/AzoneraTemple.unity";
        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        private static Font _font;
        private static Sprite _sprite;

        [MenuItem("Azonera/★ Zbuduj Loch Referencyjny (Temple + Classic HUD)", priority = 1)]
        public static void BuildDungeon()
        {
            // NewScene() jest zabronione w trybie PLAY — bez tej blokady generator rzucał
            // InvalidOperationException i zostawiał użytkownika z pustą sceną „Untitled".
            if (!AzoneraEditorGuards.EnsureNotPlaying("Zbuduj Loch Referencyjny")) return;

            _mats.Clear();
            AzoneraMaterialLibrary.ResetCache();
            EnsureFolder(MatFolder); EnsureFolder(SceneFolder);
            // Tekstury muszą być zaimportowane z właściwymi ustawieniami ZANIM powstaną materiały.
            EnsureTexturesImported();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildRoom();
            var player = BuildPlayer();
            var cam = BuildCamera(player.transform);
            BuildManagers();
            BuildClassicHUD();
            BuildNPC();
            BuildTestMonsters();

            var pc = player.GetComponent<PlayerController>();
            SetRef(pc, "_cameraTransform", cam.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Azonera] Scena lochu + Classic HUD zbudowana: " + ScenePath);
            EditorUtility.DisplayDialog("Azonera",
                "Loch referencyjny + Classic HUD gotowy!\nScena: AzoneraTemple\nNaciśnij PLAY.", "OK");
        }

        // ============================================================ LIGHTING (dark dungeon)
        private static void BuildLighting()
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.5f, 0.55f, 0.72f);
            sun.intensity = 0.22f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(60f, -40f, 0f);

            // Ambient trójstrefowy: chłodne niebo, neutralne zenit-horyzont, cieplejszy odbłysk
            // od podłogi. Płaski ambient spłaszczał bryły — tu każda ściana dostaje inny ton.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.11f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.09f, 0.085f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.075f, 0.05f);
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.032f, 0.045f);
            RenderSettings.fogDensity = 0.022f;

            BuildReflectionProbe();
            TryPostFX();
        }

        /// <summary>
        /// Sonda odbić — bez niej metal (palniki, złoto, zbroja gracza) odbija czarną pustkę
        /// i wygląda jak matowy plastik. Z nią łapie ciepłe światło wnętrza.
        /// </summary>
        private static void BuildReflectionProbe()
        {
            var go = new GameObject("Reflection Probe");
            go.transform.position = new Vector3(0f, 2.5f, 0f);
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(34f, 10f, 28f);
            probe.resolution = 256;
            probe.cullingMask = ~0;
            probe.intensity = 1f;
            probe.boxProjection = true;
        }

        /// <summary>
        /// Pełny stos color gradingu — to on odpowiada za „filmowy" wygląd zamiast surowego renderu.
        /// Najważniejszy element to TONEMAPPING ACES: bez niego jasne miejsca (ogień, emisja)
        /// wypalają się do płaskiej bieli, a ciemne do czarnej plamy. ACES zachowuje detal
        /// w obu skrajnościach i daje kontrast, z którego żyje dark fantasy.
        /// </summary>
        private static void TryPostFX()
        {
            try
            {
                var volGo = new GameObject("Global Volume");
                var vol = volGo.AddComponent<UnityEngine.Rendering.Volume>();
                vol.isGlobal = true;
                vol.priority = 1f;
                var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();

                // 1. Tonemapping ACES — filmowa krzywa, ratuje przepalenia ognia i emisji.
                var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
                tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

                // 2. Bloom — ciepła poświata pochodni i kryształów. Próg nisko, by łapał ogień.
                var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                bloom.intensity.Override(1.35f);
                bloom.threshold.Override(0.75f);
                bloom.scatter.Override(0.72f);
                bloom.tint.Override(new Color(1f, 0.86f, 0.62f));
                bloom.highQualityFiltering.Override(true);

                // 3. Ekspozycja/kontrast/nasycenie — baza nastroju.
                var ca = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
                ca.postExposure.Override(0.35f);   // scena była zbyt ciemna, by cokolwiek odczytać
                ca.contrast.Override(22f);
                ca.saturation.Override(-4f);
                ca.colorFilter.Override(new Color(1f, 0.96f, 0.9f));

                // 4. Rozdzielenie tonalne: chłodne cienie vs ciepłe światła = głębia lochu.
                var smh = profile.Add<UnityEngine.Rendering.Universal.ShadowsMidtonesHighlights>(true);
                smh.shadows.Override(new Vector4(0.86f, 0.92f, 1.12f, 0f));   // cienie w błękit
                smh.midtones.Override(new Vector4(1f, 0.99f, 0.96f, 0f));
                smh.highlights.Override(new Vector4(1.1f, 1.02f, 0.88f, 0f)); // światła w bursztyn

                // 5. Balans bieli — lekko cieplejszy, kamień przestaje być siny.
                var wb = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
                wb.temperature.Override(8f);
                wb.tint.Override(-2f);

                // 6. Winieta — kieruje wzrok na środek kadru (standard prezentacji izometrycznej).
                var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                vig.intensity.Override(0.38f);
                vig.smoothness.Override(0.55f);
                vig.color.Override(new Color(0.02f, 0.02f, 0.04f));

                // 7. Delikatne ziarno — zbija „cyfrową czystość" renderu, dodaje faktury.
                var grain = profile.Add<UnityEngine.Rendering.Universal.FilmGrain>(true);
                grain.type.Override(UnityEngine.Rendering.Universal.FilmGrainLookup.Thin1);
                grain.intensity.Override(0.15f);
                grain.response.Override(0.8f);

                AssetDatabase.CreateAsset(profile, MatFolder + "/DungeonVolumeProfile.asset");
                vol.sharedProfile = profile;
            }
            catch (System.Exception e) { Debug.LogWarning("[Azonera] PostFX pominięty: " + e.Message); }
        }

        // ============================================================ ROOM
        private static void BuildRoom()
        {
            var env = new GameObject("Environment").transform;

            // Materiały PBR: prawdziwy kamień/cegła/drewno/metal zamiast jednolitych kolorów.
            // Tiling dobrany do realnej skali brył (1 unit = 1 metr) — kostka bruku ma mieć
            // rozmiar kostki bruku, a nie rozciągniętej plamy na całą ścianę.
            // Fotorealistyczne materiały PBR (Poly Haven, CC0). Tiling dobrany do skali brył
            // (1 unit = 1 metr): płyta posadzki ma mieć rozmiar płyty, cegła rozmiar cegły.
            var floor = Mat("FloorStone", new Color(0.15f, 0.15f, 0.17f), 0.12f,
                            texSlug: "slate_floor", tiling: 10f);
            var wall = Mat("WallStone", new Color(0.11f, 0.11f, 0.13f), 0.08f,
                           texSlug: "castle_brick_01", tiling: 6f);
            var brick = Mat("Brick", new Color(0.17f, 0.16f, 0.15f), 0.1f,
                            texSlug: "medieval_wall_01", tiling: 4f);
            var pillarMat = Mat("Pillar", new Color(0.19f, 0.18f, 0.17f), 0.12f,
                                texSlug: "medieval_blocks_05", tiling: 2.5f);
            // Dywan: neutralna tkanina przefarbowana na głęboką czerwień (detal splotu zostaje).
            var carpet = Mat("Carpet", new Color(0.42f, 0.07f, 0.08f), 0.06f,
                             texSlug: "dirty_carpet", tiling: 6f, tintTexture: true);
            var gold = Mat("Gold", new Color(0.85f, 0.62f, 0.18f), 0.75f, 1f, new Color(0.35f, 0.24f, 0.05f));
            var metal = Mat("BrazierMetal", new Color(0.35f, 0.32f, 0.3f), 0.45f, 1f,
                            texSlug: "rusty_metal_04", tiling: 1.5f);
            var wood = Mat("Wood", new Color(0.2f, 0.13f, 0.08f), 0.08f,
                           texSlug: "medieval_wood", tiling: 2f);

            const float W = 30f, D = 24f, H = 5.5f, T = 1f;

            // Podłoga (ciemny kamień)
            Prim(PrimitiveType.Plane, "Floor", env, Vector3.zero, new Vector3(W / 10f, 1f, D / 10f), Quaternion.identity, floor);
            // Ściany
            Prim(PrimitiveType.Cube, "Wall_N", env, new Vector3(0, H / 2, D / 2), new Vector3(W + T, H, T), Quaternion.identity, wall);
            Prim(PrimitiveType.Cube, "Wall_S", env, new Vector3(0, H / 2, -D / 2), new Vector3(W + T, H, T), Quaternion.identity, wall);
            Prim(PrimitiveType.Cube, "Wall_W", env, new Vector3(-W / 2, H / 2, 0), new Vector3(T, H, D), Quaternion.identity, wall);
            Prim(PrimitiveType.Cube, "Wall_E", env, new Vector3(W / 2, H / 2, 0), new Vector3(T, H, D), Quaternion.identity, wall);
            // Fryz/cokół
            Prim(PrimitiveType.Cube, "Trim_N", env, new Vector3(0, 0.25f, D / 2 - 0.6f), new Vector3(W, 0.5f, 0.4f), Quaternion.identity, brick);
            Prim(PrimitiveType.Cube, "Trim_S", env, new Vector3(0, 0.25f, -D / 2 + 0.6f), new Vector3(W, 0.5f, 0.4f), Quaternion.identity, brick);

            // Dywan (czerwony runner przez środek)
            Prim(PrimitiveType.Cube, "Carpet", env, new Vector3(0, 0.03f, 0), new Vector3(4f, 0.06f, D - 3f), Quaternion.identity, carpet);
            Prim(PrimitiveType.Cube, "Carpet_Cross", env, new Vector3(0, 0.03f, 0), new Vector3(W - 4f, 0.06f, 3f), Quaternion.identity, carpet);

            // Kolumny (2 rzędy)
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                BuildPillar(env, new Vector3(i * 5f, 0, 7f), pillarMat, H);
                BuildPillar(env, new Vector3(i * 5f, 0, -7f), pillarMat, H);
            }

            // Palniki (braziers) wzdłuż ścian — ciepłe światło i klimat
            Vector3[] brazierSpots =
            {
                new Vector3(-W/2+2, 0, 8), new Vector3(W/2-2, 0, 8),
                new Vector3(-W/2+2, 0, -8), new Vector3(W/2-2, 0, -8),
                new Vector3(-W/2+2, 0, 0), new Vector3(W/2-2, 0, 0),
                new Vector3(-6, 0, D/2-2), new Vector3(6, 0, D/2-2),
            };
            foreach (var s in brazierSpots) BuildBrazier(env, s, metal);

            // Ołtarz centralny z zielonym blaskiem (jak fontanna na referencji)
            BuildAltar(env, new Vector3(0, 0, 0), brick, metal);

            // Skarb w rogu
            BuildTreasure(env, new Vector3(W / 2 - 5, 0, -D / 2 + 5), gold, wood);

            // Niebieskie kryształy „spawn" (jak markery na referencji)
            var crystal = Mat("Crystal", new Color(0.15f, 0.3f, 0.7f), 0.3f, 0f, new Color(0.2f, 0.45f, 1f) * 2f);
            Prim(PrimitiveType.Cube, "Spawn_A", env, new Vector3(-4, 0.9f, D / 2 - 3), new Vector3(0.8f, 1.6f, 0.3f), Quaternion.Euler(0, 45, 0), crystal);
            Prim(PrimitiveType.Cube, "Spawn_B", env, new Vector3(4, 0.9f, D / 2 - 3), new Vector3(0.8f, 1.6f, 0.3f), Quaternion.Euler(0, 45, 0), crystal);

            // Prawdziwe rekwizyty zamiast prymitywów — beczki, skrzynie, meble, posągi.
            BuildProps(env, W, D);
        }

        /// <summary>
        /// Zagospodarowuje wnętrze prawdziwymi modelami (Poly Haven CC0). Rozmieszczenie jest
        /// PROJEKTOWANE, nie losowe: magazyn w jednym rogu, biblioteka przy ścianie, jadalnia
        /// przy drugiej, posągi flankują ołtarz, drobiazgi wypełniają puste miejsca.
        /// Referencja wizualna wprost tego wymaga — wnętrze ma „żyć", nie być pustą salą.
        /// </summary>
        private static void BuildProps(Transform env, float W, float D)
        {
            var props = new GameObject("Props").transform;
            props.SetParent(env, false);
            var rng = new System.Random(20260906);

            // --- Magazyn: beczki i skrzynie w zachodnim rogu ---
            var store = new GameObject("Zone_Storage").transform; store.SetParent(props, false);
            PropVaried("Barrel_01", store, new Vector3(-W / 2 + 1.9f, 0f, -7.0f), rng);
            PropVaried("Barrel_01", store, new Vector3(-W / 2 + 1.6f, 0f, -5.6f), rng);
            PropVaried("barrel_03", store, new Vector3(-W / 2 + 2.9f, 0f, -6.4f), rng);
            PropVaried("wine_barrel_01", store, new Vector3(-W / 2 + 1.8f, 0f, -4.0f), rng);
            PropVaried("wooden_crate_01", store, new Vector3(-W / 2 + 3.0f, 0f, -4.6f), rng);
            PropVaried("wooden_crate_02", store, new Vector3(-W / 2 + 2.1f, 0f, -2.6f), rng);
            // Skrzynia postawiona NA skrzyni — pionowa kompozycja, nie płaski dywan obiektów.
            Prop("wooden_crate_01", store, new Vector3(-W / 2 + 2.1f, 0.62f, -2.6f), 34f);
            PropVaried("wicker_basket_01", store, new Vector3(-W / 2 + 3.4f, 0f, -2.0f), rng);
            PropVaried("wooden_bucket_01", store, new Vector3(-W / 2 + 1.5f, 0f, -1.2f), rng);
            Prop("wooden_ladder", store, new Vector3(-W / 2 + 1.15f, 0f, 1.4f), 96f);

            // --- Biblioteka: regały i księgi przy ścianie północnej ---
            var lib = new GameObject("Zone_Library").transform; lib.SetParent(props, false);
            Prop("wooden_bookshelf_worn", lib, new Vector3(-9.5f, 0f, D / 2 - 1.4f), 180f);
            Prop("wooden_bookshelf_worn", lib, new Vector3(-6.6f, 0f, D / 2 - 1.4f), 180f);
            Prop("decorative_book_set_01", lib, new Vector3(-8.0f, 0f, D / 2 - 2.3f), 24f);
            Prop("WoodenTable_01", lib, new Vector3(-8.0f, 0f, D / 2 - 3.6f), 90f);
            Prop("wooden_candlestick", lib, new Vector3(-8.0f, 0.78f, D / 2 - 3.6f));
            Prop("decorative_book_set_01", lib, new Vector3(-8.5f, 0.76f, D / 2 - 3.3f), 200f);
            Prop("wooden_stool_01", lib, new Vector3(-7.0f, 0f, D / 2 - 4.4f), 40f);

            // --- Jadalnia / obozowisko: stół, ławy, naczynia, ognisko ---
            var mess = new GameObject("Zone_Mess").transform; mess.SetParent(props, false);
            Prop("WoodenTable_01", mess, new Vector3(9.0f, 0f, D / 2 - 4.0f), 12f);
            Prop("painted_wooden_bench", mess, new Vector3(9.0f, 0f, D / 2 - 5.4f), 12f);
            Prop("wooden_stool_01", mess, new Vector3(10.6f, 0f, D / 2 - 3.4f), 300f);
            Prop("wooden_bowl_01", mess, new Vector3(8.6f, 0.78f, D / 2 - 4.0f));
            Prop("brass_pot_01", mess, new Vector3(9.6f, 0.78f, D / 2 - 3.8f), 45f);
            Prop("ceramic_pot", mess, new Vector3(10.9f, 0f, D / 2 - 2.2f), 160f);
            Prop("brass_candleholders", mess, new Vector3(9.2f, 0.78f, D / 2 - 4.4f), 20f);
            Prop("stone_fire_pit", mess, new Vector3(11.0f, 0f, D / 2 - 6.6f), 15f);

            // --- Sanktuarium: posągi flankujące ołtarz + świeczniki ---
            var shrine = new GameObject("Zone_Shrine").transform; shrine.SetParent(props, false);
            Prop("gothic_statue", shrine, new Vector3(-3.2f, 0f, 3.6f), 150f, 1.15f);
            Prop("gothic_statue", shrine, new Vector3(3.2f, 0f, 3.6f), 210f, 1.15f);
            Prop("wooden_candlestick", shrine, new Vector3(-1.9f, 0f, 2.6f));
            Prop("wooden_candlestick", shrine, new Vector3(1.9f, 0f, 2.6f));
            Prop("Lantern_01", shrine, new Vector3(-2.6f, 0f, -2.4f), 30f);
            Prop("Lantern_01", shrine, new Vector3(2.6f, 0f, -2.4f), 330f);

            // --- Skarbiec: kufer + rozsypane kamienie ---
            var vault = new GameObject("Zone_Vault").transform; vault.SetParent(props, false);
            Prop("treasure_chest", vault, new Vector3(W / 2 - 5f, 0f, -D / 2 + 5f), 215f, 1.1f);
            Prop("wooden_crate_02", vault, new Vector3(W / 2 - 3.4f, 0f, -D / 2 + 4.0f), 70f);
            Prop("kite_shield", vault, new Vector3(W / 2 - 6.4f, 0f, -D / 2 + 4.2f), 25f);

            // --- Gruz i kamienie: rozsypane, by wnętrze nie było wysprzątane ---
            var rubble = new GameObject("Zone_Rubble").transform; rubble.SetParent(props, false);
            Vector3[] rubbleSpots =
            {
                new Vector3(-11.5f, 0f, 9.2f), new Vector3(12.2f, 0f, 9.6f),
                new Vector3(-12.6f, 0f, -9.4f), new Vector3(6.4f, 0f, -9.8f),
                new Vector3(-4.6f, 0f, -10.2f), new Vector3(13.1f, 0f, 1.6f),
                new Vector3(-13.2f, 0f, 4.4f),
            };
            foreach (var spot in rubbleSpots)
                PropVaried(rng.Next(2) == 0 ? "stone_01" : "namaqualand_stones_01", rubble, spot, rng, 0.9f);
        }

        private static void BuildPillar(Transform parent, Vector3 pos, Material mat, float h)
        {
            var p = new GameObject("Pillar").transform; p.SetParent(parent, false); p.position = pos;
            Prim(PrimitiveType.Cube, "Base", p, new Vector3(0, 0.3f, 0), new Vector3(1.6f, 0.6f, 1.6f), Quaternion.identity, mat);
            Prim(PrimitiveType.Cylinder, "Shaft", p, new Vector3(0, h / 2, 0), new Vector3(0.9f, h / 2, 0.9f), Quaternion.identity, mat);
            Prim(PrimitiveType.Cube, "Cap", p, new Vector3(0, h - 0.2f, 0), new Vector3(1.6f, 0.5f, 1.6f), Quaternion.identity, mat);
        }

        private static void BuildBrazier(Transform parent, Vector3 pos, Material metal)
        {
            var b = new GameObject("Brazier").transform; b.SetParent(parent, false); b.position = pos;
            Prim(PrimitiveType.Cylinder, "Stand", b, new Vector3(0, 0.7f, 0), new Vector3(0.25f, 0.7f, 0.25f), Quaternion.identity, metal);
            Prim(PrimitiveType.Cylinder, "Bowl", b, new Vector3(0, 1.5f, 0), new Vector3(0.9f, 0.25f, 0.9f), Quaternion.identity, metal);

            var fireMat = Mat("Fire", new Color(1f, 0.55f, 0.15f), 0f, 0f, new Color(1f, 0.5f, 0.12f) * 4f);
            var flame = Prim(PrimitiveType.Sphere, "Flame", b, new Vector3(0, 1.75f, 0), new Vector3(0.7f, 0.9f, 0.7f), Quaternion.identity, fireMat);
            Object.DestroyImmediate(flame.GetComponent<Collider>());

            var lightGo = new GameObject("FireLight"); lightGo.transform.SetParent(b, false);
            lightGo.transform.localPosition = new Vector3(0, 1.9f, 0);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.62f, 0.3f); l.intensity = 6.5f; l.range = 13f;
            // Miękkie cienie od pochodni — to one budują głębię wnętrza (kolumny rzucają cień
            // na podłogę i ściany). Atlas 2048 dzielony na 8 palników daje 512 px na źródło.
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.75f;
            l.shadowBias = 0.08f;
            l.shadowNormalBias = 0.5f;
            lightGo.AddComponent<TorchFlicker>().Configure(l, 6.5f);
        }

        private static void BuildAltar(Transform parent, Vector3 pos, Material stone, Material metal)
        {
            var a = new GameObject("Altar").transform; a.SetParent(parent, false); a.position = pos;
            Prim(PrimitiveType.Cylinder, "Base", a, new Vector3(0, 0.25f, 0), new Vector3(3f, 0.25f, 3f), Quaternion.identity, stone);
            Prim(PrimitiveType.Cylinder, "Rim", a, new Vector3(0, 0.5f, 0), new Vector3(2.4f, 0.2f, 2.4f), Quaternion.identity, metal);
            var water = Mat("AltarWater", new Color(0.1f, 0.5f, 0.3f), 0.9f, 0f, new Color(0.1f, 0.9f, 0.4f) * 1.4f);
            var w = Prim(PrimitiveType.Cylinder, "Water", a, new Vector3(0, 0.55f, 0), new Vector3(2f, 0.06f, 2f), Quaternion.identity, water);
            Object.DestroyImmediate(w.GetComponent<Collider>());
            var glow = new GameObject("Glow"); glow.transform.SetParent(a, false); glow.transform.localPosition = new Vector3(0, 1f, 0);
            var gl = glow.AddComponent<Light>(); gl.type = LightType.Point; gl.color = new Color(0.3f, 1f, 0.5f); gl.intensity = 2.5f; gl.range = 7f; gl.shadows = LightShadows.None;
        }

        /// <summary>
        /// Rozsypane złoto wokół skrzyni. Sam kufer to już prawdziwy model (BuildProps →
        /// Zone_Vault), tutaj zostają tylko monety: spłaszczone dyski, nie kule — z góry
        /// czytają się jak leżące monety, a nie jak kulki.
        /// </summary>
        private static void BuildTreasure(Transform parent, Vector3 pos, Material gold, Material wood)
        {
            var t = new GameObject("Treasure").transform; t.SetParent(parent, false); t.position = pos;
            var rng = new System.Random(99);
            for (int i = 0; i < 46; i++)
            {
                float ang = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                float rad = 0.4f + (float)rng.NextDouble() * 1.5f;
                float x = Mathf.Cos(ang) * rad;
                float z = Mathf.Sin(ang) * rad;
                float y = 0.02f + (float)rng.NextDouble() * 0.06f;
                var rot = Quaternion.Euler((float)rng.NextDouble() * 14f - 7f,
                                           (float)rng.NextDouble() * 360f,
                                           (float)rng.NextDouble() * 14f - 7f);
                var c = Prim(PrimitiveType.Cylinder, "Coin", t, new Vector3(x, y, z),
                             new Vector3(0.11f, 0.012f, 0.11f), rot, gold);
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
        }

        // ============================================================ PLAYER
        private static GameObject BuildPlayer()
        {
            var knight = AssetDatabase.LoadAssetAtPath<ClassData>("Assets/Azonera/ScriptableObjects/Classes/Class_Knight.asset");
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0, 1f, -5f);

            var col = player.AddComponent<CapsuleCollider>(); col.height = 2f; col.radius = 0.5f;
            var rb = player.AddComponent<Rigidbody>();
            rb.mass = 60f; rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var stats = player.AddComponent<CharacterStats>();
            SetRef(stats, "_classData", knight);
            SetInt(stats, "_level", 1);

            player.AddComponent<Azonera.Inventory.Inventory>();
            player.AddComponent<EquipmentController>();
            player.AddComponent<MeleeAttacker>();
            player.AddComponent<PlayerController>();
            player.AddComponent<Azonera.Player.PlayerActions>();
            player.AddComponent<PlayerSkills>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual_DEBUG"; body.transform.SetParent(player.transform, false);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = Mat("PlayerBody_DEBUG", new Color(0.55f, 0.5f, 0.45f), 0.2f, 0.3f);
            var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
            front.name = "Facing"; front.transform.SetParent(player.transform, false);
            front.transform.localPosition = new Vector3(0, 0.2f, 0.55f); front.transform.localScale = new Vector3(0.25f, 0.25f, 0.4f);
            Object.DestroyImmediate(front.GetComponent<Collider>());
            front.GetComponent<Renderer>().sharedMaterial = Mat("PlayerTrim_DEBUG", new Color(0.85f, 0.75f, 0.4f), 0.3f, 0.6f);
            return player;
        }

        private static UnityEngine.Camera BuildCamera(Transform target)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.farClipPlane = 600f; cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            camGo.AddComponent<AudioListener>();
            var iso = camGo.AddComponent<IsometricCameraController>();
            SetRef(iso, "_target", target);
            SetFloat(iso, "_pitch", 55f);
            SetFloat(iso, "_distance", 16f);

            // Roof-hiding: ściany/kolumny między kamerą a graczem znikają (zostaje ich cień).
            var hider = camGo.AddComponent<CameraOcclusionHider>();
            hider.Target = target;
            return cam;
        }

        private static void BuildManagers()
        {
            var go = new GameObject("GameSystems");
            go.AddComponent<GameManager>();
            go.AddComponent<DebugPanel>();
        }

        // ============================================================ NPC
        private static void BuildNPC()
        {
            var npc = new GameObject("NPC_GuideAlwin") { };
            npc.transform.position = new Vector3(-4f, 1f, -3f);
            var col = npc.AddComponent<CapsuleCollider>(); col.height = 2f; col.radius = 0.5f;
            npc.AddComponent<NPCInteractable>();
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual_DEBUG"; vis.transform.SetParent(npc.transform, false);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = Mat("NPCBody_DEBUG", new Color(0.3f, 0.42f, 0.55f), 0.2f);
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "Marker"; mark.transform.SetParent(npc.transform, false);
            mark.transform.localPosition = new Vector3(0, 1.8f, 0); mark.transform.localScale = Vector3.one * 0.25f;
            mark.transform.localRotation = Quaternion.Euler(45, 45, 0);
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.GetComponent<Renderer>().sharedMaterial = Mat("Quest_DEBUG", new Color(1f, 0.85f, 0.2f), 0f, 0f, new Color(1f, 0.8f, 0.15f) * 2f);
        }

        // ============================================================ SPAWNY POTWORÓW
        /// <summary>
        /// Zamiast statycznych potworów stawiamy SPAWNERY — łowisko odnawia się po wyczyszczeniu,
        /// tak jak w prawdziwym MMORPG. Encje składa MonsterFactory w runtime.
        /// </summary>
        private static void BuildTestMonsters()
        {
            var root = new GameObject("Spawns").transform;
            AddSpawner(root, "Monster_MarshSnake", new Vector3(-8, 0, 6), count: 2, radius: 2.5f, respawn: 18f);
            AddSpawner(root, "Monster_Goblin", new Vector3(7, 0, 8), count: 2, radius: 3f, respawn: 26f);
            AddSpawner(root, "Monster_Wolf", new Vector3(0, 0, 8.5f), count: 1, radius: 2f, respawn: 32f);
            AddSpawner(root, "Monster_Skeleton", new Vector3(0, 0, -9f), count: 1, radius: 2f, respawn: 45f);
        }

        private static void AddSpawner(Transform parent, string assetName, Vector3 pos,
                                       int count, float radius, float respawn)
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterData>(
                $"Assets/Azonera/ScriptableObjects/Monsters/{assetName}.asset");
            if (data == null) { Debug.LogWarning("[Azonera] Brak MonsterData: " + assetName); return; }

            var go = new GameObject("Spawn_" + data.DisplayName);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<MonsterSpawner>().Configure(data, count, radius, respawn);
        }

        // ============================================================ CLASSIC HUD
        private static void BuildClassicHUD()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var canvasGo = new GameObject("Classic HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<ClassicHUDController>();
            canvasGo.AddComponent<BattleListController>();
            var root = canvasGo.transform;

            BuildTopLeftVitals(root);
            BuildRightSidebar(root);
            BuildChat(root);
            BuildHotbars(root);
        }

        // -- portret + HP/MP + level (lewy górny) --
        private static void BuildTopLeftVitals(Transform root)
        {
            var panel = Panel(root, "Vitals", new Vector2(0, 1), new Vector2(16, -16), new Vector2(300, 84), new Color(0.05f, 0.05f, 0.06f, 0.8f));

            // portret
            var port = Panel(panel, "Portrait", new Vector2(0, 1), new Vector2(8, -8), new Vector2(68, 68), new Color(0.12f, 0.11f, 0.1f, 1f));
            Outline(port.gameObject, new Color(0.5f, 0.42f, 0.2f, 1f));
            Label(port, "PortraitIcon", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), "🛡", 34, new Color(0.8f, 0.7f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Normal);
            var badge = Panel(port, "LevelBadge", new Vector2(0, 0), new Vector2(-4, -4), new Vector2(26, 20), new Color(0.1f, 0.09f, 0.08f, 1f));
            Label(badge, "Val_LevelBadge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 20), "1", 13, new Color(0.9f, 0.8f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);

            // HP bar
            var hpBg = Bar(panel, "HP_BG", new Vector2(84, -10), new Vector2(206, 24), new Color(0.15f, 0.04f, 0.04f, 1f));
            BarFill(hpBg, "Fill_HP", new Color(0.72f, 0.15f, 0.15f));
            Label(hpBg, "Val_HPBar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 24), "185 / 185", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            // MP bar
            var mpBg = Bar(panel, "MP_BG", new Vector2(84, -40), new Vector2(206, 24), new Color(0.04f, 0.06f, 0.16f, 1f));
            BarFill(mpBg, "Fill_MP", new Color(0.24f, 0.42f, 0.85f));
            Label(mpBg, "Val_MPBar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 24), "40 / 40", 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            // FPS/Ping (jak na referencji)
            var perf = Panel(root, "Perf", new Vector2(0, 1), new Vector2(16, -108), new Vector2(150, 40), new Color(0, 0, 0, 0.35f));
            Label(perf, "FPS", new Vector2(0, 1), new Vector2(6, -4), new Vector2(140, 18), "FPS: 60", 13, new Color(0.5f, 0.85f, 0.5f), TextAnchor.UpperLeft, FontStyle.Normal);
            Label(perf, "Ping", new Vector2(0, 1), new Vector2(6, -22), new Vector2(140, 18), "Ping: — (SP)", 13, new Color(0.6f, 0.6f, 0.65f), TextAnchor.UpperLeft, FontStyle.Normal);
        }

        // -- prawy pasek: minimapa, Skills, Inventory doll, Backpack, Battle List --
        private static void BuildRightSidebar(Transform root)
        {
            float w = 320f;
            var col = Panel(root, "Sidebar", new Vector2(1, 1), new Vector2(-8, -8), new Vector2(w, 1060), new Color(0.06f, 0.06f, 0.07f, 0.9f));
            col.pivot = new Vector2(1, 1);

            float y = -6f;
            // Minimapa
            var mini = SubPanel(col, "Minimap", ref y, w - 12, 150);
            var map = Panel(mini, "Map", new Vector2(0, 1), new Vector2(6, -26), new Vector2(w - 24, 116), new Color(0.09f, 0.08f, 0.07f, 1f));
            Outline(map.gameObject, new Color(0.3f, 0.25f, 0.15f, 1f));
            Label(map, "Centre", new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(120, 16), "Centre", 12, new Color(0.7f, 0.65f, 0.5f), TextAnchor.UpperCenter, FontStyle.Normal);

            // Skills
            var skills = SubPanel(col, "Skills", ref y, w - 12, 372);
            float ry = -26f;
            SkillRow(skills, "Experience", "Val_Exp", "0", ref ry, gold: true);
            SkillRow(skills, "Level", "Val_Level", "1", ref ry, gold: true);
            SkillRow(skills, "Hit Points", "Val_HP", "185", ref ry);
            SkillRow(skills, "Mana", "Val_Mana", "40", ref ry);
            SkillRow(skills, "Soul Points", "Val_Soul", "100", ref ry);
            SkillRow(skills, "Capacity", "Val_Cap", "470", ref ry);
            SkillRow(skills, "Speed", "Val_Speed", "220", ref ry);
            SkillRow(skills, "Food", "Val_Food", "11:00", ref ry);
            SkillRow(skills, "Stamina", "Val_Stamina", "42:00", ref ry);
            Separator(skills, ref ry);
            SkillRow(skills, "Magic Level", "Val_MagicLevel", "0", ref ry);
            SkillRow(skills, "Fist Fighting", "Val_Fist", "10", ref ry);
            SkillRow(skills, "Club Fighting", "Val_Club", "10", ref ry);
            SkillRow(skills, "Sword Fighting", "Val_Sword", "12", ref ry);
            SkillRow(skills, "Axe Fighting", "Val_Axe", "10", ref ry);
            SkillRow(skills, "Distance", "Val_Distance", "10", ref ry);
            SkillRow(skills, "Shielding", "Val_Shielding", "11", ref ry);
            SkillRow(skills, "Fishing", "Val_Fishing", "10", ref ry);

            // Inventory paper-doll
            var inv = SubPanel(col, "Inventory", ref y, w - 12, 180);
            BuildDoll(inv, w - 24);

            // Backpack grid
            var bp = SubPanel(col, "Backpack", ref y, w - 12, 120);
            BuildGrid(bp, 6, 2, 42, 6);

            // Battle List — wiersze wypełnia BattleListController w runtime (wrogowie w pobliżu).
            var bl = SubPanel(col, "Battle List", ref y, w - 12, 212);
            Label(bl, "BLEmpty", new Vector2(0, 1), new Vector2(8, -30), new Vector2(w - 40, 20), "— brak celów —", 12, new Color(0.5f, 0.5f, 0.55f), TextAnchor.UpperLeft, FontStyle.Italic);
        }

        private static void BuildDoll(RectTransform panel, float w)
        {
            // klasyczny układ 3x4 (hełm/amulet/plecak, zbroja+ręce, nogi, buty+pierścień/amunicja)
            string[,] slots =
            {
                { "Amulet", "Helmet", "Backpack" },
                { "R.Hand", "Armor", "L.Hand" },
                { "Ring", "Legs", "Ammo" },
                { "", "Boots", "" },
            };
            float slot = 46f, gap = 8f, startX = (w - (3 * slot + 2 * gap)) / 2f + 6f, startY = -30f;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++)
                {
                    if (string.IsNullOrEmpty(slots[r, c])) continue;
                    var s = Slot(panel, "Slot_" + slots[r, c], new Vector2(startX + c * (slot + gap), startY - r * (slot + gap)), slot);
                    Label(s, "L", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slot, slot), slots[r, c], 9, new Color(0.4f, 0.4f, 0.42f), TextAnchor.MiddleCenter, FontStyle.Normal);
                }
        }

        private static void BuildGrid(RectTransform panel, int cols, int rows, float slot, float gap)
        {
            float startX = 8f, startY = -30f;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    Slot(panel, $"BP_{r}_{c}", new Vector2(startX + c * (slot + gap), startY - r * (slot + gap)), slot);
        }

        // -- chat (lewy dolny) --
        private static void BuildChat(Transform root)
        {
            var chat = Panel(root, "Chat", new Vector2(0, 0), new Vector2(16, 92), new Vector2(560, 150), new Color(0.04f, 0.04f, 0.05f, 0.85f));
            chat.pivot = new Vector2(0, 0);
            string[] tabs = { "Local Chat", "Server Log", "World Chat", "Advertising", "Help" };
            float tx = 6f;
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = Panel(chat, "Tab_" + i, new Vector2(0, 1), new Vector2(tx, -4), new Vector2(96, 22), new Color(0.1f, 0.09f, 0.08f, i == 0 ? 1f : 0.6f));
                Label(tab, "T", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 22), tabs[i], 11, i == 0 ? new Color(0.9f, 0.5f, 0.3f) : new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter, FontStyle.Normal);
                tx += 100f;
            }
            Label(chat, "ChatText", new Vector2(0, 0), new Vector2(8, 6), new Vector2(544, 108), "Witaj w Azonerze.\nUżyj WSAD by się poruszać, LPM by atakować.\n[System] Loch: Świątynia Prób.", 13, new Color(0.75f, 0.75f, 0.78f), TextAnchor.LowerLeft, FontStyle.Normal);
        }

        // -- hotbary (dolny środek) --
        private static void BuildHotbars(Transform root)
        {
            for (int row = 0; row < 2; row++)
            {
                var bar = Panel(root, "Hotbar" + row, new Vector2(0.5f, 0), new Vector2(0, 44 - row * 44), new Vector2(760, 42), new Color(0.05f, 0.05f, 0.06f, 0.85f));
                bar.pivot = new Vector2(0.5f, 0);
                float x = 8f;
                for (int i = 0; i < 16; i++)
                {
                    var s = Slot(bar, $"HK_{row}_{i}", new Vector2(x, -4), 34f);
                    Label(s, "Key", new Vector2(0, 1), new Vector2(2, -1), new Vector2(16, 12), (i + 1 <= 9 ? (i + 1).ToString() : ""), 9, new Color(0.6f, 0.6f, 0.6f), TextAnchor.UpperLeft, FontStyle.Normal);
                    x += 46f;
                }
            }
        }

        // ============================================================ UI HELPERS
        private static RectTransform SubPanel(RectTransform parent, string title, ref float y, float w, float h)
        {
            var p = Panel(parent, title, new Vector2(0, 1), new Vector2(6, y), new Vector2(w, h), new Color(0.08f, 0.08f, 0.09f, 0.95f));
            var bar = Panel(p, "Title", new Vector2(0, 1), new Vector2(0, 0), new Vector2(w, 22), new Color(0.14f, 0.12f, 0.1f, 1f));
            Label(bar, "T", new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(w - 16, 22), title, 13, new Color(0.85f, 0.72f, 0.45f), TextAnchor.MiddleLeft, FontStyle.Bold);
            y -= h + 6f;
            return p;
        }

        private static void SkillRow(RectTransform panel, string label, string valueKey, string val, ref float y, bool gold = false)
        {
            Label(panel, "lbl", new Vector2(0, 1), new Vector2(10, y), new Vector2(180, 18), label, 13, new Color(0.72f, 0.72f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal);
            Label(panel, valueKey, new Vector2(1, 1), new Vector2(-10, y), new Vector2(120, 18), val, 13, gold ? new Color(0.9f, 0.78f, 0.45f) : Color.white, TextAnchor.UpperRight, FontStyle.Bold);
            y -= 19f;
        }

        private static void Separator(RectTransform panel, ref float y)
        {
            Panel(panel, "sep", new Vector2(0, 1), new Vector2(10, y - 4), new Vector2(280, 1), new Color(1, 1, 1, 0.08f));
            y -= 10f;
        }

        private static RectTransform Slot(RectTransform parent, string name, Vector2 anchoredPos, float size)
        {
            var s = Panel(parent, name, new Vector2(0, 1), anchoredPos, new Vector2(size, size), new Color(0.13f, 0.12f, 0.11f, 1f));
            Outline(s.gameObject, new Color(0.28f, 0.24f, 0.18f, 1f));
            return s;
        }

        private static void Outline(GameObject go, Color c)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = c; o.effectDistance = new Vector2(1.4f, -1.4f);
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = color; img.sprite = Sprite(); img.type = Image.Type.Sliced;
            return rt;
        }

        private static RectTransform Bar(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color bg)
        {
            var rt = Panel(parent, name, new Vector2(0, 1), anchoredPos, size, bg);
            return rt;
        }

        private static Image BarFill(RectTransform bg, string name, Color fill)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(bg, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
            var img = go.GetComponent<Image>(); img.color = fill; img.sprite = Sprite();
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left; img.fillAmount = 1f;
            return img;
        }

        private static Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, string text, int fontSize, Color color, TextAnchor align, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = align; t.fontStyle = style;
            t.font = FontRef(); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Sprite Sprite()
        {
            if (_sprite == null) _sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return _sprite;
        }

        private static Font FontRef()
        {
            if (_font == null) { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            return _font;
        }

        // ============================================================ WORLD HELPERS

        /// <summary>Zestawy tekstur PBR używane przez ten loch (katalogi w Art/Textures).</summary>
        private static readonly string[] TextureSets =
        {
            "slate_floor", "castle_brick_01", "medieval_wall_01", "medieval_blocks_05",
            "dirty_carpet", "rusty_metal_04", "medieval_wood",
            // zestawy z wcześniejszej tury — nadal używane przez inne materiały/sceny
            "cobblestone_floor_06", "dark_brick_wall", "dark_wood", "box_profile_metal_sheet"
        };

        /// <summary>
        /// Materiał lochu. Deleguje do współdzielonej <see cref="AzoneraMaterialLibrary"/>,
        /// żeby loch i wioska korzystały z jednego standardu PBR.
        /// Gdy zestaw tekstur nie jest jeszcze w projekcie, materiał degraduje się
        /// do wersji jednolitej zamiast wyświetlać biały, „wyprany" placeholder.
        /// </summary>
        private static Material Mat(string name, Color color, float smoothness, float metallic = 0f,
            Color emission = default, string texSlug = null, float tiling = 1f, bool tintTexture = false)
        {
            if (_mats.TryGetValue(name, out var cached) && cached != null) return cached;

            string slug = AzoneraMaterialLibrary.HasTextureSet(texSlug) ? texSlug : null;
            var mat = AzoneraMaterialLibrary.Get(MatFolder, name, color, smoothness, metallic,
                                                 emission, slug, tiling, tintTexture);
            _mats[name] = mat;
            return mat;
        }

        // ============================================================ REKWIZYTY (CC0)
        private const string PropPrefabRoot = "Assets/Azonera/Prefabs/Props";

        /// <summary>
        /// Stawia rekwizyt z biblioteki prefabów (Poly Haven CC0, przygotowane przez
        /// <see cref="AzoneraPropImporter"/>). Gdy prefab jeszcze nie istnieje, zwraca null —
        /// generator działa dalej, tylko bez tej dekoracji.
        /// </summary>
        private static GameObject Prop(string slug, Transform parent, Vector3 pos,
                                       float yaw = 0f, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropPrefabRoot}/P_{slug}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[Azonera] Brak prefabu rekwizytu: {slug} (uruchom Azonera → 2. Zaimportuj rekwizyty)");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>Rekwizyt z losowym obrotem i lekką wariacją skali — nic nie wygląda na klonowane.</summary>
        private static GameObject PropVaried(string slug, Transform parent, Vector3 pos,
                                             System.Random rng, float baseScale = 1f)
        {
            float yaw = (float)rng.NextDouble() * 360f;
            float scale = baseScale * (0.92f + (float)rng.NextDouble() * 0.16f);
            return Prop(slug, parent, pos, yaw, scale);
        }

        /// <summary>Reimport zestawów tekstur, by zadziałał AzoneraTextureImporter (normalki/mapy liniowe).</summary>
        private static void EnsureTexturesImported() => AzoneraMaterialLibrary.EnsureImported(TextureSets);

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localRotation = rot; go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>(); if (rend != null && mat != null) rend.sharedMaterial = mat;
            return go;
        }

        private static void SetRef(Object comp, string field, Object value)
        {
            var so = new SerializedObject(comp); var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        private static void SetInt(Object comp, string field, int value)
        {
            var so = new SerializedObject(comp); var p = so.FindProperty(field);
            if (p != null) { p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        private static void SetFloat(Object comp, string field, float value)
        {
            var so = new SerializedObject(comp); var p = so.FindProperty(field);
            if (p != null) { p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
