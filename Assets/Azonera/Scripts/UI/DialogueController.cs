using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Azonera.UI
{
    /// <summary>
    /// Minimalny, rozbudowywalny kontroler dialogu. Pokazuje panel z imieniem NPC
    /// i kolejnymi liniami; klik przewija. Blokuje input świata na czas rozmowy.
    /// </summary>
    public class DialogueController : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _lineText;
        [SerializeField] private Text _hintText;

        private string[] _lines;
        private int _index;
        private bool _open;
        private bool _consumeFirstClick;

        public bool IsOpen => _open;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        public void Show(string npcName, string[] lines)
        {
            if (lines == null || lines.Length == 0) return;
            _lines = lines;
            _index = 0;
            _open = true;
            _consumeFirstClick = true;
            UIState.ModalOpen = true;

            if (_panel != null) _panel.SetActive(true);
            if (_nameText != null) _nameText.text = npcName;
            if (_hintText != null) _hintText.text = "▶ Klik / Spacja — dalej";
            Render();
        }

        private void Update()
        {
            if (!_open) return;

            bool advance = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) advance = true;
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                advance = true;

            if (_consumeFirstClick) { _consumeFirstClick = false; return; }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (!advance) return;

            _index++;
            if (_index >= _lines.Length) Close();
            else Render();
        }

        private void Render()
        {
            if (_lineText != null && _lines != null && _index < _lines.Length)
                _lineText.text = _lines[_index];
        }

        public void Close()
        {
            _open = false;
            UIState.ModalOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
