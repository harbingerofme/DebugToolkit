## **NEW FEATURES**

Object & Character names can now be queried using partial language invariants. For example: `give_item uku` will give one ukulele under "en".
Elites now spawn with correct stats, and should be update-proofed.

## **COMMANDS**

* **next_stage** - Advance to the next stage: next_stage {specific stage}. If no stage is entered, the next stage in progression is selected.
* **add_portal** - Teleporter will attempt to spawn a blue, gold, or celestial portal: `add_portal {blue|gold|celestial}`
* **true_kill** - Truly kill a player, ignoring revival effects
* **respawn** - Respawn a player at the map spawnpoint: `respawn {0-3}`
* **player_list** - Shows list of players with their ID


* **list_body** - List all Bodies and their language invariant
* **list_ai** - List all Masters and their language invariants

* **give_item** - Give item directly to the player's inventory: `give_item {localised_object_name} {count} {playername}`
* **give_equip** - Give equipment directly to a player's inventory: `give_equip {localised_object_name} {playername}`


* **spawn_ai** - Spawn an AI: `spawn_ai {localised_objectname} {EliteIndex[0=Fire,1=Overloading,2=Ice,3=Malechite,4=Haunted]} {TeamIndex[0=N,1=P,2=M]} {BrainDead[0,1]}`
* **spawn_as** - Spawn as a new character. Type body_list for a full list of characters: `spawn_as {localised_objectname} {playername}`
* **spawn_body** - Spawns a CharacterBody: `Requires 1 argument: spawn_body {localised_objectname}`
* **no_enemies** - Toggles enemy spawns
* **change_team** - Change team to Neutral, Player or Monster: change_team `{0=Neutral,1=Player,2=Monster)`