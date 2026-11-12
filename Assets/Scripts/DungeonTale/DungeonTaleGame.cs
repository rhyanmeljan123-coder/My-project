using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DungeonTaleGame : MonoBehaviour
{
    [Header("Map")]
    public Tilemap floorMap, wallMap;
    public TileBase floorTile, wallTile;
    public TileBase floorFlashTile, floorGreenTile, floorMetalTile, floorRedTile, floorWoodTile;
    private readonly List<GameObject> dungeonObjects = new List<GameObject>();
    [Header("Atlas sprites")]
    public Sprite heroSprite;
    public Sprite playerIdleSprite;
    public Sprite[] heroWalkFrames;
    public Sprite[] walkDownFrames, walkLeftFrames, walkRightFrames, walkUpFrames;
    public Sprite[] attackDownFrames, attackLeftFrames, attackRightFrames, attackUpFrames;
    public Sprite slimeSprite, skeletonSprite, batSprite, ratSprite, coinSprite, torchSprite, spikeSprite, doorSprite;
    [Header("2D Pixel Dungeon Pack")]
    public Sprite packChestSprite, packTorchSprite, packSpikeSprite, packCoinSprite, packFlaskSprite;
    public Sprite potionRed, potionBlue, potionGreen, keySprite, swordSprite, shieldSprite, skullSprite, vaseSprite, chainSprite, boneSprite, carpetSprite;
    [Header("Player stats")]
    public int playerHealth = 6, maxHealth = 6, coins, potions;
    public int enemiesRemaining = 8;
    private Text hud, message;
    private Image healthFill;
    private RectTransform healthFillRect;
    private Text healthLabel;
    private Sprite uiWhiteSprite;
    private Transform player;
    private bool finished, hasKey;
    private int dungeonLevel = 1;
    private GameObject nextDungeonPortal;
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
        Place(packTorchSprite ? packTorchSprite : torchSprite,new Vector2(-15,10),1.2f,8); Place(packTorchSprite ? packTorchSprite : torchSprite,new Vector2(15,10),1.2f,8);
        Place(packTorchSprite ? packTorchSprite : torchSprite,new Vector2(-15,-10),1.2f,8); Place(packTorchSprite ? packTorchSprite : torchSprite,new Vector2(15,-10),1.2f,8);
        Place(carpetSprite,new Vector2(0,0),3.5f,1);
        Place(skullSprite,new Vector2(-10,8),1,4); Place(skullSprite,new Vector2(10,-8),1,4);
        Place(vaseSprite,new Vector2(-12,-7),1,5); Place(vaseSprite,new Vector2(12,7),1,5);
        Place(chainSprite,new Vector2(-3,10),1.2f,5); Place(chainSprite,new Vector2(3,-10),1.2f,5);
        Place(boneSprite,new Vector2(7,9),.8f,4); Place(boneSprite,new Vector2(-8,-9),.8f,4);
        // New pack treasure props: chests mark reward corners in every floor.
        Place(packChestSprite,new Vector2(-13,8),1.25f,6);
        Place(packChestSprite,new Vector2(13,-8),1.25f,6);
    }
    void Place(Sprite s, Vector2 p, float scale, int order) { if(!s)return; var g=new GameObject("Dungeon Decoration");dungeonObjects.Add(g);g.transform.position=p;g.transform.localScale=Vector3.one*scale;var r=g.AddComponent<SpriteRenderer>();r.sprite=s;r.sortingOrder=order; }
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
        var e=new GameObject("Dungeon Monster");dungeonObjects.Add(e);e.transform.position=pos;
        var sr=e.AddComponent<SpriteRenderer>();sr.sprite=sprite? sprite:slimeSprite;sr.sortingOrder=9;
        e.AddComponent<CircleCollider2D>().isTrigger=true;
        var brain=e.AddComponent<DungeonEnemy>();brain.game=this;brain.target=player;brain.speed=speed;brain.SetHealth(hp);
    }
    void CreatePickups() {
        Vector2[] coinsAt={new(-11,5),new(-5,9),new(5,9),new(12,5),new(-11,-5),new(-4,-9),new(4,-9),new(12,-5)};
        foreach(var p in coinsAt) { var c=MakePickup("Gold Coin",packCoinSprite ? packCoinSprite : coinSprite,p,.85f);c.AddComponent<CircleCollider2D>().isTrigger=true;c.AddComponent<DungeonCoin>().game=this; }
        MakeItem("Red Potion",packFlaskSprite ? packFlaskSprite : potionRed,new Vector2(-3,6),1);
        MakeItem("Blue Potion",potionBlue,new Vector2(5,5),1);
        MakeItem("Green Potion",potionGreen,new Vector2(9,-5),1);
        MakeItem("Dungeon Key",keySprite,new Vector2(0,-9),1);
        MakeItem("Sword Relic",swordSprite,new Vector2(-13,-2),.9f);
        MakeItem("Shield Relic",shieldSprite,new Vector2(13,2),.9f);
    }
    GameObject MakePickup(string n,Sprite s,Vector2 p,float size){var g=new GameObject(n);dungeonObjects.Add(g);g.transform.position=p;g.transform.localScale=Vector3.one*size;var r=g.AddComponent<SpriteRenderer>();r.sprite=s;r.sortingOrder=7;return g;}
    void MakeItem(string n,Sprite s,Vector2 p,float size){if(!s)return;var g=MakePickup(n,s,p,size);g.AddComponent<CircleCollider2D>().isTrigger=true;g.AddComponent<DungeonItemPickup>().game=this;}
    void CreateTraps() {
        Vector2[] positions={new(-6,2),new(-6,3),new(6,-2),new(6,-3),new(11,0),new(-11,0)};
        foreach(var p in positions){Sprite trapArt = packSpikeSprite ? packSpikeSprite : spikeSprite;if(!trapArt)continue;var g=MakePickup("Spike Trap",trapArt,p,.9f);g.AddComponent<BoxCollider2D>().isTrigger=true;g.AddComponent<DungeonSpike>().game=this;}
    }
    void CreateExit(){var g=MakePickup("Dungeon Exit",doorSprite,new Vector2(15,0),1.8f);g.AddComponent<BoxCollider2D>().isTrigger=true;g.AddComponent<DungeonExit>().game=this;}
    void CreateUI() {
        // Give UI Images a real, readable white sprite so the health bar is rendered.
        var whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        uiWhiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        var canvasGo=new GameObject("Dungeon HUD");
        var canvas=canvasGo.AddComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=100;
        var scaler=canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        scaler.matchWidthOrHeight=0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        hud=MakeText(canvas.transform,"HUD",new Vector2(18,-16),new Vector2(800,42),21,TextAnchor.UpperLeft);
        CreateHealthBar(canvas.transform);
        message=MakeText(canvas.transform,"Message",new Vector2(0,18),new Vector2(850,54),20,TextAnchor.LowerCenter);
        message.rectTransform.anchorMin=new Vector2(.5f,0);message.rectTransform.anchorMax=new Vector2(.5f,0);
        message.text="WASD / Arrows: Move     SPACE: Attack     E: Use potion";
    }
    Text MakeText(Transform parent,string n,Vector2 pos,Vector2 size,int fs,TextAnchor align){
        var g=new GameObject(n);g.transform.SetParent(parent,false);var rt=g.AddComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=size;
        var t=g.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.fontSize=fs;t.color=Color.white;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Overflow;return t;
    }
    void CreateHealthBar(Transform parent)
    {
        var background = new GameObject("Health Bar Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(parent, false);
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(24, -64);
        bgRect.sizeDelta = new Vector2(300, 32);
        var bgImage = background.GetComponent<Image>();
        bgImage.sprite = uiWhiteSprite;
        bgImage.color = new Color(0.035f, 0.04f, 0.055f, 0.98f);
        bgImage.raycastTarget = false;

        var fill = new GameObject("Health Bar Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(background.transform, false);
        healthFillRect = fill.GetComponent<RectTransform>();
        healthFillRect.anchorMin = new Vector2(0, 0);
        healthFillRect.anchorMax = new Vector2(0, 1);
        healthFillRect.pivot = new Vector2(0, 0.5f);
        healthFillRect.anchoredPosition = new Vector2(4, 0);
        healthFillRect.sizeDelta = new Vector2(292, -8);
        healthFill = fill.GetComponent<Image>();
        healthFill.sprite = uiWhiteSprite;
        healthFill.color = new Color(0.15f, 0.85f, 0.28f, 1f);
        healthFill.raycastTarget = false;

        healthLabel = MakeText(parent, "Health Label", new Vector2(336, -65), new Vector2(190, 34), 22, TextAnchor.UpperLeft);
        healthLabel.fontStyle = FontStyle.Bold;
        healthLabel.color = Color.white;
    }

    public void UpdateHud()
    {
        if (hud) hud.text = $"DUNGEON TALE     GOLD: {coins}     POTIONS: {potions}     MONSTERS: {enemiesRemaining}";
        float healthRatio = maxHealth > 0 ? Mathf.Clamp01((float)playerHealth / maxHealth) : 0f;
        if (healthFillRect) healthFillRect.sizeDelta = new Vector2(292f * healthRatio, -8f);
        if (healthFill)
        {
            healthFill.color = playerHealth <= 2 ? new Color(0.95f, 0.12f, 0.1f) :
                               playerHealth <= 4 ? new Color(1f, 0.62f, 0.08f) :
                               new Color(0.15f, 0.85f, 0.28f);
        }
        if (healthLabel) healthLabel.text = $"HEALTH  {playerHealth}/{maxHealth}";
    }
    public void ShowMessage(string s){if(message)message.text=s;}
    public void DamagePlayer(){if(finished||Time.time<invulnerableUntil)return;invulnerableUntil=Time.time+0.8f;playerHealth--;UpdateHud();if(playerHealth<=0){finished=true;ShowMessage("YOU WERE DEFEATED - Press R to restart");}}
    public void EnemyDefeated()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        UpdateHud();
        if (enemiesRemaining == 0)
        {
            ShowMessage("DUNGEON CLEARED! A portal to the next dungeon has appeared!");
            SpawnNextDungeonPortal();
        }
    }

    void SpawnNextDungeonPortal()
    {
        if (nextDungeonPortal != null) return;
        nextDungeonPortal = new GameObject("Next Dungeon Portal");
        nextDungeonPortal.transform.position = Vector3.zero;
        nextDungeonPortal.transform.localScale = Vector3.one * 1.8f;
        var sr = nextDungeonPortal.AddComponent<SpriteRenderer>();
        sr.sprite = doorSprite != null ? doorSprite : heroSprite;
        sr.color = new Color(0.72f, 0.3f, 1f, 1f);
        sr.sortingOrder = 15;
        var trigger = nextDungeonPortal.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        var portal = nextDungeonPortal.AddComponent<DungeonPortal>();
        portal.game = this;
    }

    public void EnterNextDungeon()
    {
        if (finished || enemiesRemaining > 0) return;

        dungeonLevel++;
        if (nextDungeonPortal != null) Destroy(nextDungeonPortal);
        nextDungeonPortal = null;

        // Remove every object belonging to the previous room, but keep the
        // player, camera and HUD so health, coins and potions carry over.
        foreach (var oldObject in dungeonObjects)
            if (oldObject != null) Destroy(oldObject);
        dungeonObjects.Clear();

        if (player != null) player.position = new Vector3(-14, 0, 0);
        hasKey = false;

        BuildNextDungeonLayout();
        DecorateDungeon();
        CreateLevelEnemies();
        CreatePickups();
        CreateTraps();
        CreateExit();

        ShowMessage("DUNGEON " + dungeonLevel + " - " + GetDungeonName() + "!");
        UpdateHud();
    }

    string GetDungeonName()
    {
        switch ((dungeonLevel - 2) % 5)
        {
            case 0: return "FORGOTTEN RUINS";
            case 1: return "INFERNAL CAVERN";
            case 2: return "IRON CRYPT";
            case 3: return "SUNDERED HALLS";
            default: return "CRYSTAL CATACOMBS";
        }
    }

    void BuildNextDungeonLayout()
    {
        if (floorMap == null || wallMap == null || wallTile == null) return;

        TileBase themeFloor;
        switch ((dungeonLevel - 2) % 5)
        {
            case 0: themeFloor = floorGreenTile ? floorGreenTile : floorTile; break;
            case 1: themeFloor = floorRedTile ? floorRedTile : floorTile; break;
            case 2: themeFloor = floorMetalTile ? floorMetalTile : floorTile; break;
            case 3: themeFloor = floorWoodTile ? floorWoodTile : floorTile; break;
            default: themeFloor = floorFlashTile ? floorFlashTile : floorTile; break;
        }

        floorMap.ClearAllTiles();
        wallMap.ClearAllTiles();
        for (int x = -17; x <= 17; x++)
        for (int y = -12; y <= 12; y++)
        {
            floorMap.SetTile(new Vector3Int(x, y, 0), themeFloor);
            if (x == -17 || x == 17 || y == -12 || y == 12)
                wallMap.SetTile(new Vector3Int(x, y, 0), wallTile);
        }

        // Each theme gets a different walkable floor plan. Keep the center
        // lane open so the player can reach every section of the room.
        int pattern = (dungeonLevel - 2) % 5;
        if (pattern == 0) // Overgrown ruins: broken side chambers
        {
            AddWallLine(-8, -8, -8, 4, -2);
            AddWallLine(8, -4, 8, 8, 2);
            AddWallLine(-4, 7, 4, 7, 0);
            AddWallLine(-4, -7, 4, -7, 0);
        }
        else if (pattern == 1) // Lava cavern: staggered rock bridges
        {
            AddWallLine(-12, -5, -5, -5, -9);
            AddWallLine(1, 5, 12, 5, 7);
            AddWallLine(-7, 1, -7, 8, 4);
            AddWallLine(7, -8, 7, -1, -4);
        }
        else if (pattern == 2) // Iron crypt: central cross and side vaults
        {
            AddWallLine(-10, 0, -3, 0, -7);
            AddWallLine(3, 0, 10, 0, 7);
            AddWallLine(0, -9, 0, -3, -6);
            AddWallLine(0, 3, 0, 9, 6);
        }
        else if (pattern == 3) // Sundered halls: alternating long corridors
        {
            AddWallLine(-13, -7, -3, -7, -8);
            AddWallLine(3, 7, 13, 7, 8);
            AddWallLine(-10, 1, -2, 1, -6);
            AddWallLine(2, -1, 10, -1, 6);
        }
        else // Crystal catacombs: separated chambers and narrow openings
        {
            AddWallLine(-11, -8, -11, 5, 1);
            AddWallLine(11, -5, 11, 8, -1);
            AddWallLine(-7, 8, -2, 8, -5);
            AddWallLine(2, -8, 7, -8, 5);
        }

        floorMap.RefreshAllTiles();
        wallMap.RefreshAllTiles();
    }

    void AddWallLine(int x1, int y1, int x2, int y2, int gap)
    {
        int dx = x2 == x1 ? 0 : (x2 > x1 ? 1 : -1);
        int dy = y2 == y1 ? 0 : (y2 > y1 ? 1 : -1);
        int x = x1, y = y1, index = 0;
        while (true)
        {
            if (index != gap && !(x == -14 && y == 0))
                wallMap.SetTile(new Vector3Int(x, y, 0), wallTile);
            if (x == x2 && y == y2) break;
            x += dx; y += dy; index++;
        }
    }

    void CreateLevelEnemies()
    {
        Vector2[] positions = {
            new Vector2(-12, 7), new Vector2(-7, 9), new Vector2(-2, 8),
            new Vector2(6, 9), new Vector2(12, 6), new Vector2(12, -6),
            new Vector2(6, -9), new Vector2(-2, -8), new Vector2(-8, -8),
            new Vector2(-13, -4), new Vector2(2, 3), new Vector2(10, 2)
        };
        int count = Mathf.Min(positions.Length, 8 + (dungeonLevel - 1) * 2);
        for (int i = 0; i < count; i++)
        {
            Sprite type = i % 4 == 0 ? skeletonSprite :
                          i % 4 == 1 ? slimeSprite :
                          i % 4 == 2 ? batSprite : ratSprite;
            float speed = (type == batSprite ? 2.1f : type == ratSprite ? 2.4f :
                           type == skeletonSprite ? 1.25f : 1.7f) + (dungeonLevel - 1) * 0.08f;
            SpawnEnemy(type, positions[i], speed, (type == skeletonSprite ? 3 : 2) + dungeonLevel - 1);
        }
        enemiesRemaining = count;
    }

    public void CollectCoin(){coins++;UpdateHud();}
    public void CollectItem(string item){if(item=="Dungeon Key"){hasKey=true;ShowMessage("Dungeon key collected! The exit is unlocked.");}else if(item.Contains("Potion")){potions++;ShowMessage("Potion collected. Press E to restore health.");}UpdateHud();}
    public void UsePotion(){if(potions<=0||playerHealth>=maxHealth)return;potions--;playerHealth=Mathf.Min(maxHealth,playerHealth+3);UpdateHud();ShowMessage("Potion used. Health restored.");}
    public void HitTrap(){DamagePlayer();}
    public void ReachExit(){if(enemiesRemaining>0){ShowMessage("Defeat every monster first!");return;}if(!hasKey){ShowMessage("The exit is locked. Find the dungeon key!");return;}finished=true;ShowMessage("DUNGEON CLEARED! Gold: "+coins+" - Press R to play again");}
    void Update(){if(Keyboard.current==null)return;if(Keyboard.current.eKey.wasPressedThisFrame)UsePotion();if(finished&&Keyboard.current.rKey.wasPressedThisFrame)SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
    public bool IsFinished(){return finished;}

    // Fallback HUD drawn directly by Unity's IMGUI system. This remains visible
    // even if the runtime Canvas/UI font setup fails in a project version.
    void OnGUI()
    {
        const float x = 18f;
        const float y = 18f;
        const float barWidth = 250f;
        const float barHeight = 24f;

        GUI.color = new Color(0.02f, 0.025f, 0.035f, 0.95f);
        GUI.Box(new Rect(x, y, barWidth + 16f, 58f), GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(new Rect(x + 8f, y + 3f, barWidth, 20f),
            "HEALTH  " + playerHealth + " / " + maxHealth);

        GUI.color = new Color(0.12f, 0.14f, 0.17f, 1f);
        GUI.Box(new Rect(x + 8f, y + 27f, barWidth, barHeight), GUIContent.none);

        float ratio = maxHealth > 0 ? Mathf.Clamp01((float)playerHealth / maxHealth) : 0f;
        GUI.color = playerHealth <= 2 ? new Color(0.95f, 0.12f, 0.1f) :
                    playerHealth <= 4 ? new Color(1f, 0.62f, 0.08f) :
                    new Color(0.15f, 0.85f, 0.28f);
        GUI.DrawTexture(new Rect(x + 11f, y + 30f, (barWidth - 6f) * ratio, barHeight - 6f), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(x + 8f, y + 62f, 520f, 26f),
            "GOLD: " + coins + "     POTIONS: " + potions + "     MONSTERS: " + enemiesRemaining);
    }

}