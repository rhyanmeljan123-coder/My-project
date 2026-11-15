using UnityEngine;
using System.Collections.Generic;
public class DungeonEnemy : MonoBehaviour
{
    public DungeonTaleGame game; public Transform target; public float speed=1.6f;
    private float nextAttackTime;
    private int health = 2;
    private int maxHealth = 2;
    [Header("Monster Rewards")]
    public int xpReward = 20;
    [Range(0f, 1f)] public float coinDropChance = 0.3f;
    [Range(0f, 1f)] public float potionDropChance = 0.1f;
    private SpriteRenderer spriteRenderer;
    private Transform visualTransform;
    private Vector3 visualBaseLocalPosition;
    private Vector3 visualBaseLocalScale;
    private Color visualBaseColor = Color.white;
    private Rigidbody2D body;
    private bool isAttacking;
    private bool attackDamagePending;
    private float attackAnimationTime;
    private Vector2 attackDirection = Vector2.down;
    private const float AttackCooldown = 1.1f;
    private const float AttackDuration = 0.42f;
    private Vector2 moveDirection;
    private readonly List<Vector2> path = new List<Vector2>();
    private float nextPathRefresh;
    private Vector3Int lastTargetCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    private const float RepathInterval = 0.35f;
    private Sprite[] idleFrames, walkFrames, attackFrames, hurtFrames, deathFrames;
    private float frameTimer;
    private int frameIndex;
    private bool playingHurt;
    private bool playingDeath;

    public void SetWraithAnimations(SpriteRenderer visual, Sprite[] idle, Sprite[] walk, Sprite[] attack, Sprite[] hurt, Sprite[] death)
    {
        // Assign the exact renderer created by SpawnWraith; do not rely on
        // GetComponentInChildren timing during AddComponent/Awake.
        if (visual != null)
        {
            spriteRenderer = visual;
            visualTransform = visual.transform;
            visualBaseLocalPosition = visualTransform.localPosition;
            visualBaseLocalScale = visualTransform.localScale;
            visualBaseColor = visual.color;
            visual.enabled = true;
            visual.gameObject.SetActive(true);
        }
        idleFrames = idle; walkFrames = walk; attackFrames = attack; hurtFrames = hurt; deathFrames = death;
        if (idleFrames != null && idleFrames.Length > 0)
        {
            spriteRenderer.sprite = idleFrames[0];
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = true;
            SyncColliderToVisual();
            Debug.Log("Dungeon Tale: Wraith visual assigned: " + idleFrames[0].name);
        }
        else
        {
            Debug.LogError("Dungeon Tale: Wraith has no idle sprite frames. Check CraftPix sprite import.");
        }
    }

