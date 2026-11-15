using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class DungeonTaleBootstrap
{
    const string Root = "Assets/Tileset/Dungeon Tale/Assets/";

    static DungeonTaleBootstrap()
    {
        EditorApplication.delayCall += AutoAssignCharacterAssets;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += AutoAssignCharacterAssets;
    }

    static void AutoAssignCharacterAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var game = UnityEngine.Object.FindObjectOfType<DungeonTaleGame>();
        if (game == null) return;
        AssignCharacterSprites(game);
        if (game.soldierIdleSprite == null || game.orcIdleSprite == null)
        {
            Debug.LogWarning("Dungeon Tale: Soldier/Orc sprite sheets were not imported yet. Check the Tiny RPG Character Asset Pack folder is inside Assets.");
            return;
        }
        EditorUtility.SetDirty(game);
        if (game.gameObject.scene.IsValid() && game.gameObject.scene.isLoaded)
            EditorSceneManager.SaveScene(game.gameObject.scene);
        Debug.Log("Dungeon Tale: Soldier and Orc character sprites assigned to the open scene.");
    }

    static void AssignCharacterSprites(DungeonTaleGame game)
    {
        game.soldierIdleSprite = FindCharacterSprite("Soldier", "Idle");
        game.soldierWalkFrames = FindCharacterFrames("Soldier", "Walk");
        game.soldierAttackFrames = FindCharacterFrames("Soldier", "Attack01");
        game.orcIdleSprite = FindCharacterSprite("Orc", "Idle");
        game.orcWalkFrames = FindCharacterFrames("Orc", "Walk");
        game.orcAttackFrames = FindCharacterFrames("Orc", "Attack01");
        AssignWraithAssets(game);
    }
    [MenuItem("Dungeon Tale/Build Playable Dungeon Scene")]
    public static void Build()
    {
        TileBase floor = FindTile("Floor Dirt");
        TileBase wall = FindTile("Wall");
        Sprite hero = FindSprite("Hero");
        if (!floor || !wall || !hero) {
            EditorUtility.DisplayDialog("Dungeon Tale", "Wait for the Dungeon Tale tileset to import, then rebuild.", "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var grid = new GameObject("Dungeon Grid");
        grid.AddComponent<Grid>();
        var floorGo = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
        floorGo.transform.SetParent(grid.transform, false);
        var wallGo = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        wallGo.transform.SetParent(grid.transform, false);
        var floorMap = floorGo.GetComponent<Tilemap>();
        var wallMap = wallGo.GetComponent<Tilemap>();
        wallGo.GetComponent<TilemapRenderer>().sortingOrder = 2;

        const int w = 35, h = 25;
        for (int x = -w/2; x <= w/2; x++)
        for (int y = -h/2; y <= h/2; y++) {
            var c = new Vector3Int(x,y,0);
            // Use one consistent base floor tile. Mixing the themed floor tiles
            // across every cell made the dungeon look like a scrambled atlas.
            floorMap.SetTile(c, floor);
            bool border = x == -w/2 || x == w/2 || y == -h/2 || y == h/2;
            bool pillar = (x == -8 || x == 8) && (y == -6 || y == 6);
            bool divider = x == 0 && y >= -7 && y <= 7 && y != 0;
            if (border || pillar || divider) wallMap.SetTile(c, wall);
        }
        floorMap.RefreshAllTiles(); wallMap.RefreshAllTiles();

        var go = new GameObject("Dungeon Tale Game");
        var game = go.AddComponent<DungeonTaleGame>();
        game.floorMap = floorMap; game.wallMap = wallMap; game.floorTile = floor; game.wallTile = wall;
        game.floorFlashTile = FindTile("Floor Flash");
        game.floorGreenTile = FindTile("Floor Green");
        game.floorMetalTile = FindTile("Floor Metal");
        game.floorRedTile = FindTile("Floor Red");
        game.floorWoodTile = FindTile("Floor Wood");
        game.playerIdleSprite = FindSprite("Player_0");
        game.heroSprite = game.playerIdleSprite ? game.playerIdleSprite : (FindSprite("Hero_0") ? FindSprite("Hero_0") : hero);
        game.walkDownFrames = FindSpriteSequence("Player_", 0, 6);
        game.walkLeftFrames = FindSpriteSequence("Player_", 18, 6);
        game.walkRightFrames = FindSpriteSequence("Player_", 6, 6);
        game.walkUpFrames = FindSpriteSequence("Player_", 12, 6);
        game.heroWalkFrames = game.walkDownFrames;
        // Verified against the Sprite Editor preview: 41-44 is the horizontal
        // right-facing swing, 45-49 is the upward swing, and 36-40 is downward.
        // The controller mirrors the horizontal swing for left (A), avoiding
        // the unrelated/swimming frames Player_50-Player_53.
        game.attackDownFrames = FindSpriteSequence("Player_", 36, 5);
        game.attackLeftFrames = FindSpriteSequence("Player_", 41, 4);
        game.attackRightFrames = FindSpriteSequence("Player_", 41, 4);
        game.attackUpFrames = FindSpriteSequence("Player_", 45, 5);
        game.soldierIdleSprite = FindCharacterSprite("Soldier", "Idle");
        game.soldierWalkFrames = FindCharacterFrames("Soldier", "Walk");
        game.soldierAttackFrames = FindCharacterFrames("Soldier", "Attack01");
        game.orcIdleSprite = FindCharacterSprite("Orc", "Idle");
        game.orcWalkFrames = FindCharacterFrames("Orc", "Walk");
        game.orcAttackFrames = FindCharacterFrames("Orc", "Attack01");
        AssignWraithAssets(game);
        game.slimeSprite = FindSprite("Char_Slime");
        game.skeletonSprite = FindSprite("Char_Skeletone");
        game.batSprite = FindSprite("Char_Bat");
        game.ratSprite = FindSprite("Char_Rat");
        game.coinSprite = FindSprite("Coin_0");
        game.torchSprite = FindSprite("Torch_0");
        game.spikeSprite = FindSprite("Spikes_0");
        game.doorSprite = FindSprite("Env_Door_V");
        game.potionRed = FindSprite("Item_Poition_Red");
        game.potionBlue = FindSprite("Item_Poition_Blue");
        game.potionGreen = FindSprite("Item_Poition_Green");
        game.keySprite = FindSprite("Env_Key");
        game.swordSprite = FindSprite("Item_Sword");
        game.shieldSprite = FindSprite("Item_Shield");
        game.skullSprite = FindSprite("Prop_Skull");
        game.vaseSprite = FindSprite("Prop_Vase_A");
        game.chainSprite = FindSprite("Prop_Chain_A");
        game.boneSprite = FindSprite("Prop_Bone");
        game.carpetSprite = FindSprite("Carpet_C");
        game.packChestSprite = FindSprite("chest_1_0");
        game.packTorchSprite = FindSprite("torch_1_0");
        game.packSpikeSprite = FindSprite("peaks_1_0");
        game.packCoinSprite = FindSprite("coin_1_0");
        game.packFlaskSprite = FindSprite("flasks_1_1_0");

        var cam = Camera.main;
        if (!cam) { var c = new GameObject("Main Camera"); cam = c.AddComponent<Camera>(); cam.tag = "MainCamera"; c.AddComponent<AudioListener>(); }
        cam.orthographic = true; cam.orthographicSize = 9;
        cam.backgroundColor = new Color(.035f,.045f,.07f);
        cam.transform.position = new Vector3(-14,0,-10);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/DungeonTale.unity", true) };
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DungeonTale.unity");
        Selection.activeGameObject = grid;
        SceneView.lastActiveSceneView?.FrameSelected();
        EditorUtility.DisplayDialog("Dungeon Tale", "Dungeon scene saved with the tileset atlas and environment variations. Press Play.", "OK");
    }

    static TileBase FindTile(string n) {
        foreach (var id in AssetDatabase.FindAssets(n)) {
            var p = AssetDatabase.GUIDToAssetPath(id);
            if (!p.StartsWith(Root + "Tiles/")) continue;
            var t = AssetDatabase.LoadAssetAtPath<TileBase>(p);
            if (t && t.name == n) return t;
        }
        return null;
    }
    static Sprite[] FindSpriteSequence(string prefix, int start, int count) {
        var result = new Sprite[count];
        for (int i = 0; i < count; i++) result[i] = FindSprite(prefix + (start + i));
        return result;
    }

    static Sprite FindCharacterSprite(string character, string animation)
    {
        Sprite[] frames = FindCharacterFrames(character, animation);
        return frames.Length > 0 ? frames[0] : null;
    }

    static void EnsureCharacterSheetSliced(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) return;

        bool importerChanged = importer.textureType != TextureImporterType.Sprite;
        importer.textureType = TextureImporterType.Sprite;

        const int cellSize = 100;
        int columns = texture.width / cellSize;
        int rows = texture.height / cellSize;
        if (columns <= 0 || rows <= 0) return;

        bool hasSpriteSubAssets = false;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite) { hasSpriteSubAssets = true; break; }

        if (columns * rows == 1)
        {
            if (importer.spriteImportMode != SpriteImportMode.Single || importerChanged)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return;
        }

        if (importer.spriteImportMode == SpriteImportMode.Multiple && hasSpriteSubAssets) return;

        var metadata = new System.Collections.Generic.List<SpriteMetaData>();
        string baseName = path.Substring(path.LastIndexOf('/') + 1);
        int extension = baseName.LastIndexOf('.');
        if (extension >= 0) baseName = baseName.Substring(0, extension);

        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            metadata.Add(new SpriteMetaData
            {
                name = baseName + "_" + (row * columns + column),
                rect = new Rect(column * cellSize, texture.height - (row + 1) * cellSize, cellSize, cellSize),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }

        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritesheet = metadata.ToArray();
        importer.SaveAndReimport();
    }

    static Sprite[] FindCharacterFrames(string character, string animation)
    {
        string expectedFile = character + "_" + animation + ".png";
        var candidates = new System.Collections.Generic.List<string>();
        foreach (var id in AssetDatabase.FindAssets("t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (path.Contains("Tiny RPG Character Asset Pack") &&
                path.Contains("/" + character + "/") &&
                path.EndsWith(expectedFile, System.StringComparison.OrdinalIgnoreCase))
                candidates.Add(path);
        }
        candidates.Sort((a, b) => {
            bool aPreferred = a.Contains("with shadows");
            bool bPreferred = b.Contains("with shadows");
            if (aPreferred != bPreferred) return aPreferred ? -1 : 1;
            return string.CompareOrdinal(a, b);
        });
        foreach (string path in candidates)
        {
            EnsureCharacterSheetSliced(path);
            var frames = new System.Collections.Generic.List<Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) frames.Add(sprite);
            if (frames.Count == 0)
            {
                var singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (singleSprite != null) frames.Add(singleSprite);
            }
            frames.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));
            if (frames.Count > 0) return frames.ToArray();
        }
        return new Sprite[0];
    }

    [MenuItem("Dungeon Tale/Assign Character Assets To Current Scene")]
    public static void AssignCharacterAssetsToCurrentScene()
    {
        var game = UnityEngine.Object.FindObjectOfType<DungeonTaleGame>();
        if (game == null)
        {
            EditorUtility.DisplayDialog("Dungeon Tale", "Open the DungeonTale scene first.", "OK");
            return;
        }
        game.soldierIdleSprite = FindCharacterSprite("Soldier", "Idle");
        game.soldierWalkFrames = FindCharacterFrames("Soldier", "Walk");
        game.soldierAttackFrames = FindCharacterFrames("Soldier", "Attack01");
        game.orcIdleSprite = FindCharacterSprite("Orc", "Idle");
        game.orcWalkFrames = FindCharacterFrames("Orc", "Walk");
        game.orcAttackFrames = FindCharacterFrames("Orc", "Attack01");
        AssignWraithAssets(game);
        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        EditorUtility.DisplayDialog("Dungeon Tale",
            "Character assets assigned. Soldier frames: " + game.soldierWalkFrames.Length +
            ", Orc frames: " + game.orcWalkFrames.Length + ".", "OK");
    }

    static Sprite[] FindWraithFrames(int variant, string animation, int frameCount)
    {
        string id = "Wraith_0" + variant;
        string folder = "Assets/craftpix-net-563568-free-wraith-tiny-style-2d-sprites/PNG/" + id + "/PNG Sequences/" + animation + "/";
        var result = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < frameCount; i++)
        {
            string fileName;
            if (animation == "Walking") fileName = id + "_Moving Forward_" + i.ToString("000") + ".png";
            else if (animation == "Attacking") fileName = id + "_Attack_" + i.ToString("000") + ".png";
            else if (animation == "Dying") fileName = id + "_Dying_" + i.ToString("000") + ".png";
            else if (animation == "Hurt") fileName = id + "_Hurt_" + i.ToString("000") + ".png";
            else fileName = id + "_Idle_" + i.ToString("000") + ".png";

            string path = folder + fileName;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) result.Add(sprite);
        }
        return result.ToArray();
    }

    static void AssignWraithAssets(DungeonTaleGame game)
    {
        for (int i = 0; i < 3; i++)
        {
            int variant = i + 1;
        game.wraith01IdleFrames = FindWraithFrames(1, "Idle", 12);
        game.wraith01WalkFrames = FindWraithFrames(1, "Walking", 12);
        game.wraith01AttackFrames = FindWraithFrames(1, "Attacking", 12);
        game.wraith01HurtFrames = FindWraithFrames(1, "Hurt", 12);
        game.wraith01DeathFrames = FindWraithFrames(1, "Dying", 15);
        game.wraith02IdleFrames = FindWraithFrames(2, "Idle", 12);
        game.wraith02WalkFrames = FindWraithFrames(2, "Walking", 12);
        game.wraith02AttackFrames = FindWraithFrames(2, "Attacking", 12);
        game.wraith02HurtFrames = FindWraithFrames(2, "Hurt", 12);
        game.wraith02DeathFrames = FindWraithFrames(2, "Dying", 15);
        game.wraith03IdleFrames = FindWraithFrames(3, "Idle", 12);
        game.wraith03WalkFrames = FindWraithFrames(3, "Walking", 12);
        game.wraith03AttackFrames = FindWraithFrames(3, "Attacking", 12);
        game.wraith03HurtFrames = FindWraithFrames(3, "Hurt", 12);
        game.wraith03DeathFrames = FindWraithFrames(3, "Dying", 15);
        }
    }

    static Sprite FindSprite(string n) {
        foreach (var id in AssetDatabase.FindAssets("t:Texture2D")) {
            var p = AssetDatabase.GUIDToAssetPath(id);
            if (!p.StartsWith(Root + "Sprites/") && !p.StartsWith("Assets/player/") && !p.StartsWith("Assets/2D Pixel Dungeon Asset Pack/")) continue;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                if (o is Sprite s && s.name == n) return s;
        }
        return null;
    }
}