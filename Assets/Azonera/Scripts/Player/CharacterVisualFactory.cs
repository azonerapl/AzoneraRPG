using System.Collections.Generic;
using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Buduje proceduralnie ARTYKUŁOWANĄ postać rycerza z brył — z prawdziwymi stawami
    /// (biodra→kolana, barki→łokcie, kręgosłup, szyja) i warstwowym pancerzem oraz materiałami PBR.
    /// Zwraca korzeń z komponentem <see cref="KnightRig"/> (referencje stawów) do animacji przez KnightAnimator.
    /// To maksymalny realizm osiągalny bez zewnętrznych modeli; węzeł gotowy pod podmianę na docelowy model/render.
    /// </summary>
    public static class CharacterVisualFactory
    {
        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        private static Material M(string name, Color c, float metallic, float smoothness, Color emission = default)
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

        private static Transform J(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static Transform P(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col); }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go.transform;
        }

        public static GameObject BuildKnight(Transform parent)
        {
            var steel  = M("Steel",     new Color(0.50f, 0.52f, 0.56f), 1f, 0.62f);
            var dark   = M("DarkSteel", new Color(0.22f, 0.23f, 0.27f), 1f, 0.50f);
            var mail   = M("Chainmail", new Color(0.30f, 0.31f, 0.34f), 1f, 0.35f);
            var leather= M("Leather",   new Color(0.16f, 0.10f, 0.06f), 0f, 0.25f);
            var gold   = M("Gold",      new Color(0.83f, 0.66f, 0.28f), 1f, 0.72f, new Color(0.14f, 0.10f, 0.03f));
            var cape   = M("Cape",      new Color(0.35f, 0.06f, 0.07f), 0f, 0.20f);
            var skin   = M("Skin",      new Color(0.76f, 0.58f, 0.48f), 0f, 0.35f);
            var hair   = M("Hair",      new Color(0.55f, 0.42f, 0.22f), 0f, 0.30f);
            var blade  = M("Blade",     new Color(0.72f, 0.74f, 0.80f), 1f, 0.85f);
            var wood   = M("ShieldWood",new Color(0.26f, 0.16f, 0.09f), 0.1f, 0.25f);

            var root = new GameObject("KnightVisual");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            root.transform.localScale = Vector3.one * 1.15f;
            var rig = root.AddComponent<KnightRig>();

            // ===================== MIEDNICA =====================
            var pelvis = J("Pelvis", root.transform, new Vector3(0f, -0.36f, 0f));
            P(PrimitiveType.Cube, "Hips", pelvis, new Vector3(0, 0, 0), new Vector3(0.44f, 0.2f, 0.28f), dark);
            P(PrimitiveType.Cube, "Belt", pelvis, new Vector3(0, 0.06f, 0.13f), new Vector3(0.42f, 0.08f, 0.05f), gold);
            P(PrimitiveType.Cube, "Fauld_F", pelvis, new Vector3(0, -0.16f, 0.12f), new Vector3(0.4f, 0.22f, 0.05f), steel, new Vector3(12, 0, 0));
            P(PrimitiveType.Cube, "Fauld_B", pelvis, new Vector3(0, -0.16f, -0.12f), new Vector3(0.4f, 0.2f, 0.05f), steel, new Vector3(-10, 0, 0));
            P(PrimitiveType.Cube, "Tasset_L", pelvis, new Vector3(-0.17f, -0.2f, 0.1f), new Vector3(0.2f, 0.24f, 0.05f), steel, new Vector3(10, 0, 3));
            P(PrimitiveType.Cube, "Tasset_R", pelvis, new Vector3(0.17f, -0.2f, 0.1f), new Vector3(0.2f, 0.24f, 0.05f), steel, new Vector3(10, 0, -3));

            // ===================== KRĘGOSŁUP / TORS =====================
            var spine = J("Spine", pelvis, new Vector3(0f, 0.06f, 0f));
            rig.Spine = spine;
            P(PrimitiveType.Cube, "Cuirass", spine, new Vector3(0, 0.3f, 0), new Vector3(0.5f, 0.5f, 0.32f), steel);
            P(PrimitiveType.Sphere, "Breast", spine, new Vector3(0, 0.34f, 0.1f), new Vector3(0.48f, 0.42f, 0.26f), steel);
            P(PrimitiveType.Cube, "AbMail", spine, new Vector3(0, 0.12f, 0.12f), new Vector3(0.4f, 0.2f, 0.08f), mail);
            P(PrimitiveType.Cube, "ChestRidge", spine, new Vector3(0, 0.34f, 0.2f), new Vector3(0.05f, 0.36f, 0.04f), gold);
            P(PrimitiveType.Sphere, "Emblem", spine, new Vector3(0, 0.36f, 0.21f), new Vector3(0.14f, 0.17f, 0.05f), gold);

            // ---- Klatka (barki, szyja, peleryna) ----
            var chest = J("Chest", spine, new Vector3(0f, 0.52f, 0f));

            // ===================== RAMIĘ L =====================
            var shL = J("ShoulderL", chest, new Vector3(-0.3f, 0.02f, 0f)); rig.ShoulderL = shL;
            P(PrimitiveType.Sphere, "Pauldron_L1", shL, new Vector3(0, 0.02f, 0), new Vector3(0.3f, 0.26f, 0.32f), steel);
            P(PrimitiveType.Sphere, "Pauldron_L2", shL, new Vector3(0, -0.07f, 0), new Vector3(0.32f, 0.16f, 0.34f), dark);
            P(PrimitiveType.Cylinder, "UpperArm_L", shL, new Vector3(0, -0.16f, 0), new Vector3(0.14f, 0.16f, 0.14f), mail);
            var elL = J("ElbowL", shL, new Vector3(0f, -0.3f, 0f)); rig.ElbowL = elL;
            P(PrimitiveType.Sphere, "Couter_L", elL, new Vector3(0, 0, 0), new Vector3(0.16f, 0.14f, 0.16f), steel);
            P(PrimitiveType.Cylinder, "Forearm_L", elL, new Vector3(0, -0.14f, 0.02f), new Vector3(0.13f, 0.15f, 0.13f), steel);
            P(PrimitiveType.Cube, "Gauntlet_L", elL, new Vector3(0, -0.28f, 0.06f), new Vector3(0.15f, 0.14f, 0.2f), dark);
            // Tarcza w lewej ręce
            var shieldT = P(PrimitiveType.Cylinder, "Shield", elL, new Vector3(-0.06f, -0.28f, 0.16f), new Vector3(0.46f, 0.04f, 0.46f), wood, new Vector3(90, 0, 8));
            P(PrimitiveType.Cylinder, "Shield_Boss", shieldT, new Vector3(0, 0.55f, 0), new Vector3(0.3f, 0.4f, 0.3f), gold);
            P(PrimitiveType.Cylinder, "Shield_Rim", shieldT, new Vector3(0, 0.02f, 0), new Vector3(1.08f, 0.5f, 1.08f), dark);

            // ===================== RAMIĘ R (miecz) =====================
            var shR = J("ShoulderR", chest, new Vector3(0.3f, 0.02f, 0f)); rig.ShoulderR = shR;
            P(PrimitiveType.Sphere, "Pauldron_R1", shR, new Vector3(0, 0.02f, 0), new Vector3(0.3f, 0.26f, 0.32f), steel);
            P(PrimitiveType.Sphere, "Pauldron_R2", shR, new Vector3(0, -0.07f, 0), new Vector3(0.32f, 0.16f, 0.34f), dark);
            P(PrimitiveType.Cylinder, "UpperArm_R", shR, new Vector3(0, -0.16f, 0), new Vector3(0.14f, 0.16f, 0.14f), mail);
            var elR = J("ElbowR", shR, new Vector3(0f, -0.3f, 0f)); rig.ElbowR = elR;
            P(PrimitiveType.Sphere, "Couter_R", elR, new Vector3(0, 0, 0), new Vector3(0.16f, 0.14f, 0.16f), steel);
            P(PrimitiveType.Cylinder, "Forearm_R", elR, new Vector3(0, -0.14f, 0.02f), new Vector3(0.13f, 0.15f, 0.13f), steel);
            P(PrimitiveType.Cube, "Gauntlet_R", elR, new Vector3(0, -0.28f, 0.06f), new Vector3(0.15f, 0.14f, 0.2f), dark);
            // Miecz w prawej ręce
            var sword = J("Sword", elR, new Vector3(0f, -0.34f, 0.12f));
            sword.localRotation = Quaternion.Euler(22f, 0f, 6f);
            P(PrimitiveType.Cylinder, "Grip", sword, new Vector3(0, -0.02f, 0), new Vector3(0.05f, 0.1f, 0.05f), leather);
            P(PrimitiveType.Cube, "Guard", sword, new Vector3(0, 0.1f, 0), new Vector3(0.26f, 0.05f, 0.06f), gold);
            P(PrimitiveType.Cube, "Blade", sword, new Vector3(0, 0.66f, 0), new Vector3(0.07f, 0.6f, 0.03f), blade);
            P(PrimitiveType.Sphere, "Pommel", sword, new Vector3(0, -0.1f, 0), new Vector3(0.09f, 0.09f, 0.09f), gold);

            // ===================== SZYJA / GŁOWA =====================
            var neck = J("Neck", chest, new Vector3(0f, 0.12f, 0f)); rig.Neck = neck;
            P(PrimitiveType.Cube, "Gorget", neck, new Vector3(0, 0, 0), new Vector3(0.3f, 0.1f, 0.26f), dark);
            P(PrimitiveType.Cylinder, "NeckCol", neck, new Vector3(0, 0.06f, 0), new Vector3(0.11f, 0.06f, 0.11f), skin);
            P(PrimitiveType.Sphere, "Head", neck, new Vector3(0, 0.2f, 0.01f), new Vector3(0.22f, 0.26f, 0.24f), skin);
            P(PrimitiveType.Sphere, "Hair", neck, new Vector3(0, 0.26f, -0.03f), new Vector3(0.24f, 0.2f, 0.26f), hair);
            P(PrimitiveType.Sphere, "Helmet", neck, new Vector3(0, 0.24f, 0), new Vector3(0.27f, 0.26f, 0.29f), steel);
            P(PrimitiveType.Cube, "Visor", neck, new Vector3(0, 0.19f, 0.13f), new Vector3(0.18f, 0.05f, 0.05f), dark);
            P(PrimitiveType.Cube, "NoseGuard", neck, new Vector3(0, 0.2f, 0.14f), new Vector3(0.04f, 0.13f, 0.05f), steel);
            P(PrimitiveType.Cube, "Plume", neck, new Vector3(0, 0.42f, -0.02f), new Vector3(0.05f, 0.18f, 0.24f), cape);

            // ===================== PELERYNA =====================
            var capeJ = J("Cape", chest, new Vector3(0f, 0.1f, -0.15f));
            P(PrimitiveType.Cube, "Cape_Up", capeJ, new Vector3(0, -0.3f, 0), new Vector3(0.5f, 0.55f, 0.04f), cape, new Vector3(8, 0, 0));
            P(PrimitiveType.Cube, "Cape_Low", capeJ, new Vector3(0, -0.85f, -0.06f), new Vector3(0.62f, 0.65f, 0.04f), cape, new Vector3(16, 0, 0));

            // ===================== NOGA L =====================
            var hipL = J("HipL", pelvis, new Vector3(-0.15f, -0.02f, 0f)); rig.HipL = hipL;
            P(PrimitiveType.Cylinder, "Thigh_L", hipL, new Vector3(0, -0.16f, 0), new Vector3(0.17f, 0.18f, 0.17f), mail);
            P(PrimitiveType.Cube, "Cuisse_L", hipL, new Vector3(0, -0.16f, 0.03f), new Vector3(0.19f, 0.28f, 0.14f), steel);
            var kneeL = J("KneeL", hipL, new Vector3(0f, -0.32f, 0f)); rig.KneeL = kneeL;
            P(PrimitiveType.Sphere, "Poleyn_L", kneeL, new Vector3(0, 0, 0.02f), new Vector3(0.18f, 0.16f, 0.2f), steel);
            P(PrimitiveType.Cylinder, "Greave_L", kneeL, new Vector3(0, -0.15f, 0.01f), new Vector3(0.15f, 0.16f, 0.15f), steel);
            P(PrimitiveType.Cube, "Sabaton_L", kneeL, new Vector3(0, -0.3f, 0.08f), new Vector3(0.18f, 0.12f, 0.3f), dark);

            // ===================== NOGA R =====================
            var hipR = J("HipR", pelvis, new Vector3(0.15f, -0.02f, 0f)); rig.HipR = hipR;
            P(PrimitiveType.Cylinder, "Thigh_R", hipR, new Vector3(0, -0.16f, 0), new Vector3(0.17f, 0.18f, 0.17f), mail);
            P(PrimitiveType.Cube, "Cuisse_R", hipR, new Vector3(0, -0.16f, 0.03f), new Vector3(0.19f, 0.28f, 0.14f), steel);
            var kneeR = J("KneeR", hipR, new Vector3(0f, -0.32f, 0f)); rig.KneeR = kneeR;
            P(PrimitiveType.Sphere, "Poleyn_R", kneeR, new Vector3(0, 0, 0.02f), new Vector3(0.18f, 0.16f, 0.2f), steel);
            P(PrimitiveType.Cylinder, "Greave_R", kneeR, new Vector3(0, -0.15f, 0.01f), new Vector3(0.15f, 0.16f, 0.15f), steel);
            P(PrimitiveType.Cube, "Sabaton_R", kneeR, new Vector3(0, -0.3f, 0.08f), new Vector3(0.18f, 0.12f, 0.3f), dark);

            return root;
        }
    }
}
