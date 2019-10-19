# RoR2Cheats

This mod adds various cheat commands to the console. See below for all commands plus explanation.
The console can be opened with `ctrl+alt+~`. Additionally `help {command}` may be used to get help, and `find {term}` can be used to find commands with that term.

[Harb's](https://thunderstore.io/package/Harb/) and ['s](https://thunderstore.io/package/paddywan/) reimplementation of [Morris1927's](https://thunderstore.io/package/Morris1927/) [RoR2Cheats](https://thunderstore.io/package/Morris1927/RoR2Cheats/). Derived with permission. 

Mods recommended for combined use:

* [KeyBindForConsole](https://thunderstore.io/package/kristiansja/KeyBindForConsole/)
* [SimpleMacros](https://thunderstore.io/package/recursiveGecko/SimpleMacros/)

---

## **COMMANDS**
Verbiage: if an argument is encapsulated with brackets, it means it's either `(choose one)`, `{needed freeform}`, or `[optional freeform]`. The following may be used to indicate the default value: `def X`.

* **next_stage** - Advance to the next stage: `next_stage [specific stage]`. If no stage is entered, the next stage in progression is selected.
* **fixed_time** - Sets the time that has progressed in the run. Affects difficulty. `fixed_time [time]`. If no time is supplied, prints the current time to console.
* **add_portal** - Teleporter will attempt to spawn a blue, gold, or celestial portal: `add_portal (blue|gold|celestial)`
* **seed** - Set the seed for all next runs this session. `seed [new seed]`. Use `0` to specify the game should generate its own seed. If used without argument, it's equivalent to the vanilla `run_get_seed`.
* **kill_all** - Kills all members of a specified team. `kill_all [teamindex def 2]` Team indexes: 0=neutral,1=player,2=monster. 
* **true_kill** - Truly kill a player, ignoring revival effects `true_kill [player def 0]`
* **respawn** - Respawn a player at the map spawnpoint: `respawn [0-3 def 0]`
* **time_scale** -  Sets the timescale of the game. 0.5 would mean everything happens at half speed. `time_scale [time_scale]`. If no argument is supplied, gives the current timescale.

* **player_list** - Shows list of players with their ID
* **list_body** - List all Bodies and their language invariant
* **list_ai** - List all Masters and their language invariants

* **give_item** - Give item directly to the player's inventory: `give_item {localised_object_name} [count] [player]`
* **give_equip** - Give equipment directly to a player's inventory: `give_equip {localised_object_name} [player]`
* **give_money** - Gives the desired player/team money `give_money [(all | [player]) def all]`

* **spawn_ai** - Spawn an AI: `spawn_ai {localised_objectname} [eliteIndex def 0] [teamIndex def 0] [braindead def 0]`. Elite indexes: 0=Fire,1=Overloading,2=Ice,3=Malachite,4=Celestine. Team indexes: 0=neutral,1=player,2=monster. 
* **spawn_as** - Spawn as a new character. Type body_list for a full list of characters: `spawn_as {localised_objectname} {playername}`
* **spawn_body** - Spawns a CharacterBody: `spawn_body {localised_objectname}`
* **no_enemies** - Toggles enemy spawns
* **change_team** - Change team to Neutral, Player or Monster: `change_team {teamindex}`. Team indexes: 0=neutral,1=player,2=monster. 
* **god** - toggles HealthComponent.TakeDamage for all players. AKA: you can't take damage.


### Unlocked Vanilla Commands
* **sv_time_transmit_interval** - How long it takes for the server to issue a time update to clients. `sv_time_transmit_interval [time]`
* **run_scene_override** - Overrides the first scene to enter in a run. `run_scene_override [stage]`
* **stage1_pod** - Whether or not to use the pod when spawning on the first stage. `stage1_pod [(0|1)]`
* **run_set_stages_cleared**  - Sets the amount of stages cleared. This does not change the current stage. `run_set_stages_cleared {stagecount}`. This obsoletes `stage_clear_count` from previous RoR2Cheats versions.
* **team_set_level** - Sets the specified team to the specified level: `team_set_level {teamindex} {level}` Team indexes: 0=neutral,1=player,2=monster. This obsoletes `give_exp` from previous RoR2Cheats versions.
* **loadout_set_skill_variant** - Sets the skill variant for the sender's user profile: `loadout_set_skill_variant {body_name} {skill_slot_index} {skill_variant_index}`. Note that this does not use the loose bodymatching from custom commands.

### Commands slated for deletion
* **give_exp** - Use `team_set_level` instead.
* You can now use language invariants, so the following lists have been invalidated.
    * **list_items** 
    * **list_equipments**
* **stage_clear_count** - Use `run_set_stages_cleared` instead.

---

## Changelog

### 3.0.0
* "Initial Release"
* **General**
    * Reworked almost every command to be more maintainable. This should have no impact on current users.
    *  FOV is now outside the scope of this project and has thus been removed
* **NEW FEATURES**
    * Object & Character names can now be queried using partial language invariants. For example: `give_item uku` will give one ukulele under "en".
    * Several vanilla cheats have now been unlocked.
* **Fixes**
    * No longer forcefully changes FOV settings.
    * No longer hooks stuff for disabling enemies, improving mod inter compatibility.
    * `seed` is now networked.
    * Elites now spawn with correct stats, and should be update-proofed.

### < 3.0.0
See [the old package](https://thunderstore.io/package/Morris1927/)