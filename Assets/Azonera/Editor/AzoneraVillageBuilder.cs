#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Azonera.Monsters;
using Azonera.NPC;
using Azonera.Player;
using Azonera.World;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Buduje vertical slice Azonery na assetach KayKit Medieval (CC0): osadę, drogę, rzekę,
    /// gęsty las i pierścień gór — z zachowaniem WSZYSTKICH istniejących systemów gry
    /// (gracz, kamera izometryczna, Classic HUD, GameManager, NPC, spawnery potworów).
    ///
    /// Świadome decyzje projektowe:
    /// • Kafle heksagonalne służą WYŁĄCZNIE jako podłoże — gameplay pozostaje swobodny (WSAD),
    ///   nie robimy z Azonery strategii heksowej. Hex to siatka scenografii, nie mechanika.
    /// • Geometria siatki jest WYLICZANA z realnych wymiarów modelu kafla, nie zaszyta w kodzie —
    ///   pakiet może dostać kafle innej skali i generator nadal się zepnie bez szczelin.
    /// • Wszystko poza graczem/potworami jest oznaczone jako statyczne: static batching
    ///   plus jeden wspólny materiał całego pakietu to warunek sensownego FPS przy setkach obiektów.
    /// • Rozmieszczenie jest PROJEKTOWANE (strefy, osie widokowe, zagęszczenia), nie losowe.
    ///   Losowość dotyczy wyłącznie wariacji obrotu i skali, żeby nic nie wyglądało na klonowane.
    /// </summary>
    public static class AzoneraVillageBuilder
    {
        private const string PrefabRoot = "Assets/Azonera/Prefabs/KayKit";
        private const string SceneFolder = "Assets/Azonera/Scenes";
        private const string ScenePath = SceneFolder + "/Azonera_KayKit_VerticalSlice.unity";

        /// <summary>Promień pola heksów (w kaflach) — cały grywalny fragment.</summary>
        private const int MapRadius = 15;

        /// <summary>
        /// Korekta obrotu kafli drogi/rzeki. Modele mają wewnętrzny kierunek pasa, którego nie da
        /// się odczytać z geometrii — jeśli droga wyjdzie ustawiona w poprzek, zmień o 30 lub 60.
        /// </summary>
        private const float RibbonYawOffset = 0f;

        // Wymiary kafla wyliczone przy starcie budowy.
        private static float _hexW, _hexD;
        private static bool _flatTop;

        private static readonly Dictionary<string, GameObject> _prefabCache =
            new Dictionary<string, GameObject>();

        [MenuItem("Azonera/★ Zbuduj Vertical Slice KayKit (osada + las)", priority = 2)]
        public static void BuildVerticalSlice()
        {
            if (!AzoneraEditorGuards.EnsureNotPlaying("Zbuduj Vertical Slice KayKit")) return;

            _prefabCache.Clear();
            if (!MeasureHex()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var world = new GameObject("World").transform;

            var occupied = new HashSet<Vector2Int>();
            BuildGround(world, occupied);
            BuildRiver(world, occupied);
            BuildRoad(world, occupied);
            BuildVillage(world);
            BuildForest(world, occupied);
            BuildBorder(world);

            // --- istniejące systemy gry, bez zmian w ich logice ---
            var player = AzoneraDungeonBuilder.BuildPlayer();
            player.transform.position = PlayerSpawn();
            var cam = AzoneraDungeonBuilder.BuildCamera(player.transform);
            AzoneraDungeonBuilder.BuildManagers();
            AzoneraDungeonBuilder.BuildClassicHUD();
            BuildNpcs();
            BuildSpawners();

            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.SetCamera(cam.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Azonera] Vertical slice KayKit zbudowany: {ScenePath}");
            EditorUtility.DisplayDialog("Azonera",
                "Vertical Slice KayKit gotowy!\n\nScena: Azonera_KayKit_VerticalSlice\n" +
                "WSAD – ruch, kółko – zoom, Q/E – obrót kamery,\nLPM – atak/rozmowa, Tab – cel, F1 – panel dev.",
                "OK");
        }

        // ============================================================ SIATKA HEKSÓW

        /// <summary>
        /// Odczytuje realne wymiary kafla i wykrywa jego orientację. Bez tego siatka
        /// miałaby szczeliny albo nachodziła na siebie przy każdej zmianie skali pakietu.
        /// </summary>
        private static bool MeasureHex()
        {
            var grass = LoadPrefab("tiles", "hex_grass");
            if (grass == null)
            {
                Debug.LogError("[Azonera] Brak prefabu P_hex_grass — uruchom najpierw " +
                               "Azonera → 3. Zaimportuj KayKit.");
                return false;
            }

            var rends = grass.GetComponentsInChildren<MeshRenderer>();
            if (rends.Length == 0) { Debug.LogError("[Azonera] Kafel bez renderera."); return false; }

            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            _hexW = b.size.x;
            _hexD = b.size.z;
            _flatTop = _hexW >= _hexD;
            Debug.Log($"[Azonera] Kafel: {_hexW:F3} x {_hexD:F3}, {(_flatTop ? "flat-top" : "pointy-top")}");
            return _hexW > 0.01f && _hexD > 0.01f;
        }

        /// <summary>Współrzędne osiowe heksa → pozycja w świecie.</summary>
        private static Vector3 HexToWorld(int q, int r)
        {
            if (_flatTop)
                return new Vector3(0.75f * _hexW * q, 0f, _hexD * (r + q * 0.5f));
            return new Vector3(_hexW * (q + r * 0.5f), 0f, 0.75f * _hexD * r);
        }

        private static IEnumerable<Vector2Int> HexDisc(int radius)
        {
            for (int q = -radius; q <= radius; q++)
            {
                int lo = Mathf.Max(-radius, -q - radius);
                int hi = Mathf.Min(radius, -q + radius);
                for (int r = lo; r <= hi; r++) yield return new Vector2Int(q, r);
            }
        }

        // ============================================================ PODŁOŻE

        private static void BuildGround(Transform world, HashSet<Vector2Int> occupied)
        {
            var root = new GameObject("Ground").transform;
            root.SetParent(world, false);

            foreach (var h in HexDisc(MapRadius))
            {
                Place("tiles", "hex_grass", root, HexToWorld(h.x, h.y), YawStep(h));
                occupied.Add(h);
            }
        }

        /// <summary>Obrót kafla o wielokrotność 60° — łamie widoczną powtarzalność siatki.</summary>
        private static float YawStep(Vector2Int h)
        {
            int seed = (h.x * 73856093) ^ (h.y * 19349663);
            return 60f * (Mathf.Abs(seed) % 6);
        }

        // ============================================================ RZEKA

        /// <summary>
        /// Rzeka biegnie prostą kolumną heksów przez zachodnią część mapy. Prosty przebieg
        /// jest celowy: kafle zakrętów mają zaszyte kierunki połączeń, których nie da się
        /// odczytać programowo — prosta linia zawsze łączy się poprawnie.
        /// </summary>
        private static void BuildRiver(Transform world, HashSet<Vector2Int> occupied)
        {
            var root = new GameObject("River").transform;
            root.SetParent(world, false);

            const int riverQ = -7;
            for (int r = -MapRadius; r <= MapRadius; r++)
            {
                var h = new Vector2Int(riverQ, r);
                if (!occupied.Contains(h)) continue;
                Place("tiles", "hex_water", root, HexToWorld(h.x, h.y), 0f);
                occupied.Remove(h);   // teren zajęty — las tu nie wyrośnie
            }

            // Most na przecięciu z drogą — jedyne przejście na drugi brzeg.
            var crossing = HexToWorld(riverQ, 0);
            Place("buildings", "building_bridge_A", root, crossing, RibbonYawOffset);
        }

        // ============================================================ DROGA

        private static void BuildRoad(Transform world, HashSet<Vector2Int> occupied)
        {
            var root = new GameObject("Road").transform;
            root.SetParent(world, false);

            // Droga wschód-zachód przez środek osady, omijając kafel mostu.
            for (int q = -MapRadius; q <= MapRadius; q++)
            {
                var h = new Vector2Int(q, 0);
                if (!occupied.Contains(h)) continue;
                Place("tiles", "hex_road_A", root, HexToWorld(h.x, h.y), RibbonYawOffset);
                occupied.Remove(h);
            }
        }

        // ============================================================ OSADA

        /// <summary>
        /// Osada „Brzask" — zaprojektowany układ, nie rozrzut. Budynki stoją przy drodze,
        /// tyłem do lasu, z placem targowym w centrum. Każdy dostaje towarzyszące rekwizyty,
        /// bo pusty budynek czyta się jak makieta.
        /// </summary>
        private static void BuildVillage(Transform world)
        {
            var root = new GameObject("Village_Brzask").transform;
            root.SetParent(world, false);
            var rng = new System.Random(9061);

            // Kolor stronnictwa Azonery: niebieski (spójny dla całej osady).
            const string F = "blue";

            // --- plac centralny ---
            PlaceAt("buildings", $"building_market_{F}", root, 0f, 2.4f, 20f);
            PlaceAt("buildings", $"building_well_{F}", root, 2.6f, 0.4f, 145f);
            PlaceAt("buildings", "building_stage_A", root, -2.8f, 2.0f, 0f);

            // --- pierzeja północna (tyłem do lasu) ---
            PlaceAt("buildings", $"building_tavern_{F}", root, -6.5f, 5.6f, 165f);
            PlaceAt("buildings", $"building_home_A_{F}", root, -1.6f, 6.2f, 172f);
            PlaceAt("buildings", $"building_home_B_{F}", root, 3.2f, 6.0f, 188f);
            PlaceAt("buildings", $"building_church_{F}", root, 8.4f, 6.4f, 195f);

            // --- pierzeja południowa (rzemiosło, bliżej drogi) ---
            PlaceAt("buildings", $"building_blacksmith_{F}", root, -5.4f, -5.2f, 12f);
            PlaceAt("buildings", $"building_lumbermill_{F}", root, 1.8f, -5.8f, -8f);
            PlaceAt("buildings", $"building_barracks_{F}", root, 7.6f, -5.4f, 5f);

            // --- młyny: wodny nad rzeką, wietrzny na wzniesieniu ---
            PlaceAt("buildings", $"building_watermill_{F}", root, -10.6f, 3.2f, 95f);
            PlaceAt("buildings", $"building_windmill_{F}", root, 12.8f, 2.2f, 210f);

            // --- strażnica przy wjeździe ---
            PlaceAt("buildings", $"building_tower_A_{F}", root, 13.4f, -3.0f, 0f);

            // --- ogrodzenia wyznaczające granicę osady ---
            var fences = new GameObject("Fences").transform; fences.SetParent(root, false);
            for (int i = 0; i < 7; i++)
                PlaceAt("buildings", "fence_wood_straight", fences, -9.0f + i * 1.6f, 8.6f, 0f);
            for (int i = 0; i < 6; i++)
                PlaceAt("buildings", "fence_wood_straight", fences, 1.5f + i * 1.6f, -8.4f, 0f);

            // --- rekwizyty: warsztat kowala ---
            var props = new GameObject("Props").transform; props.SetParent(root, false);
            PlaceAt("props", "weaponrack", props, -6.6f, -3.8f, 24f);
            PlaceAt("props", "target", props, -8.4f, -3.2f, 40f);
            PlaceAt("props", "resource_stone", props, -4.0f, -3.6f, 0f);
            PlaceAt("props", "crate_A_big", props, -3.2f, -4.4f, 18f);

            // --- rekwizyty: tartak ---
            PlaceAt("props", "resource_lumber", props, 0.4f, -3.9f, 15f);
            PlaceAt("props", "wheelbarrow", props, 2.9f, -3.6f, 200f);
            PlaceAt("props", "pallet", props, 3.8f, -4.4f, 0f);

            // --- rekwizyty: targ i karczma ---
            PlaceAt("props", "barrel", props, -5.2f, 3.9f, 0f);
            PlaceAt("props", "barrel", props, -4.6f, 4.4f, 40f);
            PlaceAt("props", "crate_B_big", props, -7.6f, 4.0f, 12f);
            PlaceAt("props", "sack", props, 1.2f, 2.2f, 30f);
            PlaceAt("props", "sack", props, 1.7f, 2.6f, 110f);
            PlaceAt("props", "crate_open", props, -1.2f, 2.4f, 210f);
            PlaceAt("props", "bucket_water", props, 3.4f, 0.9f, 0f);

            // --- obozowisko przy strażnicy ---
            PlaceAt("props", "tent", props, 11.4f, -4.6f, 200f);
            PlaceAt("props", "bucket_arrows", props, 9.2f, -4.0f, 0f);
            PlaceAt("props", "crate_long_A", props, 10.2f, -3.2f, 75f);
            PlaceAt("props", $"flag_{F}", props, 13.4f, -1.4f, 0f);
            PlaceAt("props", $"flag_{F}", props, -0.2f, 4.0f, 0f);

            // --- drobiazgi rozrzucone z wariacją ---
            for (int i = 0; i < 6; i++)
            {
                float x = -8f + (float)rng.NextDouble() * 20f;
                float z = -2f + (float)rng.NextDouble() * 4f;
                string[] small = { "bucket_empty", "crate_A_small", "crate_B_small", "sack" };
                PlaceAt("props", small[rng.Next(small.Length)], props, x, z,
                        (float)rng.NextDouble() * 360f);
            }
        }

        // ============================================================ LAS

        /// <summary>
        /// Gęsty las wypełniający wszystko poza osadą, drogą i rzeką. Gęstość rośnie wraz
        /// z odległością od centrum: przy osadzie pojedyncze drzewa, na obrzeżach zwarty bór.
        /// To buduje naturalną „ścianę" ograniczającą fragment świata bez niewidzialnych barier.
        /// </summary>
        private static void BuildForest(Transform world, HashSet<Vector2Int> occupied)
        {
            var root = new GameObject("Forest").transform;
            root.SetParent(world, false);
            var rng = new System.Random(4711);

            string[] dense = { "trees_A_large", "trees_B_large", "trees_A_medium", "trees_B_medium" };
            string[] sparse = { "trees_A_small", "trees_B_small", "tree_single_A", "tree_single_B" };
            string[] rocks = { "rock_single_A", "rock_single_B", "rock_single_C", "rock_single_D", "rock_single_E" };

            foreach (var h in occupied)
            {
                Vector3 pos = HexToWorld(h.x, h.y);

                // Serce osady zostaje puste — budynki mają miejsce.
                float distFromVillage = new Vector2(pos.x, pos.z).magnitude;
                if (distFromVillage < 16f) continue;

                float edge = Mathf.InverseLerp(16f, MapRadius * _hexW * 0.75f, distFromVillage);
                double roll = rng.NextDouble();

                if (roll < 0.10 + 0.25 * edge)
                {
                    PlaceVaried(root, dense[rng.Next(dense.Length)], pos, rng, 1.0f);
                }
                else if (roll < 0.30 + 0.30 * edge)
                {
                    PlaceVaried(root, sparse[rng.Next(sparse.Length)], pos, rng, 1.0f);
                }
                else if (roll < 0.36)
                {
                    PlaceVaried(root, rocks[rng.Next(rocks.Length)], pos, rng, 0.9f);
                }
            }

            // Zagajnik przy osadzie — przejście między polaną a borem, nie ostra ściana.
            var grove = new GameObject("Grove").transform; grove.SetParent(root, false);
            Vector3[] groveSpots =
            {
                new Vector3(-12f, 0f, 9f), new Vector3(-9f, 0f, 11.5f), new Vector3(-4f, 0f, 12f),
                new Vector3(6f, 0f, 11.5f), new Vector3(11f, 0f, 9.5f),
                new Vector3(-12f, 0f, -9f), new Vector3(-6f, 0f, -11f), new Vector3(5f, 0f, -11.5f),
                new Vector3(12f, 0f, -8.5f),
            };
            foreach (var s in groveSpots)
                PlaceVaried(grove, sparse[rng.Next(sparse.Length)], s, rng, 1.0f);
        }

        // ============================================================ OBRZEŻE

        /// <summary>Góry i wzgórza domykające horyzont — fragment świata ma mieć tło.</summary>
        private static void BuildBorder(Transform world)
        {
            var root = new GameObject("Border").transform;
            root.SetParent(world, false);
            var rng = new System.Random(31337);

            string[] mountains = { "mountain_A_grass_trees", "mountain_B_grass_trees", "mountain_C_grass_trees" };
            string[] hills = { "hills_A_trees", "hills_B_trees", "hills_C_trees" };

            float ring = MapRadius * _hexW * 0.78f;
            for (int i = 0; i < 26; i++)
            {
                float a = (i / 26f) * Mathf.PI * 2f;
                float rad = ring + (float)rng.NextDouble() * 3f;
                var pos = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad);
                string set = (i % 3 == 0) ? mountains[rng.Next(mountains.Length)] : hills[rng.Next(hills.Length)];
                PlaceVaried(root, set, pos, rng, 1.0f);
            }
        }

        // ============================================================ NPC I POTWORY

        private static void BuildNpcs()
        {
            var root = new GameObject("NPCs").transform;

            AddNpc(root, "Guide Alwin", new Vector3(1.6f, 0.9f, 1.8f), new[]
            {
                "Witaj w Brzasku, wędrowcze. To ostatnia osada przed Borem.",
                "Za rzeką kręcą się bestie — kowal naostrzy ci ostrze, jeśli masz czym zapłacić.",
                "Wróć żywy, a opowiem ci, co kryje się głębiej w lesie."
            });

            AddNpc(root, "Kowal Bruno", new Vector3(-5.6f, 0.9f, -3.4f), new[]
            {
                "Kuźnia czynna od świtu. Żelazo nie kuje się samo.",
                "Przynieś mi rudę z gór, a zrobię z ciebie kogoś."
            });

            AddNpc(root, "Zielarka Wiera", new Vector3(-1.0f, 0.9f, 5.2f), new[]
            {
                "Zioła z Boru leczą, ale nie każde. Niektóre zabijają szybciej niż wilk.",
                "Uważaj na mgłę za mostem."
            });
        }

        private static void AddNpc(Transform parent, string name, Vector3 pos, string[] lines)
        {
            var go = new GameObject("NPC_" + name.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f; col.radius = 0.4f; col.center = new Vector3(0f, 0.9f, 0f);

            var npc = go.AddComponent<NPCInteractable>();
            npc.NpcName = name;
            npc.DialogueLines = lines;

            // PLACEHOLDER wizualny — do zastąpienia modelem postaci, gdy pakiet postaci trafi
            // do projektu. Węzeł nazwany zgodnie z konwencją podmiany.
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual_DEBUG";
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            vis.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
        }

        /// <summary>Łowiska za rzeką i na skraju lasu — z dala od osady, jak w każdym MMORPG.</summary>
        private static void BuildSpawners()
        {
            var root = new GameObject("Spawns").transform;
            AddSpawner(root, "Monster_MarshSnake", new Vector3(-13.5f, 1f, 5.5f), 3, 3.5f, 20f);
            AddSpawner(root, "Monster_Wolf", new Vector3(-14.0f, 1f, -6.0f), 2, 4f, 30f);
            AddSpawner(root, "Monster_Goblin", new Vector3(16.0f, 1f, 8.0f), 3, 4f, 28f);
            AddSpawner(root, "Monster_Skeleton", new Vector3(15.0f, 1f, -10.0f), 2, 3.5f, 45f);
        }

        private static void AddSpawner(Transform parent, string asset, Vector3 pos,
                                       int count, float radius, float respawn)
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterData>(
                $"Assets/Azonera/ScriptableObjects/Monsters/{asset}.asset");
            if (data == null) { Debug.LogWarning("[Azonera] Brak MonsterData: " + asset); return; }

            var go = new GameObject("Spawn_" + data.DisplayName);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<MonsterSpawner>().Configure(data, count, radius, respawn);
        }

        private static Vector3 PlayerSpawn() => new Vector3(6.0f, 1.2f, 1.2f);

        // ============================================================ OŚWIETLENIE

        /// <summary>
        /// Oświetlenie dnia dla stylizowanego świata: ciepłe słońce pod niskim kątem
        /// (długie, czytelne cienie budynków), chłodne niebo w cieniach, delikatna mgła
        /// dająca głębię planów. Bez przesady z efektami — wydajność jest priorytetem.
        /// </summary>
        private static void BuildLighting()
        {
            var sunGo = new GameObject("Directional Light");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.35f;
            sunGo.transform.rotation = Quaternion.Euler(46f, 38f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.58f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.45f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.16f);
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.70f, 0.78f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 130f;

            BuildPostFx();
        }

        private static void BuildPostFx()
        {
            try
            {
                var volGo = new GameObject("Global Volume");
                var vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.priority = 1f;

                var profile = ScriptableObject.CreateInstance<VolumeProfile>();

                var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
                tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

                var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                bloom.intensity.Override(0.55f);
                bloom.threshold.Override(1.05f);
                bloom.scatter.Override(0.6f);
                bloom.highQualityFiltering.Override(true);

                var ca = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
                ca.postExposure.Override(0.1f);
                ca.contrast.Override(12f);
                ca.saturation.Override(10f);   // stylizowany świat znosi żywsze barwy

                var smh = profile.Add<UnityEngine.Rendering.Universal.ShadowsMidtonesHighlights>(true);
                smh.shadows.Override(new Vector4(0.9f, 0.96f, 1.14f, 0f));
                smh.highlights.Override(new Vector4(1.06f, 1.02f, 0.94f, 0f));

                var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                vig.intensity.Override(0.24f);
                vig.smoothness.Override(0.6f);

                string dir = "Assets/Azonera/Materials/KayKit";
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Azonera/Materials", "KayKit");
                AssetDatabase.CreateAsset(profile, dir + "/VillageVolumeProfile.asset");
                vol.sharedProfile = profile;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Azonera] PostFX pominięty: " + e.Message);
            }
        }

        // ============================================================ POMOCNICZE

        private static GameObject LoadPrefab(string category, string name)
        {
            string key = category + "/" + name;
            if (_prefabCache.TryGetValue(key, out var cached)) return cached;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{category}/P_{name}.prefab");
            if (prefab == null) Debug.LogWarning($"[Azonera] Brak prefabu KayKit: {key}");
            _prefabCache[key] = prefab;
            return prefab;
        }

        private static GameObject Place(string category, string name, Transform parent, Vector3 pos, float yaw)
        {
            var prefab = LoadPrefab(category, name);
            if (prefab == null) return null;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        /// <summary>Skrót dla obiektów układanych we współrzędnych świata (osada, rekwizyty).</summary>
        private static GameObject PlaceAt(string category, string name, Transform parent,
                                          float x, float z, float yaw)
            => Place(category, name, parent, new Vector3(x, 0f, z), yaw);

        private static GameObject PlaceVaried(Transform parent, string name, Vector3 pos,
                                              System.Random rng, float baseScale)
        {
            // Drzewa, skały, wzgórza i góry mieszkają w tej samej kategorii prefabów.
            const string category = "nature";

            var go = Place(category, name, parent,
                           pos + new Vector3((float)rng.NextDouble() * 0.9f - 0.45f, 0f,
                                             (float)rng.NextDouble() * 0.9f - 0.45f),
                           (float)rng.NextDouble() * 360f);
            if (go == null) return null;

            float s = baseScale * (0.88f + (float)rng.NextDouble() * 0.26f);
            go.transform.localScale = new Vector3(s, s * (0.92f + (float)rng.NextDouble() * 0.18f), s);
            return go;
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
