using UnityEngine;

public class DungeonPortal : MonoBehaviour
{
    public DungeonTaleGame game;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DungeonPlayerController>() == null) return;
        if (game != null) game.EnterNextDungeon();
    }
}
