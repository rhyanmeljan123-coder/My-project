using UnityEngine;
public class DungeonSpike : MonoBehaviour
{
    public DungeonTaleGame game; private float nextHit;
    void OnTriggerStay2D(Collider2D other){if(other.GetComponent<DungeonPlayerController>()&&Time.time>=nextHit){nextHit=Time.time+1.0f;game.HitTrap();}}
}