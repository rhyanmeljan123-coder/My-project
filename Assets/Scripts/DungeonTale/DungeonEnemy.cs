using UnityEngine;
using System.Collections.Generic;
public class DungeonEnemy : MonoBehaviour
{
    public DungeonTaleGame game; public Transform target; public float speed=1.6f;
    private float attackTimer;
    private int health = 2;
    private int maxHealth = 2;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Vector2 moveDirection;
    private readonly List<Vector2> path = new List<Vector2>();
    private float nextPathRefresh;
    private Vector3Int lastTargetCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    private const float RepathInterval = 0.35f;

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
        if (distance <= 0.9f)
        {
            path.Clear();
            if (Time.time >= attackTimer)
            {
                attackTimer = Time.time + 1.1f;
                game.DamagePlayer();
            }
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
        if (body == null || moveDirection.sqrMagnitude < 0.001f) return;
        body.MovePosition(body.position + moveDirection * speed * Time.fixedDeltaTime);
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