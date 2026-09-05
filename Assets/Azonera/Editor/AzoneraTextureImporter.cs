#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Automatyczne ustawienia importu tekstur CC0 w Assets/Azonera/Art/Textures:
    /// mapy normalnych → NormalMap; AO/Rough/ARM/Metal/Disp → Linear (nie sRGB); albedo (diff) → sRGB (domyślnie).
    /// Dzięki temu materiały PBR wyglądają poprawnie bez ręcznej konfiguracji każdej tekstury.
    /// </summary>
    public class AzoneraTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Azonera/Art/Textures/")) return;

            var ti = (TextureImporter)assetImporter;
            string f = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            if (f.Contains("_nor") || f.EndsWith("_normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else if (f.Contains("_ao") || f.Contains("_rough") || f.Contains("_arm") ||
                     f.Contains("_metal") || f.Contains("_disp") || f.Contains("_gloss"))
            {
                ti.sRGBTexture = false; // mapy danych — przestrzeń liniowa
            }
            ti.streamingMipmaps = true;
        }
    }
}
#endif
