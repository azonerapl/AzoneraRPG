using UnityEngine;
using Azonera.Items;
using Azonera.Inventory;

namespace Azonera.Loot
{
    /// <summary>
    /// Przedmiot leżący w świecie. Gdy gracz podejdzie w zasięg zbierania,
    /// trafia do plecaka. Tworzony przez <see cref="Spawn"/> (np. z lootu potwora).
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        public ItemData Item;
        public int Count = 1;

        private static readonly Color GlowFallback = new Color(0.95f, 0.8f, 0.35f);
        private float _pickupRadius = 1.4f;
        private float _spin = 45f;
        private Transform _visual;

        public static ItemPickup Spawn(ItemData item, int count, Vector3 position)
        {
            var go = new GameObject($"Pickup_{(item != null ? item.DisplayName : "Item")}");
            position.y = 0.35f;
            go.transform.position = position + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));

            var pickup = go.AddComponent<ItemPickup>();
            pickup.Item = item;
            pickup.Count = count;

            // Placeholder wizualny — mały świecący klejnot
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.35f;
            visual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            Object.Destroy(visual.GetComponent<Collider>());
            var rend = visual.GetComponent<Renderer>();
            var col = item != null ? item.RarityColor : GlowFallback;
            rend.material.color = col;
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", col * 1.5f);
            pickup._visual = visual.transform;

            return pickup;
        }

        // Gracz jest jeden na scenę — szukamy go raz, a nie co klatkę dla każdego leżącego itemu.
        // Przy kilkudziesięciu dropach FindWithTag w Update() był realnym kosztem.
        private static Transform _playerTransform;
        private static Azonera.Inventory.Inventory _playerInventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlayerCache()
        {
            _playerTransform = null;
            _playerInventory = null;
        }

        private static bool ResolvePlayer()
        {
            if (_playerTransform != null && _playerInventory != null) return true;
            var go = GameObject.FindWithTag("Player");
            if (go == null) return false;
            _playerTransform = go.transform;
            _playerInventory = go.GetComponent<Azonera.Inventory.Inventory>();
            return _playerInventory != null;
        }

        private void Update()
        {
            if (_visual != null) _visual.Rotate(Vector3.up, _spin * Time.deltaTime, Space.World);

            if (Item == null || !ResolvePlayer()) return;

            // Porównanie kwadratów dystansu — bez pierwiastkowania co klatkę.
            if ((transform.position - _playerTransform.position).sqrMagnitude > _pickupRadius * _pickupRadius)
                return;

            int added = _playerInventory.AddItem(Item, Count);
            if (added <= 0) return; // plecak pełny — item zostaje na ziemi

            Debug.Log($"[Loot] Podniesiono: {Item.DisplayName} x{added}");
            Destroy(gameObject);
        }
    }
}
