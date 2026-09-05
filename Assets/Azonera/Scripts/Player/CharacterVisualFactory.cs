using System.Collections.Generic;
using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Buduje proceduralnie uzbrojoną postać humanoidalną (rycerz) z brył — proporcjonalny BLOCKOUT
    /// z pancerzem, mieczem, tarczą i peleryną oraz materiałami PBR. To realna sylwetka postaci
    /// (nie kapsuła), a węzeł jest gotowy pod podmianę na docelowy model/render. Runtime + edytor.
    /// Postać zwrócona jako korzeń wyśrodkowany w pivocie gracza (stopy ~ localY = -1, głowa ~ +1).
    /// </summary>
    public static class CharacterVisualFactory
    {
        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        private static Material Mat(string name, Color c, float metallic, float smoothness, Color emission = default)
        {
            if (_mats.TryGetValue(name, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (emission != default)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", emission);
            }
            _mats[name] = m;
            return m;
        }

        private static Transform Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Quaternion rot = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot == default ? Quaternion.identity : rot;
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go.transform;
        }

        /// <summary>Buduje rycerza jako dziecko <paramref name="parent"/> (zwykle węzeł „Visual" gracza).</summary>
        public static GameObject BuildKnight(Transform parent)
        {
            var steel = Mat("Steel", new Color(0.36f, 0.38f, 0.42f), 0.9f, 0.55f);
            var dark = Mat("DarkSteel", new Color(0.17f, 0.18f, 0.22f), 0.85f, 0.45f);
            var leather = Mat("Leather", new Color(0.18f, 0.12f, 0.07f), 0.1f, 0.2f);
            var gold = Mat("Gold", new Color(0.82f, 0.62f, 0.2f), 1f, 0.7f, new Color(0.25f, 0.18f, 0.05f));
            var cape = Mat("Cape", new Color(0.32f, 0.05f, 0.06f), 0.05f, 0.25f);
            var skin = Mat("Skin", new Color(0.78f, 0.6f, 0.5f), 0f, 0.3f);
            var wood = Mat("ShieldWood", new Color(0.26f, 0.16f, 0.09f), 0.1f, 0.25f);
            var blade = Mat("Blade", new Color(0.7f, 0.72f, 0.78f), 1f, 0.8f);

            var root = new GameObject("KnightVisual");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            var t = root.transform;

            // --- Nogi / stopy ---
            Part(PrimitiveType.Cube, "Boot_L", t, new Vector3(-0.17f, -0.92f, 0.06f), new Vector3(0.22f, 0.16f, 0.34f), dark);
            Part(PrimitiveType.Cube, "Boot_R", t, new Vector3(0.17f, -0.92f, 0.06f), new Vector3(0.22f, 0.16f, 0.34f), dark);
            Part(PrimitiveType.Cylinder, "Leg_L", t, new Vector3(-0.17f, -0.62f, 0f), new Vector3(0.18f, 0.3f, 0.18f), leather);
            Part(PrimitiveType.Cylinder, "Leg_R", t, new Vector3(0.17f, -0.62f, 0f), new Vector3(0.18f, 0.3f, 0.18f), leather);
            Part(PrimitiveType.Cube, "Greave_L", t, new Vector3(-0.17f, -0.62f, 0.02f), new Vector3(0.2f, 0.28f, 0.16f), steel);
            Part(PrimitiveType.Cube, "Greave_R", t, new Vector3(0.17f, -0.62f, 0.02f), new Vector3(0.2f, 0.28f, 0.16f), steel);

            // --- Biodra / pas ---
            Part(PrimitiveType.Cube, "Hips", t, new Vector3(0f, -0.34f, 0f), new Vector3(0.5f, 0.2f, 0.28f), dark);
            Part(PrimitiveType.Cube, "Belt", t, new Vector3(0f, -0.28f, 0.12f), new Vector3(0.4f, 0.08f, 0.06f), gold);

            // --- Tors / kirys ---
            Part(PrimitiveType.Cube, "Torso", t, new Vector3(0f, -0.02f, 0f), new Vector3(0.56f, 0.5f, 0.32f), steel);
            Part(PrimitiveType.Cube, "Chest_Trim", t, new Vector3(0f, 0.06f, 0.17f), new Vector3(0.5f, 0.34f, 0.05f), gold);
            Part(PrimitiveType.Sphere, "Ab", t, new Vector3(0f, -0.2f, 0.14f), new Vector3(0.42f, 0.22f, 0.2f), dark);

            // --- Naramienniki ---
            Part(PrimitiveType.Sphere, "Pauldron_L", t, new Vector3(-0.34f, 0.24f, 0f), new Vector3(0.28f, 0.24f, 0.3f), steel);
            Part(PrimitiveType.Sphere, "Pauldron_R", t, new Vector3(0.34f, 0.24f, 0f), new Vector3(0.28f, 0.24f, 0.3f), steel);

            // --- Ramiona ---
            Part(PrimitiveType.Cylinder, "UpperArm_L", t, new Vector3(-0.36f, 0.02f, 0f), new Vector3(0.15f, 0.22f, 0.15f), dark, Quaternion.Euler(0, 0, 6));
            Part(PrimitiveType.Cylinder, "UpperArm_R", t, new Vector3(0.36f, 0.02f, 0f), new Vector3(0.15f, 0.22f, 0.15f), dark, Quaternion.Euler(0, 0, -6));
            Part(PrimitiveType.Cylinder, "Forearm_L", t, new Vector3(-0.4f, -0.32f, 0.06f), new Vector3(0.14f, 0.2f, 0.14f), steel);
            Part(PrimitiveType.Cylinder, "Forearm_R", t, new Vector3(0.4f, -0.32f, 0.06f), new Vector3(0.14f, 0.2f, 0.14f), steel);
            Part(PrimitiveType.Cube, "Gauntlet_L", t, new Vector3(-0.42f, -0.52f, 0.1f), new Vector3(0.15f, 0.13f, 0.18f), dark);
            Part(PrimitiveType.Cube, "Gauntlet_R", t, new Vector3(0.42f, -0.52f, 0.1f), new Vector3(0.15f, 0.13f, 0.18f), dark);

            // --- Szyja / głowa / hełm ---
            Part(PrimitiveType.Cylinder, "Neck", t, new Vector3(0f, 0.28f, 0f), new Vector3(0.12f, 0.06f, 0.12f), skin);
            Part(PrimitiveType.Sphere, "Head", t, new Vector3(0f, 0.42f, 0.01f), new Vector3(0.24f, 0.27f, 0.25f), skin);
            Part(PrimitiveType.Sphere, "Helmet", t, new Vector3(0f, 0.45f, 0f), new Vector3(0.28f, 0.26f, 0.29f), steel);
            Part(PrimitiveType.Cube, "Visor", t, new Vector3(0f, 0.42f, 0.13f), new Vector3(0.2f, 0.05f, 0.05f), dark);
            Part(PrimitiveType.Cube, "Crest", t, new Vector3(0f, 0.6f, -0.02f), new Vector3(0.05f, 0.14f, 0.22f), gold);

            // --- Peleryna ---
            Part(PrimitiveType.Cube, "Cape", t, new Vector3(0f, -0.05f, -0.19f), new Vector3(0.52f, 0.9f, 0.04f), cape, Quaternion.Euler(6, 0, 0));

            // --- Tarcza (lewa ręka) ---
            var shield = Part(PrimitiveType.Cylinder, "Shield", t, new Vector3(-0.5f, -0.28f, 0.2f), new Vector3(0.44f, 0.04f, 0.44f), wood, Quaternion.Euler(90, 0, 8));
            Part(PrimitiveType.Cylinder, "Shield_Boss", shield, new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 0.4f, 0.3f), gold);

            // --- Miecz (prawa ręka) ---
            var sword = new GameObject("Sword").transform; sword.SetParent(t, false);
            sword.localPosition = new Vector3(0.5f, -0.5f, 0.16f);
            sword.localRotation = Quaternion.Euler(18f, 0f, 6f);
            Part(PrimitiveType.Cylinder, "Grip", sword, new Vector3(0f, -0.02f, 0f), new Vector3(0.05f, 0.1f, 0.05f), leather);
            Part(PrimitiveType.Cube, "Guard", sword, new Vector3(0f, 0.1f, 0f), new Vector3(0.26f, 0.05f, 0.06f), gold);
            Part(PrimitiveType.Cube, "Blade", sword, new Vector3(0f, 0.62f, 0f), new Vector3(0.07f, 0.55f, 0.03f), blade);
            Part(PrimitiveType.Sphere, "Pommel", sword, new Vector3(0f, -0.1f, 0f), new Vector3(0.09f, 0.09f, 0.09f), gold);

            // --- Detale zbroi (tassety, nakolanniki, emblemat, kołnierz) ---
            Part(PrimitiveType.Cube, "Tasset_L", t, new Vector3(-0.16f, -0.44f, 0.14f), new Vector3(0.22f, 0.2f, 0.06f), steel, Quaternion.Euler(10, 0, 4));
            Part(PrimitiveType.Cube, "Tasset_R", t, new Vector3(0.16f, -0.44f, 0.14f), new Vector3(0.22f, 0.2f, 0.06f), steel, Quaternion.Euler(10, 0, -4));
            Part(PrimitiveType.Cube, "Tasset_C", t, new Vector3(0f, -0.46f, 0.15f), new Vector3(0.2f, 0.24f, 0.05f), leather);
            Part(PrimitiveType.Sphere, "Knee_L", t, new Vector3(-0.17f, -0.78f, 0.08f), new Vector3(0.2f, 0.16f, 0.2f), steel);
            Part(PrimitiveType.Sphere, "Knee_R", t, new Vector3(0.17f, -0.78f, 0.08f), new Vector3(0.2f, 0.16f, 0.2f), steel);
            Part(PrimitiveType.Sphere, "Emblem", t, new Vector3(0f, 0.08f, 0.19f), new Vector3(0.14f, 0.16f, 0.05f), gold);
            Part(PrimitiveType.Cube, "Gorget", t, new Vector3(0f, 0.2f, 0.04f), new Vector3(0.34f, 0.1f, 0.28f), dark);
            Part(PrimitiveType.Cube, "NoseGuard", t, new Vector3(0f, 0.42f, 0.15f), new Vector3(0.04f, 0.14f, 0.05f), steel);

            // --- Peleryna: dolny, szerszy fragment (falowanie) ---
            Part(PrimitiveType.Cube, "Cape_Lower", t, new Vector3(0f, -0.55f, -0.22f), new Vector3(0.62f, 0.75f, 0.04f), cape, Quaternion.Euler(12, 0, 0));

            return root;
        }
    }
}
