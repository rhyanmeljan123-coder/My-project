# Dungeon Tale - First Playable Build

This is a separate Unity 2D game inside the **My-project** repository. It does not use or modify the separate **2d game** project.

## Build the scene
1. Open this repository's Unity project in Unity.
2. Wait for scripts and the Dungeon Tale tileset to import.
3. From the Unity menu, choose **Dungeon Tale > Build Playable Dungeon Scene**.
4. Open `Assets/Scenes/DungeonTale.unity` if it is not already open.
5. Press **Play**.

The editor command creates a playable dungeon scene and adds it to Build Settings. It uses the Floor Dirt, Wall, and Hero assets from `Assets/Tileset/Dungeon Tale`.

## Controls
- **WASD / Arrow keys** - Move
- **Space** - Attack nearby slimes
- **R** - Restart after winning or losing

## First playable features
- Tilemap dungeon with walls, pillars, and a central divider
- Player movement and camera follow
- Four slimes that chase and damage the player
- Two-hit melee combat
- Collectible gold coins
- Health and gameplay HUD
- Exit that unlocks after all enemies are defeated
