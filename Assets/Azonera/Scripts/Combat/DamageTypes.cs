using UnityEngine;

namespace Azonera.Combat
{
    /// <summary>Kategoria obrażeń w systemie walki Azonery.</summary>
    public enum DamageType
    {
        Physical,
        Magic,
        True // ignoruje obronę / pancerz
    }

    /// <summary>
    /// Pakiet informacji o pojedynczym trafieniu. Tworzony przez atakującego,
    /// konsumowany przez <see cref="IDamageable"/>. Trzymamy tu wszystko, co
    /// serwer MMO musiałby zwalidować w przyszłości.
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public bool IsCritical;
        public GameObject Source;     // kto zadał obrażenia
        public Vector3 HitPoint;      // gdzie (dla VFX)

        public DamageInfo(float amount, DamageType type, GameObject source, bool isCritical = false, Vector3 hitPoint = default)
        {
            Amount = amount;
            Type = type;
            Source = source;
            IsCritical = isCritical;
            HitPoint = hitPoint;
        }
    }

    /// <summary>Kontrakt dla wszystkiego, co może otrzymać obrażenia i zginąć.</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void ApplyDamage(DamageInfo info);
        Transform Transform { get; }
    }

    /// <summary>Kontrakt dla obiektów, z którymi gracz może wejść w interakcję (NPC, drzwi, loot).</summary>
    public interface IInteractable
    {
        float InteractionRange { get; }
        void Interact(GameObject interactor);
        string InteractionPrompt { get; }
    }
}
