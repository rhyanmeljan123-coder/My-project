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
    public float attackFramesPerSecond = 8f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;
    private float nextAttack;
    private float animationTimer;
    private int walkFrame;
    private Sprite idleSprite;
    private Sprite[] walkSprites = new Sprite[0];
    private float swingTimer;
    private int attackFrame;
    private float attackFrameTimer;
    private Sprite[] currentAttackFrames = new Sprite[0];
    private Sprite[] currentWalkFrames = new Sprite[0];
    private bool attackFlipX;
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
        if (game != null)
        {
            idleSprite = game.playerIdleSprite != null ? game.playerIdleSprite : idleSprite;
            currentWalkFrames = game.walkDownFrames;
        }
        if (spriteRenderer != null && idleSprite != null) spriteRenderer.sprite = idleSprite;
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
        if (swingTimer > 0f)
        {
            AnimateAttack();
            swingTimer = Mathf.Max(0f, swingTimer - Time.deltaTime);
            if (swingTimer <= 0f) RestoreMovementSprite();
        }
        else
        {
            SelectWalkDirection();
            Animate(movement.sqrMagnitude > 0.01f);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && swingTimer <= 0f && Time.time >= nextAttack)
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

    void Attack()
    {
        if (game == null) return;

        // Each direction has its own short swing sequence on the imported sheet.
        if (Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y))
        {
            // The sheet contains a right-facing horizontal swing. Mirror it for A/left.
            currentAttackFrames = game.attackRightFrames;
            attackFlipX = facingDirection.x < 0;
        }
        else
        {
            attackFlipX = false;
            currentAttackFrames = facingDirection.y > 0 ? game.attackUpFrames : game.attackDownFrames;
        }
        spriteRenderer.flipX = attackFlipX;

        bool hasSwingAnimation = currentAttackFrames != null && currentAttackFrames.Length > 0;
        if (hasSwingAnimation)
        {
            attackFrame = 0;
            attackFrameTimer = 0f;
            float frameDuration = 1f / Mathf.Max(1f, attackFramesPerSecond);
            swingTimer = currentAttackFrames.Length * frameDuration;
            if (currentAttackFrames[0] != null) spriteRenderer.sprite = currentAttackFrames[0];
        }
        Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + facingDirection * 0.65f, attackRange);
        foreach (Collider2D hit in hits)
        {
            DungeonEnemy enemy = hit.GetComponent<DungeonEnemy>();
            if (enemy != null) enemy.TakeHit();
        }
    }


    void SelectWalkDirection()
    {
        if (game == null) return;
        if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            currentWalkFrames = movement.x < 0 ? game.walkLeftFrames : game.walkRightFrames;
        else if (movement.y > 0.01f)
            currentWalkFrames = game.walkUpFrames;
        else if (movement.y < -0.01f)
            currentWalkFrames = game.walkDownFrames;
        else if (currentWalkFrames == null || currentWalkFrames.Length == 0)
            currentWalkFrames = game.walkDownFrames;
    }

    void Animate(bool walking)
    {
        if (spriteRenderer == null) return;
        if (!walking || currentWalkFrames == null || currentWalkFrames.Length == 0)
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
            walkFrame = (walkFrame + 1) % currentWalkFrames.Length;
        }
        if (currentWalkFrames[walkFrame] != null) spriteRenderer.sprite = currentWalkFrames[walkFrame];
    }

    void AnimateAttack()
    {
        if (spriteRenderer == null || currentAttackFrames == null || currentAttackFrames.Length == 0) return;

        // Show each directional swing frame for a full frame interval.
        float frameDuration = 1f / Mathf.Max(1f, attackFramesPerSecond);
        attackFrameTimer += Time.deltaTime;
        while (attackFrameTimer >= frameDuration && attackFrame < currentAttackFrames.Length - 1)
        {
            attackFrameTimer -= frameDuration;
            attackFrame++;
            Sprite nextFrame = currentAttackFrames[attackFrame];
            if (nextFrame != null) spriteRenderer.sprite = nextFrame;
        }
    }

    void RestoreMovementSprite()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = false;
        if (movement.sqrMagnitude > 0.01f && currentWalkFrames != null && currentWalkFrames.Length > 0)
        {
            int index = Mathf.Clamp(walkFrame, 0, currentWalkFrames.Length - 1);
            if (currentWalkFrames[index] != null) spriteRenderer.sprite = currentWalkFrames[index];
        }
        else if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}