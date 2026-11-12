using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class DungeonPlayerController : MonoBehaviour
{
    public DungeonTaleGame game;
    public float moveSpeed = 5f;
    public float attackRange = 1.35f;
    public float attackCooldown = 0.35f;

    private Rigidbody2D body;
    private Vector2 movement;
    private float nextAttack;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (game != null && game.IsFinished())
        {
            movement = Vector2.zero;
            return;
        }

        movement = ReadMovement();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && Time.time >= nextAttack)
        {
            nextAttack = Time.time + attackCooldown;
            Attack();
        }
    }

    Vector2 ReadMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    void FixedUpdate()
    {
        if (body == null) return;
        body.MovePosition(body.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    void Attack()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D hit in hits)
        {
            DungeonEnemy enemy = hit.GetComponent<DungeonEnemy>();
            if (enemy != null) enemy.TakeHit();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
