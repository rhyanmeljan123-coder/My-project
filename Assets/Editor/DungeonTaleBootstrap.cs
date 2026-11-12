using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;

public static class DungeonTaleBootstrap
{
    [MenuItem("Dungeon Tale/Build Playable Dungeon Scene")]
    public static void Build()
    {
        TileBase floorTile = FindTile("Floor Dirt");
        TileBase wallTile = FindTile("Wall");
        Sprite hero = FindSprite("Hero_0");

        if (floorTile == null || wallTile == null || hero == null)
        {
            EditorUtility.DisplayDialog("Dungeon Tale - Missing Assets",
                "Could not find Floor Dirt, Wall, or Hero_0 in Assets/Tileset/Dungeon Tale. Wait for Unity to finish importing, then try again.",
                "OK");
            Debug.LogError("Dungeon Tale build stopped: required assets were not found.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var grid = new GameObject("Dungeon Grid");
        grid.AddComponent<Grid>().cellSize = Vector3.one;

        var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
        floorObject.transform.SetParent(grid.transform, false);
        var floorMap = floorObject.GetComponent<Tilemap>();
        floorObject.GetComponent<TilemapRenderer>().sortingOrder = 0;

        var wallObject = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        wallObject.transform.SetParent(grid.transform, false);
        var wallMap = wallObject.GetComponent<Tilemap>();
        wallObject.GetComponent<TilemapRenderer>().sortingOrder = 2;

        // Bake the map into the scene asset so it is visible in Scene view before Play.
        const int width = 31;
        const int height = 21;
        for (int x = -width / 2; x <= width / 2; x++)
        for (int y = -height / 2; y <= height / 2; y++)
        {
            var cell = new Vector3Int(x, y, 0);
            floorMap.SetTile(cell, floorTile);

            bool border = x == -width / 2 || x == width / 2 ||
                          y == -height / 2 || y == height / 2;
            bool pillar = (x == -5 || x == 5) && (y == -4 || y == 4);
            bool divider = x == 0 && y >= -5 && y <= 5 && y != 0;

            if (border || pillar || divider)
                wallMap.SetTile(cell, wallTile);
        }
        floorMap.RefreshAllTiles();
        wallMap.RefreshAllTiles();

        var gameObject = new GameObject("Dungeon Tale Game");
        var game = gameObject.AddComponent<DungeonTaleGame>();
        game.floorMap = floorMap;
        game.wallMap = wallMap;
        game.floorTile = floorTile;
        game.wallTile = wallTile;
        game.heroSprite = hero;

        // Add an editor-visible hero marker. Runtime setup creates the controllable hero.
        var heroPreview = new GameObject("Hero Preview (Editor)");
        heroPreview.transform.position = new Vector3(-12, 0, -0.1f);
        var previewRenderer = heroPreview.AddComponent<SpriteRenderer>();
        previewRenderer.sprite = hero;
        previewRenderer.sortingOrder = 5;

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
        Selection.activeGameObject = grid;
        SceneView.lastActiveSceneView?.FrameSelected();
        EditorUtility.DisplayDialog("Dungeon Tale Ready",
            "The dungeon tiles are now saved in the scene and visible in Scene view. Press Play to start.",
            "OK");
        Debug.Log("Dungeon Tale scene created with editor-visible tilemaps.");
    }

    static TileBase FindTile(string assetName)
    {
        string[] ids = AssetDatabase.FindAssets(assetName);
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!path.Contains("Tileset/Dungeon Tale")) continue;
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null && tile.name == assetName) return tile;
        }
        return null;
    }

    static Sprite FindSprite(string spriteName)
    {
        string[] ids = AssetDatabase.FindAssets("t:Texture2D");
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!path.Contains("Tileset/Dungeon Tale")) continue;
            foreach (Object item in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (item is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }
        }
        return null;
    }
}
