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
using Azonera.UI;
using Azonera.Core;
using Azonera.DevTools;
using Azonera.VFX;

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
            _mats.Clear();
            EnsureFolder(MatFolder); EnsureFolder(SceneFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildRoom();
            var player = BuildPlayer();
            var cam = BuildCamera(player.transform);
            BuildManagers();
            BuildClassicHUD();

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

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.07f, 0.10f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.05f);
            RenderSettings.fogDensity = 0.028f;

            TryPostFX();
        }

        private static void TryPostFX()
        {
            try
            {
                var volGo = new GameObject("Global Volume");
                var vol = volGo.AddComponent<UnityEngine.Rendering.Volume>();
                vol.isGlobal = true;
                var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                bloom.intensity.Override(1.1f); bloom.threshold.Override(0.8f);
                bloom.tint.Override(new Color(1f, 0.85f, 0.6f));
                var ca = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
                ca.postExposure.Override(0.05f); ca.contrast.Override(18f); ca.saturation.Override(-6f);
                var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                vig.intensity.Override(0.42f); vig.smoothness.Override(0.5f);
                AssetDatabase.CreateAsset(profile, MatFolder + "/DungeonVolumeProfile.asset");
                vol.sharedProfile = profile;
            }
            catch (System.Exception e) { Debug.LogWarning("[Azonera] PostFX pominięty: " + e.Message); }
        }

        // ============================================================ ROOM
        private static void BuildRoom()
        {
            var env = new GameObject("Environment").transform;

            var floor = Mat("FloorStone", new Color(0.15f, 0.15f, 0.17f), 0.12f);
            var wall = Mat("WallStone", new Color(0.11f, 0.11f, 0.13f), 0.08f);
            var brick = Mat("Brick", new Color(0.17f, 0.16f, 0.15f), 0.1f);
            var pillarMat = Mat("Pillar", new Color(0.19f, 0.18f, 0.17f), 0.12f);
            var carpet = Mat("Carpet", new Color(0.28f, 0.05f, 0.06f), 0.05f);
            var gold = Mat("Gold", new Color(0.85f, 0.62f, 0.18f), 0.6f, 0.9f, new Color(0.5f, 0.35f, 0.08f));
            var metal = Mat("BrazierMetal", new Color(0.09f, 0.08f, 0.07f), 0.5f, 0.8f);
            var wood = Mat("Wood", new Color(0.2f, 0.13f, 0.08f), 0.05f);

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

            // Beczki/skrzynie dla wypełnienia
            for (int i = 0; i < 5; i++)
                Prim(PrimitiveType.Cylinder, "Barrel", env, new Vector3(-W / 2 + 2.2f, 0.6f, -8 + i * 1.6f), new Vector3(0.7f, 0.6f, 0.7f), Quaternion.identity, wood);
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
            l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.28f); l.intensity = 4.5f; l.range = 11f;
            l.shadows = LightShadows.None;
            lightGo.AddComponent<TorchFlicker>().Configure(l, 4.5f);
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

        private static void BuildTreasure(Transform parent, Vector3 pos, Material gold, Material wood)
        {
            var t = new GameObject("Treasure").transform; t.SetParent(parent, false); t.position = pos;
            var rng = new System.Random(99);
            for (int i = 0; i < 40; i++)
            {
                float x = (float)(rng.NextDouble() - 0.5) * 3f;
                float z = (float)(rng.NextDouble() - 0.5) * 3f;
                float y = 0.1f + (float)rng.NextDouble() * 0.3f;
                var c = Prim(PrimitiveType.Sphere, "Coin", t, new Vector3(x, y, z), Vector3.one * 0.25f, Quaternion.identity, gold);
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
            Prim(PrimitiveType.Cube, "Chest", t, new Vector3(0, 0.4f, 0), new Vector3(1.4f, 0.8f, 0.9f), Quaternion.identity, wood);
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
            return cam;
        }

        private static void BuildManagers()
        {
            var go = new GameObject("GameSystems");
            go.AddComponent<GameManager>();
            go.AddComponent<DebugPanel>();
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

            // Battle List
            var bl = SubPanel(col, "Battle List", ref y, w - 12, 70);
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
        private static Material Mat(string name, Color color, float smoothness, float metallic = 0f, Color emission = default)
        {
            if (_mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatFolder}/M_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader); AssetDatabase.CreateAsset(mat, path);
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
            EditorUtility.SetDirty(mat); _mats[name] = mat; return mat;
        }

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
