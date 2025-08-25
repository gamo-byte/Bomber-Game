using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombController : MonoBehaviour
{
    [Header("Bomb")]
    public KeyCode inputKey = KeyCode.Space;
    public GameObject bombPrefab;
    public float bombFuseTime = 3f;
    public int bombAmount = 1;
    private int bombsRemaining;

    [Header("Explosion")]
    public Explosion explosionPrefab;
    public float explosionDuration = 1f;
    public int explosionRadius = 1;

    [Header("Collision Masks")]
    // blockingLayerMask: Solid walls or anything that should STOP the blast
    public LayerMask blockingLayerMask;

    [Header("Destructible")]
    public DestructibleManager destructibleManager;

    private void OnEnable()
    {
        bombsRemaining = bombAmount;
    }

    private void Update()
    {
        if (bombsRemaining > 0 && Input.GetKeyDown(inputKey))
        {
            StartCoroutine(PlaceBomb());
        }
    }

    private IEnumerator PlaceBomb()
    {
        Vector2 position = transform.position;
        position.x = Mathf.Round(position.x);
        position.y = Mathf.Round(position.y);

        GameObject bomb = Instantiate(bombPrefab, position, Quaternion.identity);
        bombsRemaining--;

        yield return new WaitForSeconds(bombFuseTime);

        // Snap again (in case the bomb moved slightly)
        position = bomb.transform.position;
        position.x = Mathf.Round(position.x);
        position.y = Mathf.Round(position.y);

        // Center explosion
        Explosion center = Instantiate(explosionPrefab, position, Quaternion.identity);
        center.setActiveRenderer(center.start);
        center.DestroyAfter(explosionDuration);

        // Spread in 4 directions
        ExplodeLine(position, Vector2.up, explosionRadius);
        ExplodeLine(position, Vector2.down, explosionRadius);
        ExplodeLine(position, Vector2.left, explosionRadius);
        ExplodeLine(position, Vector2.right, explosionRadius);

        Destroy(bomb);
        bombsRemaining++;
    }

    // Walk step-by-step and handle blocking vs. destructible separately
    private void ExplodeLine(Vector2 origin, Vector2 direction, int length)
    {
        for (int i = 1; i <= length; i++)
        {
            Vector2 pos = origin + direction * i;

            // 1) If we hit a blocking collider, stop the propagation in this direction
            //    (size 1x1 assumes your tile cell size is 1; adjust if needed)
            if (Physics2D.OverlapBox(pos, Vector2.one * 0.5f, 0f, blockingLayerMask))
                break;

            // 2) Spawn explosion segment
            Explosion segment = Instantiate(explosionPrefab, pos, Quaternion.identity);
            segment.setActiveRenderer(i < length ? segment.middle : segment.end);
            segment.SetDirection(direction);
            segment.DestroyAfter(explosionDuration);

            // 3) Clear destructible tile at this cell (if there is one) and KEEP GOING
            ClearDestructible(pos);
        }
    }

    private void ClearDestructible(Vector2 position)
    {
        // Deal 1 damage to any block type at this position; keep ray going regardless.
        if (destructibleManager != null)
        {
            destructibleManager.DamageAtWorldPosition(position, 1);
        }
    }

    // Let player walk off the bomb before it becomes solid
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Bomb"))
        {
            other.isTrigger = false;
        }
    }

    public void AddBomb()
    {
        bombAmount++;
        bombsRemaining++;
    }
}
