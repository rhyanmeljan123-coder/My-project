using UnityEngine;

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