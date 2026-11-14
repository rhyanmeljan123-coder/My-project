using UnityEngine;
using System.Collections.Generic;
public class DungeonEnemy : MonoBehaviour
{
    public DungeonTaleGame game; public Transform target; public float speed=1.6f;
    private float nextAttackTime;
    private int health = 2;
    private int maxHealth = 2;
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
        if (isAttacking || target == null || game == null) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= 0.9f)
        {
            // Stop chasing at close range. Damage is applied only by the
            // solid-collider contact callback below.
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

        // The player must be the object actually touching this solid monster collider.
        Transform other = collision.gameObject != null ? collision.gameObject.transform : null;
        if (other == null || (other != target && !other.IsChildOf(target))) return;

        // Require the player to be physically touching and very close to the
        // monster's root. This prevents damage from stray/oversized colliders.
        float contactDistance = Vector2.Distance(body.position, target.position);
        if (contactDistance > 1.15f) return;

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
        health -= Mathf.Max(1, damage);
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