using UnityEngine;
public class DungeonItemPickup : MonoBehaviour
{
    public DungeonTaleGame game;
    void OnTriggerEnter2D(Collider2D other){if(!other.GetComponent<DungeonPlayerController>())return;game.CollectItem(gameObject.name);Destroy(gameObject);}
}