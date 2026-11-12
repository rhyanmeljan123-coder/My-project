using UnityEngine;

public class DungeonEnemy : MonoBehaviour
{
    public DungeonTaleGame game;
    public Transform target;
    public float speed = 1.6f;
    private float attackTimer;
    private int health = 2;

    void Update()
    {
        if (target == null || game == null || game.IsFinished()) return;
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > 0.85f)
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        else if (Time.time >= attackTimer)
        {
            attackTimer = Time.time + 1.1f;
            game.DamagePlayer();
        }
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
            transform.localScale *= 0.85f;
        }
    }
}

public class DungeonCoin : MonoBehaviour
{
    public DungeonTaleGame game;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DungeonPlayerController>() == null) return;
        game.CollectCoin();
        Destroy(gameObject);
    }
}

public class DungeonExit : MonoBehaviour
{
    public DungeonTaleGame game;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DungeonPlayerController>() != null) game.ReachExit();
    }
}

public class DungeonCameraFollow : MonoBehaviour
{
    public Transform target;
    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(target.position.x, target.position.y, -10);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * 8f);
    }
}
