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

        TileBase[] floors = { floor, FindTile("Floor Green"), FindTile("Floor Metal"), FindTile("Floor Red"), FindTile("Floor Wood"), FindTile("Floor Flash") };
        const int w = 35, h = 25;
        for (int x = -w/2; x <= w/2; x++)
        for (int y = -h/2; y <= h/2; y++) {
            var c = new Vector3Int(x,y,0);
            int variant = Mathf.Abs((x * 13 + y * 7) % floors.Length);
            floorMap.SetTile(c, floors[variant] ? floors[variant] : floor);
            bool border = x == -w/2 || x == w/2 || y == -h/2 || y == h/2;
            bool pillar = (x == -8 || x == 8) && (y == -6 || y == 6);
            bool divider = x == 0 && y >= -7 && y <= 7 && y != 0;
            if (border || pillar || divider) wallMap.SetTile(c, wall);
        }
        floorMap.RefreshAllTiles(); wallMap.RefreshAllTiles();

        var go = new GameObject("Dungeon Tale Game");
        var game = go.AddComponent<DungeonTaleGame>();
        game.floorMap = floorMap; game.wallMap = wallMap; game.floorTile = floor; game.wallTile = wall;
        game.heroSprite = hero;
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
    static Sprite FindSprite(string n) {
        foreach (var id in AssetDatabase.FindAssets("t:Texture2D")) {
            var p = AssetDatabase.GUIDToAssetPath(id);
            if (!p.StartsWith(Root + "Sprites/")) continue;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                if (o is Sprite s && s.name == n) return s;
        }
        return null;
    }
}