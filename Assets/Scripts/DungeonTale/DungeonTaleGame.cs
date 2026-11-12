using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class DungeonTaleGame : MonoBehaviour
{
    [Header("Map")]
    public Tilemap floorMap, wallMap;
    public TileBase floorTile, wallTile;
    [Header("Atlas sprites")]
    public Sprite heroSprite;
    public Sprite[] heroWalkFrames;
    public Sprite slimeSprite, skeletonSprite, batSprite, ratSprite, coinSprite, torchSprite, spikeSprite, doorSprite;
    public Sprite potionRed, potionBlue, potionGreen, keySprite, swordSprite, shieldSprite, skullSprite, vaseSprite, chainSprite, boneSprite, carpetSprite;
    [Header("Player stats")]
    public int playerHealth = 6, maxHealth = 6, coins, potions;
    public int enemiesRemaining = 8;
    private Text hud, message;
    private Transform player;
    private bool finished, hasKey;
    private float invulnerableUntil;

    void Start() {
        CreatePlayer(); DecorateDungeon(); CreateEnemies(); CreatePickups(); CreateTraps(); CreateExit(); CreateUI(); UpdateHud();
    }
    void CreatePlayer() {
        var go = new GameObject("Player - Hero");
        go.transform.position = new Vector3(-14,0,0);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=heroSprite; sr.sortingOrder=10;
        var rb=go.AddComponent<Rigidbody2D>(); rb.gravityScale=0; rb.freezeRotation=true; rb.interpolation=RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CapsuleCollider2D>();
        var controller=go.AddComponent<DungeonPlayerController>(); controller.game=this;
        player=go.transform;
        var cam=Camera.main; if(cam){ cam.orthographic=true; cam.orthographicSize=8.5f; cam.transform.position=new Vector3(-14,0,-10); var follow=cam.GetComponent<DungeonCameraFollow>(); if(!follow)follow=cam.gameObject.AddComponent<DungeonCameraFollow>(); follow.target=player; }
    }
    void DecorateDungeon() {
        Place(torchSprite,new Vector2(-15,10),1.2f,8); Place(torchSprite,new Vector2(15,10),1.2f,8);
        Place(torchSprite,new Vector2(-15,-10),1.2f,8); Place(torchSprite,new Vector2(15,-10),1.2f,8);
        Place(carpetSprite,new Vector2(0,0),3.5f,1);
        Place(skullSprite,new Vector2(-10,8),1,4); Place(skullSprite,new Vector2(10,-8),1,4);
        Place(vaseSprite,new Vector2(-12,-7),1,5); Place(vaseSprite,new Vector2(12,7),1,5);
        Place(chainSprite,new Vector2(-3,10),1.2f,5); Place(chainSprite,new Vector2(3,-10),1.2f,5);
        Place(boneSprite,new Vector2(7,9),.8f,4); Place(boneSprite,new Vector2(-8,-9),.8f,4);
    }
    void Place(Sprite s, Vector2 p, float scale, int order) { if(!s)return; var g=new GameObject("Dungeon Decoration");g.transform.position=p;g.transform.localScale=Vector3.one*scale;var r=g.AddComponent<SpriteRenderer>();r.sprite=s;r.sortingOrder=order; }
    void CreateEnemies() {
        SpawnEnemy(slimeSprite,new Vector2(-8,7),1.7f,2);
        SpawnEnemy(slimeSprite,new Vector2(7,7),1.7f,2);
        SpawnEnemy(skeletonSprite,new Vector2(10,2),1.25f,3);
        SpawnEnemy(skeletonSprite,new Vector2(7,-7),1.25f,3);
        SpawnEnemy(batSprite,new Vector2(-8,-7),2.1f,1);
        SpawnEnemy(batSprite,new Vector2(12,-3),2.1f,1);
        SpawnEnemy(ratSprite,new Vector2(-12,5),2.4f,1);
        SpawnEnemy(ratSprite,new Vector2(4,9),2.4f,1);
    }
    void SpawnEnemy(Sprite sprite, Vector2 pos, float speed, int hp) {
        var e=new GameObject("Dungeon Monster");e.transform.position=pos;
        var sr=e.AddComponent<SpriteRenderer>();sr.sprite=sprite? sprite:slimeSprite;sr.sortingOrder=9;
        e.AddComponent<CircleCollider2D>().isTrigger=true;
        var brain=e.AddComponent<DungeonEnemy>();brain.game=this;brain.target=player;brain.speed=speed;brain.SetHealth(hp);
    }
    void CreatePickups() {
        Vector2[] coinsAt={new(-11,5),new(-5,9),new(5,9),new(12,5),new(-11,-5),new(-4,-9),new(4,-9),new(12,-5)};
        foreach(var p in coinsAt) { var c=MakePickup("Gold Coin",coinSprite,p,.85f);c.AddComponent<CircleCollider2D>().isTrigger=true;c.AddComponent<DungeonCoin>().game=this; }
        MakeItem("Red Potion",potionRed,new Vector2(-3,6),1);
        MakeItem("Blue Potion",potionBlue,new Vector2(5,5),1);
        MakeItem("Green Potion",potionGreen,new Vector2(9,-5),1);
        MakeItem("Dungeon Key",keySprite,new Vector2(0,-9),1);
        MakeItem("Sword Relic",swordSprite,new Vector2(-13,-2),.9f);
        MakeItem("Shield Relic",shieldSprite,new Vector2(13,2),.9f);
    }
    GameObject MakePickup(string n,Sprite s,Vector2 p,float size){var g=new GameObject(n);g.transform.position=p;g.transform.localScale=Vector3.one*size;var r=g.AddComponent<SpriteRenderer>();r.sprite=s;r.sortingOrder=7;return g;}
    void MakeItem(string n,Sprite s,Vector2 p,float size){if(!s)return;var g=MakePickup(n,s,p,size);g.AddComponent<CircleCollider2D>().isTrigger=true;g.AddComponent<DungeonItemPickup>().game=this;}
    void CreateTraps() {
        Vector2[] positions={new(-6,2),new(-6,3),new(6,-2),new(6,-3),new(11,0),new(-11,0)};
        foreach(var p in positions){if(!spikeSprite)continue;var g=MakePickup("Spike Trap",spikeSprite,p,.9f);g.AddComponent<BoxCollider2D>().isTrigger=true;g.AddComponent<DungeonSpike>().game=this;}
    }
    void CreateExit(){var g=MakePickup("Dungeon Exit",doorSprite,new Vector2(15,0),1.8f);g.AddComponent<BoxCollider2D>().isTrigger=true;g.AddComponent<DungeonExit>().game=this;}
    void CreateUI() {
        var canvasGo=new GameObject("Dungeon HUD");var canvas=canvasGo.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;canvasGo.AddComponent<GraphicRaycaster>();
        hud=MakeText(canvas.transform,"HUD",new Vector2(18,-16),new Vector2(800,42),21,TextAnchor.UpperLeft);
        message=MakeText(canvas.transform,"Message",new Vector2(0,18),new Vector2(850,54),20,TextAnchor.LowerCenter);
        message.rectTransform.anchorMin=new Vector2(.5f,0);message.rectTransform.anchorMax=new Vector2(.5f,0);
        message.text="WASD / Arrows: Move     SPACE: Attack     E: Use potion";
    }
    Text MakeText(Transform parent,string n,Vector2 pos,Vector2 size,int fs,TextAnchor align){
        var g=new GameObject(n);g.transform.SetParent(parent,false);var rt=g.AddComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=size;
        var t=g.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=fs;t.color=Color.white;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Overflow;return t;
    }
    public void UpdateHud(){if(hud)hud.text=$"DUNGEON TALE     HP: {playerHealth}/{maxHealth}     GOLD: {coins}     POTIONS: {potions}     MONSTERS: {enemiesRemaining}";}
    public void ShowMessage(string s){if(message)message.text=s;}
    public void DamagePlayer(){if(finished||Time.time<invulnerableUntil)return;invulnerableUntil=Time.time+0.8f;playerHealth--;UpdateHud();if(playerHealth<=0){finished=true;ShowMessage("YOU WERE DEFEATED - Press R to restart");}}
    public void EnemyDefeated(){enemiesRemaining=Mathf.Max(0,enemiesRemaining-1);UpdateHud();if(enemiesRemaining==0)ShowMessage(hasKey?"All monsters defeated! Reach the exit.":"All monsters defeated! Find the key and reach the exit.");}
    public void CollectCoin(){coins++;UpdateHud();}
    public void CollectItem(string item){if(item=="Dungeon Key"){hasKey=true;ShowMessage("Dungeon key collected! The exit is unlocked.");}else if(item.Contains("Potion")){potions++;ShowMessage("Potion collected. Press E to restore health.");}UpdateHud();}
    public void UsePotion(){if(potions<=0||playerHealth>=maxHealth)return;potions--;playerHealth=Mathf.Min(maxHealth,playerHealth+3);UpdateHud();ShowMessage("Potion used. Health restored.");}
    public void HitTrap(){DamagePlayer();}
    public void ReachExit(){if(enemiesRemaining>0){ShowMessage("Defeat every monster first!");return;}if(!hasKey){ShowMessage("The exit is locked. Find the dungeon key!");return;}finished=true;ShowMessage("DUNGEON CLEARED! Gold: "+coins+" - Press R to play again");}
    void Update(){if(Keyboard.current==null)return;if(Keyboard.current.eKey.wasPressedThisFrame)UsePotion();if(finished&&Keyboard.current.rKey.wasPressedThisFrame)SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
    public bool IsFinished(){return finished;}
}