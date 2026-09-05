using UnityEngine;

namespace Azonera.Player
{
    /// <summary>
    /// Referencje stawów artykułowanej postaci (rig z brył). Wypełniane przez CharacterVisualFactory,
    /// czytane przez KnightAnimator. Dzięki temu animacja nie szuka niczego po nazwach.
    /// </summary>
    public class KnightRig : MonoBehaviour
    {
        public Transform Spine;
        public Transform Neck;
        public Transform HipL, HipR;
        public Transform KneeL, KneeR;
        public Transform ShoulderL, ShoulderR;
        public Transform ElbowL, ElbowR;
    }
}
