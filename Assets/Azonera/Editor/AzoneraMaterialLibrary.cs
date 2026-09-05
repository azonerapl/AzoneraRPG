#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Współdzielona fabryka materiałów PBR dla generatorów scen (loch, wioska, przyszłe lokacje).
    /// Jedno miejsce, w którym powstają materiały = jeden spójny standard wizualny (Art Bible §3):
    /// Base Color + Normal + Ambient Occlusion + Smoothness/Metallic, poprawne przestrzenie barw.
    ///
    /// Tekstury leżą w <c>Assets/Azonera/Art/Textures/&lt;slug&gt;/&lt;slug&gt;_&lt;mapa&gt;_1k.jpg</c>
    /// (konwencja bibliotek CC0). Import konfiguruje <c>AzoneraTextureImporter</c>.
    /// Materiał bez slugu = jednolity kolor (świadomy placeholder nastrojowy, nie finał).
    /// </summary>
    public static class AzoneraMaterialLibrary
    {
        public const string TextureRoot = "Assets/Azonera/Art/Textures";

        /// <summary>Mapy szukane dla każdego zestawu tekstur.</summary>
        private static readonly string[] MapSuffixes = { "diff", "nor_gl", "ao", "rough" };

        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        /// <summary>Czyści pamięć podręczną — wołać na starcie każdej budowy sceny.</summary>
        public static void ResetCache() => _cache.Clear();

        /// <summary>
        /// Zwraca (tworząc lub aktualizując) materiał o podanej nazwie w podanym folderze.
        /// </summary>
        /// <param name="texSlug">Nazwa katalogu zestawu tekstur; null = materiał jednolity.</param>
        /// <param name="tiling">Ile razy tekstura powtarza się na jednostkę UV obiektu.</param>
        public static Material Get(string folder, string name, Color color, float smoothness,
                                   float metallic = 0f, Color emission = default,
                                   string texSlug = null, float tiling = 1f)
        {
            string key = folder + "/" + name;
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            string path = $"{folder}/M_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            ApplySurface(mat, color, smoothness, metallic, emission, texSlug, tiling);

            EditorUtility.SetDirty(mat);
            _cache[key] = mat;
            return mat;
        }

        private static void ApplySurface(Material mat, Color color, float smoothness, float metallic,
                                         Color emission, string texSlug, float tiling)
        {
            bool textured = !string.IsNullOrEmpty(texSlug);

            // Przy teksturze albedo musi być niemal białe — inaczej przyciemniamy prawdziwe kolory mapy.
            Color baseCol = textured ? new Color(0.95f, 0.95f, 0.95f) : color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (textured) ApplyMaps(mat, texSlug, tiling);
            else ClearMaps(mat);

            if (emission != default)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
        }

        private static void ApplyMaps(Material mat, string slug, float tiling)
        {
            var diff = LoadMap(slug, "diff");
            var normal = LoadMap(slug, "nor_gl");
            var ao = LoadMap(slug, "ao");

            if (diff != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", diff);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", diff);
            }

            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
                if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1f);
            }

            if (ao != null && mat.HasProperty("_OcclusionMap"))
            {
                mat.SetTexture("_OcclusionMap", ao);
                mat.EnableKeyword("_OCCLUSIONMAP");
                if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", 1f);
            }

            var scale = new Vector2(tiling, tiling);
            if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", scale);
            if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", scale);
        }

        private static void ClearMaps(Material mat)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
            if (mat.HasProperty("_BumpMap")) { mat.SetTexture("_BumpMap", null); mat.DisableKeyword("_NORMALMAP"); }
            if (mat.HasProperty("_OcclusionMap")) { mat.SetTexture("_OcclusionMap", null); mat.DisableKeyword("_OCCLUSIONMAP"); }
        }

        private static Texture2D LoadMap(string slug, string map)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureRoot}/{slug}/{slug}_{map}_1k.jpg");
        }

        /// <summary>
        /// Czy zestaw tekstur o podanym slugu jest dostępny w projekcie.
        /// Pozwala generatorom łagodnie degradować się do materiału jednolitego.
        /// </summary>
        public static bool HasTextureSet(string slug)
        {
            return !string.IsNullOrEmpty(slug) && LoadMap(slug, "diff") != null;
        }

        /// <summary>
        /// Wymusza reimport zestawów tekstur, żeby zadziałał <c>AzoneraTextureImporter</c>
        /// (normalki jako NormalMap, AO/rough w przestrzeni liniowej). Bez tego świeżo
        /// wrzucone pliki mogą być zaimportowane z domyślnymi, błędnymi ustawieniami.
        /// </summary>
        public static void EnsureImported(params string[] slugs)
        {
            foreach (var slug in slugs)
            {
                foreach (var map in MapSuffixes)
                {
                    string path = $"{TextureRoot}/{slug}/{slug}_{map}_1k.jpg";
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
#endif
