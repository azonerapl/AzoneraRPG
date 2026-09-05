#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

namespace Azonera.EditorTools
{
    /// <summary>
    /// Buduje grywalną scenę "Azonera Starting Village": teren, wioska, oświetlenie dark fantasy,
    /// gracz, kamera izometryczna, HUD, NPC i potwory. Wszystko proceduralnie i idempotentnie.
    /// </summary>
    public static class AzoneraSceneBuilder
    {
        private const string MatFolder = "Assets/Azonera/Materials";
        private const string SceneFolder = "Assets/Azonera/Scenes";
        private const string ScenePath = SceneFolder + "/AzoneraStartingVillage.unity";

        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        [MenuItem("Azonera/★ Zbuduj Vertical Slice (wszystko)", priority = 0)]
        public static void BuildEverything()
        {
            AzoneraDataBuilder.BuildData();
            BuildScene();
            EditorUtility.DisplayDialog("Azonera",
                "Vertical Slice zbudowany!\n\nScena: AzoneraStartingVillage\nNaciśnij PLAY.\n\nWSAD – ruch, kółko – zoom, Q/E – obrót kamery,\nLPM – atak/interakcja, F1 – panel dev, F5/F9 – zapis/wczytanie.",
                "OK");
        }

        [MenuItem("Azonera/2. Zbuduj scenę wioski")]
        public static void BuildSceneMenu() { BuildScene(); }

        public static void BuildScene()
        {
            _mats.Clear();
            EnsureFolder(SceneFolder);
            EnsureFolder(MatFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildEnvironment();
            var player = BuildPlayer();
            var cam = BuildCamera(player.transform);
            BuildUI(out var dialogue);
            BuildManagers();
            BuildNPC();
            BuildMonsters();

            // link kamery w kontrolerze gracza
            var pc = player.GetComponent<PlayerController>();
            SetRef(pc, "_cameraTransform", cam.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Azonera] Scena wioski zbudowana: " + ScenePath);
        }

        // ============================================================ LIGHTING
        private static void BuildLighting()
        {
            var sunGo = new GameObject("Directional Light (Moon)");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.62f, 0.68f, 0.85f);
            sun.intensity = 0.95f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.09f, 0.10f, 0.14f);
            RenderSettings.fogDensity = 0.018f;

            TryBuildPostProcessing();
        }