    void SyncColliderToVisual()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        // The visual is a child; convert its sprite bounds into the enemy
        // root's local space so the hitbox follows the actual rendered size.
        var hitbox = GetComponent<BoxCollider2D>();
        if (hitbox == null) hitbox = gameObject.AddComponent<BoxCollider2D>();
        Vector3 scale = visualTransform != null ? visualTransform.localScale : Vector3.one;
        Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
        Vector3 spriteCenter = spriteRenderer.sprite.bounds.center;
        hitbox.size = new Vector2(
            Mathf.Max(0.05f, spriteSize.x * Mathf.Abs(scale.x) * 0.9f),
            Mathf.Max(0.05f, spriteSize.y * Mathf.Abs(scale.y) * 0.9f));
        Vector3 childPosition = visualTransform != null ? visualTransform.localPosition : Vector3.zero;
        hitbox.offset = new Vector2(
            childPosition.x + spriteCenter.x * scale.x,
            childPosition.y + spriteCenter.y * scale.y);
        hitbox.isTrigger = false;
    }

    void ApplyFrame(Sprite[] frames, int index)
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        spriteRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    void AnimateFrames(Sprite[] frames, float fps, bool loop)
    {
        if (frames == null || frames.Length == 0) return;
        frameTimer += Time.deltaTime;
        float frameLength = 1f / Mathf.Max(1f, fps);
        while (frameTimer >= frameLength)
        {
            frameTimer -= frameLength;
            if (frameIndex < frames.Length - 1) frameIndex++;
            else if (loop) frameIndex = 0;
        }
        ApplyFrame(frames, frameIndex);
    }

    void Awake()
    {
        // Keep animation on the child visual so the root collider remains solid and stationary.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            visualTransform = spriteRenderer.transform;
            visualBaseLocalPosition = visualTransform.localPosition;
            visualBaseLocalScale = visualTransform.localScale;
            visualBaseColor = spriteRenderer.color;
        }

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
        if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
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

        if (game != null && game.IsFinished())
        {
            if (isAttacking) FinishAttackAnimation();
            return;
        }

        UpdateAttackAnimation();
        if (playingHurt)
        {
            AnimateFrames(hurtFrames, 18f, false);
            if (frameIndex >= (hurtFrames != null ? hurtFrames.Length - 1 : 0)) { playingHurt = false; frameIndex = 0; frameTimer = 0f; }
            return;
        }
        if (isAttacking || target == null || game == null) return;

        // Detect melee range from collider geometry instead of relying only
        // on OnCollisionStay2D. This works consistently for every selected
        // player sprite/scale, including Soldier and Orc.
        Collider2D playerCollider = target.GetComponent<Collider2D>();
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (playerCollider != null && enemyCollider != null)
        {
            ColliderDistance2D separation = enemyCollider.Distance(playerCollider);
            if (separation.isValid && separation.distance <= 0.12f &&
                Time.time >= nextAttackTime)
            {
                BeginAttackAnimation();
                attackDamagePending = true;
                nextAttackTime = Time.time + AttackCooldown;
                path.Clear();
                return;
            }
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= 0.18f)
        {
            path.Clear();
            return;
        }

        Vector3Int targetCell = game.wallMap != null
            ? game.wallMap.WorldToCell(target.position)
            : new Vector3Int(Mathf.RoundToInt(target.position.x), Mathf.RoundToInt(target.position.y), 0);

        if (Time.time >= nextPathRefresh || targetCell != lastTargetCell || path.Count == 0)
        {
            RebuildPath(targetCell);
            lastTargetCell = targetCell;
            nextPathRefresh = Time.time + RepathInterval;
        }

        while (path.Count > 0 && Vector2.Distance(body.position, path[0]) < 0.12f)
            path.RemoveAt(0);

        if (path.Count > 0)
            moveDirection = (path[0] - body.position).normalized;

        if (moveDirection.sqrMagnitude > 0.001f) AnimateFrames(walkFrames, 10f, true);
        else AnimateFrames(idleFrames, 5f, true);
    }

    void FixedUpdate()
    {
        if (body == null || isAttacking || moveDirection.sqrMagnitude < 0.001f) return;
        body.MovePosition(body.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (target == null || game == null || game.IsFinished() || isAttacking || Time.time < nextAttackTime)
            return;

        // Backup contact path for cases where the physics solver reports a
        // collision before the next Update proximity check.
        Transform other = collision.collider != null ? collision.collider.transform : null;
        if (other == null || (other != target && !other.IsChildOf(target))) return;
        BeginAttackAnimation();
        attackDamagePending = true;
        nextAttackTime = Time.time + AttackCooldown;
    }

    void BeginAttackAnimation()
    {
        Vector2 toPlayer = (Vector2)target.position - body.position;
        attackDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.down;
        attackAnimationTime = 0f;
        attackDamagePending = false;
        isAttacking = true;
    }

    void UpdateAttackAnimation()
    {
        if (!isAttacking) return;

        attackAnimationTime += Time.deltaTime;
        AnimateFrames(attackFrames, 16f, false);
        float t = Mathf.Clamp01(attackAnimationTime / AttackDuration);
        float lunge;

        // Brief wind-up, quick forward strike, then a smooth return to idle.
        if (t < 0.28f)
        {
            float phase = Mathf.SmoothStep(0f, 1f, t / 0.28f);
            lunge = Mathf.Lerp(0f, -0.08f, phase);
        }
        else if (t < 0.58f)
        {
            float phase = Mathf.SmoothStep(0f, 1f, (t - 0.28f) / 0.30f);
            lunge = Mathf.Lerp(-0.08f, 0.22f, phase);
        }
        else
        {
            float phase = Mathf.SmoothStep(0f, 1f, (t - 0.58f) / 0.42f);
            lunge = Mathf.Lerp(0.22f, 0f, phase);
        }

        // Apply damage on the strike beat, not when the wind-up begins.
        if (attackDamagePending && t >= 0.58f)
        {
            attackDamagePending = false;
            if (game != null && !game.IsFinished()) game.DamagePlayer();
        }

        float squash = Mathf.Sin(t * Mathf.PI);
        if (visualTransform != null)
        {
            visualTransform.localPosition = visualBaseLocalPosition +
                new Vector3(attackDirection.x, attackDirection.y, 0f) * lunge;
            visualTransform.localScale = new Vector3(
                visualBaseLocalScale.x * (1f + 0.10f * squash),
                visualBaseLocalScale.y * (1f - 0.07f * squash),
                visualBaseLocalScale.z);
        }

        if (spriteRenderer != null)
        {
            float flash = Mathf.Clamp01(1f - Mathf.Abs(t - 0.52f) / 0.18f);
            Color strikeColor = new Color(1f, 0.65f, 0.65f, visualBaseColor.a);
            spriteRenderer.color = Color.Lerp(visualBaseColor, strikeColor, flash * 0.55f);
        }

        if (attackAnimationTime >= AttackDuration)
            FinishAttackAnimation();
    }

    void FinishAttackAnimation()
    {
        isAttacking = false;
        attackDamagePending = false;
        attackAnimationTime = 0f;
        if (visualTransform != null)
        {
            visualTransform.localPosition = visualBaseLocalPosition;
            visualTransform.localScale = visualBaseLocalScale;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = visualBaseColor;
    }

    void RebuildPath(Vector3Int goalCell)
    {
        path.Clear();
        if (game == null || game.wallMap == null) return;

        Vector3Int startCell = game.wallMap.WorldToCell(body.position);
        if (!game.IsWalkableDungeonCell(startCell))
        {
            // If a monster spawned against a wall, begin from the nearest open cell.
            bool found = false;
            for (int radius = 1; radius <= 3 && !found; radius++)
            for (int x = -radius; x <= radius && !found; x++)
            for (int y = -radius; y <= radius && !found; y++)
            {
                if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                Vector3Int candidate = startCell + new Vector3Int(x, y, 0);
                if (game.IsWalkableDungeonCell(candidate))
                {
                    startCell = candidate;
                    found = true;
                }
            }
            if (!found) return;
        }

        // Route to a free cell adjacent to the player, never through a wall.
        Vector3Int[] offsets = {
            new Vector3Int(0,1,0), new Vector3Int(1,0,0),
            new Vector3Int(0,-1,0), new Vector3Int(-1,0,0),
            new Vector3Int(1,1,0), new Vector3Int(1,-1,0),
            new Vector3Int(-1,-1,0), new Vector3Int(-1,1,0)
        };
        Vector3Int destination = goalCell;
        if (!game.IsWalkableDungeonCell(destination))
        {
            float best = float.MaxValue;
            foreach (var offset in offsets)
            {
                Vector3Int candidate = goalCell + offset;
                if (!game.IsWalkableDungeonCell(candidate)) continue;
                float score = (candidate - startCell).sqrMagnitude;
                if (score < best) { best = score; destination = candidate; }
            }
        }

        var open = new List<Vector3Int> { startCell };
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, float> { [startCell] = 0f };
        var closed = new HashSet<Vector3Int>();
        int safety = 0;

        while (open.Count > 0 && safety++ < 1800)
        {
            int bestIndex = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                Vector3Int node = open[i];
                float score = gScore[node] + Heuristic(node, destination);
                if (score < bestScore) { bestScore = score; bestIndex = i; }
            }

            Vector3Int current = open[bestIndex];
            open.RemoveAt(bestIndex);
            if (current == destination)
            {
                ReconstructPath(cameFrom, current);
                return;
            }
            if (!closed.Add(current)) continue;

            foreach (var offset in offsets)
            {
                Vector3Int next = current + offset;
                if (!game.IsWalkableDungeonCell(next) || closed.Contains(next)) continue;

                bool diagonal = offset.x != 0 && offset.y != 0;
                if (diagonal &&
                    (!game.IsWalkableDungeonCell(current + new Vector3Int(offset.x, 0, 0)) ||
                     !game.IsWalkableDungeonCell(current + new Vector3Int(0, offset.y, 0))))
                    continue;

                float tentative = gScore[current] + (diagonal ? 1.4142f : 1f);
                if (!gScore.TryGetValue(next, out float oldScore) || tentative < oldScore)
                {
                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    if (!open.Contains(next)) open.Add(next);
                }
            }
        }
    }

    float Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.4142f * Mathf.Min(dx, dy);
    }

    void ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var cells = new List<Vector3Int> { current };
        while (cameFrom.TryGetValue(current, out Vector3Int previous))
        {
            current = previous;
            cells.Add(current);
        }
        cells.Reverse();
        for (int i = 1; i < cells.Count; i++)
            path.Add(game.wallMap.GetCellCenterWorld(cells[i]));
    }
    public void TakeHit(int damage)
    {
        DungeonTaleAudio.PlaySfx("enemy_hit");
        health -= Mathf.Max(1, damage);
        if (health <= 0)
        {
            playingDeath = true;
            if (deathFrames != null && deathFrames.Length > 0)
            {
                StartCoroutine(PlayDeathAndRemove());
            }
            else
            {
                game.EnemyDefeated(xpReward, transform.position, coinDropChance, potionDropChance);
                Destroy(gameObject);
            }
        }
        else
        {
            playingHurt = true;
            frameIndex = 0; frameTimer = 0f;
            transform.localScale *= .96f;
        }
    }

    System.Collections.IEnumerator PlayDeathAndRemove()
    {
        isAttacking = false;
        moveDirection = Vector2.zero;
        frameIndex = 0; frameTimer = 0f;
        float duration = deathFrames.Length / 12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            AnimateFrames(deathFrames, 12f, false);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (game != null) game.EnemyDefeated(xpReward, transform.position, coinDropChance, potionDropChance);
        Destroy(gameObject);
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