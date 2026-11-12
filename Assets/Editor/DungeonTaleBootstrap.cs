using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class DungeonTaleBootstrap
{
    const string Root = "Assets/Tileset/Dungeon Tale/Assets/";
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

    static Sprite FindSprite(string n) {
        foreach (var id in AssetDatabase.FindAssets("t:Texture2D")) {
            var p = AssetDatabase.GUIDToAssetPath(id);
            if (!p.StartsWith(Root + "Sprites/") && !p.StartsWith("Assets/player/")) continue;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                if (o is Sprite s && s.name == n) return s;
        }
        return null;
    }
}