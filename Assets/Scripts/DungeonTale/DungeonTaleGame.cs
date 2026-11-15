using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
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
    [Header("Playable Characters")]
    public Sprite soldierIdleSprite, orcIdleSprite;
    public Sprite[] soldierWalkFrames, orcWalkFrames;
    public Sprite[] soldierAttackFrames, orcAttackFrames;
    [HideInInspector] public int selectedCharacter;
    public Sprite slimeSprite, skeletonSprite, batSprite, ratSprite, coinSprite, torchSprite, spikeSprite, doorSprite;
    public Sprite[] wraith01IdleFrames;
    public Sprite[] wraith01WalkFrames;
    public Sprite[] wraith01AttackFrames;
    public Sprite[] wraith01HurtFrames;
    public Sprite[] wraith01DeathFrames;
    public Sprite[] wraith02IdleFrames;
    public Sprite[] wraith02WalkFrames;
    public Sprite[] wraith02AttackFrames;
    public Sprite[] wraith02HurtFrames;
    public Sprite[] wraith02DeathFrames;
    public Sprite[] wraith03IdleFrames;
    public Sprite[] wraith03WalkFrames;
    public Sprite[] wraith03AttackFrames;
    public Sprite[] wraith03HurtFrames;
    public Sprite[] wraith03DeathFrames;
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
    private bool gameStarted;
    private GameObject screenOverlay;
    private Text overlayTitle, overlaySubtitle;
    private Button primaryOverlayButton, secondaryOverlayButton;
    private GameObject menuPanel, characterPanel, settingsPanel, endPanel;
    private Text endTitle, endSubtitle, characterSelectionLabel, qualityLabel, fullscreenLabel;
    private Button continueButton, qualityButton, fullscreenButton;
    private Slider masterVolumeSlider;
    private readonly Button[] characterButtons = new Button[3];
    private readonly Image[] characterPortraitImages = new Image[3];
    private bool hasSavedGame;

    private const string SavePrefix = "DungeonTale_";
    private GameObject levelClearBanner;
    private Text levelClearTitle, levelClearSubtitle;
    private float levelClearBannerHideAt = -1f;
    private int currentWave = 1;
    private const int WavesPerLevel = 3;
    private bool waitingForNextWave;
    private float nextWaveStartAt;
    private bool levelBonusPending;
    private bool showingLevelBonusChoice;
    public int playerLevel = 1, experience = 0, experienceToNextLevel = 100, attackPower = 1;
    public float criticalChance = 0.05f;
    private float playerSpeedBonus;
    private bool upgradeChoiceOpen;
    private GameObject levelUpPanel;
    private Transform levelUpCardsRoot;
    private Image experienceFill;
    private RectTransform experienceFillRect;
    private Text experienceLabel;
    [Header("Survival Upgrades")]
    private int maxShieldCharges;
    private int shieldCharges;
    private float shieldRegenInterval = 12f;
    private float nextShieldRegen;
    private float dodgeChance;
    private float experienceGainMultiplier = 1f;
    private float healthRegenInterval = 12f;
    private float nextHealthRegen;
    private bool healthRegenEnabled;

    void Start() {
        AudioListener.volume = PlayerPrefs.GetFloat(SavePrefix + "Volume", 1f);
        int savedQuality = PlayerPrefs.GetInt(SavePrefix + "Quality", QualitySettings.GetQualityLevel());
        if (savedQuality >= 0 && savedQuality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(savedQuality, true);
        Screen.fullScreen = PlayerPrefs.GetInt(SavePrefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        hasSavedGame = PlayerPrefs.GetInt(SavePrefix + "HasSave", 0) == 1;
        bool launchNewGame = PlayerPrefs.GetInt(SavePrefix + "StartImmediately", 0) == 1;
        PlayerPrefs.DeleteKey(SavePrefix + "StartImmediately");
        selectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "Character", 0), 0, 2);
        CreatePlayer(); DecorateDungeon(); CreateEnemies(); CreatePickups(); CreateTraps(); CreateUI(); UpdateHud();
        if (launchNewGame) BeginNewGame(); else ShowMainMenu();
    }
    void CreatePlayer() {
        var go = new GameObject("Player - Hero");
        go.transform.position = new Vector3(-14,0,0);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=GetCharacterIdleSprite(selectedCharacter); sr.sortingOrder=10;
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
    void CreateEnemies()
    {
        StartWave(1);
    }

    void StartWave(int wave)
    {
        currentWave = Mathf.Clamp(wave, 1, WavesPerLevel);
        waitingForNextWave = false;
        if (levelClearBanner != null) levelClearBanner.SetActive(false);
        levelClearBannerHideAt = -1f;

        Vector2[] positions = {
            new Vector2(-12, 7), new Vector2(-7, 9), new Vector2(-2, 8),
            new Vector2(6, 9), new Vector2(12, 6), new Vector2(12, -6),
            new Vector2(6, -9), new Vector2(-2, -8), new Vector2(-8, -8),
            new Vector2(-13, -4), new Vector2(2, 3), new Vector2(10, 2)
        };
        int count = Mathf.Clamp(3 + currentWave * 2 + Mathf.Max(0, dungeonLevel - 1), 1, positions.Length);
        enemiesRemaining = count;
        for (int i = 0; i < count; i++)
        {
            int variant = (i + currentWave - 1) % 3;
            float speed = 1.5f + variant * 0.15f + (dungeonLevel - 1) * 0.04f;
            int hp = 2 + variant + currentWave - 1 + Mathf.Max(0, dungeonLevel - 1) / 2;
            SpawnWraith(variant, positions[i], speed, hp);
        }
        ShowMessage("WAVE " + currentWave + " / " + WavesPerLevel + " - DEFEAT ALL WRAITHS!");
        UpdateHud();
    }

    Sprite[] GetWraithFrames(int variant, int animation)
    {
        if (variant == 0)
        {
            if (animation == 0) return wraith01IdleFrames;
            if (animation == 1) return wraith01WalkFrames;
            if (animation == 2) return wraith01AttackFrames;
            if (animation == 3) return wraith01HurtFrames;
            return wraith01DeathFrames;
        }
        if (variant == 1)
        {
            if (animation == 0) return wraith02IdleFrames;
            if (animation == 1) return wraith02WalkFrames;
            if (animation == 2) return wraith02AttackFrames;
            if (animation == 3) return wraith02HurtFrames;
            return wraith02DeathFrames;
        }
        if (animation == 0) return wraith03IdleFrames;
        if (animation == 1) return wraith03WalkFrames;
        if (animation == 2) return wraith03AttackFrames;
        if (animation == 3) return wraith03HurtFrames;
        return wraith03DeathFrames;
    }

    void SpawnWraith(int variant, Vector2 pos, float enemySpeed, int hp)
    {
        var enemyObject = new GameObject("Wraith " + (variant + 1));
        dungeonObjects.Add(enemyObject);
        enemyObject.transform.position = pos;

        var visual = new GameObject("Wraith Visual");
        visual.transform.SetParent(enemyObject.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        Sprite[] idle = GetWraithFrames(variant, 0);
        renderer.sprite = idle != null && idle.Length > 0 ? idle[0] : null;
        renderer.sortingOrder = 9;

        // Match visible sprite height to the selected hero, not transform scale.
        Sprite heroReference = playerIdleSprite != null ? playerIdleSprite : heroSprite;
        if (heroReference != null && renderer.sprite != null &&
            heroReference.bounds.size.y > 0.001f && renderer.sprite.bounds.size.y > 0.001f)
        {
            float matchingScale = heroReference.bounds.size.y / renderer.sprite.bounds.size.y;
            visual.transform.localScale = Vector3.one * matchingScale;
        }

        // Use a box collider sized from the rendered Wraith sprite.
        // DungeonEnemy synchronizes its dimensions after the visual is assigned.
        var collider = enemyObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;

        // Enemies should keep their solid collider for walls, but must not
        // physically shove the player away on contact. Melee range is handled
        // by the distance check in DungeonEnemy.Update().
        Collider2D playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        if (playerCollider != null)
            Physics2D.IgnoreCollision(collider, playerCollider, true);

        var brain = enemyObject.AddComponent<DungeonEnemy>();
        brain.game = this;
        brain.target = player;
        brain.speed = enemySpeed + (dungeonLevel - 1) * 0.06f;
        brain.SetHealth(hp + dungeonLevel - 1);
        brain.SetWraithAnimations(renderer, idle, GetWraithFrames(variant, 1), GetWraithFrames(variant, 2),
            GetWraithFrames(variant, 3), GetWraithFrames(variant, 4));
        brain.xpReward = 25 + variant * 5;
        brain.coinDropChance = 0.30f + variant * 0.05f;
        brain.potionDropChance = 0.10f + variant * 0.02f;
        brain.xpReward += Mathf.Max(0, dungeonLevel - 1) * 3;
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
        // Spread hazards around the room so they read as separate obstacles,
        // rather than pairs of tiny decorations.
        Vector2[] positions = {
            new Vector2(-7, 3), new Vector2(6, -3),
            new Vector2(11, 0), new Vector2(-11, 0),
            new Vector2(0, 8), new Vector2(0, -8)
        };
        Sprite trapArt = packSpikeSprite ? packSpikeSprite : spikeSprite;
        if (!trapArt) {
            Debug.LogWarning("Dungeon Tale: No spike sprite was found. Check the packSpikeSprite assignment in DungeonTaleBootstrap.");
            return;
        }

        foreach (var p in positions) {
            // Dark warning plate makes the silver spikes stand out against stone.
            var plate = new GameObject("Spike Trap Warning Plate");
            dungeonObjects.Add(plate);
            plate.transform.position = new Vector3(p.x, p.y, 0.1f);
            plate.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var plateRenderer = plate.AddComponent<SpriteRenderer>();
            plateRenderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            plateRenderer.color = new Color(0.32f, 0.055f, 0.045f, 0.9f);
            plateRenderer.sortingOrder = 7;

            // Keep the hazard visually smaller than the hero and behind
            // characters, so it never hides the player sprite.
            var g = MakePickup("Spike Trap", trapArt, p, 4f);
            g.transform.position = new Vector3(p.x, p.y, 0f);
            var sr = g.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = 8;

            var hitbox = g.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.size = new Vector2(0.7f, 0.7f);

            g.AddComponent<DungeonSpike>().game = this;
        }
    }
    void CreateExit(){var g=MakePickup("Dungeon Exit",doorSprite,new Vector2(15,0),1.8f);g.AddComponent<BoxCollider2D>().isTrigger=true;g.AddComponent<DungeonExit>().game=this;}
    void CreateUI() {
        // The project uses Unity's New Input System, so UI buttons need its UI input module.
        if (EventSystem.current == null)
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

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
        CreateGameOverlay(canvas.transform);
        CreateLevelClearBanner(canvas.transform);
        CreateExperienceBar(canvas.transform);
        CreateLevelUpPanel(canvas.transform);
    }
    

    void CreateExperienceBar(Transform parent)
    {
        var bg = new GameObject("Experience Bar Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        var r = bg.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(24, -108); r.sizeDelta = new Vector2(300, 18);
        var image = bg.GetComponent<Image>(); image.sprite = uiWhiteSprite;
        image.color = new Color(0.035f, 0.04f, 0.055f, 0.98f); image.raycastTarget = false;
        var fill = new GameObject("Experience Bar Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        experienceFillRect = fill.GetComponent<RectTransform>();
        experienceFillRect.anchorMin = Vector2.zero; experienceFillRect.anchorMax = new Vector2(0, 1);
        experienceFillRect.pivot = new Vector2(0, 0.5f); experienceFillRect.anchoredPosition = new Vector2(3, 0);
        experienceFillRect.sizeDelta = new Vector2(294, -6);
        experienceFill = fill.GetComponent<Image>(); experienceFill.sprite = uiWhiteSprite;
        experienceFill.color = new Color(0.25f, 0.65f, 1f, 1f); experienceFill.raycastTarget = false;
        experienceLabel = MakeText(parent, "Experience Label", new Vector2(336, -103), new Vector2(260, 28), 18, TextAnchor.UpperLeft);
        experienceLabel.fontStyle = FontStyle.Bold;
    }

    void CreateLevelUpPanel(Transform parent)
    {
        levelUpPanel = new GameObject("Level Up Choice Panel", typeof(RectTransform), typeof(Image));
        levelUpPanel.transform.SetParent(parent, false);
        var pr = levelUpPanel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0.5f, 0.5f); pr.anchoredPosition = Vector2.zero; pr.sizeDelta = new Vector2(1180, 520);
        var panelImage = levelUpPanel.GetComponent<Image>(); panelImage.sprite = uiWhiteSprite;
        panelImage.color = new Color(0.035f, 0.025f, 0.02f, 0.98f); panelImage.raycastTarget = true;

        var heading = MakeCenteredText(levelUpPanel.transform, "Level Up Heading", new Vector2(0, 215),
            new Vector2(1080, 62), 38, FontStyle.Bold);
        heading.text = "LEVEL UP!  CHOOSE ONE CARD"; heading.color = new Color(0.72f, 0.94f, 0.58f, 1f); heading.raycastTarget = false;

        var cardsRoot = new GameObject("Upgrade Cards", typeof(RectTransform));
        cardsRoot.transform.SetParent(levelUpPanel.transform, false);
        levelUpCardsRoot = cardsRoot.transform;
        var cardsRect = cardsRoot.GetComponent<RectTransform>();
        cardsRect.anchorMin = cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRect.pivot = new Vector2(0.5f, 0.5f);
        cardsRect.anchoredPosition = new Vector2(0, -18);
        cardsRect.sizeDelta = new Vector2(1120, 390);
        levelUpPanel.SetActive(false);
    }

    void PopulateLevelUpCards()
    {
        if (levelUpCardsRoot == null) return;
        for (int i = levelUpCardsRoot.childCount - 1; i >= 0; i--)
            Destroy(levelUpCardsRoot.GetChild(i).gameObject);

        string[] titles = { "POWER", "SPEED", "CRITICAL STRIKE", "VITALITY", "ENERGY SHIELD", "DODGE", "XP BOOST" };
        string[] descriptions = {
            "Strengthen your weapon and deal more damage with every sword strike.",
            "Move faster to chase enemies and escape dangerous attacks.",
            "Sharpen your aim and increase the chance of dealing double damage.",
            "Increase your maximum health and restore health immediately.",
            "Gain a protective shield that blocks one incoming hit.",
            "Improve your reflexes and gain a chance to evade enemy damage.",
            "Gain bonus experience whenever you defeat a monster."
        };
        string[] statLabels = { " +1 SWORD DAMAGE", " +0.6 MOVE SPEED", " +10% CRITICAL CHANCE", " +2 MAX HEALTH", " +1 SHIELD CHARGE", " +10% DODGE CHANCE", " +25% XP GAIN" };
        Sprite[] art = {
            swordSprite, torchSprite, swordSprite, potionRed,
            shieldSprite, shieldSprite, packCoinSprite ? packCoinSprite : coinSprite
        };
        Color parchment = new Color(0.88f, 0.77f, 0.57f, 1f);
        Color ink = new Color(0.16f, 0.12f, 0.075f, 1f);
        Color green = new Color(0.12f, 0.36f, 0.13f, 1f);

        var heading = levelUpPanel.transform.Find("Level Up Heading");
        if (heading != null)
        {
            var headingText = heading.GetComponent<Text>();
            if (headingText != null) headingText.text = showingLevelBonusChoice ? "LEVEL COMPLETE BONUS! CHOOSE ONE CARD" : "LEVEL UP!  CHOOSE ONE CARD";
        }

        List<int> availableChoices = new List<int>();
        for (int i = 0; i < titles.Length; i++) availableChoices.Add(i);
        for (int i = availableChoices.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = availableChoices[i]; availableChoices[i] = availableChoices[swapIndex]; availableChoices[swapIndex] = temp;
        }

        for (int i = 0; i < 3; i++)
        {
            int choice = availableChoices[i];
            var card = new GameObject(titles[choice] + " Card", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(levelUpCardsRoot, false);
            var cr = card.GetComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = new Vector2((i - 1) * 365, 0);
            cr.sizeDelta = new Vector2(330, 370);

            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = uiWhiteSprite;
            cardImage.color = new Color(0.055f, 0.035f, 0.025f, 1f);

            var button = card.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.76f, 1f);
            colors.pressedColor = new Color(0.82f, 0.72f, 0.53f, 1f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            button.onClick.AddListener(() => ChooseUpgrade(choice));

            // Thick dark frame and warm parchment face, inspired by the supplied
            // fantasy item-card reference.
            var paper = new GameObject("Parchment Card Face", typeof(RectTransform), typeof(Image));
            paper.transform.SetParent(card.transform, false);
            var paperRect = paper.GetComponent<RectTransform>();
            paperRect.anchorMin = Vector2.zero; paperRect.anchorMax = Vector2.one;
            paperRect.offsetMin = new Vector2(7, 7); paperRect.offsetMax = new Vector2(-7, -7);
            paper.GetComponent<Image>().sprite = uiWhiteSprite;
            paper.GetComponent<Image>().color = parchment;
            paper.GetComponent<Image>().raycastTarget = false;

            var topShade = new GameObject("Illustration Backdrop", typeof(RectTransform), typeof(Image));
            topShade.transform.SetParent(paper.transform, false);
            var shadeRect = topShade.GetComponent<RectTransform>();
            shadeRect.anchorMin = new Vector2(0, 0.49f); shadeRect.anchorMax = new Vector2(1, 1);
            shadeRect.offsetMin = new Vector2(5, 0); shadeRect.offsetMax = new Vector2(-5, -5);
            topShade.GetComponent<Image>().sprite = uiWhiteSprite;
            topShade.GetComponent<Image>().color = new Color(0.18f, 0.12f, 0.09f, 1f);
            topShade.GetComponent<Image>().raycastTarget = false;

            var title = MakeCenteredText(paper.transform, "Card Title", new Vector2(0, -15),
                new Vector2(304, 58), 22, FontStyle.Bold);
            title.text = titles[choice];
            title.color = new Color(0.25f, 0.78f, 0.28f, 1f);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 22;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            title.raycastTarget = false;

            var illustration = new GameObject("Upgrade Illustration", typeof(RectTransform), typeof(Image));
            illustration.transform.SetParent(topShade.transform, false);
            var artRect = illustration.GetComponent<RectTransform>();
            artRect.anchorMin = artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.anchoredPosition = new Vector2(0, -10);
            artRect.sizeDelta = new Vector2(118, 118);
            var artImage = illustration.GetComponent<Image>();
            artImage.sprite = art[choice] ? art[choice] : uiWhiteSprite;
            artImage.color = art[choice] ? Color.white : new Color(0.78f, 0.68f, 0.48f, 1f);
            artImage.preserveAspect = true;
            artImage.raycastTarget = false;

            var stat = MakeCenteredText(paper.transform, "Card Stat", new Vector2(0, -119),
                new Vector2(300, 44), 16, FontStyle.Bold);
            stat.text = statLabels[choice];
            stat.color = green;
            stat.resizeTextForBestFit = true;
            stat.resizeTextMinSize = 13;
            stat.resizeTextMaxSize = 16;
            stat.horizontalOverflow = HorizontalWrapMode.Wrap;
            stat.verticalOverflow = VerticalWrapMode.Truncate;
            stat.raycastTarget = false;

            var desc = MakeCenteredText(paper.transform, "Card Description", new Vector2(0, -174),
                new Vector2(286, 94), 14, FontStyle.Normal);
            desc.text = descriptions[choice];
            desc.color = ink;
            desc.alignment = TextAnchor.UpperLeft;
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 12;
            desc.resizeTextMaxSize = 14;
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Truncate;
            desc.raycastTarget = false;

            var footer = new GameObject("Card Footer", typeof(RectTransform), typeof(Image));
            footer.transform.SetParent(paper.transform, false);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0); footerRect.anchorMax = new Vector2(1, 0);
            footerRect.pivot = new Vector2(0.5f, 0);
            footerRect.anchoredPosition = new Vector2(0, 0);
            footerRect.sizeDelta = new Vector2(0, 34);
            footer.GetComponent<Image>().sprite = uiWhiteSprite;
            footer.GetComponent<Image>().color = new Color(0.32f, 0.22f, 0.12f, 1f);
            footer.GetComponent<Image>().raycastTarget = false;

            var choose = MakeCenteredText(footer.transform, "Choose Label", Vector2.zero,
                new Vector2(300, 30), 15, FontStyle.Bold);
            choose.text = "SELECT CARD";
            choose.color = new Color(0.96f, 0.86f, 0.63f, 1f);
            choose.resizeTextForBestFit = true;
            choose.resizeTextMinSize = 12;
            choose.resizeTextMaxSize = 15;
            choose.raycastTarget = false;
        }
    }

    void ShowLevelUpChoice()
    {
        if (upgradeChoiceOpen || levelUpPanel == null) return;
        showingLevelBonusChoice = false;
        upgradeChoiceOpen = true;
        PopulateLevelUpCards();
        levelUpPanel.SetActive(true);
        UpdateHud();
    }

    void ShowLevelBonusChoice()
    {
        if (!levelBonusPending || upgradeChoiceOpen || levelUpPanel == null) return;
        showingLevelBonusChoice = true;
        upgradeChoiceOpen = true;
        PopulateLevelUpCards();
        levelUpPanel.SetActive(true);
        UpdateHud();
    }

    void ChooseUpgrade(int choice)
    {
        if (!upgradeChoiceOpen) return;
        switch (choice)
        {
            case 0:
                attackPower++;
                ShowMessage("POWER UP! Sword damage increased.");
                break;
            case 1:
                playerSpeedBonus += 0.6f;
                ShowMessage("SPEED UP! You move faster.");
                break;
            case 2:
                criticalChance = Mathf.Min(0.75f, criticalChance + 0.10f);
                ShowMessage("CRITICAL STRIKE! Critical chance increased.");
                break;
            case 3:
                maxHealth += 2;
                playerHealth = Mathf.Min(maxHealth, playerHealth + 2);
                healthRegenEnabled = true;
                if (nextHealthRegen <= Time.time) nextHealthRegen = Time.time + healthRegenInterval;
                ShowMessage("VITALITY! Maximum health increased and you recovered 2 HP.");
                break;
            case 4:
                maxShieldCharges++;
                shieldCharges = Mathf.Min(maxShieldCharges, shieldCharges + 1);
                if (nextShieldRegen <= Time.time) nextShieldRegen = Time.time + shieldRegenInterval;
                ShowMessage("ENERGY SHIELD! A shield charge is ready.");
                break;
            case 5:
                dodgeChance = Mathf.Min(0.60f, dodgeChance + 0.10f);
                ShowMessage("DODGE UP! Chance to avoid damage increased.");
                break;
            case 6:
                experienceGainMultiplier += 0.25f;
                ShowMessage("XP BOOST! You now earn 25% more XP.");
                break;
        }
        bool selectedLevelBonus = showingLevelBonusChoice;
        showingLevelBonusChoice = false;
        upgradeChoiceOpen = false;
        levelUpPanel.SetActive(false);
        UpdateHud();
        if (selectedLevelBonus)
        {
            levelBonusPending = false;
            EnterNextDungeon();
            return;
        }
        if (experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel; playerLevel++;
            experienceToNextLevel = 100 + (playerLevel - 1) * 50;
            ShowLevelUpChoice();
            return;
        }
        if (levelBonusPending) ShowLevelBonusChoice();
    }

    void AddExperience(int amount)
    {
        experience += Mathf.Max(0, Mathf.RoundToInt(amount * experienceGainMultiplier));
        if (experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel; playerLevel++;
            experienceToNextLevel = 100 + (playerLevel - 1) * 50;
            ShowLevelUpChoice();
        }
        UpdateHud();
    }

    public float GetPlayerMoveSpeed(float baseSpeed) { return baseSpeed + playerSpeedBonus; }
    public int RollPlayerAttackDamage()
    {
        bool critical = Random.value < criticalChance;
        if (critical) ShowMessage("CRITICAL HIT!");
        return attackPower * (critical ? 2 : 1);
    }

    void CreateLevelClearBanner(Transform parent)
    {
        levelClearBanner = new GameObject("Level Clear Banner", typeof(RectTransform), typeof(Image));
        levelClearBanner.transform.SetParent(parent, false);
        var bannerRect = levelClearBanner.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.anchoredPosition = Vector2.zero;
        bannerRect.sizeDelta = new Vector2(620, 190);
        var bannerImage = levelClearBanner.GetComponent<Image>();
        bannerImage.color = new Color(0.025f, 0.12f, 0.16f, 0.96f);
        bannerImage.raycastTarget = false;

        levelClearTitle = MakeCenteredText(levelClearBanner.transform, "Level Clear Title",
            new Vector2(0, 42), new Vector2(580, 76), 48, FontStyle.Bold);
        levelClearTitle.color = new Color(0.35f, 1f, 0.72f, 1f);
        levelClearTitle.text = "LEVEL 1 CLEAR!";
        levelClearSubtitle = MakeCenteredText(levelClearBanner.transform, "Level Clear Subtitle",
            new Vector2(0, -35), new Vector2(560, 52), 22, FontStyle.Normal);
        levelClearSubtitle.text = "All monsters defeated! Enter the portal to continue.";
        levelClearBanner.SetActive(false);
    }

    GameObject CreateOverlayPanel(Transform parent, string objectName, Vector2 size)
    {
        var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(0.055f, 0.085f, 0.13f, 0.98f);
        return panel;
    }

    void CreateGameOverlay(Transform parent)
    {
        screenOverlay = new GameObject("Game Screen Overlay", typeof(RectTransform), typeof(Image));
        screenOverlay.transform.SetParent(parent, false);
        var overlayRect = screenOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero; overlayRect.offsetMax = Vector2.zero;
        var overlayImage = screenOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0.015f, 0.025f, 0.05f, 0.96f);
        overlayImage.raycastTarget = true;

        menuPanel = CreateOverlayPanel(screenOverlay.transform, "Main Menu Panel", new Vector2(620, 700));
        overlayTitle = MakeCenteredText(menuPanel.transform, "Screen Title", new Vector2(0, 275), new Vector2(560, 80), 48, FontStyle.Bold);
        overlaySubtitle = MakeCenteredText(menuPanel.transform, "Screen Description", new Vector2(0, 205), new Vector2(540, 55), 18, FontStyle.Normal);
        MakeMenuButton(menuPanel.transform, "Start Game", 105, StartNewGame);
        continueButton = MakeMenuButton(menuPanel.transform, "Continue", 25, ContinueGame);
        MakeMenuButton(menuPanel.transform, "Choose Character", -55, ShowCharacterSelection);
        MakeMenuButton(menuPanel.transform, "Settings", -135, ShowSettings);
        MakeMenuButton(menuPanel.transform, "Exit Game", -215, ExitGame);

        characterPanel = CreateOverlayPanel(screenOverlay.transform, "Choose Character Panel", new Vector2(850, 560));
        MakeCenteredText(characterPanel.transform, "Character Screen Title", new Vector2(0, 220), new Vector2(760, 65), 38, FontStyle.Bold).text = "CHOOSE YOUR CHARACTER";
        characterSelectionLabel = MakeCenteredText(characterPanel.transform, "Character Selected", new Vector2(0, -155), new Vector2(760, 42), 20, FontStyle.Bold);
        string[] characterNames = { "CURRENT HERO", "SOLDIER", "ORC" };
        Sprite[] previews = { playerIdleSprite ? playerIdleSprite : heroSprite, soldierIdleSprite, orcIdleSprite };
        for (int i = 0; i < 3; i++)
        {
            int characterIndex = i;
            var card = new GameObject(characterNames[i] + " Choice", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(characterPanel.transform, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i - 1) * 260, 15);
            rect.sizeDelta = new Vector2(220, 280);
            card.GetComponent<Image>().sprite = uiWhiteSprite;
            card.GetComponent<Image>().color = new Color(0.12f, 0.2f, 0.29f, 1f);
            var button = card.GetComponent<Button>();
            button.onClick.AddListener(() => SelectCharacter(characterIndex));
            characterButtons[i] = button;
            var portrait = new GameObject("Character Preview", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(card.transform, false);
            var pr = portrait.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.anchoredPosition = new Vector2(0, 35);
            pr.sizeDelta = new Vector2(125, 125);
            var portraitImage = portrait.GetComponent<Image>();
            portraitImage.sprite = previews[i];
            portraitImage.preserveAspect = true;
            portraitImage.color = previews[i] ? Color.white : new Color(1f, 1f, 1f, 0.12f);
            characterPortraitImages[i] = portraitImage;
            var name = MakeCenteredText(card.transform, "Character Name", new Vector2(0, -105), new Vector2(200, 45), 22, FontStyle.Bold);
            name.text = characterNames[i];
        }
        MakeMenuButton(characterPanel.transform, "Back", -225, ShowMainMenu);
        characterPanel.SetActive(false);

        settingsPanel = CreateOverlayPanel(screenOverlay.transform, "Settings Panel", new Vector2(700, 560));
        MakeCenteredText(settingsPanel.transform, "Settings Title", new Vector2(0, 220), new Vector2(620, 60), 40, FontStyle.Bold).text = "SETTINGS";
        MakeCenteredText(settingsPanel.transform, "Master Volume Label", new Vector2(0, 135), new Vector2(520, 35), 22, FontStyle.Bold).text = "MASTER AUDIO VOLUME";
        var sliderObject = new GameObject("Master Volume Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(settingsPanel.transform, false);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0, 85);
        sliderRect.sizeDelta = new Vector2(430, 28);
        var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
        track.transform.SetParent(sliderObject.transform, false);
        var tr = track.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        track.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.3f, 1f);
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        var far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = new Vector2(0, 0.25f); far.anchorMax = new Vector2(1, 0.75f); far.offsetMin = new Vector2(4, 0); far.offsetMax = new Vector2(-4, 0);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = new Color(0.2f, 0.75f, 0.95f, 1f);
        var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        var har = handleArea.GetComponent<RectTransform>();
        har.anchorMin = Vector2.zero; har.anchorMax = Vector2.one; har.offsetMin = new Vector2(8, 0); har.offsetMax = new Vector2(-8, 0);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var hr = handle.GetComponent<RectTransform>();
        hr.sizeDelta = new Vector2(22, 32);
        handle.GetComponent<Image>().color = Color.white;
        masterVolumeSlider = sliderObject.GetComponent<Slider>();
        masterVolumeSlider.fillRect = fr; masterVolumeSlider.handleRect = hr;
        masterVolumeSlider.targetGraphic = handle.GetComponent<Image>();
        masterVolumeSlider.minValue = 0f; masterVolumeSlider.maxValue = 1f;
        masterVolumeSlider.value = PlayerPrefs.GetFloat(SavePrefix + "Volume", 1f);
        masterVolumeSlider.onValueChanged.AddListener(value => { AudioListener.volume = value; PlayerPrefs.SetFloat(SavePrefix + "Volume", value); });
        qualityButton = MakeMenuButton(settingsPanel.transform, "Graphics Quality", 5, CycleGraphicsQuality);
        qualityLabel = qualityButton.GetComponentInChildren<Text>();
        fullscreenButton = MakeMenuButton(settingsPanel.transform, "Fullscreen", -75, ToggleFullscreen);
        fullscreenLabel = fullscreenButton.GetComponentInChildren<Text>();
        MakeMenuButton(settingsPanel.transform, "Back", -190, ShowMainMenu);
        settingsPanel.SetActive(false);

        endPanel = CreateOverlayPanel(screenOverlay.transform, "End Screen Panel", new Vector2(620, 440));
        endTitle = MakeCenteredText(endPanel.transform, "End Screen Title", new Vector2(0, 125), new Vector2(560, 80), 42, FontStyle.Bold);
        endSubtitle = MakeCenteredText(endPanel.transform, "End Screen Description", new Vector2(0, 35), new Vector2(540, 90), 18, FontStyle.Normal);
        primaryOverlayButton = MakeOverlayButton(endPanel.transform, "Primary Action", new Vector2(0, -85), "PLAY AGAIN");
        secondaryOverlayButton = MakeOverlayButton(endPanel.transform, "Secondary Action", new Vector2(0, -165), "MAIN MENU");
        endPanel.SetActive(false);
    }

    Button MakeMenuButton(Transform parent, string caption, float y, UnityEngine.Events.UnityAction action)
    {
        var button = MakeOverlayButton(parent, caption + " Button", new Vector2(0, y), caption);
        button.onClick.AddListener(action);
        return button;
    }

    void ShowCharacterSelection()
    {
        menuPanel.SetActive(false); settingsPanel.SetActive(false); endPanel.SetActive(false);
        characterPanel.SetActive(true);
        UpdateCharacterSelectionUI();
    }

    void SelectCharacter(int index)
    {
        selectedCharacter = Mathf.Clamp(index, 0, 2);
        PlayerPrefs.SetInt(SavePrefix + "Character", selectedCharacter);
        PlayerPrefs.Save();
        var controller = player != null ? player.GetComponent<DungeonPlayerController>() : null;
        Sprite chosenSprite = GetCharacterIdleSprite(selectedCharacter);
        if (player != null && chosenSprite != null)
        {
            var playerRenderer = player.GetComponent<SpriteRenderer>();
            if (playerRenderer != null) playerRenderer.sprite = chosenSprite;
        }
        if (controller != null) controller.ApplySelectedCharacter();
        UpdateCharacterSelectionUI();
    }

    Sprite GetCharacterPreviewSprite(int index)
    {
        if (index == 1) return soldierIdleSprite;
        if (index == 2) return orcIdleSprite;
        return playerIdleSprite != null ? playerIdleSprite : heroSprite;
    }

    void UpdateCharacterSelectionUI()
    {
        string[] names = { "CURRENT HERO", "SOLDIER", "ORC" };
        characterSelectionLabel.text = "SELECTED: " + names[selectedCharacter];
        for (int i = 0; i < characterButtons.Length; i++)
        {
            Sprite preview = GetCharacterPreviewSprite(i);
            if (characterPortraitImages[i] != null)
            {
                characterPortraitImages[i].sprite = preview;
                characterPortraitImages[i].color = preview != null ? Color.white : new Color(1f, 0.35f, 0.35f, 0.9f);
                characterPortraitImages[i].preserveAspect = true;
            }
            var image = characterButtons[i].GetComponent<Image>();
            image.color = i == selectedCharacter ? new Color(0.12f, 0.48f, 0.42f, 1f) : new Color(0.12f, 0.2f, 0.29f, 1f);
            characterButtons[i].interactable = preview != null;
        }
    }

    void ShowSettings()
    {
        menuPanel.SetActive(false); characterPanel.SetActive(false); endPanel.SetActive(false);
        settingsPanel.SetActive(true);
        masterVolumeSlider.value = PlayerPrefs.GetFloat(SavePrefix + "Volume", AudioListener.volume);
        UpdateSettingsLabels();
    }

    void UpdateSettingsLabels()
    {
        qualityLabel.text = "GRAPHICS: " + QualitySettings.names[QualitySettings.GetQualityLevel()];
        fullscreenLabel.text = "FULLSCREEN: " + (Screen.fullScreen ? "ON" : "OFF");
    }

    void CycleGraphicsQuality()
    {
        int next = (QualitySettings.GetQualityLevel() + 1) % Mathf.Max(1, QualitySettings.names.Length);
        QualitySettings.SetQualityLevel(next, true);
        PlayerPrefs.SetInt(SavePrefix + "Quality", next);
        UpdateSettingsLabels();
    }

    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        PlayerPrefs.SetInt(SavePrefix + "Fullscreen", Screen.fullScreen ? 1 : 0);
        UpdateSettingsLabels();
    }

    void ExitGame()
    {
        SaveGameProgress();
        Application.Quit();
    }

    public Sprite GetCharacterIdleSprite(int index)
    {
        if (index == 1 && soldierIdleSprite != null) return soldierIdleSprite;
        if (index == 2 && orcIdleSprite != null) return orcIdleSprite;
        return playerIdleSprite != null ? playerIdleSprite : heroSprite;
    }

    public Sprite[] GetCharacterAttackFrames(int index)
    {
        if (index == 1 && soldierAttackFrames != null && soldierAttackFrames.Length > 0) return soldierAttackFrames;
        if (index == 2 && orcAttackFrames != null && orcAttackFrames.Length > 0) return orcAttackFrames;
        return attackRightFrames;
    }

    public Sprite[] GetCharacterWalkFrames(int index)
    {
        if (index == 1 && soldierWalkFrames != null && soldierWalkFrames.Length > 0) return soldierWalkFrames;
        if (index == 2 && orcWalkFrames != null && orcWalkFrames.Length > 0) return orcWalkFrames;
        return heroWalkFrames != null && heroWalkFrames.Length > 0 ? heroWalkFrames : walkDownFrames;
    }

    void SaveGameProgress()
    {
        if (!gameStarted || finished) return;
        PlayerPrefs.SetInt(SavePrefix + "HasSave", 1);
        PlayerPrefs.SetInt(SavePrefix + "Dungeon", dungeonLevel);
        PlayerPrefs.SetInt(SavePrefix + "Health", playerHealth);
        PlayerPrefs.SetInt(SavePrefix + "MaxHealth", maxHealth);
        PlayerPrefs.SetInt(SavePrefix + "Coins", coins);
        PlayerPrefs.SetInt(SavePrefix + "Potions", potions);
        PlayerPrefs.SetInt(SavePrefix + "PlayerLevel", playerLevel);
        PlayerPrefs.SetInt(SavePrefix + "XP", experience);
        PlayerPrefs.SetInt(SavePrefix + "XPNext", experienceToNextLevel);
        PlayerPrefs.SetInt(SavePrefix + "Attack", attackPower);
        PlayerPrefs.SetFloat(SavePrefix + "Crit", criticalChance);
        PlayerPrefs.SetFloat(SavePrefix + "Speed", playerSpeedBonus);
        PlayerPrefs.SetInt(SavePrefix + "ShieldMax", maxShieldCharges);
        PlayerPrefs.SetInt(SavePrefix + "Shield", shieldCharges);
        PlayerPrefs.SetFloat(SavePrefix + "Dodge", dodgeChance);
        PlayerPrefs.SetFloat(SavePrefix + "XPMultiplier", experienceGainMultiplier);
        PlayerPrefs.SetInt(SavePrefix + "HealthRegen", healthRegenEnabled ? 1 : 0);
        if (player != null) { PlayerPrefs.SetFloat(SavePrefix + "PlayerX", player.position.x); PlayerPrefs.SetFloat(SavePrefix + "PlayerY", player.position.y); }
        PlayerPrefs.SetInt(SavePrefix + "Character", selectedCharacter);
        PlayerPrefs.Save();
        hasSavedGame = true;
        if (continueButton != null) continueButton.interactable = true;
    }

    void ContinueGame()
    {
        if (!hasSavedGame) return;
        dungeonLevel = Mathf.Max(1, PlayerPrefs.GetInt(SavePrefix + "Dungeon", 1));
        playerHealth = PlayerPrefs.GetInt(SavePrefix + "Health", maxHealth);
        maxHealth = PlayerPrefs.GetInt(SavePrefix + "MaxHealth", 6);
        coins = PlayerPrefs.GetInt(SavePrefix + "Coins", 0);
        potions = PlayerPrefs.GetInt(SavePrefix + "Potions", 0);
        playerLevel = PlayerPrefs.GetInt(SavePrefix + "PlayerLevel", 1);
        experience = PlayerPrefs.GetInt(SavePrefix + "XP", 0);
        experienceToNextLevel = PlayerPrefs.GetInt(SavePrefix + "XPNext", 100);
        attackPower = PlayerPrefs.GetInt(SavePrefix + "Attack", 1);
        criticalChance = PlayerPrefs.GetFloat(SavePrefix + "Crit", 0.05f);
        playerSpeedBonus = PlayerPrefs.GetFloat(SavePrefix + "Speed", 0f);
        maxShieldCharges = PlayerPrefs.GetInt(SavePrefix + "ShieldMax", 0);
        shieldCharges = PlayerPrefs.GetInt(SavePrefix + "Shield", 0);
        dodgeChance = PlayerPrefs.GetFloat(SavePrefix + "Dodge", 0f);
        experienceGainMultiplier = PlayerPrefs.GetFloat(SavePrefix + "XPMultiplier", 1f);
        healthRegenEnabled = PlayerPrefs.GetInt(SavePrefix + "HealthRegen", 0) == 1;
        selectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "Character", 0), 0, 2);
        if (dungeonLevel > 1)
        {
            foreach (var oldObject in dungeonObjects) if (oldObject != null) Destroy(oldObject);
            dungeonObjects.Clear();
            BuildNextDungeonLayout(); DecorateDungeon(); CreateLevelEnemies(); CreatePickups(); CreateTraps();
        }
        if (player != null)
        {
            player.position = new Vector3(PlayerPrefs.GetFloat(SavePrefix + "PlayerX", -14f), PlayerPrefs.GetFloat(SavePrefix + "PlayerY", 0f), 0f);
            var controller = player.GetComponent<DungeonPlayerController>();
            if (controller != null) controller.ApplySelectedCharacter();
        }
        UpdateHud();
        gameStarted = true;
        screenOverlay.SetActive(false);

        ShowMessage("CONTINUE - Your last dungeon checkpoint has been loaded.");
    }

    void StartNewGame()
    {
        PlayerPrefs.DeleteKey(SavePrefix + "HasSave");
        PlayerPrefs.SetInt(SavePrefix + "StartImmediately", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void BeginNewGame()
    {
        hasSavedGame = false;
        dungeonLevel = 1; playerHealth = maxHealth = 6; coins = potions = 0;
        playerLevel = 1; experience = 0; experienceToNextLevel = 100; attackPower = 1;
        criticalChance = 0.05f; playerSpeedBonus = 0f; maxShieldCharges = shieldCharges = 0;
        dodgeChance = 0f; experienceGainMultiplier = 1f; healthRegenEnabled = false;
        if (player != null) player.position = new Vector3(-14, 0, 0);
        SaveNewGameCheckpoint();
        StartGame();
    }

    void SaveNewGameCheckpoint()
    {
        gameStarted = true;
        SaveGameProgress();
        gameStarted = false;
    }

    void StartGame()
    {
        gameStarted = true;
        if (screenOverlay != null) screenOverlay.SetActive(false);

        ShowMessage("Defeat every monster, collect the key, and reach the exit!");
    }

    void ShowMainMenu()
    {
        gameStarted = false;
        if (screenOverlay == null) return;
        screenOverlay.SetActive(true);
        menuPanel.SetActive(true); characterPanel.SetActive(false); settingsPanel.SetActive(false); endPanel.SetActive(false);
        overlayTitle.text = "DUNGEON TALE";
        overlaySubtitle.text = "Explore dangerous dungeons, defeat monsters, collect treasure, and survive!";
        continueButton.interactable = hasSavedGame;
        UpdateCharacterSelectionUI();
    }

    void ShowEndScreen(bool won)
    {
        gameStarted = false;
        if (screenOverlay == null) return;
        SaveGameProgress();
        screenOverlay.SetActive(true);
        menuPanel.SetActive(false); characterPanel.SetActive(false); settingsPanel.SetActive(false); endPanel.SetActive(true);
        endTitle.text = won ? "DUNGEON CLEARED!" : "GAME OVER";
        endSubtitle.text = won
            ? "You escaped the dungeon! Gold collected: " + coins + "    Dungeon reached: " + dungeonLevel
            : "Your adventure has ended. Gold collected: " + coins + "    Dungeon reached: " + dungeonLevel;
        primaryOverlayButton.GetComponentInChildren<Text>().text = "PLAY AGAIN";
        primaryOverlayButton.onClick.RemoveAllListeners();
        primaryOverlayButton.onClick.AddListener(StartNewGame);
        secondaryOverlayButton.onClick.RemoveAllListeners();
        secondaryOverlayButton.onClick.AddListener(ShowMainMenu);
    }

    void RestartGame() { StartNewGame(); }
    void ReturnToMainMenu() { SaveGameProgress(); ShowMainMenu(); }

    Text MakeCenteredText(Transform parent, string objectName, Vector2 position, Vector2 size, int fontSize, FontStyle style)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    Button MakeOverlayButton(Transform parent, string objectName, Vector2 position, string caption)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260, 58);
        var image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.48f, 0.72f, 1f);
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.48f, 0.72f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.62f, 0.88f, 1f);
        colors.pressedColor = new Color(0.07f, 0.32f, 0.52f, 1f);
        button.colors = colors;
        var label = MakeCenteredText(go.transform, "Button Label", Vector2.zero, new Vector2(250, 54), 21, FontStyle.Bold);
        label.text = caption;
        return button;
    }

    Text MakeText(Transform parent,string n,Vector2 pos,Vector2 size,int fs,TextAnchor align){
        var g=new GameObject(n);g.transform.SetParent(parent,false);var rt=g.AddComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=pos;rt.sizeDelta=size;
        var t=g.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fs;t.color=Color.white;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Overflow;return t;
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
        if (hud) hud.text = $"DUNGEON TALE     LEVEL: {dungeonLevel}     WAVE: {currentWave}/{WavesPerLevel}     GOLD: {coins}     POTIONS: {potions}     MONSTERS: {enemiesRemaining}";
        float healthRatio = maxHealth > 0 ? Mathf.Clamp01((float)playerHealth / maxHealth) : 0f;
        if (healthFillRect) healthFillRect.sizeDelta = new Vector2(292f * healthRatio, -8f);
        if (healthFill)
        {
            healthFill.color = playerHealth <= 2 ? new Color(0.95f, 0.12f, 0.1f) :
                               playerHealth <= 4 ? new Color(1f, 0.62f, 0.08f) :
                               new Color(0.15f, 0.85f, 0.28f);
        }
        if (healthLabel) healthLabel.text = maxShieldCharges > 0 ? $"HEALTH  {playerHealth}/{maxHealth}   SHIELD {shieldCharges}/{maxShieldCharges}" : $"HEALTH  {playerHealth}/{maxHealth}";
        if (experienceFillRect) experienceFillRect.sizeDelta = new Vector2(294f * Mathf.Clamp01((float)experience / Mathf.Max(1, experienceToNextLevel)), -6f);
        if (experienceLabel) experienceLabel.text = "LV " + playerLevel + "   XP " + experience + "/" + experienceToNextLevel;
    }
    public void ShowMessage(string s){if(message)message.text=s;}
    public void DamagePlayer()
    {
        if (finished || Time.time < invulnerableUntil) return;
        invulnerableUntil = Time.time + 0.8f;
        if (dodgeChance > 0f && Random.value < dodgeChance)
        {
            ShowMessage("DODGED! You avoided the attack.");
            return;
        }
        if (shieldCharges > 0)
        {
            shieldCharges--;
            ShowMessage("SHIELD BLOCKED THE HIT!");
            UpdateHud();
            return;
        }
        playerHealth--;
        if (healthRegenEnabled && nextHealthRegen <= 0f) nextHealthRegen = Time.time + healthRegenInterval;
        UpdateHud();
        if (playerHealth <= 0) { finished = true; ShowEndScreen(false); }
    }
    public void EnemyDefeated(int xpReward, Vector2 dropPosition, float coinChance, float potionChance)
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        UpdateHud();
        AddExperience(xpReward);
        // Roll one drop per defeated monster. Potions are rarer than coins.
        float dropRoll = Random.value;
        if (dropRoll < potionChance && (packFlaskSprite || potionRed))
        {
            Sprite potionArt = packFlaskSprite ? packFlaskSprite : potionRed;
            var potionDrop = MakePickup("Monster Potion Drop", potionArt, dropPosition + Random.insideUnitCircle * 0.35f, 0.85f);
            potionDrop.AddComponent<CircleCollider2D>().isTrigger = true;
            potionDrop.AddComponent<DungeonItemPickup>().game = this;
            ShowMessage("Rare drop! A healing potion appeared.");
        }
        else if (dropRoll < potionChance + coinChance && (packCoinSprite || coinSprite))
        {
            var coinDrop = MakePickup("Monster Gold", packCoinSprite ? packCoinSprite : coinSprite,
                dropPosition + Random.insideUnitCircle * 0.35f, 0.75f);
            coinDrop.AddComponent<CircleCollider2D>().isTrigger = true;
            coinDrop.AddComponent<DungeonCoin>().game = this;
            ShowMessage("Monster dropped gold!");
        }
        if (enemiesRemaining == 0)
        {
            if (currentWave < WavesPerLevel)
            {
                ShowMessage("WAVE " + currentWave + " CLEAR!");
                if (levelClearBanner != null)
                {
                    levelClearTitle.text = "WAVE " + currentWave + " CLEAR!";
                    levelClearSubtitle.text = "Get ready for wave " + (currentWave + 1) + ".";
                    levelClearBanner.SetActive(true);
                    levelClearBannerHideAt = Time.time + 2f;
                }
                waitingForNextWave = true;
                nextWaveStartAt = Time.time + 2.5f;
            }
            else
            {
                ShowMessage("LEVEL " + dungeonLevel + " CLEAR! Choose your bonus upgrade.");
                if (levelClearBanner != null)
                {
                    levelClearTitle.text = "LEVEL " + dungeonLevel + " CLEAR!";
                    levelClearSubtitle.text = "All 3 waves defeated! Choose one bonus power-up.";
                    levelClearBanner.SetActive(true);
                    levelClearBannerHideAt = -1f;
                }
                levelBonusPending = true;
                ShowLevelBonusChoice();
            }
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
        currentWave = 1;
        waitingForNextWave = false;
        levelBonusPending = false;
        showingLevelBonusChoice = false;
        if (levelClearBanner != null) levelClearBanner.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
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

        ShowMessage("DUNGEON " + dungeonLevel + " - " + GetDungeonName() + "!");
        UpdateHud();
        SaveGameProgress();
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

    public bool IsWalkableDungeonCell(Vector3Int cell)
    {
        return wallMap == null || !wallMap.HasTile(cell);
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
        StartWave(1);
    }

    public void CollectCoin(){coins++;UpdateHud();}
    public void CollectItem(string item){if(item=="Dungeon Key"){hasKey=true;ShowMessage("Dungeon key collected! The exit is unlocked.");}else if(item.Contains("Potion")){potions++;ShowMessage("Potion collected. Press E to restore health.");}UpdateHud();}
    public void UsePotion(){if(potions<=0||playerHealth>=maxHealth)return;potions--;playerHealth=Mathf.Min(maxHealth,playerHealth+3);UpdateHud();ShowMessage("Potion used. Health restored.");}
    public void HitTrap(){DamagePlayer();}
    public void ReachExit(){if(enemiesRemaining>0){ShowMessage("Defeat every monster first!");return;}if(!hasKey){ShowMessage("The exit is locked. Find the dungeon key!");return;}finished=true;ShowEndScreen(true);}
    void Update()
    {if (levelClearBanner != null && levelClearBanner.activeSelf && levelClearBannerHideAt >= 0f && Time.time >= levelClearBannerHideAt)
        {
            levelClearBanner.SetActive(false);
            levelClearBannerHideAt = -1f;
        }

        if (waitingForNextWave && gameStarted && !finished && !upgradeChoiceOpen && Time.time >= nextWaveStartAt)
        {
            StartWave(currentWave + 1);
        }

        if (gameStarted && !finished && !upgradeChoiceOpen)
        {
            if (maxShieldCharges > 0 && shieldCharges < maxShieldCharges && Time.time >= nextShieldRegen)
            {
                shieldCharges++;
                nextShieldRegen = Time.time + shieldRegenInterval;
                ShowMessage("ENERGY SHIELD RECHARGED.");
                UpdateHud();
            }
            if (healthRegenEnabled && maxHealth > playerHealth && nextHealthRegen > 0f && Time.time >= nextHealthRegen)
            {
                playerHealth = Mathf.Min(maxHealth, playerHealth + 1);
                nextHealthRegen = playerHealth < maxHealth ? Time.time + healthRegenInterval : 0f;
                ShowMessage("VITALITY RESTORED 1 HP.");
                UpdateHud();
            }
        }

        if (!gameStarted || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) UsePotion();
    }
    public bool IsFinished(){return finished || !gameStarted || upgradeChoiceOpen;}



}