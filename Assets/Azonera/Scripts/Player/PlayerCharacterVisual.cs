using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Podmienia debugową kapsułę gracza na uzbrojoną postać rycerza (CharacterVisualFactory).
    /// Działa w istniejącej scenie bez regeneracji: chowa węzły „Visual"/„Facing" i buduje model jako dziecko.
    /// Węzeł „KnightVisual" jest gotowy pod późniejszą podmianę na docelowy model/render (np. sprite z generacji).
    /// </summary>
    public class PlayerCharacterVisual : MonoBehaviour
    {
        [SerializeField] private bool _hideDebugCapsule = true;
        private GameObject _visual;

        private void Start()
        {
            if (_visual != null) return;

            if (_hideDebugCapsule)
            {
                foreach (Transform child in transform)
                {
                    if (child.name == "Visual" || child.name == "Visual_DEBUG" || child.name == "Facing")
                        child.gameObject.SetActive(false);
                }
            }

            _visual = CharacterVisualFactory.BuildKnight(transform);
        }
    }
}
