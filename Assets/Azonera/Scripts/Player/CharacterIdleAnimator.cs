using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Lekka „żywotność" postaci bez riggu: delikatny bob (oddech) i kołysanie, wzmocnione podczas ruchu
    /// (czyta prędkość z PlayerController, jeśli jest w hierarchii). Animuje przypisany węzeł wizualny.
    /// Zastępowalne prawdziwym Animatorem po podmianie na docelowy model.
    /// </summary>
    public class CharacterIdleAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _bobAmplitude = 0.02f;
        [SerializeField] private float _swayAmplitude = 1.2f;

        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;
        private Transform _torso;
        private Vector3 _torsoBaseScale;
        private float _seed;
        private PlayerController _pc;

        private void Start()
        {
            if (_target == null) _target = transform;
            _baseLocalPos = _target.localPosition;
            _baseLocalRot = _target.localRotation;
            _torso = _target.Find("Torso");
            if (_torso != null) _torsoBaseScale = _torso.localScale;
            _seed = Random.value * 10f;
            _pc = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (_target == null) return;

            float moving = _pc != null ? Mathf.Clamp01(_pc.CurrentSpeed) : 0f;
            float amp = _bobAmplitude * (1f + moving * 2.5f);
            float speed = Mathf.Lerp(1.9f, 6f, moving);
            float bob = Mathf.Abs(Mathf.Sin(Time.time * speed + _seed)) * amp;
            _target.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);

            float sway = Mathf.Sin(Time.time * 1.4f + _seed) * _swayAmplitude;
            _target.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, sway);

            if (_torso != null)
            {
                float breathe = 1f + Mathf.Sin(Time.time * 1.6f + _seed) * 0.02f;
                _torso.localScale = new Vector3(_torsoBaseScale.x, _torsoBaseScale.y * breathe, _torsoBaseScale.z);
            }
        }
    }
}
