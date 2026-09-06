#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Automatyczne ustawienia importu tekstur CC0 w <c>Assets/Azonera/Art/</c>.
    ///
    /// Bez tego materiały PBR wyglądają źle w sposób trudny do zdiagnozowania:
    /// mapa normalnych zaimportowana jako zwykły kolor daje płaską powierzchnię,
    /// a mapy danych (AO/Rough/Metal/ARM) wczytane jako sRGB rozjaśniają się
    /// nieliniowo i psują odbicia. Konfigurujemy to raz, przy imporcie.
    /// </summary>
    public class AzoneraTextureImporter : AssetPostprocessor
    {
        /// <summary>Sufiksy map DANYCH — muszą być w przestrzeni liniowej, nie sRGB.</summary>
        private static readonly string[] LinearSuffixes =
        {
            "_ao", "_rough", "_arm", "_metal", "_disp", "_gloss", "_spec",
            "_metallicsmoothness", "_occlusion"
        };

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Azonera/Art/")) return;

            var ti = (TextureImporter)assetImporter;
            string f = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            if (f.Contains("_nor") || f.EndsWith("_normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else if (ContainsAny(f, LinearSuffixes))
            {
                ti.sRGBTexture = false;
                // Mapy ARM muszą być czytelne z kodu — rozpakowujemy je na kanały URP.
                if (f.Contains("_arm")) ti.isReadable = true;
            }

            ti.streamingMipmaps = true;
            ti.anisoLevel = 4;
        }

        private static bool ContainsAny(string value, string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
                if (value.Contains(needles[i])) return true;
            return false;
        }
    }
}
#endif
