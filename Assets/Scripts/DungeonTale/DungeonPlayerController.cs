using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class DungeonPlayerController : MonoBehaviour
{
    public DungeonTaleGame game;
    public float moveSpeed = 5f;
    public float attackRange = 1.35f;
    public float attackCooldown = 0.35f;
    public float walkFramesPerSecond = 9f;
    public float swordSwingDuration = 0.22f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;
    private float nextAttack;
    private float animationTimer;
    private int walkFrame;
    private Sprite idleSprite;
    private Sprite[] walkSprites = new Sprite[0];
    private SpriteRenderer swordRenderer;
    private float swingTimer;
    private Vector2 facingDirection = Vector2.right;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        idleSprite = spriteRenderer.sprite;
        if (game != null) walkSprites = game.heroWalkFrames;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        if (game != null) walkSprites = game.heroWalkFrames;
        if (walkSprites == null || walkSprites.Length == 0)
            walkSprites = gameObject.GetComponent<DungeonTaleGame>()?.heroWalkFrames;
        if (spriteRenderer != null && idleSprite != null) spriteRenderer.sprite = idleSprite;
        CreateSwordVisual();
    }

    void Update()
    {
        if (game != null && game.IsFinished())
        {
            movement = Vector2.zero;
            Animate(false);
            return;
        }

        movement = ReadMovement();
        if (movement.sqrMagnitude > 0.01f) facingDirection = movement.normalized;
        Animate(movement.sqrMagnitude > 0.01f);
        UpdateSwordSwing();

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
        float x = 0f, y = 0f;
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

    void Animate(bool walking)
    {
        if (spriteRenderer == null) return;
        if (movement.x < -0.01f) spriteRenderer.flipX = true;
        else if (movement.x > 0.01f) spriteRenderer.flipX = false;

        if (!walking || walkSprites == null || walkSprites.Length == 0)
        {
            animationTimer = 0f;
            walkFrame = 0;
            if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            return;
        }

        animationTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, walkFramesPerSecond);
        if (animationTimer >= frameDuration)
        {
            animationTimer %= frameDuration;
            walkFrame = (walkFrame + 1) % walkSprites.Length;
        }
        if (walkSprites[walkFrame] != null) spriteRenderer.sprite = walkSprites[walkFrame];
    }

    void Attack()
    {
        swingTimer = swordSwingDuration;
        Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + facingDirection * 0.65f, attackRange);
        foreach (Collider2D hit in hits)
        {
            DungeonEnemy enemy = hit.GetComponent<DungeonEnemy>();
            if (enemy != null) enemy.TakeHit();
        }
    }


    void CreateSwordVisual()
    {
        if (game == null || game.swordSprite == null) return;
        var sword = new GameObject("Sword Swing Visual");
        sword.transform.SetParent(transform, false);
        sword.transform.localScale = Vector3.one * 0.85f;
        swordRenderer = sword.AddComponent<SpriteRenderer>();
        swordRenderer.sprite = game.swordSprite;
        swordRenderer.sortingOrder = 15;
        swordRenderer.enabled = false;
    }

    void UpdateSwordSwing()
    {
        if (swordRenderer == null) return;
        if (swingTimer <= 0f)
        {
            swordRenderer.enabled = false;
            return;
        }

        swingTimer -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(swingTimer / Mathf.Max(0.01f, swordSwingDuration));
        float facingAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        float arcAngle = Mathf.Lerp(-115f, 115f, progress);
        swordRenderer.enabled = true;
        swordRenderer.transform.localPosition = (Vector3)(facingDirection * 0.72f);
        swordRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, facingAngle + arcAngle - 90f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}