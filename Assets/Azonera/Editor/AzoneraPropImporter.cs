#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Zamienia surowe paczki CC0 (Poly Haven) w gotowe do użycia prefaby rekwizytów Azonery.
    ///
    /// Dla każdego katalogu <c>Art/Props/&lt;slug&gt;/</c> zawierającego siatkę FBX:
    /// 1. buduje materiał URP Lit z map diff / nor_gl / ARM,
    /// 2. rozpakowuje mapę ARM na kanały, których faktycznie używa URP,
    /// 3. tworzy prefab <c>Prefabs/Props/P_&lt;slug&gt;.prefab</c> z colliderem.
    ///
    /// Dlaczego rozpakowanie ARM jest konieczne: Poly Haven pakuje AO/Roughness/Metallic
    /// w kanały R/G/B jednej tekstury. URP oczekuje zupełnie innego układu —
    /// metaliczność w R i GŁADKOŚĆ (nie szorstkość!) w kanale alfa mapy metalicznej,
    /// oraz AO w kanale G osobnej mapy. Podanie ARM wprost daje metaliczne, świecące
    /// śmieci zamiast drewna. Konwersję robimy raz, przy imporcie.
    /// </summary>
    public static class AzoneraPropImporter
    {
        private const string PropsRoot = "Assets/Azonera/Art/Props";
        private const string MaterialRoot = "Assets/Azonera/Materials/Props";
        private const string PrefabRoot = "Assets/Azonera/Prefabs/Props";

        [MenuItem("Azonera/2. Zaimportuj rekwizyty (Art/Props → prefaby)", priority = 3)]
        public static void ImportPropsMenu()
        {
            if (!AzoneraEditorGuards.EnsureNotPlaying("Zaimportuj rekwizyty")) return;
            int count = ImportAll();
            EditorUtility.DisplayDialog("Azonera",
                $"Gotowe. Przygotowano rekwizytów: {count}.\n\nPrefaby: {PrefabRoot}", "OK");
        }

        public static int ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(PropsRoot))
            {
                Debug.LogWarning($"[Azonera] Brak katalogu {PropsRoot} — nie ma czego importować.");
                return 0;
            }

            EnsureFolder(MaterialRoot);
            EnsureFolder(PrefabRoot);

            int made = 0;
            foreach (var dir in AssetDatabase.GetSubFolders(PropsRoot))
            {
                string slug = Path.GetFileName(dir);
                if (BuildProp(dir, slug)) made++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Azonera] Rekwizyty gotowe: {made}.");
            return made;
        }

        private static bool BuildProp(string dir, string slug)
        {
            string fbx = FindFirst(dir, ".fbx");
            if (fbx == null) return false;

            ConfigureModelImporter(fbx);

            var material = BuildMaterial(dir, slug);
            var prefab = BuildPrefab(fbx, slug, material);
            return prefab != null;
        }

        // ------------------------------------------------------------ import siatki

        private static void ConfigureModelImporter(string fbxPath)
        {
            var mi = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (mi == null) return;

            bool dirty = false;
            // Materiały budujemy sami — wbudowane z FBX byłyby różowe (brak shaderów URP).
            if (mi.materialImportMode != ModelImporterMaterialImportMode.None)
            { mi.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
            if (!mi.importNormals.Equals(ModelImporterNormals.Import))
            { mi.importNormals = ModelImporterNormals.Import; dirty = true; }
            if (!mi.generateSecondaryUV)
            { mi.generateSecondaryUV = true; dirty = true; }   // lightmapy w przyszłości
            if (mi.importCameras) { mi.importCameras = false; dirty = true; }
            if (mi.importLights) { mi.importLights = false; dirty = true; }

            if (dirty) mi.SaveAndReimport();
        }

        // ------------------------------------------------------------ materiał

        private static Material BuildMaterial(string dir, string slug)
        {
            string matPath = $"{MaterialRoot}/M_{slug}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var diff = LoadTexture(dir, "_diff");
            var norm = LoadTexture(dir, "_nor_gl");
            var arm = LoadTexture(dir, "_arm");

            if (diff != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diff);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);

            if (norm != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", norm);
                mat.EnableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1f);
            }

            if (arm != null)
            {
                var (metalSmooth, occlusion) = UnpackArm(arm, dir, slug);

                if (metalSmooth != null && mat.HasProperty("_MetallicGlossMap"))
                {
                    mat.SetTexture("_MetallicGlossMap", metalSmooth);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    // Przy mapie metalicznej mnożniki muszą być 1, inaczej ją wygaszają.
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 1f);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);
                    if (mat.HasProperty("_SmoothnessTextureChannel"))
                        mat.SetFloat("_SmoothnessTextureChannel", 0f); // alfa mapy metalicznej
                }

                if (occlusion != null && mat.HasProperty("_OcclusionMap"))
                {
                    mat.SetTexture("_OcclusionMap", occlusion);
                    mat.EnableKeyword("_OCCLUSIONMAP");
                    if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", 1f);
                }
            }
            else
            {
                // Brak ARM — sensowne wartości dla rekwizytów z drewna/kamienia.
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Rozpakowuje ARM (R=AO, G=Roughness, B=Metallic) na układ oczekiwany przez URP:
        /// mapa metaliczna (R=metallic, A=smoothness) oraz mapa okluzji (G=AO).
        /// Wyniki zapisujemy jako PNG obok źródła, żeby liczyć to tylko raz.
        /// </summary>
        private static (Texture2D metalSmooth, Texture2D occlusion) UnpackArm(Texture2D arm, string dir, string slug)
        {
            string msPath = $"{dir}/{slug}_metallicsmoothness.png";
            string aoPath = $"{dir}/{slug}_occlusion.png";

            var existingMs = AssetDatabase.LoadAssetAtPath<Texture2D>(msPath);
            var existingAo = AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath);
            if (existingMs != null && existingAo != null) return (existingMs, existingAo);

            if (!arm.isReadable)
            {
                Debug.LogWarning($"[Azonera] Mapa ARM '{slug}' nie jest czytelna — pomijam rozpakowanie.");
                return (null, null);
            }

            int w = arm.width, h = arm.height;
            var src = arm.GetPixels();
            var ms = new Color[src.Length];
            var ao = new Color[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                float occ = src[i].r;
                float rough = src[i].g;
                float metal = src[i].b;
                ms[i] = new Color(metal, metal, metal, 1f - rough); // smoothness = 1 - roughness
                ao[i] = new Color(occ, occ, occ, 1f);
            }

            WritePng(msPath, w, h, ms);
            WritePng(aoPath, w, h, ao);
            AssetDatabase.ImportAsset(msPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(aoPath, ImportAssetOptions.ForceUpdate);

            return (AssetDatabase.LoadAssetAtPath<Texture2D>(msPath),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath));
        }

        private static void WritePng(string assetPath, int w, int h, Color[] pixels)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ------------------------------------------------------------ prefab

        private static GameObject BuildPrefab(string fbxPath, string slug, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null) return null;

            string prefabPath = $"{PrefabRoot}/P_{slug}.prefab";
            var instance = Object.Instantiate(model);
            instance.name = "P_" + slug;

            foreach (var rend in instance.GetComponentsInChildren<MeshRenderer>())
            {
                if (material != null) rend.sharedMaterial = material;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                rend.receiveShadows = true;
            }

            // Collider na korzeniu — rekwizyty mają blokować ruch i łapać raycasty kliknięć.
            var filter = instance.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                var box = instance.AddComponent<BoxCollider>();
                var bounds = filter.sharedMesh.bounds;
                box.center = bounds.center;
                box.size = bounds.size;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // ------------------------------------------------------------ pomocnicze

        private static Texture2D LoadTexture(string dir, string suffix)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { dir }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                if (name.Contains(suffix)) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            return null;
        }

        private static string FindFirst(string dir, string extension)
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                if (file.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase))
                    return file.Replace('\\', '/');
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
