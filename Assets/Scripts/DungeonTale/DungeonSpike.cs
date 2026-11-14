using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class DungeonSpike : MonoBehaviour
{
    public DungeonTaleGame game;
    private float nextHit;

    void Awake()
    {
        SyncHitboxToTrap();
    }

    void OnValidate()
    {
        SyncHitboxToTrap();
    }

    // Keep the damage trigger matched to the actual sprite dimensions.
    // Transform scaling then resizes the art and trigger together.
    public void SyncHitboxToTrap()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        var hitbox = GetComponent<BoxCollider2D>();
        if (!spriteRenderer || !spriteRenderer.sprite || !hitbox) return;

        hitbox.size = spriteRenderer.sprite.bounds.size * 0.85f;
        hitbox.offset = spriteRenderer.sprite.bounds.center;
        hitbox.isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponent<DungeonPlayerController>() && Time.time >= nextHit)
        {
            nextHit = Time.time + 1.0f;
            if (game) game.HitTrap();
        }
    }
}
