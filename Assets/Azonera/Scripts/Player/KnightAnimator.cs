using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Prosty animator proceduralny artykułowanej postaci (KnightRig): idle (oddech/kołysanie) oraz
    /// chód/bieg (naprzemienny wymach nóg, kontr-ruch rąk, ugięcie kolan, bob kroków). Kadencja i amplituda
    /// zależą od <see cref="PlayerController.CurrentSpeed"/>. Zastępowalny prawdziwym Animatorem po podmianie modelu.
    /// </summary>
    [RequireComponent(typeof(KnightRig))]
    public class KnightAnimator : MonoBehaviour
    {
        [SerializeField] private float _legAmp = 30f;   // stopnie wymachu nogi
        [SerializeField] private float _armAmp = 24f;   // stopnie wymachu ramienia
        [SerializeField] private float _speedRef = 4f;  // prędkość „pełnego biegu"

        private KnightRig _rig;
        private PlayerController _pc;
        private Vector3 _rootBasePos;
        private float _phase;

        // bazowe rotacje stawów
        private Quaternion _bHipL, _bHipR, _bKneeL, _bKneeR, _bShL, _bShR, _bElL, _bElR, _bSpine, _bNeck;

        private void Start()
        {
            _rig = GetComponent<KnightRig>();
            _pc = GetComponentInParent<PlayerController>();
            _rootBasePos = transform.localPosition;

            if (_rig.HipL) _bHipL = _rig.HipL.localRotation;
            if (_rig.HipR) _bHipR = _rig.HipR.localRotation;
            if (_rig.KneeL) _bKneeL = _rig.KneeL.localRotation;
            if (_rig.KneeR) _bKneeR = _rig.KneeR.localRotation;
            if (_rig.ShoulderL) _bShL = _rig.ShoulderL.localRotation;
            if (_rig.ShoulderR) _bShR = _rig.ShoulderR.localRotation;
            if (_rig.ElbowL) _bElL = _rig.ElbowL.localRotation;
            if (_rig.ElbowR) _bElR = _rig.ElbowR.localRotation;
            if (_rig.Spine) _bSpine = _rig.Spine.localRotation;
            if (_rig.Neck) _bNeck = _rig.Neck.localRotation;
        }

        private void Update()
        {
            float speed = _pc != null ? _pc.CurrentSpeed : 0f;
            float moving = Mathf.Clamp01(speed / _speedRef);

            // kadencja rośnie z prędkością
            _phase += Time.deltaTime * (3f + speed * 2.2f);
            float s = Mathf.Sin(_phase);
            float legA = moving * _legAmp;
            float armA = moving * _armAmp;

            // Nogi — przeciwne fazy
            Set(_rig.HipL, _bHipL, new Vector3(s * legA, 0, 0));
            Set(_rig.HipR, _bHipR, new Vector3(-s * legA, 0, 0));
            // Kolana — ugięcie, gdy noga w tył (uproszczone)
            Set(_rig.KneeL, _bKneeL, new Vector3(Mathf.Max(0f, -s) * legA * 1.1f, 0, 0));
            Set(_rig.KneeR, _bKneeR, new Vector3(Mathf.Max(0f, s) * legA * 1.1f, 0, 0));

            // Ramiona — kontra do nóg (prawe trzyma miecz → mniejszy wymach)
            Set(_rig.ShoulderL, _bShL, new Vector3(-s * armA, 0, 0));
            Set(_rig.ShoulderR, _bShR, new Vector3(s * armA * 0.5f, 0, 0));
            // Łokcie — stałe ugięcie „gardy" + delikatny ruch
            Set(_rig.ElbowL, _bElL, new Vector3(-18f - moving * 6f, 0, 0));
            Set(_rig.ElbowR, _bElR, new Vector3(-28f - moving * 4f, 0, 0));

            // Kręgosłup — oddech + lekkie pochylenie w ruchu
            float breathe = Mathf.Sin(Time.time * 1.6f) * 1.2f;
            Set(_rig.Spine, _bSpine, new Vector3(breathe + moving * 5f, 0, 0));
            // Szyja — kompensacja pochylenia (patrzy przed siebie)
            Set(_rig.Neck, _bNeck, new Vector3(-moving * 4f, 0, 0));

            // Bob kroków + subtelne kołysanie w idle
            float bob = Mathf.Abs(Mathf.Sin(_phase)) * (0.008f + moving * 0.05f);
            float idleSway = (1f - moving) * Mathf.Sin(Time.time * 1.3f) * 0.6f;
            transform.localPosition = _rootBasePos + new Vector3(0f, bob, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, idleSway);
        }

        private static void Set(Transform joint, Quaternion baseRot, Vector3 addEuler)
        {
            if (joint == null) return;
            joint.localRotation = baseRot * Quaternion.Euler(addEuler);
        }
    }
}
