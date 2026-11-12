# Dungeon Tale - Playable Dungeon Adventure

This is a separate Unity 2D game inside **My-project**. It does not touch the separate **2d game** project.

## Build the scene
1. Open this Unity project and allow all assets to import.
2. Choose **Dungeon Tale > Build Playable Dungeon Scene**.
3. Open `Assets/Scenes/DungeonTale.unity`.
4. The tilemap and room decorations are saved in the Scene view; press Play to start.

The builder uses the original Dungeon Tale atlas, Rule Tiles, and floor variants in `Assets/Tileset/Dungeon Tale/Assets`.

## Controls
- WASD / Arrow keys: move
- Space: melee attack
- E: use a collected potion
- R: restart after victory or defeat

## Current gameplay
- Dungeon map with floor variations, walls, divider, pillars, carpet and environmental props
- Hero movement, wall collision and camera follow
- Slimes, skeletons, bats and rats with different health and movement speeds
- Melee combat and enemy defeat counter
- Coins, red/blue/green potion pickups, key, sword and shield relics
- Potion inventory and healing
- Spike traps that damage the hero
- Locked exit; defeat all monsters, collect the key, then reach the exit
- HUD for health, gold, potions and remaining monsters

## Note
Unity Editor compilation and Play Mode testing must still be completed locally after pulling the branch. The GitHub changes have not been validated by running the Unity Editor in this environment.
