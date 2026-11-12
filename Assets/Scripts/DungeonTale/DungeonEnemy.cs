using UnityEngine;
public class DungeonEnemy : MonoBehaviour
{
    public DungeonTaleGame game; public Transform target; public float speed=1.6f;
    private float attackTimer; private int health=2;
    public void SetHealth(int value){health=Mathf.Max(1,value);}
    void Update(){
        if(target==null||game==null||game.IsFinished())return;
        float d=Vector2.Distance(transform.position,target.position);
        if(d>0.8f)transform.position=Vector2.MoveTowards(transform.position,target.position,speed*Time.deltaTime);
        else if(Time.time>=attackTimer){attackTimer=Time.time+1.1f;game.DamagePlayer();}
    }
    public void TakeHit(){health--;if(health<=0){game.EnemyDefeated();Destroy(gameObject);}else transform.localScale*=.88f;}
}