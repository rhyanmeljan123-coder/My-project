using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class DungeonTaleBootstrap
{
    [MenuItem("Dungeon Tale/Build Playable Dungeon Scene")]
    public static void Build()
    {
        TileBase floorTile = FindTile("Floor Dirt");
        TileBase wallTile = FindTile("Wall");
        Sprite hero = FindSprite("Hero");

        if (floorTile == null || wallTile == null || hero == null)
        {
            EditorUtility.DisplayDialog("Dungeon Tale - Missing Assets",
                "Unity could not find the Dungeon Tale floor, wall, or hero assets. Make sure Assets/Tileset/Dungeon Tale has finished importing, then try again.",
                "OK");
            Debug.LogError("Dungeon Tale build stopped: missing floor, wall, or hero asset.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var grid = new GameObject("Dungeon Grid");
        grid.AddComponent<Grid>().cellSize = Vector3.one;

        var floor = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
        floor.transform.SetParent(grid.transform, false);
        floor.GetComponent<TilemapRenderer>().sortingOrder = 0;

        var walls = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        walls.transform.SetParent(grid.transform, false);
        walls.GetComponent<TilemapRenderer>().sortingOrder = 2;

        var gameObject = new GameObject("Dungeon Tale Game");
        var game = gameObject.AddComponent<DungeonTaleGame>();
        game.floorMap = floor.GetComponent<Tilemap>();
        game.wallMap = walls.GetComponent<Tilemap>();
        game.floorTile = floorTile;
        game.wallTile = wallTile;
        game.heroSprite = hero;

        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 8.5f;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.07f);
        camera.transform.position = new Vector3(-12, 0, -10);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/DungeonTale.unity", true)
        };
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DungeonTale.unity");
        Selection.activeGameObject = gameObject;
        EditorUtility.DisplayDialog("Dungeon Tale Ready",
            "The dungeon scene has been created. Press Play to start.",
            "Let's Play");
        Debug.Log("Dungeon Tale scene created successfully. Press Play to begin.");
    }

    static TileBase FindTile(string assetName)
    {
        // Floor Dirt and Wall are RuleTile assets, so don't filter by the Tile type.
        string[] ids = AssetDatabase.FindAssets(assetName);
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!path.Contains("Tileset/Dungeon Tale")) continue;
            var asset = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (asset != null && asset.name == assetName) return asset;
        }
        return null;
    }

    static Sprite FindSprite(string assetName)
    {
        string[] ids = AssetDatabase.FindAssets(assetName);
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!path.Contains("Tileset/Dungeon Tale")) continue;
            foreach (Object item in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (item is Sprite sprite && sprite.name == "Hero_0")
                    return sprite;
            }
        }
        return null;
    }
}
