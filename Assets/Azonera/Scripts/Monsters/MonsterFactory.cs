using UnityEngine;
using Azonera.Stats;
using Azonera.Combat;

namespace Azonera.Monsters
{
    /// <summary>
    /// Jedno miejsce składania encji potwora — używane przez runtime'owe spawnery
    /// ORAZ generatory scen w edytorze. Dzięki temu potwór postawiony w edytorze i ten
    /// zespawnowany w grze mają identyczną strukturę (koniec z dwiema wersjami prawdy).
    ///
    /// Warstwa wizualna jest odseparowana pod węzłem „Visual": gdy <see cref="MonsterData.ModelPrefab"/>
    /// jest ustawiony, używamy docelowego modelu; w przeciwnym razie powstaje jawnie
    /// oznaczony placeholder (`Visual_DEBUG`) do czasu dostarczenia assetu.
    /// </summary>
    public static class MonsterFactory
    {
        /// <summary>Nazwa węzła z grafiką — TYLKO ten węzeł wymieniamy przy podmianie modelu.</summary>
        public const string VisualNodeName = "Visual";
        public const string PlaceholderVisualName = "Visual_DEBUG";

        /// <summary>Buduje kompletną encję potwora gotową do walki.</summary>
        public static GameObject Create(MonsterData data, Vector3 position, Transform parent = null)
        {
            if (data == null)
            {
                Debug.LogWarning("[MonsterFactory] Brak MonsterData — pomijam spawn.");
                return null;
            }

            var go = new GameObject("Monster_" + data.DisplayName);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;

            var col = go.AddComponent<CapsuleCollider>();
            col.height = Mathf.Max(1f, data.PlaceholderHeight * 1.4f);
            col.radius = 0.5f;
            col.center = new Vector3(0f, 0f, 0f);

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            go.AddComponent<CharacterStats>();
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.Configure(data.AttackRange);

            var ai = go.AddComponent<MonsterAI>();
            ai.Initialize(data);

            BuildVisual(go, data);
            return go;
        }

        /// <summary>Tworzy węzeł grafiki: docelowy model albo oznaczony placeholder.</summary>
        public static GameObject BuildVisual(GameObject entity, MonsterData data)
        {
            if (data.ModelPrefab != null)
            {
                var model = Object.Instantiate(data.ModelPrefab, entity.transform);
                model.name = VisualNodeName;
                model.transform.localPosition = Vector3.zero;
                return model;
            }

            // PLACEHOLDER — kapsuła w kolorze gatunku. Do zastąpienia modelem wg Art Bible.
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = PlaceholderVisualName;
            vis.transform.SetParent(entity.transform, false);
            vis.transform.localScale = new Vector3(0.9f, Mathf.Max(0.5f, data.PlaceholderHeight * 0.7f), 0.9f);

            var vcol = vis.GetComponent<Collider>();
            if (vcol != null)
            {
                if (Application.isPlaying) Object.Destroy(vcol);
                else Object.DestroyImmediate(vcol);
            }

            var rend = vis.GetComponent<Renderer>();
            if (rend != null)
            {
                // Instancja materiału per gatunek — bez tego wszystkie potwory dzielą jeden kolor.
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                var mat = new Material(shader) { name = $"M_Mon_{data.DisplayName}_DEBUG" };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", data.PlaceholderColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", data.PlaceholderColor);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
                rend.sharedMaterial = mat;
            }
            return vis;
        }
    }
}
