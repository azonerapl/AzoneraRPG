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

        private void Update()
        {
            if (_visual != null) _visual.Rotate(Vector3.up, _spin * Time.deltaTime, Space.World);

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo == null) return;
            if (Vector3.Distance(transform.position, playerGo.transform.position) <= _pickupRadius)
            {
                var inv = playerGo.GetComponent<Azonera.Inventory.Inventory>();
                if (inv != null && Item != null)
                {
                    int added = inv.AddItem(Item, Count);
                    if (added > 0)
                    {
                        Debug.Log($"[Loot] Podniesiono: {Item.DisplayName} x{added}");
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