        private static void TryBuildPostProcessing()
        {
            try
            {
                var volGo = new GameObject("Global Volume");
                var vol = volGo.AddComponent<UnityEngine.Rendering.Volume>();
                vol.isGlobal = true;
                var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();

                var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                bloom.intensity.Override(0.7f);
                bloom.threshold.Override(0.9f);
                bloom.tint.Override(new Color(1f, 0.9f, 0.7f));

                var ca = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
                ca.postExposure.Override(0.1f);
                ca.contrast.Override(12f);
                ca.saturation.Override(-8f);

                var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                vig.intensity.Override(0.32f);
                vig.smoothness.Override(0.4f);

                AssetDatabase.CreateAsset(profile, "Assets/Azonera/Materials/AzoneraVolumeProfile.asset");
                vol.sharedProfile = profile;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Azonera] Post-processing pominięty: " + e.Message);
            }
        }

        // ============================================================ ENVIRONMENT
        private static void BuildEnvironment()
        {
            var env = new GameObject("Environment").transform;

            var grass = Mat("Grass", new Color(0.20f, 0.30f, 0.17f), 0.05f);
            var dirt = Mat("Dirt", new Color(0.28f, 0.22f, 0.15f), 0.05f);
            var stone = Mat("Stone", new Color(0.34f, 0.35f, 0.38f), 0.15f);
            var cobble = Mat("Cobblestone", new Color(0.30f, 0.30f, 0.32f), 0.1f);
            var wood = Mat("Wood", new Color(0.26f, 0.17f, 0.10f), 0.05f);
            var darkWood = Mat("DarkWood", new Color(0.16f, 0.11f, 0.07f), 0.05f);
            var roof = Mat("Roof", new Color(0.30f, 0.13f, 0.11f), 0.05f);
            var leaves = Mat("Leaves", new Color(0.14f, 0.24f, 0.14f), 0.05f);

            // Teren
            var ground = Prim(PrimitiveType.Plane, "Ground", env, Vector3.zero, new Vector3(9f, 1f, 9f), Quaternion.identity, grass);

            // Plac centralny (cobblestone)
            Prim(PrimitiveType.Cylinder, "Plaza", env, new Vector3(0, 0.02f, 0), new Vector3(1.4f, 0.02f, 1.4f), Quaternion.identity, cobble);

            // Drogi krzyżujące się na placu
            Prim(PrimitiveType.Cube, "Road_NS", env, new Vector3(0, 0.02f, 0), new Vector3(3f, 0.04f, 40f), Quaternion.identity, cobble);
            Prim(PrimitiveType.Cube, "Road_EW", env, new Vector3(0, 0.02f, 0), new Vector3(40f, 0.04f, 3f), Quaternion.identity, cobble);

            // Mury obronne (kwadrat z przejściem na północy i południu)
            BuildWalls(env, stone);

            // Budynki
            BuildHouse(env, new Vector3(-11, 0, 8), 0f, wood, roof, darkWood);
            BuildHouse(env, new Vector3(11, 0, 8), 180f, stone, roof, darkWood);
            BuildHouse(env, new Vector3(-11, 0, -8), 20f, wood, roof, darkWood);
            BuildHouse(env, new Vector3(12, 0, -7), 200f, stone, roof, darkWood);

            // Ogrodzenia drewniane wzdłuż placu
            for (int i = -2; i <= 2; i++)
            {
                if (i == 0) continue;
                BuildFence(env, new Vector3(i * 2.2f, 0, 6.5f), wood);
                BuildFence(env, new Vector3(i * 2.2f, 0, -6.5f), wood);
            }

            // Las po zachodniej stronie (poza murami)
            var rng = new System.Random(1234);
            for (int i = 0; i < 26; i++)
            {
                float x = -22f - (float)rng.NextDouble() * 12f;
                float z = -18f + (float)rng.NextDouble() * 36f;
                BuildTree(env, new Vector3(x, 0, z), wood, leaves, 0.8f + (float)rng.NextDouble());
            }
            // Kilka drzew i krzaków wewnątrz dla klimatu
            BuildTree(env, new Vector3(-6, 0, 3), wood, leaves, 1.1f);
            BuildTree(env, new Vector3(6, 0, -3), wood, leaves, 1.0f);
            for (int i = 0; i < 8; i++)
                Prim(PrimitiveType.Sphere, "Bush", env,
                    new Vector3(-14f + i * 3.4f, 0.3f, -4f + (i % 2) * 8f),
                    new Vector3(1.1f, 0.7f, 1.1f), Quaternion.identity, leaves);

            // Pochodnie wzdłuż placu i drogi
            Vector3[] torchSpots =
            {
                new Vector3(-2.2f, 0, 2.2f), new Vector3(2.2f, 0, 2.2f),
                new Vector3(-2.2f, 0, -2.2f), new Vector3(2.2f, 0, -2.2f),
                new Vector3(-2f, 0, 10f), new Vector3(2f, 0, 10f),
                new Vector3(-2f, 0, -10f), new Vector3(2f, 0, -10f),
                new Vector3(-9, 0, 0), new Vector3(9, 0, 0),
            };
            foreach (var s in torchSpots) BuildTorch(env, s, darkWood);

            // Wejście do lochu (północ, za murem)
            BuildDungeonEntrance(env, new Vector3(0, 0, 30), stone);
        }

        private static void BuildWalls(Transform parent, Material mat)
        {
            var walls = new GameObject("Walls").transform;
            walls.SetParent(parent, false);
            float H = 3f, T = 1f, L = 38f, half = 18f;
            // Zachód / Wschód (pełne)
            Prim(PrimitiveType.Cube, "Wall_W", walls, new Vector3(-half, H / 2, 0), new Vector3(T, H, L), Quaternion.identity, mat);
            Prim(PrimitiveType.Cube, "Wall_E", walls, new Vector3(half, H / 2, 0), new Vector3(T, H, L), Quaternion.identity, mat);
            // Północ / Południe (z przejściem w środku, szer. 4)
            Prim(PrimitiveType.Cube, "Wall_N1", walls, new Vector3(-11, H / 2, half), new Vector3(15, H, T), Quaternion.identity, mat);
            Prim(PrimitiveType.Cube, "Wall_N2", walls, new Vector3(11, H / 2, half), new Vector3(15, H, T), Quaternion.identity, mat);
            Prim(PrimitiveType.Cube, "Wall_S1", walls, new Vector3(-11, H / 2, -half), new Vector3(15, H, T), Quaternion.identity, mat);
            Prim(PrimitiveType.Cube, "Wall_S2", walls, new Vector3(11, H / 2, -half), new Vector3(15, H, T), Quaternion.identity, mat);
            // Wieżyczki w rogach
            foreach (var c in new[] { new Vector3(-half, 0, half), new Vector3(half, 0, half), new Vector3(-half, 0, -half), new Vector3(half, 0, -half) })
                Prim(PrimitiveType.Cylinder, "Tower", walls, new Vector3(c.x, 2f, c.z), new Vector3(2.2f, 2f, 2.2f), Quaternion.identity, mat);
        }

        private static void BuildHouse(Transform parent, Vector3 pos, float yaw, Material body, Material roofMat, Material trim)
        {
            var house = new GameObject("House").transform;
            house.SetParent(parent, false);
            house.position = pos;
            house.rotation = Quaternion.Euler(0, yaw, 0);

            Prim(PrimitiveType.Cube, "Body", house, new Vector3(0, 1.6f, 0), new Vector3(6, 3.2f, 5), Quaternion.identity, body);
            // Dach – spłaszczony, obrócony sześcian (pryzma)
            var r = Prim(PrimitiveType.Cube, "Roof", house, new Vector3(0, 3.6f, 0), new Vector3(4.6f, 4.6f, 5.6f), Quaternion.Euler(0, 0, 45f), roofMat);
            r.transform.localScale = new Vector3(4.6f, 4.6f, 5.6f);
            // Drzwi
            Prim(PrimitiveType.Cube, "Door", house, new Vector3(0, 1f, 2.55f), new Vector3(1.3f, 2f, 0.2f), Quaternion.identity, trim);
        }

        private static void BuildFence(Transform parent, Vector3 pos, Material wood)
        {
            var f = new GameObject("Fence").transform;
            f.SetParent(parent, false); f.position = pos;
            Prim(PrimitiveType.Cube, "Post_L", f, new Vector3(-1f, 0.6f, 0), new Vector3(0.2f, 1.2f, 0.2f), Quaternion.identity, wood);
            Prim(PrimitiveType.Cube, "Post_R", f, new Vector3(1f, 0.6f, 0), new Vector3(0.2f, 1.2f, 0.2f), Quaternion.identity, wood);
            Prim(PrimitiveType.Cube, "Rail", f, new Vector3(0, 0.85f, 0), new Vector3(2.2f, 0.16f, 0.14f), Quaternion.identity, wood);
        }

        private static void BuildTree(Transform parent, Vector3 pos, Material bark, Material leaves, float scale)
        {
            var t = new GameObject("Tree").transform;
            t.SetParent(parent, false); t.position = pos;
            Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 1.4f * scale, 0), new Vector3(0.4f * scale, 1.4f * scale, 0.4f * scale), Quaternion.identity, bark);
            Prim(PrimitiveType.Sphere, "Canopy", t, new Vector3(0, 3.2f * scale, 0), new Vector3(2.6f * scale, 2.8f * scale, 2.6f * scale), Quaternion.identity, leaves);
            Prim(PrimitiveType.Sphere, "Canopy2", t, new Vector3(0.6f * scale, 2.6f * scale, 0.3f * scale), new Vector3(1.8f * scale, 1.9f * scale, 1.8f * scale), Quaternion.identity, leaves);
        }

        private static void BuildTorch(Transform parent, Vector3 pos, Material wood)
        {
            var torch = new GameObject("Torch").transform;
            torch.SetParent(parent, false); torch.position = pos;
            Prim(PrimitiveType.Cylinder, "Post", torch, new Vector3(0, 1.1f, 0), new Vector3(0.14f, 1.1f, 0.14f), Quaternion.identity, wood);

            var flame = Prim(PrimitiveType.Sphere, "Flame", torch, new Vector3(0, 2.3f, 0), new Vector3(0.35f, 0.45f, 0.35f), Quaternion.identity, Mat("Flame", new Color(1f, 0.55f, 0.15f), 0f, 0f, new Color(1f, 0.5f, 0.12f) * 3f));
            Object.DestroyImmediate(flame.GetComponent<Collider>());

            var lightGo = new GameObject("TorchLight");
            lightGo.transform.SetParent(torch, false);
            lightGo.transform.localPosition = new Vector3(0, 2.4f, 0);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.6f, 0.25f);
            l.intensity = 3.2f;
            l.range = 12f;
            l.shadows = LightShadows.None; // pochodnie nie rzucają cieni – wydajność + czysty atlas
            var flick = lightGo.AddComponent<TorchFlicker>();
            flick.Configure(l, 3.2f);
        }

        private static void BuildDungeonEntrance(Transform parent, Vector3 pos, Material stone)
        {
            var d = new GameObject("DungeonEntrance").transform;
            d.SetParent(parent, false); d.position = pos;
            Prim(PrimitiveType.Cube, "Frame_L", d, new Vector3(-2, 2, 0), new Vector3(1, 4, 1), Quaternion.identity, stone);
            Prim(PrimitiveType.Cube, "Frame_R", d, new Vector3(2, 2, 0), new Vector3(1, 4, 1), Quaternion.identity, stone);
            Prim(PrimitiveType.Cube, "Frame_Top", d, new Vector3(0, 4, 0), new Vector3(5, 1, 1), Quaternion.identity, stone);
            var portal = Prim(PrimitiveType.Cube, "Portal", d, new Vector3(0, 2, 0), new Vector3(3, 3.6f, 0.3f), Quaternion.identity,
                Mat("Portal", new Color(0.05f, 0.02f, 0.08f), 0f, 0f, new Color(0.2f, 0.05f, 0.35f) * 1.2f));
            Object.DestroyImmediate(portal.GetComponent<Collider>());
        }

        // ============================================================ PLAYER
        private static GameObject BuildPlayer()
        {
            var knight = AssetDatabase.LoadAssetAtPath<ClassData>("Assets/Azonera/ScriptableObjects/Classes/Class_Knight.asset");

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1f, -4f);

            var col = player.AddComponent<CapsuleCollider>();
            col.height = 2f; col.radius = 0.5f; col.center = Vector3.zero;

            var rb = player.AddComponent<Rigidbody>();
            rb.mass = 60f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var stats = player.AddComponent<CharacterStats>();
            SetRef(stats, "_classData", knight);
            SetInt(stats, "_level", 1);

            player.AddComponent<Azonera.Inventory.Inventory>();
            player.AddComponent<EquipmentController>();
            player.AddComponent<MeleeAttacker>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerActions>();

            // Wizualny placeholder (kapsuła + wskaźnik kierunku) — łatwy do podmiany na model 3D
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0, 0, 0);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = Mat("PlayerBody", new Color(0.55f, 0.18f, 0.16f), 0.2f, 0.3f);

            var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
            front.name = "Facing";
            front.transform.SetParent(player.transform, false);
            front.transform.localPosition = new Vector3(0, 0.2f, 0.55f);
            front.transform.localScale = new Vector3(0.25f, 0.25f, 0.4f);
            Object.DestroyImmediate(front.GetComponent<Collider>());
            front.GetComponent<Renderer>().sharedMaterial = Mat("PlayerTrim", new Color(0.85f, 0.75f, 0.4f), 0.3f, 0.6f);

            return player;
        }

        // ============================================================ CAMERA
        private static UnityEngine.Camera BuildCamera(Transform target)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.farClipPlane = 600f;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            camGo.AddComponent<AudioListener>();

            var iso = camGo.AddComponent<IsometricCameraController>();
            SetRef(iso, "_target", target);
            return cam;
        }

        // ============================================================ UI / HUD
        private static void BuildUI(out DialogueController dialogue)
        {
            // EventSystem z modułem nowego Input System
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<HUDController>();

            // ---- Panel lewy górny ----
            var panel = Panel(canvasGo.transform, "TopLeft", new Vector2(0, 1), new Vector2(24, -24), new Vector2(360, 150), new Color(0.05f, 0.06f, 0.08f, 0.72f));

            var classText = Label(panel, "Class", new Vector2(0, 1), new Vector2(14, -10), new Vector2(220, 26), "Rycerz", 20, new Color(0.9f, 0.85f, 0.6f), TextAnchor.UpperLeft, FontStyle.Bold);
            var levelText = Label(panel, "Level", new Vector2(1, 1), new Vector2(-14, -10), new Vector2(130, 26), "Poziom 1", 18, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperRight, FontStyle.Bold);

            // HP
            var hpBg = Bar(panel, "HP_BG", new Vector2(14, -44), new Vector2(332, 22), new Color(0.15f, 0.05f, 0.05f, 0.9f));
            var hpFill = BarFill(hpBg, new Color(0.75f, 0.16f, 0.16f));
            var hpText = Label(hpBg, "HP_Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 22), "185 / 185", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Mana
            var mpBg = Bar(panel, "MP_BG", new Vector2(14, -72), new Vector2(332, 22), new Color(0.05f, 0.07f, 0.18f, 0.9f));
            var mpFill = BarFill(mpBg, new Color(0.25f, 0.45f, 0.85f));
            var mpText = Label(mpBg, "MP_Text", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 22), "40 / 40", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            // EXP (cienki pasek)
            var expBg = Bar(panel, "EXP_BG", new Vector2(14, -100), new Vector2(332, 10), new Color(0.08f, 0.08f, 0.05f, 0.9f));
            var expFill = BarFill(expBg, new Color(0.85f, 0.7f, 0.25f));

            var capText = Label(panel, "Cap", new Vector2(0, 1), new Vector2(14, -114), new Vector2(332, 24), "0 / 470 oz", 14, new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal);

            SetRef(hud, "_healthFill", hpFill);
            SetRef(hud, "_manaFill", mpFill);
            SetRef(hud, "_expFill", expFill);
            SetRef(hud, "_healthText", hpText);
            SetRef(hud, "_manaText", mpText);
            SetRef(hud, "_levelText", levelText);
            SetRef(hud, "_classText", classText);
            SetRef(hud, "_capacityText", capText);

            // ---- Podpowiedź sterowania (prawy dolny) ----
            var help = Panel(canvasGo.transform, "Help", new Vector2(1, 0), new Vector2(-24, 24), new Vector2(430, 96), new Color(0.05f, 0.06f, 0.08f, 0.6f));
            help.pivot = new Vector2(1, 0);
            Label(help, "HelpText", new Vector2(0, 0), new Vector2(12, 8), new Vector2(406, 80),
                "WSAD – ruch    Kółko – zoom    Q/E – obrót\nLPM – atak / rozmowa\nF1 – dev panel    F5 – zapis    F9 – wczytaj",
                15, new Color(0.8f, 0.8f, 0.85f), TextAnchor.LowerLeft, FontStyle.Normal);

            // ---- Panel dialogu (dół, ukryty) ----
            var dlgPanel = Panel(canvasGo.transform, "Dialogue", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(900, 170), new Color(0.04f, 0.05f, 0.07f, 0.92f));
            dlgPanel.pivot = new Vector2(0.5f, 0);
            var dlgName = Label(dlgPanel, "Name", new Vector2(0, 1), new Vector2(24, -14), new Vector2(500, 30), "NPC", 22, new Color(0.9f, 0.8f, 0.45f), TextAnchor.UpperLeft, FontStyle.Bold);
            var dlgLine = Label(dlgPanel, "Line", new Vector2(0, 1), new Vector2(24, -52), new Vector2(852, 90), "...", 18, new Color(0.92f, 0.92f, 0.92f), TextAnchor.UpperLeft, FontStyle.Normal);
            var dlgHint = Label(dlgPanel, "Hint", new Vector2(1, 0), new Vector2(-24, 12), new Vector2(320, 24), "▶ Klik / Spacja — dalej", 14, new Color(0.6f, 0.6f, 0.65f), TextAnchor.LowerRight, FontStyle.Italic);

            dialogue = canvasGo.AddComponent<DialogueController>();
            SetRef(dialogue, "_panel", dlgPanel.gameObject);
            SetRef(dialogue, "_nameText", dlgName);
            SetRef(dialogue, "_lineText", dlgLine);
            SetRef(dialogue, "_hintText", dlgHint);
            dlgPanel.gameObject.SetActive(false);
        }

        // ============================================================ MANAGERS
        private static void BuildManagers()
        {
            var go = new GameObject("GameSystems");
            go.AddComponent<GameManager>();
            go.AddComponent<DebugPanel>();
        }

        // ============================================================ NPC
        private static void BuildNPC()
        {
            var npc = new GameObject("NPC_GuideAlwin");
            npc.transform.position = new Vector3(3.5f, 1f, 1.5f);
            var col = npc.AddComponent<CapsuleCollider>();
            col.height = 2f; col.radius = 0.5f;
            npc.AddComponent<NPCInteractable>();

            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual"; vis.transform.SetParent(npc.transform, false);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = Mat("NPCBody", new Color(0.25f, 0.4f, 0.55f), 0.2f);

            // Znacznik nad głową
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "Marker"; mark.transform.SetParent(npc.transform, false);
            mark.transform.localPosition = new Vector3(0, 1.7f, 0);
            mark.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            mark.transform.localRotation = Quaternion.Euler(45, 45, 0);
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.GetComponent<Renderer>().sharedMaterial = Mat("Quest", new Color(1f, 0.85f, 0.2f), 0f, 0f, new Color(1f, 0.8f, 0.15f) * 2f);
        }

        // ============================================================ MONSTERS
        private static void BuildMonsters()
        {
            var root = new GameObject("Monsters").transform;
            Spawn("Monster_MarshSnake", new Vector3(-26, 0, 4), root);
            Spawn("Monster_MarshSnake", new Vector3(-24, 0, 9), root);
            Spawn("Monster_MarshSnake", new Vector3(-28, 0, -3), root);
            Spawn("Monster_Wolf", new Vector3(-23, 0, -12), root);
            Spawn("Monster_Wolf", new Vector3(-27, 0, -14), root);
            Spawn("Monster_Goblin", new Vector3(10, 0, 26), root);
            Spawn("Monster_Goblin", new Vector3(-8, 0, 25), root);
            Spawn("Monster_Skeleton", new Vector3(0, 0, 27), root); // strażnik lochu
        }

        private static void Spawn(string assetName, Vector3 pos, Transform parent)
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/Azonera/ScriptableObjects/Monsters/{assetName}.asset");
            if (data == null) { Debug.LogWarning("[Azonera] Brak MonsterData: " + assetName); return; }

            var go = new GameObject("Monster_" + data.DisplayName);
            go.transform.position = new Vector3(pos.x, 1f, pos.z);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, 1f, pos.z);

            var col = go.AddComponent<CapsuleCollider>();
            col.height = 2f; col.radius = 0.5f;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            go.AddComponent<CharacterStats>();
            go.AddComponent<MeleeAttacker>();
            var ai = go.AddComponent<MonsterAI>();
            SetRef(ai, "_data", data);

            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "Visual"; vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = new Vector3(0.9f, data.PlaceholderHeight * 0.7f, 0.9f);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = Mat("Mon_" + assetName, data.PlaceholderColor, 0.1f);
        }

        // ============================================================ HELPERS
        private static Material Mat(string name, Color color, float smoothness, float metallic = 0f, Color emission = default)
        {
            if (_mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatFolder}/M_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (emission != default)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(mat);
            _mats[name] = mat;
            return mat;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
            return go;
        }

        // ---- UI helpers ----
        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        private static RectTransform Bar(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = bgColor;
            img.sprite = BuiltinSprite();
            img.type = Image.Type.Sliced;
            return rt;
        }

        private static Image BarFill(RectTransform bg, Color fillColor)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(bg, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-2, -2);
            var img = go.GetComponent<Image>();
            img.color = fillColor;
            img.sprite = BuiltinSprite();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            return img;
        }

        private static Text Label(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, string text, int fontSize, Color color, TextAnchor align, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = align; t.fontStyle = style;
            t.font = BuiltinFont();
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Sprite _builtinSprite;
        private static Sprite BuiltinSprite()
        {
            if (_builtinSprite == null)
                _builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return _builtinSprite;
        }

        private static Font _builtinFont;
        private static Font BuiltinFont()
        {
            if (_builtinFont == null)
            {
                _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_builtinFont == null) _builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _builtinFont;
        }

        // ---- Serialized field setters ----
        private static void SetRef(Object comp, string field, Object value)
        {
            var so = new SerializedObject(comp);
            var p = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogWarning($"[Azonera] Brak pola {field} w {comp.GetType().Name}");
        }

        private static void SetInt(Object comp, string field, int value)
        {
            var so = new SerializedObject(comp);
            var p = so.FindProperty(field);
            if (p != null) { p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
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
