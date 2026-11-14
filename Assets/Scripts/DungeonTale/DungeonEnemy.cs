using UnityEngine;
public class DungeonEnemy : MonoBehaviour
{
    public DungeonTaleGame game; public Transform target; public float speed=1.6f;
    private float attackTimer;
    private int health = 2;
    private int maxHealth = 2;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Vector2 moveDirection;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Enemies must use solid colliders. Trigger colliders do not stop at walls.
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.isTrigger = false;
    }

    public void SetHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        health = maxHealth;
    }
    void Update()
    {
        moveDirection = Vector2.zero;
        if (target == null || game == null || game.IsFinished()) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > 0.8f)
        {
            moveDirection = ((Vector2)target.position - body.position).normalized;
        }
        else if (Time.time >= attackTimer)
        {
            attackTimer = Time.time + 1.1f;
            game.DamagePlayer();
        }
    }

    void FixedUpdate()
    {
        if (body == null || moveDirection.sqrMagnitude < 0.001f) return;

        // Rigidbody movement respects the solid TilemapCollider2D walls.
        Vector2 nextPosition = body.position + moveDirection * speed * Time.fixedDeltaTime;
        body.MovePosition(nextPosition);
    }
    public void TakeHit()
    {
        health--;
        if (health <= 0)
        {
            game.EnemyDefeated();
            Destroy(gameObject);
        }
        else
        {
            transform.localScale *= .88f;
        }
    }

    // Draw a small health bar above this monster in screen space.
    void OnGUI()
    {
        if (health <= 0 || Camera.main == null) return;

        float worldTop = spriteRenderer != null
            ? spriteRenderer.bounds.max.y + 0.18f
            : transform.position.y + 0.8f;
        Vector3 screen = Camera.main.WorldToScreenPoint(
            new Vector3(transform.position.x, worldTop, transform.position.z));

        if (screen.z <= 0f) return;

        const float width = 42f;
        const float height = 6f;
        float x = screen.x - width * 0.5f;
        float y = Screen.height - screen.y - height;

        GUI.color = new Color(0.04f, 0.04f, 0.05f, 0.9f);
        GUI.DrawTexture(new Rect(x - 1f, y - 1f, width + 2f, height + 2f), Texture2D.whiteTexture);

        GUI.color = new Color(0.25f, 0.08f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

        float ratio = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
        GUI.color = ratio > 0.5f ? new Color(0.2f, 0.9f, 0.25f) :
                    ratio > 0.25f ? new Color(1f, 0.65f, 0.1f) :
                    new Color(0.95f, 0.12f, 0.1f);
        GUI.DrawTexture(new Rect(x, y, width * ratio, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}