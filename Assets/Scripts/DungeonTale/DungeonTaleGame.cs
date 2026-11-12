using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;\nusing UnityEngine.InputSystem;

public class DungeonTaleGame : MonoBehaviour
{
    [Header("Dungeon assets")]
    public Tilemap floorMap;
    public Tilemap wallMap;
    public TileBase floorTile;
    public TileBase wallTile;
    public Sprite heroSprite;

    [Header("Gameplay")]
    public int playerHealth = 5;
    public int coins;
    public int enemiesRemaining = 4;

    private Text hud;
    private Text message;
    private Transform player;
    private bool finished;

    void Start()
    {
        BuildDungeon();
        CreatePlayer();
        CreateEnemies();
        CreatePickups();
        CreateExit();
        CreateUI();
        UpdateHud();
    }

    void BuildDungeon()
    {
        floorMap.ClearAllTiles();
        wallMap.ClearAllTiles();
        const int width = 31, height = 21;
        for (int x = -width / 2; x <= width / 2; x++)
        for (int y = -height / 2; y <= height / 2; y++)
        {
            floorMap.SetTile(new Vector3Int(x, y, 0), floorTile);
            bool border = x == -width / 2 || x == width / 2 || y == -height / 2 || y == height / 2;
            bool pillar = (x == -5 || x == 5) && (y == -4 || y == 4);
            bool divider = x == 0 && y >= -5 && y <= 5 && y != 0;
            if (border || pillar || divider)
                wallMap.SetTile(new Vector3Int(x, y, 0), wallTile);
        }
        floorMap.RefreshAllTiles();
        wallMap.RefreshAllTiles();
    }

    void CreatePlayer()
    {
        GameObject go = new GameObject("Dungeon Hero");
        go.transform.position = new Vector3(-12, 0, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = heroSprite;
        sr.sortingOrder = 5;
        if (sr.sprite == null) sr.color = new Color(0.25f, 0.85f, 1f);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<CapsuleCollider2D>();
        var controller = go.AddComponent<DungeonPlayerController>();
        controller.game = this;
        player = go.transform;
        Camera.main.transform.position = new Vector3(-12, 0, -10);
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 8.5f;
        var follow = Camera.main.gameObject.AddComponent<DungeonCameraFollow>();
        follow.target = player;
    }

    void CreateEnemies()
    {
        Vector2[] positions = { new Vector2(-7, 6), new Vector2(7, 6), new Vector2(8, -5), new Vector2(-7, -6) };
        foreach (var p in positions)
        {
            GameObject enemy = new GameObject("Dungeon Slime");
            enemy.transform.position = p;
            var sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = heroSprite;
            sr.color = new Color(0.75f, 0.25f, 0.9f);
            sr.sortingOrder = 4;
            enemy.transform.localScale = Vector3.one * 0.72f;
            enemy.AddComponent<CircleCollider2D>().isTrigger = true;
            var brain = enemy.AddComponent<DungeonEnemy>();
            brain.game = this;
            brain.target = player;
        }
    }

    void CreatePickups()
    {
        Vector2[] positions = { new Vector2(-9, 5), new Vector2(-3, 8), new Vector2(4, -7), new Vector2(10, 2), new Vector2(12, -7) };
        foreach (var p in positions)
        {
            GameObject coin = new GameObject("Gold Coin");
            coin.transform.position = p;
            var sr = coin.AddComponent<SpriteRenderer>();
            sr.sprite = heroSprite;
            sr.color = new Color(1f, 0.78f, 0.12f);
            sr.sortingOrder = 3;
            coin.transform.localScale = Vector3.one * 0.35f;
            coin.AddComponent<CircleCollider2D>().isTrigger = true;
            coin.AddComponent<DungeonCoin>().game = this;
        }
    }

    void CreateExit()
    {
        GameObject exit = new GameObject("Dungeon Exit");
        exit.transform.position = new Vector2(13, 8);
        var sr = exit.AddComponent<SpriteRenderer>();
        sr.sprite = heroSprite;
        sr.color = new Color(0.2f, 1f, 0.55f);
        sr.sortingOrder = 2;
        exit.transform.localScale = new Vector3(1.2f, 1.6f, 1);
        exit.AddComponent<BoxCollider2D>().isTrigger = true;
        exit.AddComponent<DungeonExit>().game = this;
    }

    void CreateUI()
    {
        var canvasGo = new GameObject("Dungeon HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        hud = MakeText(canvas.transform, "HUD", new Vector2(18, -18), new Vector2(520, 42), 22, TextAnchor.UpperLeft);
        message = MakeText(canvas.transform, "Message", new Vector2(0, 18), new Vector2(650, 55), 24, TextAnchor.LowerCenter);
        message.rectTransform.anchorMin = new Vector2(.5f, 0);
        message.rectTransform.anchorMax = new Vector2(.5f, 0);
        message.text = "WASD / Arrows: Move     SPACE: Attack";
    }

    Text MakeText(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    public void UpdateHud()
    {
        if (hud) hud.text = "DUNGEON TALE     HP: " + playerHealth + "     GOLD: " + coins + "     SLIMES: " + enemiesRemaining;
    }

    public void ShowMessage(string value)
    {
        if (message) message.text = value;
    }

    public void DamagePlayer()
    {
        if (finished) return;
        playerHealth--;
        UpdateHud();
        if (playerHealth <= 0)
        {
            finished = true;
            ShowMessage("YOU WERE DEFEATED  -  Press R to restart");
        }
    }

    public void EnemyDefeated()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        UpdateHud();
        if (enemiesRemaining == 0) ShowMessage("All slimes defeated! Find the green exit.");
    }

    public void CollectCoin()
    {
        coins++;
        UpdateHud();
    }

    public void ReachExit()
    {
        if (enemiesRemaining > 0) { ShowMessage("Defeat every slime before leaving!"); return; }
        finished = true;
        ShowMessage("DUNGEON CLEARED!  Gold collected: " + coins + "  -  Press R to play again");
    }

    void Update()
    {
        if (finished && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public bool IsFinished() { return finished; }
}
