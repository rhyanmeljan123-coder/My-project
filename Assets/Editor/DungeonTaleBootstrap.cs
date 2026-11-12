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
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var grid = new GameObject("Dungeon Grid");
        var gridComponent = grid.AddComponent<Grid>();
        gridComponent.cellSize = Vector3.one;

        var floor = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
        floor.transform.SetParent(grid.transform, false);
        var walls = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        walls.transform.SetParent(grid.transform, false);

        TileBase floorTile = FindTile("Floor Dirt");
        TileBase wallTile = FindTile("Wall");
        Sprite hero = FindSprite("Hero");

        var gameObject = new GameObject("Dungeon Tale Game");
        var game = gameObject.AddComponent<DungeonTaleGame>();
        game.floorMap = floor.GetComponent<Tilemap>();
        game.wallMap = walls.GetComponent<Tilemap>();
        game.floorTile = floorTile;
        game.wallTile = wallTile;
        game.heroSprite = hero;

        var camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = 8.5f;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.07f);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DungeonTale.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/DungeonTale.unity", true) };
        Selection.activeGameObject = gameObject;
        Debug.Log("Dungeon Tale scene created. Press Play to begin.");
    }

    static TileBase FindTile(string name)
    {
        string[] ids = AssetDatabase.FindAssets(name + " t:Tile");
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null && path.Contains("Dungeon Tale")) return tile;
        }
        Debug.LogWarning("Dungeon Tale tile not found: " + name);
        return null;
    }

    static Sprite FindSprite(string name)
    {
        string[] ids = AssetDatabase.FindAssets(name + " t:Texture2D");
        foreach (string id in ids)
        {
            string path = AssetDatabase.GUIDToAssetPath(id);
            if (!path.Contains("Dungeon Tale")) continue;
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var item in sprites)
                if (item is Sprite) return (Sprite)item;
        }
        return null;
    }
}
