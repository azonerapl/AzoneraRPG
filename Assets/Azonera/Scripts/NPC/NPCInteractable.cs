using UnityEngine;
using Azonera.Combat;
using Azonera.UI;

namespace Azonera.NPC
{
    /// <summary>
    /// Podstawowy NPC z dialogiem. Kliknięcie w zasięgu otwiera rozmowę.
    /// Rozbudowywalny: quest hooki, sklep, więcej gałęzi dialogowych.
    /// </summary>
    public class NPCInteractable : MonoBehaviour, IInteractable
    {
        [Header("Tożsamość")]
        public string NpcName = "Guide Alwin";
        [Tooltip("Kolejne linie dialogu — klik przewija.")]
        [TextArea] public string[] DialogueLines = new[]
        {
            "Witaj, wędrowcze. Witaj w Azonerze.",
            "Te ziemie nie są bezpieczne — poza wioską czają się bestie.",
            "Weź miecz, zabij kilka z nich i wróć silniejszy.",
            "Powodzenia. Będę tu, gdybyś potrzebował rady."
        };

        [Header("Interakcja")]
        [SerializeField] private float _interactionRange = 3.5f;

        public float InteractionRange => _interactionRange;
        public string InteractionPrompt => $"Porozmawiaj z {NpcName}";

        public void Interact(GameObject interactor)
        {
            var ui = FindAnyObjectByType<DialogueController>();
            if (ui != null) ui.Show(NpcName, DialogueLines);
            else Debug.Log($"[NPC] {NpcName}: {(DialogueLines.Length > 0 ? DialogueLines[0] : "...")}");
        }
    }
}
