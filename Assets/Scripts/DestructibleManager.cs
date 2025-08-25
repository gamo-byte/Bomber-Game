using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class DestructibleBlockType
{
    public string id;
    public Tilemap tilemap;
    [Min(1)] public int maxHealth = 1;

    [Header("Per-HP visuals")]
    // stageTiles[hp-1] will be used for the current hp (hp from 1..maxHealth)
    public TileBase[] stageTiles;

    [Header("Break FX / Loot (Optional)")]
    public GameObject breakVfxOrLootPrefab;
    [Range(0f, 1f)] public float itemSpawnChance = 0f;
    public GameObject[] spawnableItems;
}
public class DestructibleManager : MonoBehaviour
{
    public DestructibleBlockType[] blockTypes;

    // Per-tile current HP cache per tilemap
    private readonly Dictionary<Tilemap, Dictionary<Vector3Int, int>> _hpMaps =
        new Dictionary<Tilemap, Dictionary<Vector3Int, int>>();

    private void Awake()
    {
        // Initialize hp maps
        foreach (var bt in blockTypes)
        {
            if (bt.tilemap == null) continue;
            if (!_hpMaps.ContainsKey(bt.tilemap))
                _hpMaps[bt.tilemap] = new Dictionary<Vector3Int, int>();
        }
    }

    /// <summary>
    /// Apply damage at world position; returns true if any tile was destroyed.
    /// </summary>
    public bool DamageAtWorldPosition(Vector2 worldPos, int damage = 1)
    {
        bool destroyedAny = false;

        foreach (var bt in blockTypes)
        {
            if (bt.tilemap == null) continue;

            Vector3Int cell = bt.tilemap.WorldToCell(worldPos);
            TileBase tile = bt.tilemap.GetTile(cell);
            if (tile == null) continue;

            // get or init hp
            var map = _hpMaps[bt.tilemap];
            if (!map.TryGetValue(cell, out int hp))
                hp = bt.maxHealth;

            hp -= Mathf.Max(1, damage);

            if (hp <= 0)
            {
                bt.tilemap.SetTile(cell, null);
                map.Remove(cell);
                destroyedAny = true;

                Vector3 spawnPos = bt.tilemap.GetCellCenterWorld(cell);

                // Spawn break VFX (optional)
                if (bt.breakVfxOrLootPrefab != null)
                {
                    Instantiate(bt.breakVfxOrLootPrefab, spawnPos, Quaternion.identity);
                }

                // Spawn loot (independent of VFX)
                if (bt.spawnableItems != null && bt.spawnableItems.Length > 0 && bt.itemSpawnChance > 0f)
                {
                    // roll ∈ [0,1); using <= ensures chance=1 always spawns
                    float roll = Random.value;
                    if (roll <= bt.itemSpawnChance)
                    {
                        int idx = Random.Range(0, bt.spawnableItems.Length);
                        Instantiate(bt.spawnableItems[idx], spawnPos, Quaternion.identity);
                        // Debug.Log($"Loot spawned ({bt.id}) roll={roll}, chance={bt.itemSpawnChance}");
                    }
                }
            }
            else
            {
                // update remaining hp
                map[cell] = hp;

                // update visual by hp
                // stageTiles[hp-1] must exist; fallback safety if not assigned fully
                if (bt.stageTiles != null && bt.stageTiles.Length >= bt.maxHealth)
                {
                    var newTile = bt.stageTiles[Mathf.Clamp(hp - 1, 0, bt.stageTiles.Length - 1)];
                    bt.tilemap.SetTile(cell, newTile);
                }
            }
        }

        return destroyedAny;
    }
}

