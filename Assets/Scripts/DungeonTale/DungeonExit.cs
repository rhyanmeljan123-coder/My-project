using UnityEngine;

public class DungeonExit : MonoBehaviour
{
    public DungeonTaleGame game;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<DungeonPlayerController>() != null) game.ReachExit();
    }
}