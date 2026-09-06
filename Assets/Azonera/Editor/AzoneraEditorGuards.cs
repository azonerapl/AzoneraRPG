#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Azonera.EditorTools
{
    /// <summary>
    /// Wspólne zabezpieczenia narzędzi edytorskich Azonery.
    ///
    /// Powód istnienia: generatory scen zaczynają od <c>EditorSceneManager.NewScene()</c>,
    /// które w trybie PLAY rzuca <c>InvalidOperationException</c> i przerywa budowę w połowie —
    /// użytkownik zostaje z pustą sceną Untitled i myśli, że projekt jest zepsuty.
    /// Zamiast wyjątku pokazujemy zrozumiały komunikat i przerywamy bezpiecznie.
    /// </summary>
    public static class AzoneraEditorGuards
    {
        /// <summary>
        /// Zwraca true, gdy narzędzie MOŻE działać. W trybie PLAY zwraca false
        /// i tłumaczy użytkownikowi, co zrobić.
        /// </summary>
        public static bool EnsureNotPlaying(string toolName)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return true;

            Debug.LogWarning(
                "[Azonera] '" + toolName + "' nie dziala w trybie PLAY — zatrzymaj gre i sprobuj ponownie.");

            EditorUtility.DisplayDialog(
                "Azonera — zatrzymaj tryb PLAY",
                "'" + toolName + "' buduje scene od zera, czego Unity nie pozwala robic w trakcie gry.\n\n" +
                "1. Zatrzymaj tryb PLAY (kwadrat u gory edytora)\n" +
                "2. Uruchom to polecenie ponownie\n\n" +
                "Uwaga: gotowe sceny sa zapisane na dysku — zwykle wystarczy otworzyc\n" +
                "Assets/Azonera/Scenes/AzoneraTemple.unity i nacisnac PLAY, bez regeneracji.",
                "OK");
            return false;
        }
    }
}
#endif
