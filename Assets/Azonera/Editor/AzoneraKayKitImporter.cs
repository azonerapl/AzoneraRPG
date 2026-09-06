#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Integruje pakiet KayKit Medieval Hexagon (Kay Lousberg, licencja CC0) z projektem Azonery.
    ///
    /// Cały pakiet — 221 modeli — dzieli JEDEN atlas tekstur 1024x1024. To fundament wydajności:
    /// jeden materiał dla całego środowiska oznacza, że setki obiektów w scenie mogą trafić
    /// do jednej partii rysowania (static batching + GPU instancing). Dlatego świadomie
    /// odrzucamy materiały wbudowane w pliki FBX i podstawiamy własny, wspólny.
    ///
    /// Assety third-party leżą osobno w <c>Assets/ThirdParty/</c> i nie są mieszane
    /// z autorskimi assetami Azonery — prefaby wynikowe trafiają do przestrzeni projektu.
    /// </summary>
    public static class AzoneraKayKitImporter
    {
        private const string PackRoot = "Assets/ThirdParty/KayKit/MedievalHexagon";
        private const string ModelRoot = PackRoot + "/Models";
        private const string AtlasPath = PackRoot + "/Textures/hexagons_medieval.png";
        private const string MaterialPath = "Assets/Azonera/Materials/KayKit/M_KayKit_Medieval.mat";
        private const string PrefabRoot = "Assets/Azonera/Prefabs/KayKit";

        [MenuItem("Azonera/3. Zaimportuj KayKit (ThirdParty -> prefaby)", priority = 4)]
        public static void ImportMenu()
        {
            if (!AzoneraEditorGuards.EnsureNotPlaying("Zaimportuj KayKit")) return;
            int n = ImportAll();
            EditorUtility.DisplayDialog("Azonera",
                $"KayKit zintegrowany.\nPrefabów: {n}\n\nMateriał: {MaterialPath}\nPrefaby: {PrefabRoot}", "OK");
        }

        public static int ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(ModelRoot))
            {
                Debug.LogError($"[Azonera] Brak pakietu KayKit w {ModelRoot}.");
                return 0;
            }

            var material = BuildSharedMaterial();
            if (material == null) return 0;

            EnsureFolder(PrefabRoot);

            var fbxPaths = new List<string>();
            CollectFbx(ModelRoot, fbxPaths);
            Debug.Log($"[Azonera] KayKit: znaleziono {fbxPaths.Count} modeli.");

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < fbxPaths.Count; i++) ConfigureModel(fbxPaths[i]);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            int made = 0;
            for (int i = 0; i < fbxPaths.Count; i++)
                if (BuildPrefab(fbxPaths[i], material)) made++;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Azonera] KayKit gotowy: {made} prefabów.");
            return made;
        }

        // ------------------------------------------------------------ materiał

        /// <summary>
        /// Jeden materiał URP dla całego pakietu. Atlas jest paletowy (duże, płaskie pola koloru),
        /// więc bez map szorstkości i metaliczności — stylizowany look ma być czysty, nie błyszczący.
        /// </summary>
        private static Material BuildSharedMaterial()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (atlas == null)
            {
                Debug.LogError($"[Azonera] Brak atlasu KayKit: {AtlasPath}");
                return null;
            }

            EnsureFolder(Path.GetDirectoryName(MaterialPath).Replace('\\', '/'));

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", atlas);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", atlas);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            // Instancing: setki drzew i kafli rysowanych jednym wywołaniem.
            mat.enableInstancing = true;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------ import modeli

        private static void ConfigureModel(string fbxPath)
        {
            var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (mi == null) return;

            bool dirty = false;
            // Materiały z FBX byłyby różowe (brak shaderów URP) — podstawiamy własny, wspólny.
            if (mi.materialImportMode != ModelImporterMaterialImportMode.None)
            { mi.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
            if (mi.importNormals != ModelImporterNormals.Import)
            { mi.importNormals = ModelImporterNormals.Import; dirty = true; }
            if (mi.importCameras) { mi.importCameras = false; dirty = true; }
            if (mi.importLights) { mi.importLights = false; dirty = true; }
            if (mi.importAnimation) { mi.importAnimation = false; dirty = true; }
            if (!mi.generateSecondaryUV) { mi.generateSecondaryUV = true; dirty = true; }

            if (dirty) mi.SaveAndReimport();
        }

        // ------------------------------------------------------------ prefaby

        private static bool BuildPrefab(string fbxPath, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null) return false;

            string category = CategoryOf(fbxPath);
            string folder = $"{PrefabRoot}/{category}";
            EnsureFolder(folder);

            string name = Path.GetFileNameWithoutExtension(fbxPath);
            string prefabPath = $"{folder}/P_{name}.prefab";

            var instance = Object.Instantiate(model);
            instance.name = "P_" + name;

            foreach (var rend in instance.GetComponentsInChildren<MeshRenderer>())
            {
                rend.sharedMaterial = material;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                rend.receiveShadows = true;
            }

            AddCollider(instance, category, name);
            MarkStatic(instance);

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return prefab != null;
        }

        /// <summary>
        /// Collider dobrany do roli obiektu — najtańszy, jaki wystarcza.
        /// Chmury i rośliny wodne nie kolidują wcale, drzewa dostają wąską kapsułę wokół pnia
        /// (gracz ma przechodzić pod koroną), reszta środowiska dokładny MeshCollider.
        /// </summary>
        private static void AddCollider(GameObject go, string category, string name)
        {
            string n = name.ToLowerInvariant();

            if (n.StartsWith("cloud") || n.StartsWith("waterlily") || n.StartsWith("waterplant"))
                return;

            var bounds = CombinedBounds(go);
            if (bounds.size == Vector3.zero) return;

            if (n.StartsWith("tree"))
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.direction = 1; // oś Y
                cap.center = new Vector3(bounds.center.x, bounds.size.y * 0.5f, bounds.center.z);
                cap.radius = Mathf.Max(0.12f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.14f);
                cap.height = bounds.size.y;
                return;
            }

            if (category == "props")
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
                return;
            }

            // Budynki, kafle, skały, mury: dokładna geometria. Statyczne MeshCollidery
            // nie kosztują w runtime poza fazą szerokiego odsiewu.
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                var mc = filter.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = filter.sharedMesh;
                mc.convex = false;
            }
        }

        private static Bounds CombinedBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void MarkStatic(GameObject go)
        {
            // Static batching + occlusion culling + GI. Scenografia się nie rusza.
            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI |
                StaticEditorFlags.ReflectionProbeStatic);
            foreach (Transform child in go.transform)
                MarkStatic(child.gameObject);
        }

        // ------------------------------------------------------------ pomocnicze

        private static string CategoryOf(string path)
        {
            string p = path.Replace('\\', '/');
            if (p.Contains("/buildings/")) return "buildings";
            if (p.Contains("/decoration/nature/")) return "nature";
            if (p.Contains("/decoration/props/")) return "props";
            if (p.Contains("/tiles/")) return "tiles";
            return "misc";
        }

        private static void CollectFbx(string folder, List<string> into)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) into.Add(p);
            }
            into.Sort();
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
